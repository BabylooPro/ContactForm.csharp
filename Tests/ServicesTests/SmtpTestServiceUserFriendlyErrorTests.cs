using System.Net.Sockets;
using System.Reflection;
using API.Models;
using API.Services;
using Moq;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR SMTPTESTSERVICE USER-FRIENDLY ERROR MAPPING
    public class SmtpTestServiceUserFriendlyErrorTests
    {
        // HELPER TO CALL PRIVATE METHOD VIA REFLECTION
        private static string CallGetUserFriendlyErrorMessage(Exception ex, SmtpConfig config, string email)
        {
            var method = typeof(SmtpTestService).GetMethod(
                "GetUserFriendlyErrorMessage",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.NotNull(method);

            return (string)method!.Invoke(null, [ex, config, email])!;
        }

        // TEST FOR CTOR THROWING WHEN LOGGER IS NULL
        [Fact]
        public void Ctor_WhenLoggerNull_ThrowsArgumentNullException()
        {
            // ARRANGE - NULL LOGGER
            // ACT & ASSERT - CTOR THROWS
            Assert.Throws<ArgumentNullException>(() => new SmtpTestService(null!, new Mock<IServiceProvider>().Object));
        }

        // TEST FOR CTOR THROWING WHEN SERVICE PROVIDER IS NULL
        [Fact]
        public void Ctor_WhenServiceProviderNull_ThrowsArgumentNullException()
        {
            // ARRANGE - VALID LOGGER, NULL SERVICE PROVIDER
            var logger = new Mock<ILogger<SmtpTestService>>().Object;

            // ACT & ASSERT - CTOR THROWS
            Assert.Throws<ArgumentNullException>(() => new SmtpTestService(logger, null!));
        }

        // TEST FOR AUTH FAILURE MESSAGE (535 + AUTHENTICATION FAILED)
        [Fact]
        public void GetUserFriendlyErrorMessage_AuthFailed535_ReturnsAuthFailedMessage()
        {
            // ARRANGE - SMTP CONFIG + EXCEPTION
            var config = new SmtpConfig { Host = "h", Port = 1, Index = 0 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("535 authentication failed"),
                config,
                "user@example.com"
            );

            // ASSERT - AUTHENTICATION MESSAGE
            Assert.Contains("Authentication failed", msg);
        }

        // TEST FOR AUTH FAILURE MESSAGE (GENERIC AUTHENTICATION KEYWORD)
        [Fact]
        public void GetUserFriendlyErrorMessage_GeneralAuthFailure_ReturnsAuthFailedMessage()
        {
            // ARRANGE - SMTP CONFIG + EXCEPTION
            var config = new SmtpConfig { Host = "h", Port = 1, Index = 0 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("authentication error"),
                config,
                "user@example.com"
            );

            // ASSERT - AUTHENTICATION MESSAGE
            Assert.Contains("Authentication failed", msg);
        }

        // TEST FOR SOCKETEXCEPTION RETURNING CONNECTION MESSAGE
        [Fact]
        public void GetUserFriendlyErrorMessage_SocketException_ReturnsConnectionMessage()
        {
            // ARRANGE - SMTP CONFIG + SOCKETEXCEPTION
            var config = new SmtpConfig { Host = "smtp.example", Port = 25, Index = 0 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new SocketException(),
                config,
                "user@example.com"
            );

            // ASSERT - CONNECTION MESSAGE
            Assert.Contains("Connection failed", msg);
            Assert.Contains("smtp.example:25", msg);
        }

        // TEST FOR SSL/TLS MESSAGE WHEN EXCEPTION TEXT CONTAINS SSL HANDSHAKE
        [Fact]
        public void GetUserFriendlyErrorMessage_SslOrTls_ReturnsTlsMessage()
        {
            // ARRANGE - SMTP CONFIG + SSL MESSAGE
            var config = new SmtpConfig { Host = "smtp.example", Port = 465, Index = 0 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("SSL handshake error"),
                config,
                "user@example.com"
            );

            // ASSERT - TLS MESSAGE
            Assert.Contains("SSL/TLS", msg);
        }

        // TEST FOR SERVICE NOT AVAILABLE MESSAGE
        [Fact]
        public void GetUserFriendlyErrorMessage_ServiceNotAvailable_ReturnsServiceMessage()
        {
            // ARRANGE - SMTP CONFIG + SERVICE NOT AVAILABLE
            var config = new SmtpConfig { Host = "smtp.example", Port = 25, Index = 0 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("service not available"),
                config,
                "user@example.com"
            );

            // ASSERT - SERVICE MESSAGE
            Assert.Contains("not available", msg);
        }

        // TEST FOR INVALID HOST MESSAGE WHEN EXCEPTION TEXT CONTAINS HOST NOT FOUND
        [Fact]
        public void GetUserFriendlyErrorMessage_InvalidHost_ReturnsInvalidHostMessage()
        {
            // ARRANGE - SMTP CONFIG + HOST NOT FOUND
            var config = new SmtpConfig { Host = "smtp.example", Port = 25, Index = 123 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("host not found"),
                config,
                "user@example.com"
            );

            // ASSERT - INVALID HOST MESSAGE
            Assert.Contains("Invalid host or port", msg);
            Assert.Contains("SMTP_123_HOST", msg);
        }

        // TEST FOR INVALID HOST MESSAGE WHEN EXCEPTION TEXT CONTAINS HOST INVALID
        [Fact]
        public void GetUserFriendlyErrorMessage_InvalidHost_InvalidKeyword_ReturnsInvalidHostMessage()
        {
            // ARRANGE - SMTP CONFIG + HOST INVALID
            var config = new SmtpConfig { Host = "smtp.example", Port = 25, Index = 1 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("host invalid"),
                config,
                "user@example.com"
            );

            // ASSERT - INVALID HOST MESSAGE
            Assert.Contains("Invalid host or port", msg);
        }

        // TEST FOR INVALID HOST MESSAGE WHEN EXCEPTION TEXT CONTAINS HOST UNREACHABLE
        [Fact]
        public void GetUserFriendlyErrorMessage_InvalidHost_UnreachableKeyword_ReturnsInvalidHostMessage()
        {
            // ARRANGE - SMTP CONFIG + HOST UNREACHABLE
            var config = new SmtpConfig { Host = "smtp.example", Port = 25, Index = 1 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("host unreachable"),
                config,
                "user@example.com"
            );

            // ASSERT - INVALID HOST MESSAGE
            Assert.Contains("Invalid host or port", msg);
        }

        // TEST FOR NETWORK ERROR MESSAGE
        [Fact]
        public void GetUserFriendlyErrorMessage_NetworkError_ReturnsNetworkMessage()
        {
            // ARRANGE - SMTP CONFIG + NETWORK ERROR
            var config = new SmtpConfig { Host = "smtp.example", Port = 25, Index = 0 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("network unreachable"),
                config,
                "user@example.com"
            );

            // ASSERT - NETWORK MESSAGE
            Assert.Contains("Network error", msg);
        }

        // TEST FOR MISSING ENVIRONMENT VARIABLE ERROR PASSING THROUGH ORIGINAL MESSAGE
        [Fact]
        public void GetUserFriendlyErrorMessage_MissingEnvVar_ReturnsOriginalMessage()
        {
            // ARRANGE - SMTP CONFIG + MISSING ENV VAR ERROR
            var config = new SmtpConfig { Host = "smtp.example", Port = 25, Index = 0 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new InvalidOperationException("SMTP_0_PASSWORD environment variable is missing"),
                config,
                "user@example.com"
            );

            // ASSERT - ORIGINAL MESSAGE PRESERVED
            Assert.Contains("environment variable", msg);
        }

        // TEST FOR FALLBACK MESSAGE WHEN NO SPECIAL CASE MATCHES
        [Fact]
        public void GetUserFriendlyErrorMessage_Fallback_ReturnsGenericMessage()
        {
            // ARRANGE - SMTP CONFIG + GENERIC ERROR
            var config = new SmtpConfig { Host = "smtp.example", Port = 25, Index = 0 };

            // ACT - GET USER-FRIENDLY MESSAGE
            var msg = CallGetUserFriendlyErrorMessage(
                new Exception("something else"),
                config,
                "user@example.com"
            );

            // ASSERT - GENERIC FALLBACK
            Assert.Contains("should be checked", msg);
        }
    }
}
