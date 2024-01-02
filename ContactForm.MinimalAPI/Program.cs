using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MailKit.Net.Smtp;
using MimeKit;
using dotenv.net;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

public partial class Program
{
    public static void Main(string[] args)
    {
        //TODO: RETRIEVING USER CHOICES FOR ENVIRONMENT AND LOGGING TYPE VIA STARTUPHELPER
        // var (environment, loggingType) = StartupHelper.GetStartupOptions();
        // Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

        // INITIAL CONFIGURATION AND ENVIRONMENT VARIABLE LOADING
        var builder = WebApplication.CreateBuilder(args);

        //TODO: APPLICATION CONFIGURATION VIA USER CHOICE STARTUPHELPER AFTER BUILDER INITIAL
        // StartupHelper.ConfigureApp(builder, environment, loggingType);


        // JSON SERVICE CONFIGURATION
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null;
        });

        // CORS CONFIGURATION (Cross-Origin Resource Sharing)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://example.com", "https://localhost:7129", "http://localhost:5130")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        var app = builder.Build();

        // ERROR MANAGEMENT MIDDLEWARE
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { Message = "ERROR: INTERCEPTED BY MIDDLEWARE" });
                app.Logger.LogError(ex, "ERROR: INTERCEPTED BY MIDDLEWARE");
            }
        });

        // APPLY CORS CONFIGURATION & ENVIRONMENT VARIABLE LOADING
        app.UseCors();
        DotEnv.Load();

        // SMTP SERVICE VARIABLE DEFINITION
        var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
        var smtpPortString = Environment.GetEnvironmentVariable("SMTP_PORT");
        var smtpEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL");
        var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        var receptionEmail = Environment.GetEnvironmentVariable("RECEPTION_EMAIL");

        // TEST ROUTE
        app.MapGet("/test", () => "TEST: ROUTE IS WORKING");

        // EMAIL SENDING ROUTE
        app.MapPost("/send-email", async (EmailRequest request, ILogger<Program> logger) =>
        {
            // REQUEST VALIDATION
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
            {
                return Results.BadRequest(validationResults);
            }

            try
            {
                // CREATING AND CONFIGURING EMAIL MESSAGE
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("", $"{smtpEmail}")); // Sender of email
                emailMessage.To.Add(new MailboxAddress("", $"{receptionEmail}")); // Receiver of email
                emailMessage.To.Add(new MailboxAddress("", $"{request.Email}")); // A copy of email for user who uses "Contact Form" service
                emailMessage.Subject = "New Message from Contact Form";
                emailMessage.Body = new TextPart("plain")
                {
                    Text = $"Name: {request.Username}\nEmail: {request.Email}\nMessage: {request.Message}"
                };

                // SENDING MESSAGE VIA SMTP
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    //CHECKING IF ENVIRONMENT VARIABLES ARE NULL
                    //TODO: OPTIMIZE ENVIRONMENT VARIABLE CHECKING BY REPLACING MULTIPLE 'IF' STATEMENTS WITH A MORE ELEGANT AND EFFICIENT SOLUTION
                    var missingVariables = new List<string>();
                    if (smtpHost is null) missingVariables.Add("SMTP_HOST");
                    if (smtpPortString is null) missingVariables.Add("SMTP_PORT");
                    if (smtpEmail is null) missingVariables.Add("SMTP_EMAIL");
                    if (smtpPassword is null) missingVariables.Add("SMTP_PASSWORD");
                    if (receptionEmail is null) missingVariables.Add("RECEPTION_EMAIL");

                    if (missingVariables.Any())
                    {
                        var missingVariablesString = string.Join(", ", missingVariables);
                        throw new InvalidOperationException($"SMTP ENVIRONMENT VARIABLES ARE NOT PROPERLY SET: {missingVariablesString}");
                    }

                    if (!int.TryParse(smtpPortString, out var smtpPort))
                    {
                        throw new FormatException("'SMTP_PORT' IS NOT A VALID NUMBER");
                    }

                    await client.ConnectAsync(smtpHost, smtpPort, true);
                    await client.AuthenticateAsync(smtpEmail, smtpPassword);

                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }

                // LOGGING SUCCESS OF SENDING
                logger.LogInformation($"CONTACT FORM SUCCESS: EMAIL SENT TO {smtpEmail?.ToUpper()} & {receptionEmail?.ToUpper()} FROM {request.Email?.ToUpper()}", request.Email);
                return Results.Ok(new { Message = "SUCCESS: EMAIL SENT" });

            }
            catch (FormatException ex)
            {
                logger.LogError(ex, $"CONTACT FORM ERROR: FAILED TO SEND EMAIL TO {smtpEmail?.ToUpper()} FROM {request.Email?.ToUpper()}", request.Email);
                return Results.BadRequest(new { Message = $"FORMAT ERROR: {ex.Message}" });
            }
            catch (SmtpCommandException ex)
            {
                logger.LogError(ex, $"CONTACT FORM ERROR: FAILED TO SEND EMAIL TO {smtpEmail?.ToUpper()} FROM {request.Email?.ToUpper()}", request.Email);
                return Results.BadRequest(new { Message = $"SMTP COMMAND ERROR: {ex.Message}" });
            }
            catch (SmtpProtocolException ex)
            {
                logger.LogError(ex, $"CONTACT FORM ERROR: FAILED TO SEND EMAIL TO {smtpEmail?.ToUpper()} FROM {request.Email?.ToUpper()}", request.Email);
                return Results.BadRequest(new { Message = $"SMTP PROTOCOL ERROR: {ex.Message}" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"CONTACT FORM ERROR: FAILED TO SEND EMAIL TO {smtpEmail?.ToUpper()} FROM {request.Email?.ToUpper()}", request.Email);
                return Results.BadRequest(new { Message = $"UNEXPECTED ERROR: {ex.Message}" });
            }
        });

        // APPLICATION START
        app.Run();
    }

}

// EMAIL REQUEST CLASS
public class EmailRequest
{
    [Required, EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Username { get; set; }

    [Required]
    public string? Message { get; set; }
}

