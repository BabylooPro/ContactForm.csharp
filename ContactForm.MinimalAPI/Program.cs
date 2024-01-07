using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MailKit.Net.Smtp;
using MimeKit;
using dotenv.net;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

public partial class Program
{
    public static void Main(string[] args)
    {
        // INITIAL CONFIGURATION AND ENVIRONMENT VARIABLE LOADING
        var builder = WebApplication.CreateBuilder(args);

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
                await context.Response.WriteAsJsonAsync(new { Message = "CONTACT FORM ERROR: INTERCEPTED BY MIDDLEWARE" });
                app.Logger.LogError(ex, "CONTACT FORM ERROR: INTERCEPTED BY MIDDLEWARE");
            }
        });

        // APPLY CORS CONFIGURATION & ENVIRONMENT VARIABLE LOADING
        app.UseCors();
        DotEnv.Load();

        var missingVariables = CheckEnvironmentVariables("SMTP_HOST", "SMTP_PORT", "SMTP_EMAIL", "SMTP_PASSWORD", "RECEPTION_EMAIL");
        if (missingVariables.Any())
        {
            var missingVariablesString = string.Join(", ", missingVariables);
            throw new InvalidOperationException($"SMTP ENVIRONMENT VARIABLES ARE NOT PROPERLY SET: {missingVariablesString}");
        }

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
                emailMessage.From.Add(new MailboxAddress("", smtpEmail)); // Sender of email
                emailMessage.To.Add(new MailboxAddress("", receptionEmail)); // Receiver of email
                emailMessage.To.Add(new MailboxAddress("", request.Email)); // A copy of email for user who uses "Contact Form" service
                emailMessage.Subject = "New Message from Contact Form";
                emailMessage.Body = new TextPart("plain")
                {
                    Text = $"Name: {request.Username}\nEmail: {request.Email}\nMessage: {request.Message}"
                };

                // SENDING MESSAGE VIA SMTP
                using (var client = new SmtpClient())
                {
                    // CHECK IF SMTP CLIENT CONFIGURATION IS VALID
                    if (!int.TryParse(smtpPortString, out var smtpPort))
                    {
                        throw new FormatException("'SMTP_PORT' IS NOT A VALID NUMBER");
                    }

                    // CONNECTING TO SMTP SERVER
                    await client.ConnectAsync(smtpHost, smtpPort, true);

                    // VALIDATE SMTP CREDENTIALS BEFORE AUTHENTICATING //TODO: NEED MORE TESTING AND OPTIMIZATION (ONCE ON TEN IT SHOWS !)
                    // EXPLANATION: This section checks if the SMTP_EMAIL and SMTP_PASSWORD environment variables are correctly set. If either is incorrect due to a grammatical mistake, it logs an error and returns a bad request response.
                    // However, the current issue is that if these variables are incorrectly set (e.g., with wrong but non-empty values), this condition won't be triggered, and authentication failure may occur elsewhere in the process.
                    // Thus, if the error message "SMTP_EMAIL OR SMTP_PASSWORD IS NOT CORRECTLY SET" does not consistently appear, it may be due to the variables being considered as a simple "Error: authentication failed: UGFzc3dvcmQ6" rather than being incorrectly set.
                    // Rectifying these additional logs and more detailed error handling might be necessary to capture authentication failures due to incorrect values and to display these logs.
                    if (string.IsNullOrEmpty(smtpEmail) || string.IsNullOrEmpty(smtpPassword))
                    {
                        logger.LogError("CONTACT FORM ERROR: SMTP_EMAIL OR SMTP_PASSWORD IS NOT SET CORRECTLY");
                        return Results.BadRequest(new { Message = "ERROR: SMTP_EMAIL OR SMTP_PASSWORD IS NOT SET CORRECTLY" });
                    }

                    // AUTHENTICATING WITH SMTP SERVER
                    await client.AuthenticateAsync(smtpEmail, smtpPassword);

                    // SENDING EMAIL MESSAGE
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }

                // LOGGING SUCCESS OF SENDING
                logger.LogInformation($"CONTACT FORM SUCCESS: EMAIL SENT TO {smtpEmail?.ToUpper()} & {receptionEmail?.ToUpper()} FROM {request.Email?.ToUpper()}", request.Email);
                return Results.Ok(new { Message = "SUCCESS: EMAIL SENT" });
            }
            catch (SmtpCommandException ex)
            {
                if (ex.Message.Contains("AUTHENTICATION FAILED", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogError(ex, "CONTACT FORM ERROR: AUTHENTICATION FAILED");
                    return Results.BadRequest(new { Message = "ERROR: AUTHENTICATION FAILED" });
                }
                else
                {
                    logger.LogError(ex, "CONTACT FORM ERROR: SMTP COMMAND ERROR");
                    return Results.BadRequest(new { Message = $"SMTP COMMAND ERROR: {ex.Message}" });
                }
            }
            catch (SmtpProtocolException ex)
            {
                logger.LogError(ex, "CONTACT FORM ERROR: SMTP PROTOCOL ERROR");
                return Results.BadRequest(new { Message = $"SMTP PROTOCOL ERROR: {ex.Message}" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CONTACT FORM ERROR: FAILED TO SEND EMAIL TO {SmtpEmail} FROM {RequestEmail}", smtpEmail?.ToUpper(), request.Email?.ToUpper());
                return Results.BadRequest(new { Message = $"ERROR: {ex.Message}" });
            }
        });

        // APPLICATION START
        app.Run();
    }

    // ENVIRONMENT VARIABLE CHECKING | //TODO: NEED MORE TESTING ON DIFFERENT ENVIRONMENT VARIABLES SOURCES (KUBERNETES, OPERATING SYSTEM, ETC.)
    // EXPLANATION: This method checks whether specified environment variables are present and not empty.
    // It operates independently of variable source, whether they come from a .env file, Kubernetes, or the operating system.
    // If any variables are missing or empty, it returns a list of these for appropriate error handling.
    private static List<string> CheckEnvironmentVariables(params string[] variableNames)
    {
        var missingVariables = new List<string>();
        foreach (var name in variableNames)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            {
                missingVariables.Add(name);
            }
        }
        return missingVariables;
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
