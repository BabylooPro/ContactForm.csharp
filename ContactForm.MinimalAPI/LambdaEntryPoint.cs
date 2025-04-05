using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;

namespace ContactForm.MinimalAPI
{
    // LAMBDA FUNCTION ENTRY POINT
    public class LambdaEntryPoint : APIGatewayProxyFunction
    {
        // CONFIGURE WEB HOST BUILDER
        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .ConfigureServices(services =>
                {
                    var webBuilder = WebApplication.CreateBuilder();
                    Program.ConfigureServices(webBuilder, services);
                })
                .Configure(app =>
                {
                    app.UseCors(builder =>
                        builder
                            .WithOrigins(
                                "http://localhost:3000",
                                "https://maxremy.dev",
                                "https://keypops.app"
                            )
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                    );
                    Program.ConfigureApp(app);
                });
        }

        public override async Task<APIGatewayProxyResponse> FunctionHandlerAsync(
            APIGatewayProxyRequest request,
            ILambdaContext lambdaContext
        )
        {
            // CORS HEADER TO GET AND POST RESPONSE
            try
            {
                // PROCESS REQUEST THROUGH BASE HANDLER
                var response = await base.FunctionHandlerAsync(request, lambdaContext);

                // INITIALIZE HEADER DICTIONARY IF NULL
                if (response.Headers == null)
                {
                    response.Headers = new Dictionary<string, string>();
                }

                // CORS HEADERS TO ENABLE CROSS-ORIGIN REQUEST
                response.Headers["Access-Control-Allow-Origin"] = GetOriginHeader(request);
                response.Headers["Access-Control-Allow-Headers"] =
                    "Content-Type, Authorization, X-Api-Key, X-Amz-Date, X-Amz-Security-Token";
                response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
                
                // SECURITY HEADERS
                response.Headers["X-Content-Type-Options"] = "nosniff";
                response.Headers["X-Frame-Options"] = "DENY";
                response.Headers["X-XSS-Protection"] = "1; mode=block";
                response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                response.Headers["Content-Security-Policy"] = "default-src 'self'";

                return response;
            }
            catch (Exception ex)
            {
                // LOG AND RETHROW
                Console.Error.WriteLine($"Error in Lambda handler: {ex}");
                throw;
            }
        }
        
        // HELPER METHOD TO GET ORIGIN HEADER BASED ON REQUEST
        private string GetOriginHeader(APIGatewayProxyRequest request)
        {
            var allowedOrigins = new[] 
            {
                "http://localhost:3000",
                "https://maxremy.dev",
                "https://keypops.app"
            };
            
            // CHECK IF ORIGIN HEADER IS PRESENT
            if (request.Headers != null && 
                request.Headers.TryGetValue("Origin", out var origin) &&
                allowedOrigins.Contains(origin))
            {
                return origin;
            }
            
            // FALLBACK TO REFERER IF NO ORIGIN
            if (request.Headers != null && 
                request.Headers.TryGetValue("Referer", out var referer))
            {
                foreach (var allowedOrigin in allowedOrigins)
                {
                    if (referer.StartsWith(allowedOrigin))
                    {
                        return allowedOrigin;
                    }
                }
            }
            
            // DEFAULT TO FIRST ALLOWED ORIGIN IF NOT MATCHED
            return allowedOrigins[0];
        }
    }
}
