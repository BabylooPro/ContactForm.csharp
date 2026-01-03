using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace ContactForm.MinimalAPI.Middleware
{
    // MIDDLEWARE TO HANDLE CUSTOM ERROR MESSAGES FOR UNSUPPORTED API VERSIONS
    public partial class ApiVersionErrorMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        // INVOKE METHOD FOR HANDLING UNSUPPORTED API VERSION ERRORS
        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == 404 && context.Request.Path.Value?.StartsWith("/api") == true)
            {
                var requestedVersion = ExtractRequestedVersion(context);
                
                if (requestedVersion != null && requestedVersion != "1.0")
                {
                    var errorResponse = new
                    {
                        status = 404,
                        title = "Unsupported API Version",
                        detail = $"The API version '{requestedVersion}' is not supported. Supported versions: 1.0",
                        requestedVersion,
                        supportedVersions = new[] { "1.0" }
                    };

                    var jsonResponse = JsonSerializer.Serialize(errorResponse);

                    context.Response.Clear();
                    context.Response.StatusCode = 404;
                    context.Response.ContentType = "application/json";
                    
                    await context.Response.WriteAsync(jsonResponse);
                }
            }
        }

        // HELPER METHOD TO EXTRACT REQUESTED VERSION FROM REQUEST
        private static string? ExtractRequestedVersion(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path != null)
            {
                var pathMatch = PathVersionRegex().Match(path);
                if (pathMatch.Success)
                {
                    return pathMatch.Groups[1].Value;
                }
            }

            if (context.Request.Query.TryGetValue("api-version", out var queryVersion))
            {
                return queryVersion.ToString();
            }

            if (context.Request.Headers.TryGetValue("X-Version", out var headerVersion))
            {
                return headerVersion.ToString();
            }

            return null;
        }

        [GeneratedRegex(@"/api/v([\d.]+)/")]
        private static partial Regex PathVersionRegex();
    }

    // EXTENSION METHOD TO MAKE REGISTRATION CLEANER
    public static class ApiVersionErrorMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiVersionError(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiVersionErrorMiddleware>();
        }
    }
}
