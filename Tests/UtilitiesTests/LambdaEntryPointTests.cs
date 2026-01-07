using System.Reflection;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using API;
using API.Models;
using Microsoft.AspNetCore;

namespace Tests.UtilitiesTests
{
    // UNIT TESTS FOR LAMBDA ENTRY POINT (INIT + PRIVATE HELPERS)
    public class LambdaEntryPointTests
    {
        // SNAPSHOT ENVIRONMENT VARIABLES FOR TEST ISOLATION
        private static Dictionary<string, string?> SnapshotEnv(params string[] keys)
        {
            var snapshot = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var key in keys) snapshot[key] = Environment.GetEnvironmentVariable(key);
            return snapshot;
        }

        // RESTORE ENVIRONMENT VARIABLES FOR TEST ISOLATION
        private static void RestoreEnv(Dictionary<string, string?> snapshot)
        {
            foreach (var (key, value) in snapshot) Environment.SetEnvironmentVariable(key, value);
        }

        private sealed class TestLambdaEntryPoint : LambdaEntryPoint
        {
            public void CallInit(IWebHostBuilder builder) => base.Init(builder);
        }

        // TEST FOR INIT REGISTERING SERVICES WITHOUT THROWING
        [Fact]
        public void Init_RegistersConfigurationWithoutThrowing()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS", "SMTP_0_PASSWORD", "SMTP_RECEPTION_EMAIL", "SMTP_CATCHALL_EMAIL");

            try
            {
                // ARRANGE - CONFIGURATION + REQUIRED PASSWORD VARS
                var configs = new List<SmtpConfig>
                {
                    new()
                    {
                        Index = 0,
                        Host = "127.0.0.1",
                        Port = 1,
                        Email = "test@example.com",
                        Description = "Test SMTP"
                    }
                };

                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", JsonSerializer.Serialize(configs));
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test-password");
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

                // ARRANGE - BUILD WEB HOST
                var builder = WebHost.CreateDefaultBuilder().UseEnvironment("Testing");
                var entryPoint = new TestLambdaEntryPoint();
                entryPoint.CallInit(builder);

                // ACT - BUILD HOST
                using var host = builder.Build();

                // ASSERT - HOST WAS BUILT
                Assert.NotNull(host.Services);
            }
            finally
            {
                // CLEANUP - RESTORE ENVIRONMENT
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR PROCESSAPIVERSIONINREQUEST COVERING ALL DECISION PATHS
        [Fact]
        public void ProcessApiversionInRequest_CoversAllDecisionPaths()
        {
            var method = typeof(LambdaEntryPoint).GetMethod("ProcessApiversionInRequest", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // ARRANGE - PATH VERSION
            var req1 = new APIGatewayProxyRequest
            {
                Headers = null,
                Path = "/api/v1/test"
            };

            // ACT - INVOKE
            method!.Invoke(null, [req1]);

            // ASSERT - HEADERS INITIALIZED
            Assert.NotNull(req1.Headers);

            // ARRANGE - QUERY STRING VERSION
            var req2 = new APIGatewayProxyRequest
            {
                Headers = new Dictionary<string, string>(),
                Path = "/api/test",
                QueryStringParameters = new Dictionary<string, string> { ["api-version"] = "1.0" }
            };

            // ACT - INVOKE
            method.Invoke(null, [req2]);

            // ARRANGE - HEADER VERSION
            var req3 = new APIGatewayProxyRequest
            {
                Headers = new Dictionary<string, string> { ["X-Version"] = "2.0" },
                Path = "/api/test",
                QueryStringParameters = new Dictionary<string, string>()
            };

            // ACT - INVOKE
            method.Invoke(null, [req3]);

            // ARRANGE - NO VERSION
            var req4 = new APIGatewayProxyRequest
            {
                Headers = new Dictionary<string, string>(),
                Path = "/api/test",
                QueryStringParameters = null
            };

            // ACT - INVOKE
            method.Invoke(null, [req4]);

            // ARRANGE - NULL PATH
            var req5 = new APIGatewayProxyRequest
            {
                Headers = new Dictionary<string, string>(),
                Path = null,
                QueryStringParameters = new Dictionary<string, string>()
            };

            // ACT - INVOKE
            method.Invoke(null, [req5]);
        }

        // TEST FOR GETORIGINHEADER COVERING ALL FALLBACKS
        [Fact]
        public void GetOriginHeader_CoversAllFallbacks()
        {
            var envSnapshot = SnapshotEnv("CORS_1_ORIGIN", "CORS_2_ORIGIN");

            try
            {
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://allowed.example");
                Environment.SetEnvironmentVariable("CORS_2_ORIGIN", "https://second.example");

                var method = typeof(LambdaEntryPoint).GetMethod("GetOriginHeader", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);

                // ARRANGE - ORIGIN HEADER IS LOCALHOST
                var reqLocalOrigin = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string> { ["Origin"] = "http://localhost:1234" }
                };

                // ASSERT - RETURNS ORIGIN AS-IS
                Assert.Equal("http://localhost:1234", (string)method!.Invoke(null, [reqLocalOrigin])!);

                // ARRANGE - ORIGIN HEADER IS IN ALLOWED LIST
                var reqAllowedOrigin = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string> { ["Origin"] = "https://allowed.example" }
                };

                // ASSERT - RETURNS ORIGIN
                Assert.Equal("https://allowed.example", (string)method.Invoke(null, [reqAllowedOrigin])!);

                // ARRANGE - REFERER IS LOCALHOST
                var reqLocalReferer = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string> { ["Referer"] = "http://localhost:9999/page" }
                };

                // ASSERT - RETURNS REFERER
                Assert.Equal("http://localhost:9999/page", (string)method.Invoke(null, [reqLocalReferer])!);

                // ARRANGE - REFERER STARTS WITH AN ALLOWED ORIGIN
                var reqAllowedReferer = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string> { ["Referer"] = "https://allowed.example/some/page" }
                };

                // ASSERT - RETURNS MATCHING ALLOWED ORIGIN
                Assert.Equal("https://allowed.example", (string)method.Invoke(null, [reqAllowedReferer])!);

                // ARRANGE - REFERER DOES NOT MATCH ANY ALLOWED ORIGIN
                var reqUnmatchedReferer = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string> { ["Referer"] = "https://not-allowed.example/some/page" }
                };

                // ASSERT - FALLS BACK TO FIRST ALLOWED ORIGIN
                Assert.Equal("https://allowed.example", (string)method.Invoke(null, [reqUnmatchedReferer])!);

                // ARRANGE - NO ORIGIN OR REFERER
                var reqNoHeaders = new APIGatewayProxyRequest { Headers = new Dictionary<string, string>() };

                // ASSERT - FALLS BACK TO FIRST ALLOWED ORIGIN
                Assert.Equal("https://allowed.example", (string)method.Invoke(null, [reqNoHeaders])!);

                // ARRANGE - NO ORIGINS CONFIGURED
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", null);
                Environment.SetEnvironmentVariable("CORS_2_ORIGIN", null);

                var reqFallbackOrigin = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string?> { ["Origin"] = null! }
                        .ToDictionary(k => k.Key, v => v.Value)
                };

                // ASSERT - FALLS BACK TO "*"
                Assert.Equal("*", (string)method.Invoke(null, [reqFallbackOrigin])!);

                var reqNullHeaders = new APIGatewayProxyRequest { Headers = null };

                // ASSERT - FALLS BACK TO "*"
                Assert.Equal("*", (string)method.Invoke(null, [reqNullHeaders])!);
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }
    }
}
