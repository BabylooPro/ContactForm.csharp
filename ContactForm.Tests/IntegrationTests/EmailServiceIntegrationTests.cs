using Moq;
using ContactForm.MinimalAPI.Services;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Security;
using MimeKit;

namespace ContactForm.Tests.IntegrationTests
{
    // INTEGRATION TESTS FOR EMAIL SERVICE
    public class EmailServiceIntegrationTests
    {
        // DEPENDENCY INJECTION
        private readonly Mock<ILogger<EmailService>> _loggerMock;
        private readonly Mock<IOptions<SmtpSettings>> _smtpSettingsMock;
        private readonly Mock<ISmtpClientWrapper> _smtpClientMock;
        private readonly Mock<IEmailTemplateService> _templateServiceMock;
        private readonly EmailService _emailService;

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailServiceIntegrationTests()
        {
            // MOCKING LOGGER AND SMTP CLIENT
            _loggerMock = new Mock<ILogger<EmailService>>();
            _smtpClientMock = new Mock<ISmtpClientWrapper>();
            _templateServiceMock = new Mock<IEmailTemplateService>();
            
            // SETTING ENVIRONMENT VARIABLES
            var smtpSettings = new SmtpSettings
            {
                Configurations =
                [
                    new()
                    {
                        Host = "smtp.hostinger.com",
                        Port = 465,
                        Email = "test@example.com",
                        Description = "Test SMTP",
                        Index = 0
                    }
                ],
                ReceptionEmail = "reception@example.com"
            };
            
            _smtpSettingsMock = new Mock<IOptions<SmtpSettings>>();
            _smtpSettingsMock.Setup(x => x.Value).Returns(smtpSettings);

            // SETUP ENVIRONMENT VARIABLES FOR PASSWORDS
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test-password");

            // SETUP SMTP CLIENT MOCK
            _smtpClientMock.Setup(x => x.ConnectWithTokenAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.CompletedTask);

            _smtpClientMock.Setup(x => x.AuthenticateWithTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.CompletedTask);

            _smtpClientMock.Setup(x => x.SendWithTokenAsync(
                It.IsAny<MimeMessage>(),
                It.IsAny<CancellationToken>()
            )).ReturnsAsync(string.Empty);

            _smtpClientMock.Setup(x => x.DisconnectWithTokenAsync(
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.CompletedTask);

            _smtpClientMock.SetupGet(x => x.IsConnected).Returns(false);

            // SETUP EMAIL TRACKER MOCK
            var emailTrackerMock = new Mock<IEmailTrackingService>();
            emailTrackerMock.Setup(x => x.IsEmailUnique(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((true, (TimeSpan?)null, 0));

            // SETUP TEMPLATE SERVICE MOCK
            _templateServiceMock.Setup(x => x.GetTemplate(It.IsAny<PredefinedTemplate>()))
                .Returns(new EmailTemplate
                {
                    Name = "Test Template",
                    Subject = "Test Subject",
                    Body = "Test Body",
                    IsHtml = false
                });

            _emailService = new EmailService(
                _loggerMock.Object, 
                _smtpSettingsMock.Object, 
                _smtpClientMock.Object, 
                emailTrackerMock.Object,
                _templateServiceMock.Object
            );
        }

        // TEST FOR SENDING EMAIL WITH TEMPLATE
        [Fact]
        public async Task SendEmailAsync_WithTemplate_ReturnsTrue()
        {
            // ARRANGE - EMAIL REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message",
                Template = PredefinedTemplate.Modern
            };

            // ACT - SEND EMAIL
            var result = await _emailService.SendEmailAsync(request, 0);

            // ASSERT - SUCCESS + TEMPLATE
            Assert.True(result);
            _templateServiceMock.Verify(
                x => x.GetTemplate(PredefinedTemplate.Modern),
                Times.Once
            );
            _smtpClientMock.Verify(
                x => x.SendWithTokenAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
            
            // ASSERT - CHECK SUBJECT CONTAINS ID
            _smtpClientMock.Verify(
                x => x.SendWithTokenAsync(
                    It.Is<MimeMessage>(msg => msg.Subject != null && msg.Subject.Contains(" - [") && msg.Subject.EndsWith("]")),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        // TEST FOR SENDING EMAIL SUCCEEDS WHEN SMTP CLIENT OPERATES NORMALLY
        [Fact]
        public async Task SendEmailAsync_ValidRequest_ReturnsTrue()
        {
            // ARRANGE - EMAIL REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message"
            };

            // ACT - SEND EMAIL
            var result = await _emailService.SendEmailAsync(request, 0);

            // ASSERT - EMAIL SENT
            Assert.True(result);
            _smtpClientMock.Verify(
                x => x.SendWithTokenAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        // TEST FOR EMAIL SUBJECT CONTAINS UNIQUE ID
        [Fact]
        public async Task SendEmailAsync_SubjectContainsUniqueId()
        {
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message"
            };
            MimeMessage? capturedMessage = null;
            _smtpClientMock
                .Setup(x => x.SendWithTokenAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
                .Callback<MimeMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .ReturnsAsync(string.Empty);

            // ACT - SEND EMAIL
            await _emailService.SendEmailAsync(request, 0);

            // ASSERT - VERIFY SUBJECT CONTAINS ID
            Assert.NotNull(capturedMessage);
            Assert.NotNull(capturedMessage.Subject);
            Assert.Contains(" - [", capturedMessage.Subject);
            Assert.EndsWith("]", capturedMessage.Subject);
            
            // ASSERT - EXTRACT ID FROM SUBJECT
            var idStart = capturedMessage.Subject.IndexOf(" - [") + 4;
            var idEnd = capturedMessage.Subject.IndexOf("]", idStart);
            var emailId = capturedMessage.Subject.Substring(idStart, idEnd - idStart);
            
            // ASSERT - VERIFY ID FORMAT
            Assert.Equal(8, emailId.Length);
            Assert.True(emailId.All(c => char.IsLetterOrDigit(c) && (char.IsUpper(c) || char.IsDigit(c))));
            
            // ASSERT - VERIFY EMAILID IS ASSIGNED TO REQUEST
            Assert.NotNull(request.EmailId);
            Assert.Equal(emailId, request.EmailId);
        }

        // TEST FOR EMAIL ID IS LOGGED IN INTEGRATION TEST
        [Fact]
        public async Task SendEmailAsync_EmailIdIsLogged()
        {
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message"
            };

            // ACT - SEND EMAIL
            await _emailService.SendEmailAsync(request, 0);

            // ASSERT - VERIFY LOGS CONTAIN ID
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[ID:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        // TEST FOR SENDING EMAIL FAILS WHEN SMTP CLIENT FAILS TO CONNECT
        [Fact]
        public async Task SendEmailAsync_InvalidSmtpId_ThrowsException()
        {
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message"
            };

            // ACT - THROW EXCEPTION
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _emailService.SendEmailAsync(request, 999)
            );

            // ASSERT - MESSAGE CONTAINS
            Assert.Contains("SMTP_999 configuration not found", exception.Message);
        }
        
        // TEST FOR GETTING SMTP CONFIG BY ID
        [Fact]
        public void GetSmtpConfigById_ValidId_ReturnsConfig()
        {
            // ACT - GET SMTP CONFIG
            var config = _emailService.GetSmtpConfigById(0);

            // ASSERT - CHECK RESULT
            Assert.NotNull(config);
            Assert.Equal("smtp.hostinger.com", config.Host);
        }

        // TEST FOR GETTING SMTP CONFIG BY ID FAILS WHEN CONFIG IS NOT FOUND
        [Fact]
        public void GetSmtpConfigById_InvalidId_ThrowsException()
        {
            // ACT - GET SMTP CONFIG
            var exception = Assert.Throws<InvalidOperationException>(
                () => _emailService.GetSmtpConfigById(999)
            );

            // ASSERT - CHECK RESULT
            Assert.Contains("SMTP_999 configuration not found", exception.Message);
        }

        // TEST FOR GETTING ALL SMTP CONFIGS
        [Fact]
        public void GetAllSmtpConfigs_ReturnsAllConfigs()
        {
            // ACT - GET SMTP CONFIGS
            var configs = _emailService.GetAllSmtpConfigs();

            // ASSERT - CHECK RESULT
            Assert.Single(configs);
            Assert.Equal("smtp.hostinger.com", configs[0].Host);
        }
    }
}
