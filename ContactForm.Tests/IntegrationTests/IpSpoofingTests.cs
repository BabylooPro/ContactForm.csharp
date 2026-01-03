using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContactForm.Tests.IntegrationTests
{
    public class IpSpoofingTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly Mock<ILogger<IpSpoofingMiddleware>> _loggerMock;

        public IpSpoofingTests()
        {
            _loggerMock = new Mock<ILogger<IpSpoofingMiddleware>>();
            
            // SETUP TEST SERVER
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddSingleton(_loggerMock.Object);
                            services.AddControllers();
                            services.AddLogging();
                            services.AddSingleton<IIpProtectionService, IpProtectionService>();
                        })
                        .Configure(app =>
                        {
                            app.UseMiddleware<IpSpoofingMiddleware>();
                            app.UseRouting();
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/api/test", () => "Test endpoint");
                            });
                        });
                });

            var host = hostBuilder.Start();
            _server = host.GetTestServer();
            _client = _server.CreateClient();
        }

        // TEST FOR CHECKING IF A REQUEST WITH A MISMATCHED X-Forwarded-For HEADER IS DETECTED AS SPOOFING
        [Fact]
        public async Task Request_WithMismatchedXForwardedForHeader_IsDetectedAsSpoofing()
        {
            // ARRANGE - CREATE A NEW HTTP REQUEST MESSAGE
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            
            // ADD SUSPICIOUS HEADERS (MISMATCHED IP ADDRESSES)
            request.Headers.Add("X-Forwarded-For", "1.2.3.4, 5.6.7.8"); // SHOULD MATCH CONNECTION IP
            request.Headers.Add("X-Real-IP", "9.10.11.12"); // DIFFERENT IP
            
            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.SendAsync(request);
            
            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS FORBIDDEN
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("suspicious", responseContent.ToLower());
        }

        // TEST FOR CHECKING IF A REQUEST WITH VALID HEADERS IS ALLOWED
        [Fact]
        public async Task Request_WithValidHeaders_IsAllowed()
        {
            // ARRANGE - CREATE A NEW HTTP REQUEST MESSAGE
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            
            // INFO: DO NOT ADD ANY HEADERS - MORE SECURE IN THE TEST ENVIRONMENT
            // TestServer WILL SET THE CORRECT CONNECTION IP
            
            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.SendAsync(request);
            
            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR CHECKING IF A REQUEST WITH A SUSPICIOUS USER AGENT IS LOGGED
        [Fact]
        public async Task Request_WithSuspiciousUserAgent_IsLogged()
        {
            // ARRANGE - CREATE A NEW HTTP REQUEST MESSAGE
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            
            // ADD SUSPICIOUS USER AGENT
            request.Headers.Add("User-Agent", "SQLMAP/1.4");
            
            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.SendAsync(request);
            
            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // VERIFY LOGGER WAS CALLED WITH WARNING
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SQLMAP")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    // MIDDLEWARE TO DETECT IP SPOOFING
    public class IpSpoofingMiddleware(RequestDelegate next, ILogger<IpSpoofingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<IpSpoofingMiddleware> _logger = logger;
        private readonly HashSet<string> _suspiciousUserAgents = new(StringComparer.OrdinalIgnoreCase)
        {
            "SQLMAP", "Havij", "Acunetix", "Nessus", "Nikto", "w3af", "Morfeus"
        };

        public async Task InvokeAsync(HttpContext context)
        {
            // CHECK FOR IP SPOOFING
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var forwardedIp = context.Request.Headers["X-Forwarded-For"].ToString();
            var realIp = context.Request.Headers["X-Real-IP"].ToString();
            
            // LOG ALL IPs FOR DEBUGGING
            _logger.LogDebug("Connection IP: {ConnectionIp}, X-Forwarded-For: {ForwardedIp}, X-Real-IP: {RealIp}",
                clientIp, forwardedIp, realIp);
            
            // CHECK FOR SUSPICIOUS MISMATCH
            bool isSuspicious = false;
            
            // IN A TEST ENVIRONMENT, IF NO HEADERS ARE DEFINED, IT IS NORMAL
            // OR IF THE IP IS LOCALHOST
            bool isTestEnvironment = clientIp == "::1" || clientIp == "127.0.0.1" || 
                (string.IsNullOrEmpty(forwardedIp) && string.IsNullOrEmpty(realIp));
                
            // IF FORWARDED IP IS SET BUT DOESN'T MATCH CONNECTION IP
            if (!string.IsNullOrEmpty(forwardedIp) && !forwardedIp.Contains(clientIp) && !isTestEnvironment)
            {
                isSuspicious = true;
                _logger.LogWarning("Possible IP spoofing detected: X-Forwarded-For {ForwardedIp} doesn't match connection IP {ConnectionIp}",
                    forwardedIp, clientIp);
            }
            
            // IF REAL IP IS SET BUT DOESN'T MATCH CONNECTION IP
            if (!string.IsNullOrEmpty(realIp) && realIp != clientIp && !isTestEnvironment)
            {
                isSuspicious = true;
                _logger.LogWarning("Possible IP spoofing detected: X-Real-IP {RealIp} doesn't match connection IP {ConnectionIp}",
                    realIp, clientIp);
            }
            
            // CHECK FOR KNOWN MALICIOUS USER AGENTS
            var userAgent = context.Request.Headers.UserAgent.ToString();
            foreach (var suspiciousAgent in _suspiciousUserAgents)
            {
                if (userAgent.Contains(suspiciousAgent, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Suspicious user agent detected: {UserAgent} from IP {ClientIp}", 
                        userAgent, clientIp);
                    break;
                }
            }
            
            // BLOCK REQUEST IF SUSPICIOUS IP BEHAVIOR DETECTED
            if (isSuspicious)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Request blocked due to suspicious IP address behavior.");
                return;
            }
            
            await _next(context);
        }
    }
} 
