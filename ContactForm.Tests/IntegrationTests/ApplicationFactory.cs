using System.Text.Json;
using ContactForm.MinimalAPI;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Services;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ContactForm.Tests.IntegrationTests
{
    // FACTORY FOR CREATING APPLICATION
    internal class ApplicationFactory : WebApplicationFactory<Program>
    {
        static ApplicationFactory()
        {
            // SET SMTP CONFIGURATIONS FROM ENVIRONMENT VARIABLE FOR TESTING
            var testConfigurations = new List<SmtpConfig>
            {
                new()
                {
                    Host = "smtp.hostinger.com",
                    Port = 465,
                    Email = "test@example.com",
                    Description = "Test SMTP",
                    Index = 0,
                },
            };

            // SETTING ENVIRONMENT VARIABLES FOR TESTING
            var smtpConfigurationsJson = JsonSerializer.Serialize(testConfigurations);
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", smtpConfigurationsJson);
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test-password");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {

            // REMOVING EMAIL SERVICE AND ADDING MOCKED EMAIL SERVICE
            builder.ConfigureServices(services =>
            {
                // GETTING EMAIL SERVICE DESCRIPTOR
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IEmailService)
                );

                // REMOVING EMAIL SERVICE IF EXISTS
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // CONFIGURING TEST SMTP SETTINGS
                var smtpSettings = new SmtpSettings
                {
                    Configurations =
                    [
                        new()
                        {
                            Host = "smtp.hostinger.com",
                            Port = 465,
                            Email = "test@example.com",
                            Description = "Test SMTP",
                            Index = 0,
                        },
                    ],
                    ReceptionEmail = "reception@example.com",
                    CatchAllEmail = "catchall@example.com",
                };

                // CONFIGURING TEST SMTP SETTINGS
                services.Configure<SmtpSettings>(options =>
                {
                    options.Configurations = smtpSettings.Configurations;
                    options.ReceptionEmail = smtpSettings.ReceptionEmail;
                    options.CatchAllEmail = smtpSettings.CatchAllEmail;
                });

                // REGISTER TEST EMAIL SERVICE
                services.AddScoped<IEmailService, EmailService>();
            });
        }
    }
}
