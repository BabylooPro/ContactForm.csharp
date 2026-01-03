using System.Linq;
using System.Text.Json;

namespace ContactForm.MinimalAPI.Middleware
{
    public class ApiVersionCheckMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Value?.StartsWith("/api") == true && !context.Request.Path.Value.StartsWith("/api/v")) {
                bool hasQuery = context.Request.Query.ContainsKey("api-version");
                bool hasHeader = context.Request.Headers.Keys.Any(k => k.Equals("X-Version", StringComparison.OrdinalIgnoreCase));
                bool hasVersion = hasQuery || hasHeader;

                // HANDLE AMBIGUOUS API VERSION: PRIORITIZE QUERY STRING OVER HEADER
                if (hasQuery && hasHeader)
                {
                    Console.WriteLine($"[API WARNING] Ambiguous API version detected (both query string and header present). Prioritizing query string. Request: {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
                    
                    // REMOVE HEADER TO AVOID AMBIGUITY - QUERY STRING TAKES PRIORITY
                    var headerKey = context.Request.Headers.Keys.FirstOrDefault(k => k.Equals("X-Version", StringComparison.OrdinalIgnoreCase));
                    if (headerKey != null)
                    {
                        context.Request.Headers.Remove(headerKey);
                    }
                }

                if (!hasVersion)
                {
                    Console.WriteLine($"[API WARNING] Missing API version on request: {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
                    var response = new
                    {
                        status = 400,
                        title = "API Version Required",
                        detail = "API version must be specified using one of the following methods:",
                        example = new
                        {
                            urlPath = "/api/v1/resource",
                            queryString = "?api-version=1.0",
                            header = "X-Version: 1.0"
                        }
                    };

                    var payload = JsonSerializer.Serialize(response);

                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(payload);
                    return;
                }
            }

            await _next(context);
        }
    }

    // EXTENSION METHOD TO MAKE REGISTRATION CLEANER
    public static class ApiVersionCheckMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiVersionCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiVersionCheckMiddleware>();
        }
    }
}
