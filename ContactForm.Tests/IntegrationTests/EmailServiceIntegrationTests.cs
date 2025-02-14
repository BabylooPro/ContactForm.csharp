using Xunit;
using Moq;
using ContactForm.MinimalAPI.Services;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using System.Threading;

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
                Configurations = new List<SmtpConfig>
                {
                    new()
                    {
                        Host = "smtp.hostinger.com",
                        Port = 465,
                        Email = "test@example.com",
                        Description = "Test SMTP",
                        Index = 0
                    }
                },
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
            emailTrackerMock.Setup(x => x.IsEmailUnique(It.IsAny<string>())).ReturnsAsync(true);

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
            // ARRANGE - CREATING EMAIL REQUEST WITH TEMPLATE
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message",
                Template = PredefinedTemplate.Modern
            };

            // ACT - SENDING EMAIL
            var result = await _emailService.SendEmailAsync(request, 0);

            // ASSERT - CHECKING IF EMAIL IS SENT SUCCESSFULLY AND TEMPLATE WAS USED
            Assert.True(result);
            _templateServiceMock.Verify(
                x => x.GetTemplate(PredefinedTemplate.Modern),
                Times.Once
            );
            _smtpClientMock.Verify(
                x => x.SendWithTokenAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        // TEST FOR SENDING EMAIL SUCCEEDS WHEN SMTP CLIENT OPERATES NORMALLY
        [Fact]
        public async Task SendEmailAsync_ValidRequest_ReturnsTrue()
        {
            // ARRANGE - CREATING EMAIL REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message"
            };

            // ACT - SENDING EMAIL
            var result = await _emailService.SendEmailAsync(request, 0);

            // ASSERT - CHECKING IF EMAIL IS SENT SUCCESSFULLY
            Assert.True(result);
            _smtpClientMock.Verify(
                x => x.SendWithTokenAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        // TEST FOR SENDING EMAIL FAILS WHEN SMTP CLIENT FAILS TO CONNECT
        [Fact]
        public async Task SendEmailAsync_InvalidSmtpId_ThrowsException()
        {
            // ARRANGE - CREATING EMAIL REQUEST
            var request = new EmailRequest
            {
                Email = "sender@example.com",
                Username = "Test User",
                Message = "Test message"
            };

            // ACT - SEND EMAIL
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _emailService.SendEmailAsync(request, 999)
            );

            // ASSERT - CHECK RESULT
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
