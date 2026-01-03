using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ContactForm.MinimalAPI;
using ContactForm.MinimalAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

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
            // ARRANGE - SETUP IP PROTECTION SERVICE TO BLOCK THE IP
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(It.IsAny<string>())).Returns(true);
            
            // CONFIGURE CONTEXT CONNECTION FEATURES TO USE OUR IP
            _server.BaseAddress = new System.Uri("http://localhost");
            
            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.GetAsync("/test");
            
            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS FORBIDDEN
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("blocked", responseContent);
            
            // VERIFY - CHECK IF THE IP PROTECTION SERVICE WAS CALLED
            _ipProtectionServiceMock.Verify(x => x.IsIpBlocked(It.IsAny<string>()), Times.AtLeastOnce);
        }

        // TEST FOR CHECKING IF A REQUEST IS ALLOWED WHEN THE RATE LIMIT IS NOT EXCEEDED
        [Fact]
        public async Task Request_IsAllowed_WhenRateLimitNotExceeded()
        {
            // ARRANGE - SETUP IP PROTECTION SERVICE TO NOT BLOCK THE IP
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(It.IsAny<string>())).Returns(false);
            
            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.GetAsync("/test");
            
            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // VERIFY - CHECK IF THE IP PROTECTION SERVICE WAS CALLED
            _ipProtectionServiceMock.Verify(x => x.IsIpBlocked(It.IsAny<string>()), Times.AtLeastOnce);
            _ipProtectionServiceMock.Verify(x => x.TrackRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        // TEST FOR CHECKING IF A REQUEST IS RATE LIMITED WHEN THE RATE LIMIT IS EXCEEDED
        [Fact]
        public async Task Request_ReturnsStatus429_WhenRateLimitExceeded()
        {
            // ARRANGE - SETUP IP PROTECTION SERVICE TO NOT BLOCK ANY IPs
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(It.IsAny<string>())).Returns(false);
            
            // ACT - MAKE REQUESTS UNTIL RATE LIMIT IS EXCEEDED
            HttpResponseMessage? response;
            
            // FIRST MAKE 10 REQUESTS (THE LIMIT)
            for (int i = 0; i < 10; i++)
            {
                response = await _client.GetAsync("/test");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            
            // THE 11TH REQUEST SHOULD BE RATE LIMITED
            response = await _client.GetAsync("/test");
            
            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS TOO MANY REQUESTS
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.True(response.Headers.Contains("Retry-After"));
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Too many requests", responseContent);
        }
    }
} 
