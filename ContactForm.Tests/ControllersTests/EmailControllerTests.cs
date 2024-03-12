using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ContactForm.MinimalAPI.Controllers;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ContactForm.Tests.Controllers
{
    // UNIT TESTS FOR EMAIL CONTROLLER
    public class EmailControllerTests
    {
        // TEST FOR SENDING EMAIL RETURNS OK RESULT WHEN EMAIL IS SENT SUCCESSFULLY
        [Fact]
        public async Task SendEmail_ReturnsOkResult_WhenEmailIsSentSuccessfully()
        {
            // ARRANGE - MOCKING EMAIL SERVICE
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService.Setup(service => service.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync((true, new List<string>()));

            var mockLogger = new Mock<ILogger<EmailController>>();
            var emailController = new EmailController(mockEmailService.Object, mockLogger.Object);

            var emailRequest = new EmailRequest
            {
                Email = "test@example.com",
                Username = "testuser",
                Message = "Test message"
            };

            // ACT - CALLING SEND EMAIL METHOD
            var result = await emailController.SendEmail(emailRequest);

            // ASSERT - CHECKING IF RESULT IS OK
            Assert.IsType<OkObjectResult>(result);
        }

        // TEST FOR SENDING EMAIL RETURNS BAD REQUEST WHEN MODEL STATE IS INVALID
        [Fact]
        public async Task SendEmail_ReturnsBadRequest_WhenEmailServiceReportsFailure()
        {
            // ARRANGE - MOCKING EMAIL SERVICE
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService.Setup(service => service.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync((false, new List<string> { "Error sending email" }));

            var mockLogger = new Mock<ILogger<EmailController>>();
            var emailController = new EmailController(mockEmailService.Object, mockLogger.Object);

            var emailRequest = new EmailRequest
            {
                Email = "test@example.com",
                Username = "testuser",
                Message = "Test message"
            };

            // ACT - CALLING SEND EMAIL METHOD
            var result = await emailController.SendEmail(emailRequest);

            // ASSERT - CHECKING IF RESULT IS BAD REQUEST
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorMessages = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
            Assert.Contains("Error sending email", errorMessages);

        }
    }
}
