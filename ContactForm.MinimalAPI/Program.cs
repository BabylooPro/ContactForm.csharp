using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Middleware;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Services;
using ContactForm.MinimalAPI.Utilities;
using dotenv.net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ContactForm.Tests")]

namespace ContactForm.MinimalAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                // CREATING WEB APPLICATION
                var builder = WebApplication.CreateBuilder(args);
                ConfigureServices(builder, builder.Services);

                // BUILDING WEB APPLICATION
                var app = builder.Build();
                ConfigureApp(app);

                // RUNNING WEB APPLICATION
                app.Run();
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains("SMTP connection test failed"))
            {
                // HANDLE SMTP CONNECTIONS FAILURE
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }

        public static void ConfigureServices(
            WebApplicationBuilder builder,
            IServiceCollection services
        )
        {
            // LOADING ENVIRONMENT VARIABLES
            DotEnv.Load();

            // GET SMTP CONFIGURATIONS FROM APPSETTINGS
            var config = builder
                .Configuration.GetSection("SmtpSettings:Configurations")
                .Get<List<SmtpConfig>>();
            if (config == null)
            {
                throw new InvalidOperationException(
                    "No SMTP configurations found in appsettings.json"
                );
            }

            // DYNAMICALLY CHECK FOR MISSING SMTP PASSWORD VARIABLES
            var missingVariables = config
                .Select(smtp => $"SMTP_{smtp.Index}_PASSWORD")
                .Where(envVar => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
                .ToList();

            if (missingVariables.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The following environment variables are missing or empty: {string.Join(", ", missingVariables)}"
                );
            }

            // ADDING SERVICES WITH CORS POLICY
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders(
                            "Content-Type",
                            "Authorization",
                            "X-Api-Key",
                            "X-Amz-Date",
                            "X-Amz-Security-Token"
                        );
                });
            });

            // ADDING JSON OPTIONS
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = null;
            });

            // CONFIGURE SERVICES
            services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISmtpTestService, SmtpTestService>();
            services.AddScoped<ISmtpClientWrapper, SmtpClientWrapper>();
            services.AddScoped<IEmailTrackingService, EmailTrackingService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();

            // ADDING CONTROLLER SUPPORT
            services.AddControllers();

            // ADDING LAMBDA SUPPORT
            services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
        }

        public static void ConfigureApp(IApplicationBuilder app)
        {
            // CONFIGURING MIDDLEWARE
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseCors();
            app.UseRouting();
            app.UseAuthorization();
            app.UseHttpsRedirection();

            // CONFIGURE ENDPOINT ROUTE FOR CONTROLLERS
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); // CONTROLLER SOURCE
                endpoints.MapGet("/test", () => "Test: route is working"); // TESTING ROUTE
            });
        }

        // OVERLOAD FOR WEBAPPLICATION
        public static void ConfigureApp(WebApplication app)
        {
            // CONFIGURE THE APPLICATION
            ConfigureApp((IApplicationBuilder)app);

            // VERIFY SMTP CONNECTIONS BEFORE STARTING THE APP
            EnsureSmtpConnectionsAsync(app.Services).GetAwaiter().GetResult();
        }

        public static async Task EnsureSmtpConnectionsAsync(IServiceProvider serviceProvider)
        {
            // GET LOGGER
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // TEST SMTP CONNECTIONS
            try
            {
                using var scope = serviceProvider.CreateScope();
                var smtpTestService = scope.ServiceProvider.GetRequiredService<ISmtpTestService>();
                await smtpTestService.TestSmtpConnections();
            }
            catch (Exception)
            {
                // NEED TO HANDLE SHUTDOWN WITHOUT DIRECT APP REFERENCE
                logger.LogCritical("SMTP TEST FAILED");
                throw new InvalidOperationException(
                    "SMTP connection test failed. Application must be terminated."
                );
            }
        }
    }
}
