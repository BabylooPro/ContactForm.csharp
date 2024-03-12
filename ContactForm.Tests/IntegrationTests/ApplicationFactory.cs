using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MailKit.Net.Smtp;
using ContactForm.MinimalAPI.Services;

namespace ContactForm.Tests.IntegrationTests
{
    // FACTORY FOR CREATING APPLICATION
    public class ApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        // CONFIGURING WEB HOST
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // REMOVING EMAIL SERVICE AND ADDING MOCKED EMAIL SERVICE
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService)); // GETTING EMAIL SERVICE

                // REMOVING EMAIL SERVICE IF IT EXISTS
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // ADDING MOCKED EMAIL SERVICE
                services.AddSingleton<IEmailService>(provider =>
                {
                    // MOCKING LOGGER AND SMTP CLIENT
                    var loggerMock = new Mock<ILogger<EmailService>>();
                    var smtpClientMock = new Mock<ISmtpClient>();

                    return new EmailService(loggerMock.Object, smtpClientMock.Object); // RETURNING MOCKED EMAIL SERVICE
                });
            });
        }
    }
}
