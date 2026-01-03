using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Services;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;

namespace ContactForm.Tests.ServicesTests
{
    // UNIT TESTS FOR EMAIL SERVICE
    public class EmailServiceTests
    {
        // DEPENDENCY INJECTION
        private readonly Mock<ILogger<EmailService>> _loggerMock;
        private readonly Mock<IOptions<SmtpSettings>> _smtpSettingsMock;
        private readonly Mock<ISmtpClientWrapper> _smtpClientMock;
        private readonly Mock<IEmailTemplateService> _templateServiceMock;
        private readonly EmailService _emailService;

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailServiceTests()
        {
            _loggerMock = new Mock<ILogger<EmailService>>();
            _smtpClientMock = new Mock<ISmtpClientWrapper>();
            _templateServiceMock = new Mock<IEmailTemplateService>();

            var smtpSettings = new SmtpSettings
            {
                Configurations =
                [
                    new()
                    {
                        Host = "smtp.hostinger.com",
                        Port = 465,
                        Email = "test@example.com",
                        TestEmail = "test-email@example.com",
                        Description = "Test SMTP",
                        Index = 0,
                    },
                ],
                ReceptionEmail = "reception@example.com",
                CatchAllEmail = "catch-all-email@example.com",
            };

            _smtpSettingsMock = new Mock<IOptions<SmtpSettings>>();
            _smtpSettingsMock.Setup(x => x.Value).Returns(smtpSettings);

            // SETUP ENVIRONMENT VARIABLES FOR PASSWORDS
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test-password");
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD_TEST", "test-password-test");

            // SETUP SMTP CLIENT MOCK
            _smtpClientMock
                .Setup(x =>
                    x.ConnectWithTokenAsync(
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<SecureSocketOptions>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(Task.CompletedTask);

            _smtpClientMock
                .Setup(x =>
                    x.AuthenticateWithTokenAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(Task.CompletedTask);

            _smtpClientMock
                .Setup(x =>
                    x.SendWithTokenAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(string.Empty);

            _smtpClientMock
                .Setup(x =>
                    x.DisconnectWithTokenAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())
                )
                .Returns(Task.CompletedTask);

            _smtpClientMock.SetupGet(x => x.IsConnected).Returns(false);

            // SETUP EMAIL TRACKER MOCK
            var emailTrackerMock = new Mock<IEmailTrackingService>();
            emailTrackerMock.Setup(x => x.IsEmailUnique(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((true, (TimeSpan?)null, 0));

            // SETUP TEMPLATE SERVICE MOCK
            _templateServiceMock
                .Setup(x => x.GetTemplate(It.IsAny<PredefinedTemplate>()))
                .Returns(
                    new EmailTemplate
                    {
                        Name = "Test Template",
                        Subject = "Test Subject",
                        Body = "Test Body",
                        IsHtml = false,
                    }
                );

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
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message",
                Template = PredefinedTemplate.Modern,
            };

            // ACT - SEND EMAIL
            var result = await _emailService.SendEmailAsync(request, 0);

            // ASSERT - VERIFY RESULT
            Assert.True(result);
            _templateServiceMock.Verify(x => x.GetTemplate(PredefinedTemplate.Modern), Times.Once);
            
            // ASSERT - VERIFY SUBJECT CONTAINS ID
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
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message",
            };

            // ACT - SEND EMAIL
            var result = await _emailService.SendEmailAsync(request, 0);

            // ASSERT - VERIFY SUCCESS
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
                Message = "Test message",
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
            
            // ASSERT - VERIFY EMAIL ID IS ASSIGNED TO REQUEST
            Assert.NotNull(request.EmailId);
            Assert.Equal(emailId, request.EmailId);
        }

        // TEST FOR EMAIL ID IS LOGGED
        [Fact]
        public async Task SendEmailAsync_EmailIdIsLogged()
        {
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message",
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

        // TEST FOR SENDING EMAIL FAILS WHEN SMTP CONFIG IS NOT FOUND
        [Fact]
        public async Task SendEmailAsync_InvalidSmtpId_ThrowsException()
        {
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message",
            };

            // ACT & ASSERT - THROW IF CONFIG MISSING
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _emailService.SendEmailAsync(request, 999));
            Assert.Contains("SMTP_999 configuration not found", exception.Message);
        }

        // TEST FOR SENDING REGULAR EMAIL
        [Fact]
        public async Task SendEmailAsync_WithRegularEmail_ReturnsFalse()
        {
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message",
            };

            // ACT - SEND EMAIL
            var result = await _emailService.SendEmailAsync(request, 0, false);

            // ASSERT - VERIFY AUTH
            Assert.True(result);
            _smtpClientMock.Verify(
                x =>
                    x.AuthenticateWithTokenAsync(
                        "test@example.com",
                        "test-password",
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        // TEST FOR SENDING EMAIL WITH TEST EMAIL
        [Fact]
        public async Task SendEmailAsync_WithTestEmail_ReturnsTrue()
        {
            // ARRANGE - CREATE REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message",
            };

            // ACT - SEND EMAIL
            var result = await _emailService.SendEmailAsync(request, 0, true);

            // ASSERT - VERIFY RESULT
            Assert.True(result);
            _smtpClientMock.Verify(
                x => x.AuthenticateWithTokenAsync(
                    "test-email@example.com",
                    "test-password-test",
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
            
            // ASSERT - VERIFY SUBJECT CONTAINS ID
            _smtpClientMock.Verify(
                x => x.SendWithTokenAsync(
                    It.Is<MimeMessage>(msg => msg.Subject != null && msg.Subject.Contains(" - [") && msg.Subject.EndsWith("]")),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        // TEST FOR GETTING SMTP CONFIG BY ID
        [Fact]
        public void GetSmtpConfigById_ValidId_ReturnsConfig()
        {
            // ACT - GET CONFIG
            var config = _emailService.GetSmtpConfigById(0);

            // ASSERT - CHECK RETURNED CONFIG
            Assert.NotNull(config);
            Assert.Equal("smtp.hostinger.com", config.Host);
        }

        // TEST FOR GETTING SMTP CONFIG BY ID FAILS WHEN CONFIG IS NOT FOUND
        [Fact]
        public void GetSmtpConfigById_InvalidId_ThrowsException()
        {
            // ACT - THROW EXCEPTION
            var exception = Assert.Throws<InvalidOperationException>(() => _emailService.GetSmtpConfigById(999));

            // ASSERT - CHECK MESSAGE
            Assert.Contains("SMTP_999 configuration not found", exception.Message);
        }

        // TEST FOR GETTING ALL SMTP CONFIGS
        [Fact]
        public void GetAllSmtpConfigs_ReturnsAllConfigs()
        {
            // ACT - GET ALL
            var configs = _emailService.GetAllSmtpConfigs();

            // ASSERT - CHECK COUNT
            Assert.Single(configs);

            // ASSERT - CHECK HOST
            Assert.Equal("smtp.hostinger.com", configs[0].Host);
        }
    }
}
