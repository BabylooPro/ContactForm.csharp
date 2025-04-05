using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ContactForm.Tests.IntegrationTests
{
    public class SecurityHeadersContextTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public SecurityHeadersContextTests()
        {
            // SETUP TEST SERVER
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<SecurityContextTestStartup>()
                        .UseTestServer();
                });

            var host = hostBuilder.Start();
            _server = host.GetTestServer();
            _client = _server.CreateClient();
        }

        // TEST FOR CHECKING IF ALL HTTP METHODS HAVE SECURITY HEADERS
        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task AllHttpMethods_HaveSecurityHeaders(string httpMethod)
        {
            // ARRANGE - CREATE A NEW HTTP REQUEST MESSAGE
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/api/methods-test");

            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.SendAsync(request);

            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS OK
            Assert.Equal(200, (int)response.StatusCode);
            VerifySecurityHeaders(response);
        }

        // TEST FOR CHECKING IF ALL ROUTES HAVE SECURITY HEADERS
        [Theory]
        [InlineData("/api/admin")]
        [InlineData("/api/public")]
        [InlineData("/")]
        [InlineData("/api/routes-test")]
        public async Task AllRoutes_HaveSecurityHeaders(string route)
        {
            // ARRANGE
            var request = new HttpRequestMessage(HttpMethod.Get, route);

            // ACT
            var response = await _client.SendAsync(request);

            // ASSERT
            Assert.Equal(200, (int)response.StatusCode);
            VerifySecurityHeaders(response);
        }

        // TEST FOR CHECKING IF ROUTES WITH AUTHENTICATION HAVE STRICTER SECURITY HEADERS
        [Fact]
        public async Task Routes_WithAuthentication_HaveStricterSecurityHeaders()
        {
            // ARRANGE - CREATE A NEW HTTP REQUEST MESSAGE
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth");
            request.Headers.Add("Authorization", "Bearer test-token");

            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.SendAsync(request);

            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS OK
            Assert.Equal(200, (int)response.StatusCode);
            
            // ASSERT - VERIFY NORMAL SECURITY HEADERS
            VerifySecurityHeaders(response);
            
            // ASSERT - VERIFY ADDITIONAL STRICTER HEADERS FOR AUTHENTICATED ROUTES
            Assert.Equal("same-origin", response.Headers.GetValues("Cross-Origin-Opener-Policy").FirstOrDefault());
            Assert.Equal("same-origin", response.Headers.GetValues("Cross-Origin-Embedder-Policy").FirstOrDefault());
        }

        // TEST FOR CHECKING IF DIFFERENT USER AGENTS RECEIVE CORRECT SECURITY HEADERS
        [Theory]
        [InlineData(null, "default")]
        [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", "browser")]
        [InlineData("PostmanRuntime/7.26.8", "api-client")]
        [InlineData("curl/7.68.0", "cli")]
        public async Task DifferentUserAgents_ReceiveCorrectSecurityHeaders(string? userAgent, string clientType)
        {
            // ARRANGE - CREATE A NEW HTTP REQUEST MESSAGE
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/client-type/{clientType}");
            if (userAgent != null)
            {
                request.Headers.Add("User-Agent", userAgent);
            }

            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.SendAsync(request);

            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS OK
            Assert.Equal(200, (int)response.StatusCode);
            VerifySecurityHeaders(response);
            
            // ASSERT - API CLIENTS MAY RECEIVE DIFFERENT CACHE CONTROL DIRECTIVES
            if (clientType == "api-client" || clientType == "cli")
            {
                Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").FirstOrDefault());
            }
        }

        // VERIFY THE SECURITY HEADERS
        private void VerifySecurityHeaders(HttpResponseMessage response)
        {
            // ASSERT - VERIFY BASE SECURITY HEADERS
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").FirstOrDefault());
            Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").FirstOrDefault());
            Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").FirstOrDefault());
            Assert.Contains("default-src 'self'", response.Headers.GetValues("Content-Security-Policy").FirstOrDefault());
        }
    }

    // TEST STARTUP CLASS THAT USES THE SAME CONFIGURATION AS THE REAL APP
    public class SecurityContextTestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddLogging();
            services.AddSingleton<IIpProtectionService, IpProtectionService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // SECURITY HEADERS MIDDLEWARE - SETUP THE SECURITY HEADERS FOR ALL REQUESTS
            app.Use(async (context, next) =>
            {
                // COMMON SECURITY HEADERS FOR ALL REQUESTS
                context.Response.Headers.XContentTypeOptions = "nosniff";
                context.Response.Headers.XFrameOptions = "DENY";
                context.Response.Headers.XXSSProtection = "1; mode=block";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                context.Response.Headers.ContentSecurityPolicy = "default-src 'self'";
                
                // IF AUTHENTICATED REQUEST, ADD STRICTER HEADERS
                if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                    context.Response.Headers["Cross-Origin-Embedder-Policy"] = "same-origin";
                }
                
                // SPECIAL HANDLING FOR API CLIENTS
                var userAgent = context.Request.Headers.UserAgent.ToString().ToLower();
                if (userAgent.Contains("postman") || userAgent.Contains("curl"))
                {
                    context.Response.Headers["Cache-Control"] = "no-store";
                }
                
                await next();
            });
            
            app.UseRouting();
            
            // TEST ENDPOINTS
            app.UseEndpoints(endpoints =>
            {
                // BASIC TEST ENDPOINT - UTILISÉ UNIQUEMENT PAR LE TEST DE SPOOFING IP
                endpoints.MapGet("/api/test", () => "Test endpoint");
                
                // ENDPOINT POUR TESTER LES MÉTHODES HTTP
                endpoints.MapMethods("/api/methods-test", new[] { "GET", "POST", "PUT", "DELETE" }, () => "Methods test endpoint");
                
                // ENDPOINT POUR TESTER LES ROUTES
                endpoints.MapGet("/api/routes-test", () => "Routes test endpoint");
                
                // ADMIN ENDPOINT
                endpoints.MapGet("/api/admin", () => "Admin endpoint");
                
                // PUBLIC ENDPOINT
                endpoints.MapGet("/api/public", () => "Public endpoint");
                
                // ROOT ENDPOINT
                endpoints.MapGet("/", () => "Root endpoint");
                
                // AUTHENTICATED ENDPOINT
                endpoints.MapGet("/api/auth", () => "Authenticated endpoint");
                
                // CLIENT TYPE ENDPOINTS
                endpoints.MapGet("/api/client-type/{type}", (string type) => $"Client type: {type}");
            });
        }
    }
} 
