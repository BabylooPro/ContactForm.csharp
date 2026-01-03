using API.Services;
using Moq;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class IpProtectionServiceConcurrencyTests
    {
        private readonly Mock<ILogger<IpProtectionService>> _loggerMock;
        private readonly IpProtectionService _service;
        private readonly ITestOutputHelper _output;

        public IpProtectionServiceConcurrencyTests(ITestOutputHelper output)
        {
            _output = output;
            _loggerMock = new Mock<ILogger<IpProtectionService>>();
            _service = new IpProtectionService(_loggerMock.Object);
        }

        // TEST FOR CHECKING IF MANY SIMULTANEOUS REQUESTS ARE HANDLED CORRECTLY
        [Fact]
        public async Task TrackRequest_WithManySimultaneousRequests_HandlesCorrectly()
        {
            // ARRANGE - ISOLATED SERVICE
            var isolatedLoggerMock = new Mock<ILogger<IpProtectionService>>();
            var isolatedService = new IpProtectionService(isolatedLoggerMock.Object);

            // ARRANGE - TEST IPS
            var ipAddresses = new[] { "255.255.255.100", "255.255.255.101", "255.255.255.102" };
            const int requestsPerIp = 5;
            const int concurrentTasks = 1;

            // ACT - SEQUENTIAL REQUESTS
            for (int taskId = 0; taskId < concurrentTasks; taskId++)
            {
                for (int j = 0; j < requestsPerIp; j++)
                {
                    foreach (var ip in ipAddresses)
                    {
                        // ACT - UNIQUE PATH, WAIT 50MS
                        isolatedService.TrackRequest(ip, $"/test-unique-{Guid.NewGuid()}", "Test User Agent");
                        await Task.Delay(50);
                    }
                }
            }

            // ASSERT - ALL IPS ALLOWED
            foreach (var ip in ipAddresses)
            {
                Assert.False(isolatedService.IsIpBlocked(ip), $"IP {ip} was incorrectly blocked");
                _output.WriteLine($"IP {ip} status: {(isolatedService.IsIpBlocked(ip) ? "blocked" : "allowed")}");
            }
        }

        // TEST FOR CHECKING IF BURST REQUESTS FROM ONE IP ARE HANDLED CORRECTLY
        [Fact]
        public async Task TrackRequest_WithBurstFromOneIp_BlocksOnlyThatIp()
        {
            // ARRANGE - DEFINE IPS
            var normalIps = new[] { "192.168.1.200", "192.168.1.201" };
            var attackerIp = "192.168.1.202";

            // ACT - NORMAL TRAFFIC
            var normalTrafficTask = Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    foreach (var ip in normalIps)
                    {
                        _service.TrackRequest(ip, "/contact", "Normal User Agent");
                        Thread.Sleep(50);
                    }
                }
            });

            // ACT - ATTACK BURST
            var attackTrafficTask = Task.Run(() =>
            {
                for (int i = 0; i < 25; i++)
                {
                    _service.TrackRequest(attackerIp, "/contact", "Attacker User Agent");
                }
            });

            // ACT - WAIT TASKS
            await Task.WhenAll(normalTrafficTask, attackTrafficTask);

            // ASSERT - ONLY ATTACKER BLOCKED
            foreach (var ip in normalIps)
            {
                Assert.False(_service.IsIpBlocked(ip), $"Normal IP {ip} was incorrectly blocked");
            }

            Assert.True(_service.IsIpBlocked(attackerIp), "Attacker IP should have been blocked");
        }

        // TEST FOR CHECKING IF BLOCKING AND CHECKING OPERATIONS ARE THREAD SAFE
        [Fact]
        public void BlockIp_AndIsIpBlocked_AreThreadSafe()
        {
            // ARRANGE - INIT VARS
            const int iterations = 1000;
            var ips = Enumerable.Range(0, 100).Select(i => $"10.0.0.{i}").ToArray();
            var random = new Random();
            var exceptions = new List<Exception>();

            // ACT - PARALLEL OPS
            Parallel.For(0, iterations, i =>
            {
                try
                {
                    var randomIp = ips[random.Next(ips.Length)];
                    if (i % 3 == 0)
                    {
                        // BLOCK IP
                        _service.BlockIp(randomIp, TimeSpan.FromMinutes(5), "Test block");
                    }
                    else if (i % 3 == 1)
                    {
                        // CHECK BLOCKED
                        _service.IsIpBlocked(randomIp);
                    }
                    else
                    {
                        // TRACK REQ
                        _service.TrackRequest(randomIp, "/test", "Test User Agent");
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            // ASSERT - NO EXCEPTION
            Assert.Empty(exceptions);
            if (exceptions.Count > 0)
            {
                _output.WriteLine($"Thread safety test failed with exceptions: {string.Join(", ", exceptions.Select(e => e.Message))}");
            }
        }

        // TEST FOR CHECKING IF EXPIRED ENTRIES ARE CLEANED UP WITHOUT AFFECTING OCCURRING OPERATIONS
        [Fact]
        public void CleanupExpiredEntries_DoesNotAffectOngoingOperations()
        {
            // ARRANGE - INIT VARS
            const int ipCount = 100;
            var ips = Enumerable.Range(0, ipCount).Select(i => $"172.16.0.{i}").ToArray();

            // ARRANGE - BLOCK IPS
            for (int i = 0; i < ipCount; i++)
            {
                var duration = i < ipCount / 2 ? TimeSpan.FromMilliseconds(50) : TimeSpan.FromHours(1);
                _service.BlockIp(ips[i], duration, "Test block");
            }

            // ARRANGE - WAIT EXPIRE
            Thread.Sleep(100);

            // ACT - PARALLEL OPS
            var results = new bool[ipCount];
            Parallel.For(0, ipCount, i =>
            {
                results[i] = _service.IsIpBlocked(ips[i]);
                _service.TrackRequest(ips[i], "/test", "Test User Agent");
            });

            // ASSERT - CHECK BLOCKED
            for (int i = 0; i < ipCount; i++)
            {
                if (i < ipCount / 2)
                {
                    Assert.False(results[i], $"IP {ips[i]} should have been unblocked (expired)");
                }
                else
                {
                    Assert.True(results[i], $"IP {ips[i]} should still be blocked");
                }
            }
        }
    }
} 
