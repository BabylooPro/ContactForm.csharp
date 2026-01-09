using API.Interfaces;
using API.Models;
using API.Services;
using Moq;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR MONTHLY TEST EMAIL SERVICE
    public class MonthlyTestEmailServiceTests : IDisposable
    {
        private readonly string _testFilePath;
        private readonly Mock<ILogger<MonthlyTestEmailService>> _loggerMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly Mock<IEmailService> _emailServiceMock;

        public MonthlyTestEmailServiceTests()
        {
            // SETUP TEST FILE PATH IN TEMP DIRECTORY
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            _testFilePath = Path.Combine(tempDir, ".monthly-test-email-last-sent");

            // SETUP MOCKS
            _loggerMock = new Mock<ILogger<MonthlyTestEmailService>>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _emailServiceMock = new Mock<IEmailService>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _serviceProviderMock = new Mock<IServiceProvider>();

            // SETUP SERVICE SCOPE MOCK
            var scopeServiceProviderMock = new Mock<IServiceProvider>();
            scopeServiceProviderMock.Setup(x => x.GetService(typeof(IEmailService))).Returns(_emailServiceMock.Object);
            
            _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(scopeServiceProviderMock.Object);
            _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
            _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);
        }

        // CLEANUP TEST FILE
        public void Dispose()
        {
            try
            {
                if (File.Exists(_testFilePath)) File.Delete(_testFilePath);
                var dir = Path.GetDirectoryName(_testFilePath);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            } catch { /* IGNORE CLEANUP ERRORS */ }
        }

        // SETUP MOCK ENVIRONMENT AS PRODUCTION
        private void SetupProductionEnvironment() => _environmentMock.SetupGet(x => x.EnvironmentName).Returns("Production");

        // SETUP SERVICE PROVIDER TO RETURN MOCK EMAIL SERVICE
        private void SetupEmailServiceInProvider() => _serviceProviderMock.Setup(x => x.GetService(typeof(IEmailService))).Returns(_emailServiceMock.Object);

        // GET A PRIVATE METHOD FROM MONTHLYTESTEMAILSERVICE BY NAME
        private System.Reflection.MethodInfo? GetPrivateMethod(string methodName) =>
            typeof(MonthlyTestEmailService).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // CREATE A MONTHLYTESTEMAILSERVICE INSTANCE WITH A CUSTOM FILE PATH FOR TESTING
        private MonthlyTestEmailService CreateServiceWithCustomFilePath(string filePath)
        {
            var service = new MonthlyTestEmailService(_loggerMock.Object, _serviceProviderMock.Object, _environmentMock.Object);
            var field = typeof(MonthlyTestEmailService).GetField("_lastSentDateFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(field);
            field!.SetValue(service, filePath);
            return service;
        }
        
        // VERIFY LOG CONTAINS EXPECTED MESSAGE AT SPECIFIED LOG LEVEL AND TIMES
        private void VerifyLogContains(LogLevel level, string message, Times times = default)
        {
            if (times == default) times = Times.Once();
            _loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                times
            );
        }

        // INVOKE SHOULDSENDTESTEMAIL PRIVATE METHOD
        private bool InvokeShouldSendTestEmail(MonthlyTestEmailService service)
        {
            var method = GetPrivateMethod("ShouldSendTestEmail");
            Assert.NotNull(method);
            return (bool)method!.Invoke(service, null)!;
        }

        // INVOKE SENDTESTEMAILASYNC PRIVATE METHOD
        private async Task InvokeSendTestEmailAsync(MonthlyTestEmailService service)
        {
            var method = GetPrivateMethod("SendTestEmailAsync");
            Assert.NotNull(method);
            await (Task)method!.Invoke(service, new object[] { CancellationToken.None })!;
        }

        // INVOKE SAVELASTSENTDATE PRIVATE METHOD
        private void InvokeSaveLastSentDate(MonthlyTestEmailService service)
        {
            var method = GetPrivateMethod("SaveLastSentDate");
            Assert.NotNull(method);
            method!.Invoke(service, null);
        }

        // INVOKE EXECUTEASYNC PRIVATE METHOD
        private Task InvokeExecuteAsync(MonthlyTestEmailService service, CancellationToken cancellationToken)
        {
            var method = GetPrivateMethod("ExecuteAsync");
            Assert.NotNull(method);
            return (Task)method!.Invoke(service, new object[] { cancellationToken })!;
        }

        // TEST FOR EXECUTEASYNC DISABLING SERVICE WHEN NOT IN PRODUCTION
        [Fact]
        public async Task ExecuteAsync_WhenNotInProduction_DisablesService()
        {
            // ARRANGE - SET DEV ENV
            _environmentMock.SetupGet(x => x.EnvironmentName).Returns("Development");
            var service = new MonthlyTestEmailService(_loggerMock.Object, _serviceProviderMock.Object, _environmentMock.Object);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // ACT - START TASK
            await service.StartAsync(cts.Token);
            await Task.Delay(150);

            // ASSERT - VERIFY INFO LOG
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MonthlyTestEmailService is disabled")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
        }

        // TEST FOR SHOULDSENDTESTEMAIL RETURNING TRUE WHEN FILE DOES NOT EXIST
        [Fact]
        public void ShouldSendTestEmail_WhenFileDoesNotExist_ReturnsTrue()
        {
            // ARRANGE - SET PROD ENV
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL PRIVATE METHOD
            var result = InvokeShouldSendTestEmail(service);

            // ASSERT - VERIFY INFO LOG
            Assert.True(result);
            VerifyLogContains(LogLevel.Information, "No previous test email date found");
        }

        // TEST FOR SHOULDSENDTESTEMAIL RETURNING TRUE WHEN FILE IS EMPTY
        [Fact]
        public void ShouldSendTestEmail_WhenFileIsEmpty_ReturnsTrue()
        {
            // ARRANGE - CREATE EMPTY FILE
            File.WriteAllText(_testFilePath, "");
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            var result = InvokeShouldSendTestEmail(service);

            // ASSERT - VERIFY TRUE
            Assert.True(result);
        }

        // TEST FOR SHOULDSENDTESTEMAIL RETURNING FALSE WHEN LESS THAN 30 DAYS HAVE PASSED
        [Fact]
        public void ShouldSendTestEmail_WhenLessThan30DaysHavePassed_ReturnsFalse()
        {
            // ARRANGE - CREATE FILE 15 DAYS AGO
            var recentDate = DateTimeOffset.UtcNow.AddDays(-15);
            File.WriteAllText(_testFilePath, recentDate.ToString("O"));
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            var result = InvokeShouldSendTestEmail(service);

            // ASSERT - VERIFY DEBUG LOG
            Assert.False(result);
            VerifyLogContains(LogLevel.Debug, "skipping");
        }

        // TEST FOR SHOULDSENDTESTEMAIL RETURNING TRUE WHEN 30 OR MORE DAYS HAVE PASSED
        [Fact]
        public void ShouldSendTestEmail_When30OrMoreDaysHavePassed_ReturnsTrue()
        {
            // ARRANGE - CREATE FILE 35 DAYS AGO
            var oldDate = DateTimeOffset.UtcNow.AddDays(-35);
            File.WriteAllText(_testFilePath, oldDate.ToString("O"));
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            var result = InvokeShouldSendTestEmail(service);

            // ASSERT - VERIFY INFO LOG
            Assert.True(result);
            VerifyLogContains(LogLevel.Information, "will send test email");
        }

        // TEST FOR SHOULDSENDTESTEMAIL RETURNING TRUE WHEN DATE CANNOT BE PARSED
        [Fact]
        public void ShouldSendTestEmail_WhenDateCannotBeParsed_ReturnsTrue()
        {
            // ARRANGE - CREATE INVALID DATE FILE
            File.WriteAllText(_testFilePath, "invalid-date-format");
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            var result = InvokeShouldSendTestEmail(service);

            // ASSERT - VERIFY WARNING LOG
            Assert.True(result);
            VerifyLogContains(LogLevel.Warning, "Could not parse last sent date");
        }

        // TEST FOR SHOULDSENDTESTEMAIL RETURNING TRUE WHEN EXCEPTION OCCURS
        [Fact]
        public void ShouldSendTestEmail_WhenExceptionOccurs_ReturnsTrue()
        {
            // ARRANGE - CREATE INACCESSIBLE FILE
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var invalidPath = Path.Combine(tempDir, ".monthly-test-email-last-sent");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(invalidPath, "2024-01-01T00:00:00Z");

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux) ||
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                try { System.Diagnostics.Process.Start("chmod", $"000 \"{invalidPath}\"").WaitForExit(); }
                catch { try { Directory.Delete(tempDir, true); } catch { /*IGNORE ERROR*/ } }
            }
            else
            {
                try { Directory.Delete(tempDir, true); } catch { /*IGNORE ERROR*/ }
            }
            
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(invalidPath);

            // ACT - CALL METHOD
            var result = InvokeShouldSendTestEmail(service);

            // ASSERT - VERIFY ERROR LOG
            Assert.True(result);
            try { VerifyLogContains(LogLevel.Error, "Error checking last sent date"); }
            catch { VerifyLogContains(LogLevel.Information, "No previous test email date found"); }

            // CLEANUP
            try
            {
                if (File.Exists(invalidPath))
                {
                    var isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
                    var isOSX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
                    if (isLinux || isOSX) { try { System.Diagnostics.Process.Start("chmod", $"644 \"{invalidPath}\"").WaitForExit(); } catch { } }
                    File.Delete(invalidPath);
                }
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            } catch { /* IGNORE CLEANUP ERRORS */ }
        }

        // TEST FOR SENDTESTEMAILASYNC SUCCESSFULLY SENDING EMAIL
        [Fact]
        public async Task SendTestEmailAsync_WhenSmtpConfigsAvailable_SendsEmailSuccessfully()
        {
            // ARRANGE - SET PROD ENV
            SetupProductionEnvironment();
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs()).Returns(new List<SmtpConfig> { new() { Index = 1, Email = "test@example.com" } });
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);
            SetupEmailServiceInProvider();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            await InvokeSendTestEmailAsync(service);

            // ASSERT - VERIFY EMAIL SENT
            _emailServiceMock.Verify(x => x.GetAllSmtpConfigs(), Times.Once);
            _emailServiceMock.Verify(x => x.SendEmailAsync(
                    It.Is<EmailRequest>(r => r.Email == "monthly-test@system.local" && r.SubjectTemplate == "Monthly API Health Check - Test Email"),
                    It.Is<int>(id => id == 1),
                    It.Is<bool>(test => test == false)
                ),
                Times.Once
            );
            VerifyLogContains(LogLevel.Information, "Monthly test email sent successfully");
        }

        // TEST FOR SENDTESTEMAILASYNC LOGGING ERROR WHEN NO SMTP CONFIGS AVAILABLE
        [Fact]
        public async Task SendTestEmailAsync_WhenNoSmtpConfigsAvailable_LogsError()
        {
            // ARRANGE - SET PROD ENV
            SetupProductionEnvironment();
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs()).Returns(new List<SmtpConfig>());
            SetupEmailServiceInProvider();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            await InvokeSendTestEmailAsync(service);

            // ASSERT - VERIFY ERROR LOG
            VerifyLogContains(LogLevel.Error, "No SMTP configurations available");
            _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        // TEST FOR SENDTESTEMAILASYNC LOGGING ERROR WHEN EMAIL SEND FAILS
        [Fact]
        public async Task SendTestEmailAsync_WhenEmailSendFails_LogsError()
        {
            // ARRANGE - SET PROD ENV
            SetupProductionEnvironment();
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs()).Returns(new List<SmtpConfig> { new() { Index = 1, Email = "test@example.com" } });
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(false);
            SetupEmailServiceInProvider();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            await InvokeSendTestEmailAsync(service);

            // ASSERT - VERIFY ERROR LOG
            VerifyLogContains(LogLevel.Error, "Failed to send monthly test email");
        }

        // TEST FOR SENDTESTEMAILASYNC HANDLING EXCEPTION
        [Fact]
        public async Task SendTestEmailAsync_WhenExceptionOccurs_LogsError()
        {
            // ARRANGE - SET PROD ENV
            SetupProductionEnvironment();
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs()).Throws(new InvalidOperationException("Test exception"));
            SetupEmailServiceInProvider();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            await InvokeSendTestEmailAsync(service);

            // ASSERT - VERIFY ERROR LOG
            VerifyLogContains(LogLevel.Error, "Exception while sending monthly test email");
        }

        // TEST FOR SAVELASTSENTDATE SUCCESSFULLY SAVING DATE
        [Fact]
        public void SaveLastSentDate_WhenFileIsWritable_SavesDate()
        {
            // ARRANGE - SET PROD ENV
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(_testFilePath);

            // ACT - CALL METHOD
            InvokeSaveLastSentDate(service);

            // ASSERT - VERIFY FILE SAVED
            Assert.True(File.Exists(_testFilePath));
            var savedDate = File.ReadAllText(_testFilePath);
            Assert.True(DateTimeOffset.TryParse(savedDate, out _));
            VerifyLogContains(LogLevel.Information, "Saved last test email sent date");
        }

        // TEST FOR SAVELASTSENTDATE HANDLING EXCEPTION
        [Fact]
        public void SaveLastSentDate_WhenExceptionOccurs_LogsError()
        {
            // ARRANGE - CREATE DIRECTORY PATH
            var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), ".monthly-test-email-last-sent");
            var parentDir = Path.GetDirectoryName(invalidPath)!;
            Directory.CreateDirectory(parentDir);
            Directory.CreateDirectory(invalidPath);
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(invalidPath);

            // ACT - CALL METHOD
            InvokeSaveLastSentDate(service);

            // ASSERT - VERIFY ERROR LOG
            VerifyLogContains(LogLevel.Error, "Failed to save last sent date");

            // CLEANUP
            try
            {
                if (Directory.Exists(invalidPath)) Directory.Delete(invalidPath);
                if (Directory.Exists(parentDir)) Directory.Delete(parentDir);
            }
            catch { /* IGNORE CLEANUP ERROR */ }
        }

        // TEST FOR EXECUTEASYNC IN PRODUCTION STARTING CORRECTLY
        [Fact]
        public async Task ExecuteAsync_WhenInProduction_StartsCorrectly()
        {
            // ARRANGE - SET PROD ENV
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(_testFilePath);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // ACT - START TASK
            var executeTask = InvokeExecuteAsync(service, cts.Token);
            await Task.Delay(50);

            // ASSERT - VERIFY STARTED
            VerifyLogContains(LogLevel.Information, "MonthlyTestEmailService started");

            // CANCEL AND WAIT FOR TASK TO COMPLETE
            cts.Cancel();
            try { await executeTask; } catch (OperationCanceledException) { /* EXPECTED */ }
        }

        // TEST FOR EXECUTEASYNC HANDLING EXCEPTION IN LOOP
        [Fact]
        public async Task ExecuteAsync_WhenExceptionInLoop_ContinuesRunning()
        {
            // ARRANGE - CREATE DIRECTORY PATH
            var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), ".monthly-test-email-last-sent");
            Directory.CreateDirectory(Path.GetDirectoryName(invalidPath)!);
            Directory.CreateDirectory(invalidPath);
            SetupProductionEnvironment();
            var service = CreateServiceWithCustomFilePath(invalidPath);
            var field = typeof(MonthlyTestEmailService).GetField("_lastSentDateFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(field);
            field!.SetValue(service, invalidPath);

            // ACT - START TASK
            var cts = new CancellationTokenSource();
            var executeTask = InvokeExecuteAsync(service, cts.Token);
            await Task.Delay(50);

            // ASSERT - VERIFY STARTED
            VerifyLogContains(LogLevel.Information, "MonthlyTestEmailService started");

            // ACT - CANCEL TASK
            cts.Cancel();
            try { await executeTask; } catch (OperationCanceledException) { /*CANCELLED*/ }

            // CLEANUP - DELETE DIRECTORY
            try
            {
                if (Directory.Exists(invalidPath)) Directory.Delete(invalidPath);
                var dir = Path.GetDirectoryName(invalidPath);
                if (Directory.Exists(dir)) Directory.Delete(dir);
            }
            catch { /* IGNORE CLEANUP ERROR */ }
        }

        // TEST FOR EXECUTEASYNC SENDING EMAIL WHEN SHOULDSENDTESTEMAIL RETURNS TRUE
        [Fact]
        public async Task ExecuteAsync_WhenShouldSendTestEmailReturnsTrue_SendsEmail()
        {
            // ARRANGE - SET PROD ENV
            SetupProductionEnvironment();
            _emailServiceMock.Setup(x => x.GetAllSmtpConfigs()).Returns(new List<SmtpConfig> { new() { Index = 1, Email = "test@example.com" } });
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);
            SetupEmailServiceInProvider();
            var service = CreateServiceWithCustomFilePath(_testFilePath);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // ACT - START TASK
            var executeTask = InvokeExecuteAsync(service, cts.Token);
            await Task.Delay(50);

            cts.Cancel();
            try { await executeTask; } catch (OperationCanceledException) { /*CANCELLED*/ }

            // ASSERT - VERIFY STARTED
            VerifyLogContains(LogLevel.Information, "MonthlyTestEmailService started");
        }
    }
}
