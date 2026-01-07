using System.Reflection;
using System.Text;
using API.Middleware;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR APIVERSIONERRORMIDDLEWARE HELPERS
    public class ApiVersionErrorMiddlewareCoverageTests
    {
        // TEST FOR EXTRACTREQUESTEDVERSION RETURNING NULL WHEN NOTHING IS PROVIDED
        [Fact]
        public void ExtractRequestedVersion_WhenNoPathQueryOrHeader_ReturnsNull()
        {
            // ARRANGE - GET PRIVATE METHOD VIA REFLECTION
            var method = typeof(ApiVersionErrorMiddleware).GetMethod(
                "ExtractRequestedVersion",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.NotNull(method);

            // ARRANGE - MINIMAL HTTP CONTEXT, NO VERSION INFO
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/api/anything";

            // ACT - INVOKE HELPER
            var result = (string?)method!.Invoke(null, [ctx]);

            // ASSERT - NULL
            Assert.Null(result);
        }

        // TEST FOR NORMALIZING MAJOR-ONLY VERSION (v1) AS SUPPORTED
        [Fact]
        public async Task InvokeAsync_WhenPathVersionIsMajorOnlyAndSupported_DoesNotRewriteResponse()
        {
            // ARRANGE - NEXT SETS 404 (SIMULATE NO MATCHED ENDPOINT)
            RequestDelegate next = ctx => { ctx.Response.StatusCode = 404; return Task.CompletedTask; };
            var middleware = new ApiVersionErrorMiddleware(next);

            // ARRANGE - HTTP CONTEXT
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/api/v1/anything";
            ctx.Response.Body = new MemoryStream();

            // ACT - INVOKE MIDDLEWARE
            await middleware.InvokeAsync(ctx);

            // ASSERT - STILL 404, NO JSON BODY WRITTEN
            Assert.Equal(404, ctx.Response.StatusCode);
            Assert.Equal(0, ctx.Response.Body.Length);
        }

        // TEST FOR NORMALIZING MAJOR-ONLY VERSION (v2) AS UNSUPPORTED
        [Fact]
        public async Task InvokeAsync_WhenPathVersionIsMajorOnlyAndUnsupported_RewritesResponse()
        {
            // ARRANGE - NEXT SETS 404 (SIMULATE NO MATCHED ENDPOINT)
            RequestDelegate next = ctx => { ctx.Response.StatusCode = 404; return Task.CompletedTask; };
            var middleware = new ApiVersionErrorMiddleware(next);

            // ARRANGE - HTTP CONTEXT
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/api/v2/anything";
            ctx.Response.Body = new MemoryStream();

            // ACT - INVOKE MIDDLEWARE
            await middleware.InvokeAsync(ctx);

            // ASSERT - JSON ERROR BODY
            Assert.Equal(404, ctx.Response.StatusCode);
            Assert.Equal("application/json", ctx.Response.ContentType);

            // ASSERT - CHECK RESPONSE BODY
            ctx.Response.Body.Position = 0;
            var json = await new StreamReader(ctx.Response.Body, Encoding.UTF8).ReadToEndAsync();
            Assert.Contains("Unsupported API Version", json);
            Assert.Contains("\"requestedVersion\":\"2\"", json);
        }
    }
}
