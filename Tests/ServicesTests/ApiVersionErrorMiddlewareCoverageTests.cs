using System.Reflection;
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
    }
}
