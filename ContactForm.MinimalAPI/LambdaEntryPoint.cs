using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Asp.Versioning.ApiExplorer;

namespace ContactForm.MinimalAPI
{
    // LAMBDA FUNCTION ENTRY POINT
    public class LambdaEntryPoint : APIGatewayProxyFunction
    {
        // CONFIGURE WEB HOST BUILDER
        protected override void Init(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var webBuilder = WebApplication.CreateBuilder();
                Program.ConfigureServices(webBuilder, services);

                // ENSURE VERSIONING SERVICES ARE REGISTERED
                if (!services.Any(s => s.ServiceType == typeof(IApiVersionDescriptionProvider)))
                {
                    services.AddApiVersioning().AddApiExplorer();
                }
            })
            .Configure(app =>
            {
                app.UseCors(builder =>
                    builder.WithOrigins(
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

        public override async Task<APIGatewayProxyResponse> FunctionHandlerAsync(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
            // CORS HEADER TO GET AND POST RESPONSE
            try
            {
                // HANDLE API VERSION IN REQUEST BEFORE PROCESSING
                ProcessApiversionInRequest(request);

                // PROCESS REQUEST THROUGH BASE HANDLER
                var response = await base.FunctionHandlerAsync(request, lambdaContext);

                // INITIALIZE HEADER DICTIONARY IF NULL
                if (response.Headers == null)
                {
                    response.Headers = new Dictionary<string, string>();
                }

                // CORS HEADERS TO ENABLE CROSS-ORIGIN REQUEST
                response.Headers["Access-Control-Allow-Origin"] = GetOriginHeader(request);
                response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Api-Key, X-Amz-Date, X-Amz-Security-Token, X-Version";
                response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
                response.Headers["Access-Control-Expose-Headers"] = "X-Version";

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

        private void ProcessApiversionInRequest(APIGatewayProxyRequest request)
        {
            // ENSURE HEADERS EXIST
            if (request.Headers == null)
            {
                request.Headers = new Dictionary<string, string>();
            }

            // CHECK IF VERSION EXIST IN PATH, QUERY AND HEADER
            if (request.Path != null && request.Path.Contains("/v"))
            {
                // CHECK PATH PARAMETER VERSIONING - /api/v1/resource
                // PATH ALREADY CONTAINS VERSION INFORMATION - NO ACTION NEEDED
            }
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("api-version"))
            {
                // CHECK QUERY STRING VERSIONING - ?api-version=1.0
                // QUERY STRING ALREADY CONTAINS VERSION - NO ACTION NEEDED
            }
            else if (request.QueryStringParameters != null && request.Headers.ContainsKey("X-Version"))
            {
                // CHECK HEADER VERSIONING - X-Version: 1.0
                // HEADER ALREADY CONTAINS VERSION - NO ACTION NEEDED
            }

            // INFO: DO NOT ADD DEFAULT VERSION - REQUIRE CLIENT TO SPECIFY VERSION
            // INFO: API WILL RETURN 400 BAD REQUEST IF NO BERSION IS SPECIFIED
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
            if (request.Headers != null && request.Headers.TryGetValue("Origin", out var origin) && allowedOrigins.Contains(origin))
            {
                return origin;
            }

            // FALLBACK TO REFERER IF NO ORIGIN
            if (request.Headers != null && request.Headers.TryGetValue("Referer", out var referer))
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
