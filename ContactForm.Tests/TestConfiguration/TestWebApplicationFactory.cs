using System.Text.Json;
using ContactForm.MinimalAPI;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace ContactForm.Tests.TestConfiguration
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        static TestWebApplicationFactory()
        {
            // SET SMTP CONFIGURATIONS FROM ENVIRONMENT VARIABLE
            var testConfigurations = new List<SmtpConfig>
            {
                new()
                {
                    Index = 1,
                    Host = "smtp.example.com",
                    Email = "test1@example.com",
                    TestEmail = "test1test@example.com",
                    Port = 465,
                    Description = "test description"
                },
                new()
                {
                    Index = 2,
                    Host = "smtp.example.com",
                    Email = "test2@example.com",
                    TestEmail = "test2test@example.com",
                    Port = 587,
                    Description = "test description"
                }
            };

            // SETTING ENVIRONMENT VARIABLES FOR TESTING
            var smtpConfigurationsJson = JsonSerializer.Serialize(testConfigurations);
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", smtpConfigurationsJson);
            Environment.SetEnvironmentVariable("SMTP_1_PASSWORD", "test-password-1");
            Environment.SetEnvironmentVariable("SMTP_1_PASSWORD_TEST", "test-password-1-test");
            Environment.SetEnvironmentVariable("SMTP_2_PASSWORD", "test-password-2");
            Environment.SetEnvironmentVariable("SMTP_2_PASSWORD_TEST", "test-password-2-test");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // ENSURE ENVIRONNEMENT IS SET TO TESTING
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // REPLACE REAL SERVICE WITH MOCKS

                var mockEmailService = new Mock<IEmailService>();

                mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);
                mockEmailService.Setup(s => s.GetAllSmtpConfigs())
                    .Returns(
                    [
                        new() { Index = 1, Email = "test1@example.com" },
                        new() { Index = 2, Email = "test2@example.com" },
                    ]);

                // MOCK SMTP TEST SERVICE
                var mockSmtpTestService = new Mock<ISmtpTestService>();
                mockSmtpTestService.Setup(s => s.TestSmtpConnections())
                    .Returns(Task.CompletedTask);

                // REGISTER MOCKS
                services.AddScoped<IEmailService>(_ => mockEmailService.Object);
                services.AddScoped<ISmtpTestService>(_ => mockSmtpTestService.Object);

            });
        }
    }
}
