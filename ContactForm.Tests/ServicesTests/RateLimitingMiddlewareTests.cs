using System.IO;
using System.Net;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Middleware;
using ContactForm.MinimalAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var clientIp = "192.168.1.1";
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
            
            var responseStream = new MemoryStream();
            context.Response.Body = responseStream;

            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(clientIp)).Returns(true);

            var middleware = new RateLimitingMiddleware(
                _nextMock,
                _loggerMock.Object,
                _ipProtectionServiceMock.Object
            );

            // ACT - INVOKE THE MIDDLEWARE
            await middleware.InvokeAsync(context);

            // ASSERT - CHECK THE RESPONSE STATUS CODE
            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            
            // VERIFY THE RESPONSE MESSAGE
            responseStream.Position = 0;
            using var reader = new StreamReader(responseStream);
            var responseBody = await reader.ReadToEndAsync();
            Assert.Contains("blocked", responseBody);
            
            // VERIFY THE IP PROTECTION SERVICE CALLS
            _ipProtectionServiceMock.Verify(x => x.IsIpBlocked(clientIp), Times.Once);
            _ipProtectionServiceMock.Verify(x => x.TrackRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // TEST FOR TRACKING REQUESTS WHEN IP IS NOT BLOCKED
        [Fact]
        public async Task InvokeAsync_TracksRequest_WhenIpIsNotBlocked()
        {
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var clientIp = "192.168.1.2";
            var path = "/api/test";
            var userAgent = "Test User Agent";
            
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
            context.Request.Path = path;
            context.Request.Headers.UserAgent = userAgent;
            
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(clientIp)).Returns(false);

            var middleware = new RateLimitingMiddleware(
                _nextMock,
                _loggerMock.Object,
                _ipProtectionServiceMock.Object
            );

            // ACT - INVOKE THE MIDDLEWARE
            await middleware.InvokeAsync(context);

            // ASSERT - CHECK THE IP PROTECTION SERVICE CALLS
            _ipProtectionServiceMock.Verify(x => x.IsIpBlocked(clientIp), Times.Once);
            _ipProtectionServiceMock.Verify(x => x.TrackRequest(clientIp, path, userAgent), Times.Once);
        }

        // TEST FOR RETURNING 429 WHEN RATE LIMIT IS EXCEEDED
        [Fact]
        public async Task InvokeAsync_Returns429_WhenRateLimitExceeded()
        {
            // ARRANGE - SETUP THE TEST ENVIRONMENT
            var clientIp = "192.168.1.3";
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
            
            var responseStream = new MemoryStream();
            context.Response.Body = responseStream;
            
            _ipProtectionServiceMock.Setup(x => x.IsIpBlocked(clientIp)).Returns(false);

            var middleware = new RateLimitingMiddleware(
                _nextMock,
                _loggerMock.Object,
                _ipProtectionServiceMock.Object
            );

            // ACT - EXHAUST THE RATE LIMIT
            // THE MIDDLEWARE ALLOWS 10 REQUESTS PER MINUTE BY DEFAULT
            for (int i = 0; i < 10; i++) // FIRST 10 REQUESTS SHOULD PASS
            {
                await middleware.InvokeAsync(context);
                // RESET THE RESPONSE FOR THE NEXT REQUEST
                context.Response.StatusCode = 200;
            }
            
            // LAST REQUEST SHOULD BE RATE LIMITED
            await middleware.InvokeAsync(context);

            // ASSERT - THE LAST REQUEST SHOULD BE RATE LIMITED
            Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
            Assert.True(context.Response.Headers.ContainsKey("Retry-After"));
            
            // VERIFY THE RESPONSE MESSAGE
            responseStream.Position = 0;
            using var reader = new StreamReader(responseStream);
            var responseBody = await reader.ReadToEndAsync();
            Assert.Contains("Too many requests", responseBody);
        }
    }
} 
