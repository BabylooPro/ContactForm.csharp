using ContactForm.MinimalAPI.Services;
using Microsoft.AspNetCore.TestHost;

namespace ContactForm.Tests.IntegrationTests
{
    public class SecurityHeadersContextTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private static readonly string[] HttpMethods = ["GET", "POST", "PUT", "DELETE"];

        public SecurityHeadersContextTests()
        {
            // SETUP TEST SERVER
            var hostBuilder = Host.CreateDefaultBuilder()
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
                            app.Use(async (context, next) =>
                            {
                                context.Response.Headers.XContentTypeOptions = "nosniff";
                                context.Response.Headers.XFrameOptions = "DENY";
                                context.Response.Headers.XXSSProtection = "1; mode=block";
                                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                                context.Response.Headers.ContentSecurityPolicy = "default-src 'self'";
                                
                                if (context.Request.Headers.ContainsKey("Authorization"))
                                {
                                    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                                    context.Response.Headers["Cross-Origin-Embedder-Policy"] = "same-origin";
                                }
                                
                                var userAgent = context.Request.Headers.UserAgent.ToString().ToLower();
                                if (userAgent.Contains("postman") || userAgent.Contains("curl"))
                                {
                                    context.Response.Headers.CacheControl = "no-store";
                                }
                                
                                await next();
                            });
                            
                            app.UseRouting();
                            
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/api/test", () => "Test endpoint");
                                endpoints.MapMethods("/api/methods-test", HttpMethods, () => "Methods test endpoint");
                                endpoints.MapGet("/api/routes-test", () => "Routes test endpoint");
                                endpoints.MapGet("/api/admin", () => "Admin endpoint");
                                endpoints.MapGet("/api/public", () => "Public endpoint");
                                endpoints.MapGet("/", () => "Root endpoint");
                                endpoints.MapGet("/api/auth", () => "Authenticated endpoint");
                                endpoints.MapGet("/api/client-type/{type}", (string type) => $"Client type: {type}");
                            });
                        });
                });

            var host = hostBuilder.Start();
            _server = host.GetTestServer();
            _client = _server.CreateClient();
        }

        // VERIFY BASE SECURITY HEADERS FOR ALL REQUESTS
        private static void VerifySecurityHeaders(HttpResponseMessage response)
        {
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").FirstOrDefault());
            Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").FirstOrDefault());
            Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").FirstOrDefault());
            Assert.Contains("default-src 'self'", response.Headers.GetValues("Content-Security-Policy").FirstOrDefault());
        }

        // TEST FOR CHECKING IF ALL HTTP METHODS HAVE SECURITY HEADERS
        [Theory]
        [InlineData("GET"), InlineData("POST"), InlineData("PUT"), InlineData("DELETE")]
        public async Task AllHttpMethods_HaveSecurityHeaders(string httpMethod)
        {
            // ARRANGE - CREATE REQUEST
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/api/methods-test");

            // ACT - SEND REQUEST
            var response = await _client.SendAsync(request);

            // ASSERT - STATUS OK, HEADERS OK
            Assert.Equal(200, (int)response.StatusCode);
            VerifySecurityHeaders(response);
        }

        // TEST FOR CHECKING IF ALL ROUTES HAVE SECURITY HEADERS
        [Theory]
        [InlineData("/api/admin"), InlineData("/api/public"), InlineData("/"), InlineData("/api/routes-test")]
        public async Task AllRoutes_HaveSecurityHeaders(string route)
        {
            // ARRANGE - CREATE REQUEST
            var request = new HttpRequestMessage(HttpMethod.Get, route);

            // ACT - SEND REQUEST
            var response = await _client.SendAsync(request);

            // ASSERT - STATUS, HEADERS
            Assert.Equal(200, (int)response.StatusCode);
            VerifySecurityHeaders(response);
        }

        // TEST FOR CHECKING IF ROUTES WITH AUTHENTICATION HAVE STRICTER SECURITY HEADERS
        [Fact]
        public async Task Routes_WithAuthentication_HaveStricterSecurityHeaders()
        {
            // ARRANGE - CREATE REQUEST
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth");
            request.Headers.Add("Authorization", "Bearer test-token");

            // ACT - SEND REQUEST
            var response = await _client.SendAsync(request);

            // ASSERT - STATUS OK
            Assert.Equal(200, (int)response.StatusCode);
            // ASSERT - SECURITY HEADERS
            VerifySecurityHeaders(response);
            // ASSERT - STRICTER HEADERS
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
            // ARRANGE - BUILD REQUEST
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/client-type/{clientType}");
            if (userAgent != null)
            {
                request.Headers.Add("User-Agent", userAgent);
            }

            // ACT - SEND REQUEST
            var response = await _client.SendAsync(request);

            // ASSERT - STATUS OK
            Assert.Equal(200, (int)response.StatusCode);
            // ASSERT - SECURITY HEADERS
            VerifySecurityHeaders(response);

            // ASSERT - CACHE CONTROL
            if (clientType == "api-client" || clientType == "cli")
            {
                Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            }
        }
    }
} 
