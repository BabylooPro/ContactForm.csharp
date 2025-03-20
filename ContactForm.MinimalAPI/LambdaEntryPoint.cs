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
                        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
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
                response.Headers["Access-Control-Allow-Origin"] = "*";
                response.Headers["Access-Control-Allow-Header"] =
                    "Content-Type, Authorization, X-Api-Key, X-Amz-Date, X-Amz-Security-Token";
                response.Headers["Access-Control-Allow-Methods"] = "GET, POST";

                return response;
            }
            catch (System.Exception ex)
            {
                // LOG ERROR AND RETURN 500 RESPONSE
                lambdaContext.Logger.LogLine($"Error: {ex}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = "Internal Server Error",
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "text/plain" },
                        { "Access-Control-Allow-Origin", "*" },
                        {
                            "Access-Control-Allow-Header",
                            "Content-Type, Authorization, X-Api-Key, X-Amz-Date, X-Amz-Security-Token"
                        },
                        { "Access-Control-Allow-Methods", "GET, POST" },
                    },
                };
            }
        }
    }
}
