using API.Interfaces;
using API.Models;

namespace API.Services
{
    // BACKGROUND SERVICE FOR SENDING MONTHLY TEST EMAILS IN PRODUCTION
    public class MonthlyTestEmailService : BackgroundService
    {
        private readonly ILogger<MonthlyTestEmailService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _environment;
        private readonly string _lastSentDateFilePath;
        private const string LastSentDateFileName = ".monthly-test-email-last-sent";

        public MonthlyTestEmailService(ILogger<MonthlyTestEmailService> logger, IServiceProvider serviceProvider, IWebHostEnvironment environment) {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _environment = environment;
            
            // STORE LAST SENT DATE IN APP BASE DIRECTORY
            var baseDirectory = AppContext.BaseDirectory;
            _lastSentDateFilePath = Path.Combine(baseDirectory, LastSentDateFileName);
        }

        // METHOD FOR EXECUTING THE BACKGROUND SERVICE
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // ONLY RUN IN PRODUCTION ENVIRONMENT (OR IF FORCED VIA ENV VAR FOR TESTING)
            var forceEnable = Environment.GetEnvironmentVariable("MONTHLY_TEST_EMAIL_FORCE_ENABLE") == "true";
            if (!_environment.IsProduction() && !forceEnable)
            {
                _logger.LogInformation("MonthlyTestEmailService is disabled - not running in Production environment");
                return;
            }

            _logger.LogInformation("MonthlyTestEmailService started - will check monthly for test email");

            // WAIT FOR APPLICATION TO FULLY START BEFORE FIRST CHECK
            await Task.Delay(TimeSpan.FromDays(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (ShouldSendTestEmail())
                    {
                        var success = await SendTestEmailAsync(stoppingToken);
                        if (success) { SaveLastSentDate(); }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in MonthlyTestEmailService");
                }

                // CHECK ONCE PER DAY
                await Task.Delay(TimeSpan.FromDays(30), stoppingToken);
            }
        }

        // METHOD FOR CHECKING IF TEST EMAIL SHOULD BE SENT
        private bool ShouldSendTestEmail()
        {
            try
            {
                if (!File.Exists(_lastSentDateFilePath))
                {
                    _logger.LogInformation("No previous test email date found - will send test email");
                    return true;
                }

                var lastSentDateStr = File.ReadAllText(_lastSentDateFilePath).Trim();
                if (string.IsNullOrWhiteSpace(lastSentDateStr)) return true;

                if (DateTimeOffset.TryParse(lastSentDateStr, out var lastSentDate))
                {
                    var now = DateTimeOffset.UtcNow;
                    var timeSinceLastSent = now - lastSentDate;

                    // SEND IF MORE THAN 30 DAYS HAVE PASSED
                    if (timeSinceLastSent.TotalDays >= 30)
                    {
                        _logger.LogInformation("Last test email was sent {Days} days ago - will send test email", timeSinceLastSent.TotalDays);
                        return true;
                    }

                    _logger.LogDebug("Last test email was sent {Days} days ago - skipping", timeSinceLastSent.TotalDays);
                    return false;
                }

                // IF DATE CAN'T BE PARSED, SEND TEST EMAIL
                _logger.LogWarning("Could not parse last sent date - will send test email");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking last sent date - will send test email");
                return true;
            }
        }

        // METHOD FOR SENDING TEST EMAIL
        private async Task<bool> SendTestEmailAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                // GET FIRST SMTP CONFIGURATION
                var smtpConfigs = emailService.GetAllSmtpConfigs();
                if (smtpConfigs.Count == 0)
                {
                    _logger.LogError("No SMTP configurations available for monthly test email");
                    return false;
                }

                var firstSmtpId = smtpConfigs[0].Index;

                // CREATE TEST EMAIL REQUEST
                var testRequest = new EmailRequest
                {
                    Email = "monthly-test@system.local",
                    Username = "Monthly Test Service",
                    Message = $"This is an automated monthly test email sent on {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC to verify the email API is functioning correctly.",
                    SubjectTemplate = "Monthly API Health Check - Test Email",
                    IsHtml = false,
                    Priority = EmailPriority.Low
                };

                _logger.LogInformation("Sending monthly test email using SMTP_{SmtpId}", firstSmtpId);

                var success = await emailService.SendEmailAsync(testRequest, firstSmtpId, useTestEmail: false);

                if (success)
                {
                    _logger.LogInformation("Monthly test email sent successfully");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to send monthly test email");
                    return false;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already been used to send a message recently"))
            {
                _logger.LogDebug("Monthly test email skipped due to rate limiting: {Message}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending monthly test email");
                return false;
            }
        }

        // METHOD FOR SAVING LAST SENT DATE
        private void SaveLastSentDate()
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                File.WriteAllText(_lastSentDateFilePath, now.ToString("O")); // ISO 8601 FORMAT
                _logger.LogInformation("Saved last test email sent date: {Date}", now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save last sent date");
            }
        }
    }
}
