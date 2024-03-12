using Xunit;
using Moq;
using MailKit.Net.Smtp;
using MimeKit;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using MailKit.Security;
using MailKit;

namespace ContactForm.Tests.IntegrationTests
{
    // INTEGRATION TESTS FOR EMAIL SERVICE
    public class EmailServiceIntegrationTests
    {
        // DEPENDENCY INJECTION
        private readonly Mock<ILogger<EmailService>> _loggerMock;
        private readonly Mock<ISmtpClient> _smtpClientMock;
        private readonly EmailService _emailService;

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailServiceIntegrationTests()
        {
            // MOCKING LOGGER AND SMTP CLIENT
            _loggerMock = new Mock<ILogger<EmailService>>();
            _smtpClientMock = new Mock<ISmtpClient>();

            // SETTING ENVIRONMENT VARIABLES
            Environment.SetEnvironmentVariable("SMTP_HOST", "smtp.example.com");
            Environment.SetEnvironmentVariable("SMTP_PORT", "587");
            Environment.SetEnvironmentVariable("SMTP_EMAIL", "your-email@example.com");
            Environment.SetEnvironmentVariable("SMTP_PASSWORD", "your-password");
            Environment.SetEnvironmentVariable("RECEPTION_EMAIL", "reception@example.com");

            _emailService = new EmailService(_loggerMock.Object, _smtpClientMock.Object); // CREATING EMAIL SERVICE
        }

        // TEST FOR SENDING EMAIL SUCCEEDS WHEN SMTP CLIENT OPERATES NORMALLY
        [Fact]
        public async Task SendEmailAsync_Succeeds_WhenSmtpClientOperatesNormally()
        {
            // ARRANGE - CREATING EMAIL REQUEST
            var emailRequest = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Sender Name",
                Message = "Test Message"
            };

            _smtpClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

            _smtpClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

            _smtpClientMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                           .Returns(Task.FromResult(string.Empty));

            _smtpClientMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

            // ACT - SENDING EMAIL
            var (isSuccess, errors) = await _emailService.SendEmailAsync(emailRequest);

            // ASSERT - CHECKING IF EMAIL IS SENT SUCCESSFULLY
            Assert.True(isSuccess);
            Assert.Empty(errors);
            _smtpClientMock.Verify(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        // TEST FOR SENDING EMAIL FAILS WHEN SMTP CLIENT FAILS TO CONNECT
        [Fact]
        public async Task SendEmailAsync_Fails_WhenSmtpClientFailsToConnect()
        {
            // ARRANGE - CREATING EMAIL REQUEST
            var emailRequest = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Sender Name",
                Message = "Test Message"
            };

            _smtpClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new Exception("Failed to connect to SMTP server."));

            // ACT - SENDING EMAIL
            var (isSuccess, errors) = await _emailService.SendEmailAsync(emailRequest);

            // ASSERT - CHECKING IF EMAIL SENDING FAILED
            Assert.False(isSuccess);
            Assert.NotEmpty(errors);
            Assert.Contains("Failed to connect to SMTP server.", errors);

            _smtpClientMock.Verify(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Never);
        }
    }
}
