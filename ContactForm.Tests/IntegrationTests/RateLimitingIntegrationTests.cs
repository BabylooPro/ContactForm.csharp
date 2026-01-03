using System.Net;
using ContactForm.MinimalAPI.Services;
using Microsoft.AspNetCore.TestHost;
using Moq;

namespace ContactForm.Tests.IntegrationTests
{
    public class RateLimitingIntegrationTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly Mock<IIpProtectionService> _ipProtectionServiceMock;

        public RateLimitingIntegrationTests()
        {
            // CREATE MOCK SERVICE
            _ipProtectionServiceMock = new Mock<IIpProtectionService>();
            
            // SETUP TEST SERVER WITH CUSTOM SERVICE
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            // REPLACE THE REAL SERVICE WITH OUR MOCK
                            services.AddSingleton(_ipProtectionServiceMock.Object);
                            services.AddControllers();
                            services.AddLogging();
                        })
                        .Configure(app =>
                        {
                            app.UseMiddleware<MinimalAPI.Middleware.RateLimitingMiddleware>();
                            app.UseRouting();
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/test", () => "Test endpoint");
                            });
                        });
                });

            var host = hostBuilder.Start();
            _server = host.GetTestServer();
            _client = _server.CreateClient();
        }

        // TEST FOR CHECKING IF A REQUEST IS BLOCKED WHEN THE IP IS BLACKLISTED
        [Fact]
        public async Task Request_IsBlocked_WhenIpIsBlacklisted()
        {
            // ARRANGE - MOCK BLOCK, SET BASEADDRESS
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(It.IsAny<string>())).Returns(true);
            _server.BaseAddress = new System.Uri("http://localhost");

            // ACT - SEND REQUEST
            var response = await _client.GetAsync("/test");

            // ASSERT - FORBIDDEN RESPONSE, CONTAINS BLOCKED
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("blocked", responseContent);

            // VERIFY - SERVICE CALLED
            _ipProtectionServiceMock.Verify(x => x.IsIpBlocked(It.IsAny<string>()), Times.AtLeastOnce);
        }

        // TEST FOR CHECKING IF A REQUEST IS ALLOWED WHEN THE RATE LIMIT IS NOT EXCEEDED
        [Fact]
        public async Task Request_IsAllowed_WhenRateLimitNotExceeded()
        {
            // ARRANGE - MOCK IP ALLOW
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(It.IsAny<string>())).Returns(false);

            // ACT - SEND REQUEST
            var response = await _client.GetAsync("/test");

            // ASSERT - STATUS OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // ASSERT - MOCK CALLED
            _ipProtectionServiceMock.Verify(x => x.IsIpBlocked(It.IsAny<string>()), Times.AtLeastOnce);
            _ipProtectionServiceMock.Verify(x => x.TrackRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        // TEST FOR CHECKING IF A REQUEST IS RATE LIMITED WHEN THE RATE LIMIT IS EXCEEDED
        [Fact]
        public async Task Request_ReturnsStatus429_WhenRateLimitExceeded()
        {
            // ARRANGE - MOCK IP UNBLOCKED
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(It.IsAny<string>())).Returns(false);
            
            // ACT - SEND MULTIPLE REQUESTS
            HttpResponseMessage? response;

            // ACT - SEND 10 REQUESTS
            for (int i = 0; i < 10; i++)
            {
                response = await _client.GetAsync("/test");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // ACT - SEND 11TH REQUEST
            response = await _client.GetAsync("/test");

            // ASSERT - STATUS 429
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.True(response.Headers.Contains("Retry-After"));
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Too many requests", responseContent);
        }
    }
} 
