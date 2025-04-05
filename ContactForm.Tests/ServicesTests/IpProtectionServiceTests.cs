using System;
using System.Threading;
using ContactForm.MinimalAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContactForm.Tests.ServicesTests
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
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var ip = "192.168.1.1";

            // ACT - CHECK IF THE IP IS BLOCKED
            var result = _service.IsIpBlocked(ip);

            // ASSERT - CHECK THE RESULT
            Assert.False(result);
        }

        // TEST FOR CHECKING IF AN IP IS BLOCKED
        [Fact]
        public void IsIpBlocked_ReturnsTrue_ForBlockedIp()
        {
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var ip = "192.168.1.2";
            _service.BlockIp(ip, TimeSpan.FromMinutes(10), "Test block");

            // ACT - CHECK IF THE IP IS BLOCKED
            var result = _service.IsIpBlocked(ip);

            // ASSERT - CHECK THE RESULT
            Assert.True(result);
        }

        // TEST FOR CHECKING IF AN IP IS NOT BLOCKED AFTER BLOCK EXPIRES
        [Fact]
        public void IsIpBlocked_ReturnsFalse_ForExpiredBlock()
        {
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var ip = "192.168.1.3";
            _service.BlockIp(ip, TimeSpan.FromMilliseconds(50), "Test block");
            
            // WAIT FOR THE BLOCK TO EXPIRE
            Thread.Sleep(100);

            // ACT - CHECK IF THE IP IS BLOCKED
            var result = _service.IsIpBlocked(ip);

            // ASSERT - CHECK THE RESULT
            Assert.False(result);
        }

        // TEST FOR CHECKING IF AN IP IS BLOCKED WHEN BURST THRESHOLD IS EXCEEDED
        [Fact]
        public void TrackRequest_BlocksIp_WhenBurstThresholdExceeded()
        {
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var ip = "192.168.1.4";
            
            // ACT - SUBMIT MANY REQUESTS IN A SHORT TIME
            for (int i = 0; i < 21; i++) // EXCEEDING THE BURST_THRESHOLD OF 20
            {
                _service.TrackRequest(ip, "/test", "Test User Agent");
            }

            // ASSERT - CHECK IF THE IP IS BLOCKED
            Assert.True(_service.IsIpBlocked(ip));
        }

        // TEST FOR CHECKING IF AN IP IS BLOCKED WHEN TOTAL THRESHOLD IS EXCEEDED
        [Fact]
        public void TrackRequest_BlocksIp_WhenTotalThresholdExceeded()
        {
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var ip = "192.168.1.5";
            
            // ACT - SUBMIT REQUESTS BUT AVOID BURST DETECTION
            for (int i = 0; i < 101; i++) // EXCEEDING TOTAL_THRESHOLD OF 100
            {
                _service.TrackRequest(ip, "/test", "Test User Agent");
                
                // ADD A SMALL DELAY TO AVOID BURST DETECTION
                if (i % 5 == 0)
                {
                    Thread.Sleep(10);
                }
            }

            // ASSERT - CHECK IF THE IP IS BLOCKED
            Assert.True(_service.IsIpBlocked(ip));
        }

        // TEST FOR CHECKING IF AN IP IS NOT BLOCKED FOR VALID TRAFFIC
        [Fact]
        public void TrackRequest_DoesNotBlockIp_ForValidTraffic()
        {
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var ip = "192.168.1.6";
            
            // ACT - SUBMIT REQUESTS AT NORMAL RATE
            for (int i = 0; i < 10; i++)
            {
                _service.TrackRequest(ip, "/test", "Test User Agent");
                Thread.Sleep(50); // SPACE OUT REQUESTS
            }

            // ASSERT - CHECK IF THE IP IS NOT BLOCKED
            Assert.False(_service.IsIpBlocked(ip));
        }

        // TEST FOR CHECKING IF AN IP IS BLOCKED FOR A SPECIFIED DURATION
        [Fact]
        public void BlockIp_Successfully_BlocksIpForSpecifiedDuration()
        {
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var ip = "192.168.1.7";
            var duration = TimeSpan.FromMilliseconds(200);
            
            // ACT - BLOCK THE IP FOR THE SPECIFIED DURATION
            _service.BlockIp(ip, duration, "Test block");
            
            // ASSERT - IP SHOULD BE BLOCKED INITIALLY
            Assert.True(_service.IsIpBlocked(ip));
            
            // WAIT FOR THE DURATION TO EXPIRE
            Thread.Sleep(300);
            
            // IP SHOULD NO LONGER BE BLOCKED
            Assert.False(_service.IsIpBlocked(ip));
        }
    }
} 
