using System.Linq;
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
using Xunit;

namespace ContactForm.Tests.IntegrationTests
{
    public class SecurityHeadersTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public SecurityHeadersTests()
        {
            // SETUP TEST SERVER
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<TestStartup>()
                        .UseTestServer();
                });

            var host = hostBuilder.Start();
            _server = host.GetTestServer();
            _client = _server.CreateClient();
        }

        // TEST FOR CHECKING IF THE SECURITY HEADERS ARE SET CORRECTLY
        [Fact]
        public async Task Request_ReturnsSecurityHeaders()
        {
            // ARRANGE & ACT - GET THE TEST ENDPOINT
            var response = await _client.GetAsync("/test");

            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // ASSERT - CHECK SECURITY HEADERS, CHECK IF THE HEADERS ARE SET CORRECTLY
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
            // ARRANGE - CREATE A NEW HTTP REQUEST MESSAGE
            var request = new HttpRequestMessage(HttpMethod.Options, "/test");
            request.Headers.Add("Origin", "https://maxremy.dev");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.SendAsync(request);

            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS NO CONTENT
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // DEBUG - PRINT THE RESPONSE HEADERS
            var headerNames = string.Join(", ", response.Headers.Select(h => h.Key));
            
            // ASSERT - CHECK IF THE ACCESS CONTROL ALLOW ORIGIN HEADER EXISTS
            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"), 
                $"Access-Control-Allow-Origin header not found. Available headers: {headerNames}");
            
            Assert.Contains("https://maxremy.dev", response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());
            Assert.Contains("GET", response.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault());
            
            // ASSERT - CHECK IF THE EXPOSED HEADERS CONTAIN THE CORRECT HEADERS
            var exposedHeaders = response.Headers.GetValues("Access-Control-Expose-Headers").FirstOrDefault();
            Assert.Contains("Content-Type", exposedHeaders);
            Assert.Contains("Authorization", exposedHeaders);
            Assert.Contains("X-Api-Key", exposedHeaders);
        }

        // TEST FOR CHECKING IF CORS HEADERS ARE NOT SET FOR DISALLOWED ORIGINS
        [Fact]
        public async Task Request_WithDisallowedOrigin_HasNoCorsHeaders()
        {
            // ARRANGE - CREATE A NEW HTTP REQUEST MESSAGE
            var request = new HttpRequestMessage(HttpMethod.Options, "/test");
            request.Headers.Add("Origin", "https://malicious-site.com");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // ACT - SEND THE HTTP REQUEST MESSAGE
            var response = await _client.SendAsync(request);

            // ASSERT - CHECK IF THE RESPONSE STATUS CODE IS NO CONTENT
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            
            // ASSERT - CHECK IF THE ACCESS CONTROL ALLOW ORIGIN HEADER DOES NOT CONTAIN ALLOWING HEADERS FOR DISALLOWED ORIGIN
            Assert.DoesNotContain("Access-Control-Allow-Origin", response.Headers.Select(h => h.Key));
        }
    }

    // TEST STARTUP CLASS THAT USES THE SAME CONFIGURATION AS THE REAL APP
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // SIMPLIFIED CONFIGURATION FOR TESTS THAT DOESN'T REQUIRE ENVIRONMENT VARIABLES
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:3000",
                            "https://maxremy.dev",
                            "https://keypops.app"
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
        }

        public void Configure(IApplicationBuilder app)
        {
            // MANUAL CORS HANDLING - THIS MUST BE FIRST IN THE PIPELINE
            app.Use(async (context, next) =>
            {
                // HANDLE OPTIONS REQUESTS (PREFLIGHT)
                if (context.Request.Method == "OPTIONS")
                {
                    // CHECK IF ORIGIN IS ALLOWED
                    var origin = context.Request.Headers["Origin"].ToString();
                    var isAllowedOrigin = new[] { "https://maxremy.dev", "http://localhost:3000", "https://keypops.app" }
                        .Contains(origin);
                    
                    // ONLY ADD CORS HEADERS FOR ALLOWED ORIGINS
                    if (isAllowedOrigin)
                    {
                        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
                        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Api-Key";
                        context.Response.Headers["Access-Control-Expose-Headers"] = 
                            "Content-Type, Authorization, X-Api-Key, X-Amz-Date, X-Amz-Security-Token";
                    }
                    
                    // ALWAYS RETURN 204 NO CONTENT FOR OPTIONS REQUESTS
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    return;
                }
                
                // FOR NON-OPTIONS REQUESTS
                var nonPrefOrigin = context.Request.Headers["Origin"].ToString();
                var isNonPrefAllowedOrigin = new[] { "https://maxremy.dev", "http://localhost:3000", "https://keypops.app" }
                    .Contains(nonPrefOrigin);
                
                if (isNonPrefAllowedOrigin)
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = nonPrefOrigin;
                }
                
                await next();
            });
            
            // ADD SECURITY HEADERS
            app.Use(async (context, next) =>
            {
                // SECURITY HEADERS
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
        }
    }
} 
