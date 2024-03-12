using Xunit;
using Moq;
using MailKit.Net.Smtp;
using MimeKit;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using MailKit.Security;

namespace ContactForm.Tests.Services
{
    // UNIT TESTS FOR EMAIL SERVICE
    public class EmailServiceTests
    {
        // DEPENDENCY INJECTION
        private readonly Mock<ILogger<EmailService>> mockLogger = new Mock<ILogger<EmailService>>();
        private readonly Mock<ISmtpClient> mockSmtpClient = new Mock<ISmtpClient>();
        private readonly EmailService emailService;
        private readonly EmailRequest emailRequest = new EmailRequest
        {
            Email = "test@example.com",
            Username = "Test User",
            Message = "Hello, World!"
        };

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailServiceTests()
        {
            Environment.SetEnvironmentVariable("SMTP_HOST", "localhost");
            Environment.SetEnvironmentVariable("SMTP_PORT", "25");
            Environment.SetEnvironmentVariable("SMTP_EMAIL", "test@example.com");
            Environment.SetEnvironmentVariable("SMTP_PASSWORD", "password");
            Environment.SetEnvironmentVariable("RECEPTION_EMAIL", "reception@example.com");

            mockLogger = new Mock<ILogger<EmailService>>();
            mockSmtpClient = new Mock<ISmtpClient>();
            emailService = new EmailService(mockLogger.Object, mockSmtpClient.Object);
        }

        // TEST FOR SENDING EMAIL RETURNS SUCCESS WHEN EMAIL IS SENT
        [Fact]
        public async Task SendEmailAsync_ReturnsSuccess_WhenEmailIsSent()
        {
            // ARRANGE - MOCKING SMTP CLIENT
            mockSmtpClient.Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);
            mockSmtpClient.Setup(client => client.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);
            //TODO: FIX THIS ERROR : Argument 1: cannot convert from 'System.Threading.Tasks.Task' to 'System.Threading.Tasks.Task<string>'CS1503
            // mockSmtpClient.Setup(client => client.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            //               .Returns(Task.CompletedTask);
            mockSmtpClient.Setup(client => client.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

            // ACT - SENDING EMAIL
            var (isSuccess, errors) = await emailService.SendEmailAsync(emailRequest);

            // ASSERT - CHECKING IF EMAIL IS SENT SUCCESSFULLY
            Assert.True(isSuccess);
            Assert.Empty(errors);
        }

        // TEST FOR SENDING EMAIL RETURNS FAILURE WHEN SMTP THROWS EXCEPTION
        [Fact]
        public async Task SendEmailAsync_ReturnsFailure_WhenSmtpThrowsException()
        {
            // Arrange
            mockSmtpClient.Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new Exception("Error sending email"));

            // Act
            var (isSuccess, errors) = await emailService.SendEmailAsync(emailRequest);

            // Assert
            Assert.False(isSuccess, "Expected isSuccess to be false when SMTP throws exception.");
            Assert.Contains(errors, error => error.Contains("Error sending email"));
        }
    }
}
