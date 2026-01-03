using System.Collections.Generic;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Controllers;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContactForm.Tests.ControllersTests
{
    // UNIT TESTS FOR EMAIL CONTROLLER
    public class EmailControllerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly EmailController _controller;

        public EmailControllerTests()
        {
            _emailServiceMock = new Mock<IEmailService>();
            var smtpSettingsMock = new Mock<IOptions<SmtpSettings>>();
            smtpSettingsMock.Setup(x => x.Value).Returns(new SmtpSettings());
            _controller = new EmailController(
                _emailServiceMock.Object,
                smtpSettingsMock.Object
            );
        }

        // TEST FOR VALID SENDING REGULAR EMAIL
        [Fact]
        public async Task SendEmail_ValidRequest_ReturnsOkResult()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "Test User",
                Message = "Test message",
            };
            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), false))
                .ReturnsAsync(true);

            // ACT - SEND EMAIL
            var result = await _controller.SendEmail(request, 0);

            // ASSERT - CHECK RESULT
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Email sent successfully using SMTP_0 ( -> )", okResult.Value);
        }

        // TEST FOR INVALID SENDING REGULAR EMAIL
        [Fact]
        public async Task SendEmail_FailedToSend_ReturnsInternalServerError()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "Test User",
                Message = "Test message",
            };
            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), false))
                .ReturnsAsync(false);

            // ACT - SEND EMAIL
            var result = await _controller.SendEmail(request, 0);

            // ASSERT - CHECK RESULT
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal(
                "Failed to send email after trying all available SMTP configurations",
                statusCodeResult.Value
            );
        }

        // TEST FOR VALID SENDING TEST EMAIL
        [Fact]
        public async Task SendTestEmail_ValidRequest_ReturnsOkResult()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "Test User",
                Message = "Test message",
            };
            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), true))
                .ReturnsAsync(true);

            // ACT - SEND EMAIL
            var result = await _controller.SendTestEmail(request, 0);

            // ASSERT - CHECK RESULT
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Test Email sent successfully using SMTP_0 ( -> )", okResult.Value);
        }

        // TEST FOR INVALID SENDING TEST EMAIL
        [Fact]
        public async Task SendTestEmail_FailedToSend_ReturnsInternalServerError()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "Test User",
                Message = "Test message",
            };
            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), true))
                .ReturnsAsync(false);

            // ACT - SEND EMAIL
            var result = await _controller.SendTestEmail(request, 0);

            // ASSERT - CHECK RESULT
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal(
                "Failed to send test email after trying all available SMTP configurations",
                statusCodeResult.Value
            );
        }

        // TEST FOR GETTING SMTP CONFIGS
        [Fact]
        public void GetSmtpConfigs_ReturnsConfigs()
        {
            // ARRANGE - SETUP
            var configs = new List<SmtpConfig>
            {
                new()
                {
                    Host = "smtp.example.com",
                    Port = 465,
                    Email = "test@example.com",
                    Description = "Test SMTP",
                    Index = 0,
                },
            };
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs()).Returns(configs);

            // ACT - GET SMTP CONFIGS
            var result = _controller.GetSmtpConfigs();

            // ASSERT - CHECK RESULT
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedConfigs = Assert.IsType<List<SmtpConfig>>(okResult.Value);
            Assert.Single(returnedConfigs);
            Assert.Equal("smtp.example.com", returnedConfigs[0].Host);
        }
    }
}
