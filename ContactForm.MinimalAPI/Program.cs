using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using dotenv.net;
using ContactForm.MinimalAPI.Services;
using ContactForm.MinimalAPI.Middleware;
using ContactForm.MinimalAPI.Utilities;
using System.Collections.Generic;
using System;

// LOADING ENVIRONMENT VARIABLES
DotEnv.Load();

// CHECKING FOR MISSING ENVIRONMENT VARIABLES
var missingVariables = EnvironmentUtils.CheckMissingEnvironmentVariables("SMTP_HOST", "SMTP_PORT", "SMTP_EMAIL", "SMTP_PASSWORD", "RECEPTION_EMAIL");
if (missingVariables.Count > 0)
{
    throw new InvalidOperationException($"The following environment variables are missing or empty : {string.Join(", ", missingVariables)}");
}

// CREATING WEB APPLICATION
var builder = WebApplication.CreateBuilder();

// ADDING SERVICES WITH CORS POLICY
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://example.com", "https://localhost:7129", "http://localhost:5108")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ADDING JSON OPTIONS
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

// ADDING EMAIL SERVICE
builder.Services.AddSingleton<IEmailService, EmailService>();

// ADDING SMTP CLIENT
builder.Services.AddTransient<MailKit.Net.Smtp.ISmtpClient>(provider =>
{
    return new MailKit.Net.Smtp.SmtpClient();
});

// ADDING CONTROLLER SUPPORT
builder.Services.AddControllers();

// BUILDING WEB APPLICATION
var app = builder.Build();

// CONFIGURING MIDDLEWARE
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();

// ADDING ROUTE FOR CONTROLLERS
app.MapControllers();

// CONFIGURING ENDPOINT
app.MapGet("/test", () => "Test: route is working"); // TESTING ROUTE

// RUNNING WEB APPLICATION
app.Run();
