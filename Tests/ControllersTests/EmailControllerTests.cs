using API.Controllers;
using API.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.ControllersTests
{
    // UNIT TESTS FOR EMAILS CONTROLLER
    public class EmailsControllerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IEmailStore> _emailStoreMock;
        private readonly EmailsController _controller;

        public EmailsControllerTests()
        {
            _emailServiceMock = new Mock<IEmailService>();
            _emailStoreMock = new Mock<IEmailStore>();
            var smtpSettingsMock = new Mock<IOptions<SmtpSettings>>();
            smtpSettingsMock.Setup(x => x.Value).Returns(new SmtpSettings());
            _controller = new EmailsController(
                _emailServiceMock.Object,
                _emailStoreMock.Object,
                smtpSettingsMock.Object
            );
        }

        // TEST FOR SUCCESSFUL EMAIL CREATION (POST /emails)
        [Fact]
        public async Task CreateEmail_ValidRequest_ReturnsCreatedResult()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest { Email = "test@example.com", Username = "Test User", Message = "Test message" };
            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), false))
                .Callback<EmailRequest, int, bool>((req, id, test) => req.EmailId = "TEST1234")
                .ReturnsAsync(true);

            // ACT - SEND EMAIL
            var result = await _controller.CreateEmail(request, 0, false);

            // ASSERT - CHECK RESULT
            var created = Assert.IsType<CreatedResult>(result);
            var response = Assert.IsType<EmailResource>(created.Value);
            Assert.Equal("TEST1234", response.Id);
            Assert.Equal(EmailStatus.Sent, response.Status);
            Assert.Equal(0, response.RequestedSmtpId);
            Assert.False(response.IsTest);
        }

        // TEST FOR FAILED DELIVERY (POST /emails) RETURNS 502 + PROBLEM DETAILS
        [Fact]
        public async Task CreateEmail_FailedToSend_ReturnsBadGateway()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest { Email = "test@example.com", Username = "Test User", Message = "Test message" };
            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), false))
                .ReturnsAsync(false);

            // ACT - SEND EMAIL
            var result = await _controller.CreateEmail(request, 0, false);

            // ASSERT - CHECK RESULT
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(502, statusCodeResult.StatusCode);
            var problem = Assert.IsType<ProblemDetails>(statusCodeResult.Value);
            Assert.Equal("Email delivery failed", problem.Title);
        }

        // TEST FOR TEST MODE (test=true) PASSED TO SERVICE
        [Fact]
        public async Task CreateEmail_TestMode_CallsServiceWithTestFlag()
        {
            // ARRANGE - SETUP
            var request = new EmailRequest { Email = "test@example.com", Username = "Test User", Message = "Test message" };
            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), true))
                .Callback<EmailRequest, int, bool>((req, id, test) => req.EmailId = "TEST5678")
                .ReturnsAsync(true);

            // ACT - SEND EMAIL
            var result = await _controller.CreateEmail(request, 0, true);

            // ASSERT - CHECK RESULT
            var created = Assert.IsType<CreatedResult>(result);
            var response = Assert.IsType<EmailResource>(created.Value);
            Assert.Equal("TEST5678", response.Id);
            Assert.True(response.IsTest);
            _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), 0, true), Times.Once);
        }

        // TEST FOR GET BY ID NOT FOUND
        [Fact]
        public void GetById_NotFound_Returns404()
        {
            // ACT - GET EMAIL BY ID
            var result = _controller.GetById("NOPE");
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // TEST FOR GET BY ID FOUND
        [Fact]
        public void GetById_Found_ReturnsOk()
        {
            // ARRANGE - SETUP STORE HIT
            var resource = new EmailResource { Id = "ABC12345" };
            _emailStoreMock.Setup(x => x.TryGet("ABC12345", out resource)).Returns(true);

            // ACT - GET EMAIL BY ID
            var result = _controller.GetById("ABC12345");

            // ASSERT - CHECK RESULT
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<EmailResource>(ok.Value);
            Assert.Equal("ABC12345", body.Id);
        }

        // TEST FOR DEFAULT SMTP ID SELECTION (FIRST IN LIST)
        [Fact]
        public async Task CreateEmail_WithoutSmtpId_UsesFirstConfiguredIndex()
        {
            // ARRANGE - CONFIGS OUT OF ORDER (FIRST ELEMENT HAS INDEX 2, SECOND HAS INDEX 1)
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs()).Returns([new() { Index = 2 }, new() { Index = 1 }]);
            var request = new EmailRequest { Email = "test@example.com", Message = "Test message" };

            // ACT - SEND EMAIL
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), false))
                .Callback<EmailRequest, int, bool>((req, id, test) => req.EmailId = "FIRST001")
                .ReturnsAsync(true);

            // ACT - CREATE EMAIL
            var result = await _controller.CreateEmail(request);

            // ASSERT - SEND CALLED WITH FIRST ELEMENT INDEX (2) AND CHECK RESULT
            _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), 2, false), Times.Once);
            var created = Assert.IsType<CreatedResult>(result);
            var body = Assert.IsType<EmailResource>(created.Value);
            Assert.Equal(2, body.RequestedSmtpId);
        }

        // TEST FOR DEFAULT SMTP ID WHEN NO CONFIGS ARE AVAILABLE
        [Fact]
        public async Task CreateEmail_WithoutSmtpId_NoConfigs_Throws()
        {
            // ARRANGE - NO CONFIGS
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs()).Returns([]);
            var request = new EmailRequest { Email = "test@example.com", Message = "Test message" };

            // ACT & ASSERT - THROW INVALIDOPERATIONEXCEPTION
            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.CreateEmail(request));
        }

        // TEST FOR LOCATION WHEN USING QUERY/HEADER VERSIONING (UNVERSIONED PATH)
        [Fact]
        public async Task CreateEmail_UnversionedPath_SetsLocationWithQueryVersion()
        {
            // ARRANGE - FORCE PATH TO NOT START WITH /api/v
            var httpContext = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.HttpContext.Request.Path = "/api/emails";
            var request = new EmailRequest { Email = "test@example.com", Message = "Test message" };

            // ACT - SEND EMAIL
            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), false))
                .Callback<EmailRequest, int, bool>((req, id, test) => req.EmailId = "LOC00001")
                .ReturnsAsync(true);

            // ACT - CREATE EMAIL
            var result = await _controller.CreateEmail(request, 1, false);

            // ASSERT - LOCATION USES QUERY STRING VERSIONING SHAPE
            var created = Assert.IsType<CreatedResult>(result);
            Assert.Equal("/api/emails/LOC00001?api-version=1.0", created.Location);
        }
    }
}
