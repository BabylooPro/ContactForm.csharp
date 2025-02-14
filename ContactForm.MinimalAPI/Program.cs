using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using dotenv.net;
using ContactForm.MinimalAPI.Services;
using ContactForm.MinimalAPI.Middleware;
using ContactForm.MinimalAPI.Utilities;
using System.Collections.Generic;
using System;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

// LOADING ENVIRONMENT VARIABLES
DotEnv.Load();

// CREATING WEB APPLICATION
var builder = WebApplication.CreateBuilder();

// GET SMTP CONFIGURATIONS FROM APPSETTINGS
var config = builder.Configuration.GetSection("SmtpSettings:Configurations").Get<List<SmtpConfig>>();
if (config == null)
{
    throw new InvalidOperationException("No SMTP configurations found in appsettings.json");
}

// DYNAMICALLY CHECK FOR MISSING SMTP PASSWORD VARIABLES
var missingVariables = config
    .Select(smtp => $"SMTP_{smtp.Index}_PASSWORD")
    .Where(envVar => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
    .ToList();

if (missingVariables.Count > 0)
{
    throw new InvalidOperationException($"The following environment variables are missing or empty: {string.Join(", ", missingVariables)}");
}

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

// CONFIGURE SERVICES
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmtpTestService, SmtpTestService>();
builder.Services.AddScoped<ISmtpClientWrapper, SmtpClientWrapper>();
builder.Services.AddScoped<IEmailTrackingService, EmailTrackingService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

// ADDING CONTROLLER SUPPORT
builder.Services.AddControllers();

// BUILDING WEB APPLICATION
var app = builder.Build();

// GET LOGGER
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// TEST SMTP CONNECTIONS
try
{
    using var scope = app.Services.CreateScope();
    var smtpTestService = scope.ServiceProvider.GetRequiredService<ISmtpTestService>();
    await smtpTestService.TestSmtpConnections();
}
catch (Exception)
{
    logger.LogCritical("SMTP TEST FAILED");
    await app.StopAsync();
    Environment.Exit(1);
}

// CONFIGURING MIDDLEWARE
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();

// ADDING ROUTE FOR CONTROLLERS
app.MapControllers();

// CONFIGURING ENDPOINT
app.MapGet("/test", () => "Test: route is working"); // TESTING ROUTE

// RUNNING WEB APPLICATION
app.Run();
