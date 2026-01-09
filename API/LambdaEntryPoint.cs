using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Asp.Versioning.ApiExplorer;
using API.Utilities;

namespace API
{
    // LAMBDA FUNCTION ENTRY POINT
    public class LambdaEntryPoint : APIGatewayProxyFunction
    {
        // CONFIGURE WEB HOST BUILDER
        protected override void Init(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                Program.ConfigureServices(services, context.Configuration);

                // ENSURE VERSIONING SERVICES ARE REGISTERED
                if (!services.Any(s => s.ServiceType == typeof(IApiVersionDescriptionProvider)))
                {
                    services.AddApiVersioning().AddApiExplorer();
                }
            })
            .Configure(app =>
            {
                // LOAD CORS ORIGINS FROM ENVIRONMENT VARIABLES
                var corsOrigins = EnvironmentUtils.LoadCorsOriginsFromEnvironment();
                
                app.UseCors(builder =>
                    builder.SetIsOriginAllowed(origin =>
                    {
                        // ALLOW ALL LOCALHOST ORIGINS (ANY PORT)
                        if (EnvironmentUtils.IsLocalhostOrigin(origin)) return true;
                        
                        // CHECK IF ORIGIN IS IN ALLOWED LIST
                        return corsOrigins.Contains(origin);
                    })
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
                response.Headers ??= new Dictionary<string, string>();

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

        private static void ProcessApiversionInRequest(APIGatewayProxyRequest request)
        {
            // ENSURE HEADERS EXIST
            request.Headers ??= new Dictionary<string, string>();

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
        private static string GetOriginHeader(APIGatewayProxyRequest request)
        {
            // LOAD CORS ORIGINS FROM ENVIRONMENT VARIABLES
            var allowedOrigins = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

            // CHECK IF ORIGIN HEADER IS PRESENT
            if (request.Headers != null && request.Headers.TryGetValue("Origin", out var origin))
            {
                // ALLOW ALL LOCALHOST ORIGINS
                if (EnvironmentUtils.IsLocalhostOrigin(origin)) return origin;
                
                // CHECK IF ORIGIN IS IN ALLOWED LIST
                if (allowedOrigins.Contains(origin)) return origin;
            }

            // FALLBACK TO REFERER IF NO ORIGIN
            if (request.Headers != null && request.Headers.TryGetValue("Referer", out var referer))
            {
                // CHECK IF REFERER IS LOCALHOST
                if (EnvironmentUtils.IsLocalhostOrigin(referer)) return referer;
                
                // CHECK IF REFERER STARTS WITH ANY ALLOWED ORIGIN
                foreach (var allowedOrigin in allowedOrigins)
                {
                    if (referer.StartsWith(allowedOrigin)) return allowedOrigin;
                }
            }

            // DEFAULT TO FIRST ALLOWED ORIGIN IF AVAILABLE, OTHERWISE RETURN ORIGIN FROM REQUEST OR EMPTY
            if (allowedOrigins.Count > 0) return allowedOrigins[0];
            
            // IF NO ORIGINS CONFIGURED, RETURN ORIGIN FROM REQUEST IF PRESENT
            if (request.Headers != null && request.Headers.TryGetValue("Origin", out var fallbackOrigin))
                return fallbackOrigin ?? "*";
            
            return "*";
        }
    }
}
