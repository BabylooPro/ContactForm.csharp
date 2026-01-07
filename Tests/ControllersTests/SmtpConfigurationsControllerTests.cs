using API.Controllers;
using API.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Tests.ControllersTests
{
    // UNIT TESTS FOR SMTP CONFIGURATIONS CONTROLLER
    public class SmtpConfigurationsControllerTests
    {
        // TEST FOR GET SMTP CONFIG BY ID
        [Fact]
        public void GetById_ValidId_ReturnsOk()
        {
            // ARRANGE - SETUP SERVICE
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(x => x.GetSmtpConfigById(1)).Returns(new SmtpConfig { Index = 1, Host = "smtp.example.com" });

            var controller = new SmtpConfigurationsController(emailServiceMock.Object);

            // ACT - GET SMTP CONFIG BY ID
            var result = controller.GetById(1);

            // ASSERT - CHECK RESULT
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<SmtpConfig>(ok.Value);
            Assert.Equal(1, body.Index);
            Assert.Equal("smtp.example.com", body.Host);
        }

        // TEST FOR GET SMTP CONFIG BY ID FAILS WHEN CONFIG IS NOT FOUND
        [Fact]
        public void GetById_InvalidId_ReturnsNotFound()
        {
            // ARRANGE - SETUP SERVICE TO THROW
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(x => x.GetSmtpConfigById(999)).Throws(new InvalidOperationException("SMTP_999 configuration not found"));

            var controller = new SmtpConfigurationsController(emailServiceMock.Object);

            // ACT - GET SMTP CONFIG BY ID
            var result = controller.GetById(999);

            // ASSERT - CHECK RESULT
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
