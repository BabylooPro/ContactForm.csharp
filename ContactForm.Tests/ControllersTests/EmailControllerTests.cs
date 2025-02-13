using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using ContactForm.MinimalAPI.Controllers;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ContactForm.Tests.ControllersTests
{
    // UNIT TESTS FOR EMAIL CONTROLLER
    public class EmailControllerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ILogger<EmailController>> _loggerMock;
        private readonly EmailController _controller;

        public EmailControllerTests()
        {
            _emailServiceMock = new Mock<IEmailService>();
            _loggerMock = new Mock<ILogger<EmailController>>();
            var smtpSettingsMock = new Mock<IOptions<SmtpSettings>>();
            smtpSettingsMock.Setup(x => x.Value).Returns(new SmtpSettings());
            _controller = new EmailController(_emailServiceMock.Object, _loggerMock.Object, smtpSettingsMock.Object);
        }

        [Fact]
        public async Task SendEmail_ValidRequest_ReturnsOkResult()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "Test User",
                Message = "Test message"
            };
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>()))
                            .ReturnsAsync(true);

            // ACT - SEND EMAIL
            var result = await _controller.SendEmail(request, 0);

            // ASSERT - CHECK RESULT
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Email sent successfully using SMTP_0 ( -> )", okResult.Value);
        }

        [Fact]
        public async Task SendEmail_FailedToSend_ReturnsInternalServerError()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "Test User",
                Message = "Test message"
            };
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>()))
                            .ReturnsAsync(false);

            // ACT - SEND EMAIL
            var result = await _controller.SendEmail(request, 0);

            // ASSERT - CHECK RESULT
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Failed to send email after trying all available SMTP configurations", statusCodeResult.Value);
        }

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
                    Index = 0
                }
            };
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs())
                            .Returns(configs);

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
