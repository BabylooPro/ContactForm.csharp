using System.Net;
using API.Services;
using Microsoft.AspNetCore.TestHost;

namespace Tests.IntegrationTests
{
    public class SecurityHeadersTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        private static readonly string[] AllowedOrigins = ["https://example.com", "http://localhost:3000", "https://example.org"];

        public SecurityHeadersTests()
        {
            // SETUP TEST SERVER
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) => { config.Sources.Clear(); })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddCors(options =>
                            {
                                options.AddDefaultPolicy(policy =>
                                {
                                    policy
                                        .WithOrigins(
                                            "http://localhost:3000",
                                            "https://example.com",
                                            "https://example.org"
                                        )
                                        .AllowAnyMethod()
                                        .AllowAnyHeader()
                                        .WithExposedHeaders(
                                            "Content-Type",
                                            "Authorization",
                                            "X-Api-Key",
                                            "X-Amz-Date",
                                            "X-Amz-Security-Token"
                                        );
                                });
                            });

                            // ADD NECESSARY SERVICES FOR TESTING
                            services.AddControllers();
                            services.AddLogging();
                            services.AddSingleton<IIpProtectionService, IpProtectionService>();
                        })
                        .Configure(app =>
                        {
                            app.Use(async (context, next) =>
                            {
                                if (context.Request.Method == "OPTIONS")
                                {
                                    var origin = context.Request.Headers.Origin.ToString();
                                    var isAllowedOrigin = AllowedOrigins.Contains(origin);
                                    
                                    // ONLY ADD CORS HEADERS FOR ALLOWED ORIGINS
                                    if (isAllowedOrigin)
                                    {
                                        context.Response.Headers.AccessControlAllowOrigin = origin;
                                        context.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE, OPTIONS";
                                        context.Response.Headers.AccessControlAllowHeaders = "Content-Type, Authorization, X-Api-Key";
                                        context.Response.Headers.AccessControlExposeHeaders = "Content-Type, Authorization, X-Api-Key, X-Amz-Date, X-Amz-Security-Token";
                                    }

                                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                                    return;
                                }
                                
                                // FOR NON-OPTIONS REQUESTS
                                var nonPrefOrigin = context.Request.Headers.Origin.ToString();
                                var isNonPrefAllowedOrigin = AllowedOrigins.Contains(nonPrefOrigin);
                                if (isNonPrefAllowedOrigin) context.Response.Headers.AccessControlAllowOrigin = nonPrefOrigin;
                                
                                await next();
                            });
                            
                            // ADD SECURITY HEADERS
                            app.Use(async (context, next) =>
                            {
                                context.Response.Headers.XContentTypeOptions = "nosniff";
                                context.Response.Headers.XFrameOptions = "DENY";
                                context.Response.Headers.XXSSProtection = "1; mode=block";
                                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                                context.Response.Headers.ContentSecurityPolicy = "default-src 'self'";
                                
                                await next();
                            });
                            
                            app.UseRouting();
                            
                            // ADD TEST ENDPOINT FOR VERIFICATION
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

        // TEST FOR CHECKING IF THE SECURITY HEADERS ARE SET CORRECTLY
        [Fact]
        public async Task Request_ReturnsSecurityHeaders()
        {
            // ARRANGE & ACT - REQUEST TEST ENDPOINT
            var response = await _client.GetAsync("/test");

            // ASSERT - STATUS CODE OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // ASSERT - SECURITY HEADERS
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").FirstOrDefault());
            Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").FirstOrDefault());
            Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").FirstOrDefault());
            Assert.Equal("default-src 'self'", response.Headers.GetValues("Content-Security-Policy").FirstOrDefault());
        }

        // TEST FOR CHECKING IF CORS HEADERS ARE SET CORRECTLY FOR ALLOWED ORIGINS
        [Fact]
        public async Task Request_WithAllowedOrigin_HasCorsHeaders()
        {
            // ARRANGE - PREPARE REQUEST
            var request = new HttpRequestMessage(HttpMethod.Options, "/test");
            request.Headers.Add("Origin", "https://example.com");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // ACT - SEND REQUEST
            var response = await _client.SendAsync(request);

            // ASSERT - EXPECT NO CONTENT
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // DEBUG - GET HEADER NAMES
            var headerNames = string.Join(", ", response.Headers.Select(h => h.Key));
            
            // ASSERT - HAS ALLOW ORIGIN
            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"), 
                $"Access-Control-Allow-Origin header not found. Available headers: {headerNames}");
            Assert.Contains("https://example.com", response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());

            // ASSERT - ALLOW METHODS
            Assert.Contains("GET", response.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault());

            // ASSERT - EXPOSE HEADERS VALUES
            var exposedHeaders = response.Headers.GetValues("Access-Control-Expose-Headers").FirstOrDefault();
            Assert.Contains("Content-Type", exposedHeaders);
            Assert.Contains("Authorization", exposedHeaders);
            Assert.Contains("X-Api-Key", exposedHeaders);
        }

        // TEST FOR CHECKING IF CORS HEADERS ARE NOT SET FOR DISALLOWED ORIGINS
        [Fact]
        public async Task Request_WithDisallowedOrigin_HasNoCorsHeaders()
        {
            // ARRANGE - SETUP REQUEST
            var request = new HttpRequestMessage(HttpMethod.Options, "/test");
            request.Headers.Add("Origin", "https://malicious-site.com");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // ACT - SEND REQUEST
            var response = await _client.SendAsync(request);

            // ASSERT - CHECK STATUS
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // ASSERT - NO CORS HEADER
            Assert.DoesNotContain("Access-Control-Allow-Origin", response.Headers.Select(h => h.Key));
        }
    }
} 
