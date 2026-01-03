using System.Net;

namespace ContactForm.MinimalAPI.Middleware
{
    // ERROR HANDLING MIDDLEWARE
    public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        // REQUEST DELEGATE AND LOGGER DEPENDENCY INJECTION
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;

        // INVOKE METHOD FOR HANDLING EXCEPTIONS
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        // METHOD FOR HANDLING EXCEPTIONS
        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // LOGGING UNHANDLED EXCEPTION
            _logger.LogError(exception, "An unhandled exception has occurred.");
            var code = HttpStatusCode.InternalServerError; // 500 IF UNHANDLED EXCEPTION OCCURS

            // RETURNING ERROR MESSAGE
            var result = System.Text.Json.JsonSerializer.Serialize(new { error = "An unexpected error has occurred. Please try again later." });
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(result); // RETURNING ERROR MESSAGE
        }
    }
}
