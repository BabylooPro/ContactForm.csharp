using ContactForm.MinimalAPI;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace ContactForm.Tests.TestConfiguration
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // ENSURE ENVIRONNEMENT IS SET TO TESTING
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // TEST CONFIG WITH SAMPLE SMTP SETTINGS
                var inMemorySettings = new Dictionary<string, string>
                {
                    // INDEX 1
                    {"SmtpSettings:Configurations:0:Index", "1"},
                    {"SmtpSettings:Configurations:0:Host", "smtp.example.com"},
                    {"SmtpSettings:Configurations:0:Email", "test1@example.com"},
                    {"SmtpSettings:Configurations:0:TestEmail", "test1test@example.com"},
                    {"SmtpSettings:Configurations:0:Port", "465"},
                    {"SmtpSettings:Configurations:0:Description", "test description"},

                    // INDEX 2
                    {"SmtpSettings:Configurations:1:Index", "2"},
                    {"SmtpSettings:Configurations:1:Host", "smtp.example.com"},
                    {"SmtpSettings:Configurations:1:Email", "test2@example.com"},
                    {"SmtpSettings:Configurations:1:TestEmail", "test2test@example.com"},
                    {"SmtpSettings:Configurations:1:Port", "587"},
                    {"SmtpSettings:Configurations:1:Description", "test description"},

                    // OTHER
                    {"SmtpSettings:ReceptionEmail", "reception@example.com"},
                };

                config.AddInMemoryCollection(inMemorySettings!);
            });

            builder.ConfigureServices(services =>
            {
                // REPLACE REAL SERVICE WITH MOCKS

                var mockEmailService = new Mock<IEmailService>();

                mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);
                mockEmailService.Setup(s => s.GetAllSmtpConfigs())
                    .Returns(new List<SmtpConfig>
                    {
                        new SmtpConfig {Index = 1, Email = "test1@example.com"},
                        new SmtpConfig {Index = 2, Email = "test2@example.com"},
                    });

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
