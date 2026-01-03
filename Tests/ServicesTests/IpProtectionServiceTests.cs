using API.Services;
using Moq;

namespace Tests.ServicesTests
{
    public class IpProtectionServiceTests
    {
        private readonly Mock<ILogger<IpProtectionService>> _loggerMock;
        private readonly IpProtectionService _service;

        public IpProtectionServiceTests()
        {
            _loggerMock = new Mock<ILogger<IpProtectionService>>();
            _service = new IpProtectionService(_loggerMock.Object);
        }

        // TEST FOR CHECKING IF AN IP IS NOT BLOCKED
        [Fact]
        public void IsIpBlocked_ReturnsFalse_ForNonBlockedIp()
        {
            // ARRANGE - DEFINE IP
            var ip = "192.168.1.1";

            // ACT - CALL METHOD
            var result = _service.IsIpBlocked(ip);

            // ASSERT - EXPECT FALSE
            Assert.False(result);
        }

        // TEST FOR CHECKING IF AN IP IS BLOCKED
        [Fact]
        public void IsIpBlocked_ReturnsTrue_ForBlockedIp()
        {
            // ARRANGE - DEFINE IP, BLOCK IP
            var ip = "192.168.1.2";
            _service.BlockIp(ip, TimeSpan.FromMinutes(10), "Test block");

            // ACT - CHECK BLOCKED
            var result = _service.IsIpBlocked(ip);

            // ASSERT - EXPECT TRUE
            Assert.True(result);
        }

        // TEST FOR CHECKING IF AN IP IS NOT BLOCKED AFTER BLOCK EXPIRES
        [Fact]
        public void IsIpBlocked_ReturnsFalse_ForExpiredBlock()
        {
            // ARRANGE - BLOCK IP
            var ip = "192.168.1.3";
            _service.BlockIp(ip, TimeSpan.FromMilliseconds(50), "Test block");

            // ARRANGE - WAIT EXPIRE
            Thread.Sleep(100);

            // ACT - CHECK BLOCKED
            var result = _service.IsIpBlocked(ip);

            // ASSERT - EXPECT FALSE
            Assert.False(result);
        }

        // TEST FOR CHECKING IF AN IP IS BLOCKED WHEN BURST THRESHOLD IS EXCEEDED
        [Fact]
        public void TrackRequest_BlocksIp_WhenBurstThresholdExceeded()
        {
            // ARRANGE - DEFINE IP
            var ip = "192.168.1.4";
            
            // ACT - MANY REQUESTS
            for (int i = 0; i < 21; i++)
            {
                _service.TrackRequest(ip, "/test", "Test User Agent");
            }

            // ASSERT - IP BLOCKED
            Assert.True(_service.IsIpBlocked(ip));
        }

        // TEST FOR CHECKING IF AN IP IS BLOCKED WHEN TOTAL THRESHOLD IS EXCEEDED
        [Fact]
        public void TrackRequest_BlocksIp_WhenTotalThresholdExceeded()
        {
            // ARRANGE - DEFINE IP
            var ip = "192.168.1.5";
            
            // ACT - SEND REQUESTS
            for (int i = 0; i < 101; i++) // EXCEED TOTAL
            {
                _service.TrackRequest(ip, "/test", "Test User Agent");
                
                // AVOID BURST
                if (i % 5 == 0)
                {
                    Thread.Sleep(10);
                }
            }

            // ASSERT - IP BLOCKED
            Assert.True(_service.IsIpBlocked(ip));
        }

        // TEST FOR CHECKING IF AN IP IS NOT BLOCKED FOR VALID TRAFFIC
        [Fact]
        public void TrackRequest_DoesNotBlockIp_ForValidTraffic()
        {
            // ARRANGE - INIT IP
            var ip = "192.168.1.6";
            
            // ACT - NORMAL TRAFFIC
            for (int i = 0; i < 10; i++)
            {
                _service.TrackRequest(ip, "/test", "Test User Agent");
                Thread.Sleep(50); // SPACED CALLS
            }

            // ASSERT - NOT BLOCKED
            Assert.False(_service.IsIpBlocked(ip));
        }

        // TEST FOR CHECKING IF AN IP IS BLOCKED FOR A SPECIFIED DURATION
        [Fact]
        public void BlockIp_Successfully_BlocksIpForSpecifiedDuration()
        {
            // ARRANGE - DEFINE IP, DURATION
            var ip = "192.168.1.7";
            var duration = TimeSpan.FromMilliseconds(200);

            // ACT - BLOCK IP
            _service.BlockIp(ip, duration, "Test block");

            // ASSERT - INITIALLY BLOCKED
            Assert.True(_service.IsIpBlocked(ip));

            // ACT - WAIT EXPIRE
            Thread.Sleep(300);

            // ASSERT - UNBLOCKED
            Assert.False(_service.IsIpBlocked(ip));
        }
    }
} 
