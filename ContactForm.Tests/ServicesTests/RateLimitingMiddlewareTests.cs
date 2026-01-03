using System.Net;
using ContactForm.MinimalAPI.Middleware;
using ContactForm.MinimalAPI.Services;
using Moq;

namespace ContactForm.Tests.ServicesTests
{
    public class RateLimitingMiddlewareTests
    {
        private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock;
        private readonly Mock<IIpProtectionService> _ipProtectionServiceMock;
        private readonly RequestDelegate _nextMock;

        public RateLimitingMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<RateLimitingMiddleware>>();
            _ipProtectionServiceMock = new Mock<IIpProtectionService>();
            _nextMock = (HttpContext httpContext) => Task.CompletedTask;
        }

        // TEST FOR BLOCKING REQUESTS WHEN IP IS BLOCKED
        [Fact]
        public async Task InvokeAsync_BlocksRequest_WhenIpIsBlocked()
        {
            // ARRANGE - SETUP CONTEXT, STREAM, MOCKS
            var clientIp = "192.168.1.1";
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
            var responseStream = new MemoryStream();
            context.Response.Body = responseStream;
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(clientIp)).Returns(true);
            var middleware = new RateLimitingMiddleware(_nextMock, _loggerMock.Object, _ipProtectionServiceMock.Object);

            // ACT - CALL MIDDLEWARE
            await middleware.InvokeAsync(context);

            // ASSERT - STATUS, BODY, SERVICE
            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            responseStream.Position = 0;
            using var reader = new StreamReader(responseStream);
            var responseBody = await reader.ReadToEndAsync();
            Assert.Contains("blocked", responseBody);
            _ipProtectionServiceMock.Verify(x => x.IsIpBlocked(clientIp), Times.Once);
            _ipProtectionServiceMock.Verify(x => x.TrackRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // TEST FOR TRACKING REQUESTS WHEN IP IS NOT BLOCKED
        [Fact]
        public async Task InvokeAsync_TracksRequest_WhenIpIsNotBlocked()
        {
            // ARRANGE - CONTEXT, MOCKS
            var clientIp = "192.168.1.2";
            var path = "/api/test";
            var userAgent = "Test User Agent";
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
            context.Request.Path = path;
            context.Request.Headers.UserAgent = userAgent;
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(clientIp)).Returns(false);
            var middleware = new RateLimitingMiddleware(_nextMock, _loggerMock.Object, _ipProtectionServiceMock.Object);

            // ACT - CALL MIDDLEWARE
            await middleware.InvokeAsync(context);

            // ASSERT - VERIFY SERVICE CALLS
            _ipProtectionServiceMock.Verify(x => x.IsIpBlocked(clientIp), Times.Once);
            _ipProtectionServiceMock.Verify(x => x.TrackRequest(clientIp, path, userAgent), Times.Once);
        }

        // TEST FOR RETURNING 429 WHEN RATE LIMIT IS EXCEEDED
        [Fact]
        public async Task InvokeAsync_Returns429_WhenRateLimitExceeded()
        {
            // ARRANGE - CONTEXT SETUP
            var clientIp = "192.168.1.3";
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
            var responseStream = new MemoryStream();
            context.Response.Body = responseStream;
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(clientIp)).Returns(false);
            var middleware = new RateLimitingMiddleware(_nextMock, _loggerMock.Object, _ipProtectionServiceMock.Object);

            // ACT - EXCEED LIMIT
            for (int i = 0; i < 10; i++)
            {
                await middleware.InvokeAsync(context);
                context.Response.StatusCode = 200;
            }
            await middleware.InvokeAsync(context);

            // ASSERT - RATE LIMITED
            Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
            Assert.True(context.Response.Headers.ContainsKey("Retry-After"));

            // ASSERT - BODY MESSAGE
            responseStream.Position = 0;
            using var reader = new StreamReader(responseStream);
            var responseBody = await reader.ReadToEndAsync();
            Assert.Contains("Too many requests", responseBody);
        }
    }
} 
