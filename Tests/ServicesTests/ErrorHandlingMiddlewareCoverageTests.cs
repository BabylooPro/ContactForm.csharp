using System.Text.Json;
using API.Middleware;
using Moq;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR ERROR HANDLING MIDDLEWARE
    public class ErrorHandlingMiddlewareCoverageTests
    {
        // TEST FOR INVOKE WRITING JSON 500 WHEN NEXT THROWS
        [Fact]
        public async Task Invoke_WhenNextThrows_WritesJson500()
        {
            // ARRANGE - BUILD HTTP CONTEXT WITH WRITABLE RESPONSE BODY
            var context = new DefaultHttpContext();
            await using var body = new MemoryStream();
            context.Response.Body = body;

            // ARRANGE - NEXT THROWS
            RequestDelegate next = _ => throw new Exception("boom");
            var logger = new Mock<ILogger<ErrorHandlingMiddleware>>().Object;
            var middleware = new ErrorHandlingMiddleware(next, logger);

            // ACT - INVOKE MIDDLEWARE
            await middleware.Invoke(context);

            // ASSERT - RESPONSE IS JSON 500
            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            // ASSERT - RESPONSE BODY CONTAINS ERROR PROPERTY
            body.Position = 0;
            using var doc = JsonDocument.Parse(await new StreamReader(body).ReadToEndAsync());
            Assert.True(doc.RootElement.TryGetProperty("error", out var err));
            Assert.False(string.IsNullOrWhiteSpace(err.GetString()));
        }
    }
}
