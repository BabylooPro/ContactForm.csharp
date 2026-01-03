using System.Threading.RateLimiting;
using ContactForm.MinimalAPI.Services;

namespace ContactForm.MinimalAPI.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly FixedWindowRateLimiter _rateLimiter;
        private readonly IIpProtectionService _ipProtectionService;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IIpProtectionService ipProtectionService) {
            _next = next;
            _logger = logger;
            _ipProtectionService = ipProtectionService;
            
            // CREATE A RATE LIMITER WITH 10 REQUESTS PER MINUTE PER IP
            _rateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // NO QUEUE - REJECT IMMEDIATELY
            });
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // GET CLIENT IP ADDRESS
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.ToString();
            var userAgent = context.Request.Headers.UserAgent.ToString();
            
            // CHECK IF IP IS BLOCKED
            if (_ipProtectionService.IsIpBlocked(clientIp))
            {
                _logger.LogWarning("Request blocked from blacklisted IP: {ClientIp}", clientIp);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Your IP address has been blocked due to suspicious activity.");
                return;
            }
            
            // TRACK THIS REQUEST FOR ABUSE DETECTION
            _ipProtectionService.TrackRequest(clientIp, path, userAgent);
            
            // ACQUIRE A RATE LIMITING LEASE
            using RateLimitLease lease = await _rateLimiter.AcquireAsync(1);
            
            // CHECK IF THE REQUEST IS ALLOWED OR DENIED DUE TO RATE LIMITING
            if (lease.IsAcquired)
            {
                await _next(context);
            }
            else
            {
                _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = "60"; // RETRY AFTER 60 SECONDS
                await context.Response.WriteAsync("Too many requests. Please try again later.");
            }
        }
    }

    // EXTENSION METHOD TO EASILY ADD THE MIDDLEWARE
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
} 
