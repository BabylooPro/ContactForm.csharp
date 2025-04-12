using System.Text.Json;

namespace ContactForm.MinimalAPI.Middleware
{
    public class ApiVersionCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiVersionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Value?.StartsWith("/api") == true &&
                !context.Request.Path.Value.StartsWith("/api/v"))
            {
                bool hasQuery = context.Request.Query.ContainsKey("api-version");
                bool hasHeader = context.Request.Headers.Keys.Any(k => k.Equals("X-Version", StringComparison.OrdinalIgnoreCase));
                bool hasVersion = hasQuery || hasHeader;

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
