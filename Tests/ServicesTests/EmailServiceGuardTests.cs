using System.Reflection;
using API.Interfaces;
using API.Models;
using API.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR EMAILSERVICE GUARD CLAUSES
    public class EmailServiceGuardTests
    {
        // TEST FOR CTOR THROWING WHEN LOGGER IS NULL
        [Fact]
        public void Ctor_WhenLoggerIsNull_ThrowsArgumentNullException()
        {
            // ARRANGE - CREATE MINIMAL DEPENDENCIES
            var smtpSettings = Options.Create(new SmtpSettings());
            var smtpClient = new Mock<ISmtpClientWrapper>().Object;
            var tracker = new Mock<IEmailTrackingService>().Object;
            var templateService = new Mock<IEmailTemplateService>().Object;

            // ACT & ASSERT - CONSTRUCTOR THROWS
            Assert.Throws<ArgumentNullException>(() =>
                new EmailService(null!, smtpSettings, smtpClient, tracker, templateService)
            );
        }

        // TEST FOR GETSMTPPASSWORD THROWING WHEN ENVIRONMENT VARIABLE IS MISSING
        [Fact]
        public void GetSmtpPassword_WhenMissingEnvironmentVariable_ThrowsInvalidOperationException()
        {
            // ARRANGE - GET PRIVATE METHOD VIA REFLECTION
            var method = typeof(EmailService).GetMethod("GetSmtpPassword", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = new Dictionary<string, string?>
            {
                ["SMTP_0_PASSWORD"] = Environment.GetEnvironmentVariable("SMTP_0_PASSWORD"),
                ["SMTP_0_PASSWORD_TEST"] = Environment.GetEnvironmentVariable("SMTP_0_PASSWORD_TEST"),
            };

            try
            {
                // ARRANGE - CLEAR PASSWORD VARIABLES
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD_TEST", null);

                var config = new SmtpConfig { Index = 0 };

                // ACT - INVOKE METHOD
                var tie = Assert.Throws<TargetInvocationException>(() => method!.Invoke(null, [config, false]));

                // ASSERT - INNER EXCEPTION IS INVALIDOPERATIONEXCEPTION
                Assert.IsType<InvalidOperationException>(tie.InnerException);
                Assert.Contains("SMTP_0_PASSWORD", tie.InnerException!.Message);
            }
            finally
            {
                // CLEANUP - RESTORE ENVIRONMENT
                foreach (var (k, v) in envSnapshot) Environment.SetEnvironmentVariable(k, v);
            }
        }
    }
}
