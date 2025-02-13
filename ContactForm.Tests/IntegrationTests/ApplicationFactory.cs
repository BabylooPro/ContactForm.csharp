using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MailKit.Net.Smtp;
using ContactForm.MinimalAPI.Services;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Interfaces;
using Microsoft.Extensions.Options;

namespace ContactForm.Tests.IntegrationTests
{
    // FACTORY FOR CREATING APPLICATION
    internal class ApplicationFactory : WebApplicationFactory<Program>
    {
        // CONFIGURING WEB HOST
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // REMOVING EMAIL SERVICE AND ADDING MOCKED EMAIL SERVICE
            builder.ConfigureServices(services =>
            {
                // GETTING EMAIL SERVICE DESCRIPTOR
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IEmailService));

                // REMOVING EMAIL SERVICE IF EXISTS
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // CONFIGURING TEST SMTP SETTINGS
                var smtpSettings = new SmtpSettings
                {
                    Configurations = new List<SmtpConfig>
                    {
                        new()
                        {
                            Host = "smtp.hostinger.com",
                            Port = 465,
                            Email = "test@example.com",
                            Description = "Test SMTP",
                            Index = 0
                        }
                    },
                    ReceptionEmail = "reception@example.com"
                };

                // CONFIGURING TEST SMTP SETTINGS
                services.Configure<SmtpSettings>(options =>
                {
                    options.Configurations = smtpSettings.Configurations;
                    options.ReceptionEmail = smtpSettings.ReceptionEmail;
                });

                // REGISTER TEST EMAIL SERVICE
                services.AddScoped<IEmailService, EmailService>();
            });
        }
    }
}
