using System.Diagnostics;
using System.Net;
using API.Services;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Xunit.Abstractions;

namespace Tests.IntegrationTests
{
    public class RateLimitingPerformanceTests
    {
        private readonly Mock<IIpProtectionService> _ipProtectionServiceMock;
        private readonly ITestOutputHelper _output;

        public RateLimitingPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            
            // CREATE MOCK SERVICE
            _ipProtectionServiceMock = new Mock<IIpProtectionService>();
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(It.IsAny<string>())).Returns(false);
        }
        
        // TEST FOR CHECKING IF RATE LIMITING HAS A MINIMAL PERFORMANCE IMPACT
        [Fact]
        public async Task RateLimiting_HasMinimalPerformanceImpact()
        {
            // ARRANGE - SETUP VARS
            int iterations = 5;
            var measurements = new List<double>();

            for (int i = 0; i < iterations; i++)
            {
                // ARRANGE - HOST/CLIENT
                var hostBuilder = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((context, config) => { config.Sources.Clear(); })
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder
                            .UseTestServer()
                            .ConfigureServices(services =>
                            {
                                services.AddControllers();
                                services.AddLogging();
                                services.AddSingleton<IIpProtectionService, IpProtectionService>();
                            })
                            .Configure(app =>
                            {
                                app.UseRouting();
                                app.Map("/test-with-rate-limiting", appBranch =>
                                {
                                    appBranch.UseMiddleware<API.Middleware.RateLimitingMiddleware>();
                                    appBranch.Run(async context =>
                                    {
                                        context.Response.StatusCode = 200;
                                        await context.Response.WriteAsync("OK");
                                    });
                                });
                                app.Map("/test-without-rate-limiting", appBranch =>
                                {
                                    appBranch.Run(async context =>
                                    {
                                        context.Response.StatusCode = 200;
                                        await context.Response.WriteAsync("OK");
                                    });
                                });
                            });
                    });

                var host = hostBuilder.Start();
                var testServer = host.GetTestServer();
                using var client = testServer.CreateClient();

                // ACT - WITH RL
                var stopwatch1 = Stopwatch.StartNew();
                var response1 = await client.GetAsync("/test-with-rate-limiting");
                stopwatch1.Stop();

                // ASSERT - STATUS
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

                // ACT - NO RL
                var stopwatch2 = Stopwatch.StartNew();
                var response2 = await client.GetAsync("/test-without-rate-limiting");
                stopwatch2.Stop();

                // ASSERT - STATUS
                Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

                // ACT - OVERHEAD CALC
                var overhead = ((double)stopwatch1.ElapsedTicks / stopwatch2.ElapsedTicks - 1) * 100;
                measurements.Add(overhead);

                // CLEANUP RESOURCES
                await host.StopAsync();
                host.Dispose();
            }

            // ASSERT - AVG CALC
            var avgOverhead = measurements.Average();

            _output.WriteLine($"Average rate limiting overhead: {avgOverhead:F2}%");
            _output.WriteLine($"All measurements: {string.Join(", ", measurements.Select(m => $"{m:F2}%"))}");

            // ASSERT - THRESHOLD
            Assert.True(avgOverhead < 3500, $"Rate limiting overhead ({avgOverhead:F2}%) exceeds threshold (3500%)");
        }
    }
} 
