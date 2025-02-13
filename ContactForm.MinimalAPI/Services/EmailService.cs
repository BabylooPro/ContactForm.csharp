using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;

namespace ContactForm.MinimalAPI.Services
{
    // SERVICE FOR SENDING EMAILS
    public class EmailService : IEmailService
    {
        // DEPENDENCY INJECTION
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpSettings _smtpSettings;
        private readonly ISmtpClientWrapper _smtpClient;
        private readonly IEmailTrackingService _emailTracker;

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailService(
            ILogger<EmailService> logger, 
            IOptions<SmtpSettings> smtpSettings,
            ISmtpClientWrapper smtpClient,
            IEmailTrackingService emailTracker)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _smtpSettings = smtpSettings.Value;
            _smtpClient = smtpClient;
            _emailTracker = emailTracker;
        }

        // METHOD FOR GETTING SMTP PASSWORD
        private string GetSmtpPassword(SmtpConfig config)
        {
            var envVar = $"SMTP_{config.Index}_PASSWORD";
            var password = Environment.GetEnvironmentVariable(envVar);
            
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException($"Environment variable {envVar} is not set");
            }
            
            return password;
        }

        // METHOD FOR GETTING SMTP CONFIG BY ID
        public SmtpConfig GetSmtpConfigById(int id)
        {
            var config = _smtpSettings.Configurations.FirstOrDefault(x => x.Index == id);
            if (config == null)
            {
                _logger.LogError("");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR: SMTP_{id} configuration not found. Available SMTP indexes: {string.Join(", ", _smtpSettings.Configurations.Select(x => x.Index))}\n");
                Console.ResetColor();
                throw new InvalidOperationException($"SMTP_{id} configuration not found");
            }
            return config;
        }

        // METHOD FOR GETTING ALL SMTP CONFIGS
        public List<SmtpConfig> GetAllSmtpConfigs()
        {
            return _smtpSettings.Configurations;
        }

        // METHOD FOR SENDING EMAIL
        public async Task<bool> SendEmailAsync(EmailRequest request, int smtpId)
        {
            try
            {
                // CHECK IF EMAIL IS NULL
                if (string.IsNullOrEmpty(request.Email))
                {
                    throw new ArgumentNullException(nameof(request.Email), "Email cannot be null or empty");
                }

                // CHECK IF EMAIL IS UNIQUE
                if (!await _emailTracker.IsEmailUnique(request.Email))
                {
                    _logger.LogWarning("");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Duplicate email detected from: {request.Email}");
                    Console.ResetColor();
                    throw new InvalidOperationException("This email has already been used to send a message");
                }

                // VALIDATE SMTP CONFIG FIRST
                var config = GetSmtpConfigById(smtpId);
                if (config == null)
                {
                    throw new InvalidOperationException($"SMTP configuration with ID {smtpId} not found");
                }

                // CREATE EMAIL MESSAGE
                var email = new MimeMessage();
                
                // SET EMAIL FROM AND TO
                email.From.Add(new MailboxAddress(config.Email, config.Email));
                email.To.Add(new MailboxAddress(_smtpSettings.ReceptionEmail, _smtpSettings.ReceptionEmail));
                email.Subject = $"Message from {request.Username}";

                // SET EMAIL BODY
                var builder = new BodyBuilder();
                builder.TextBody = $"""
                New contact form submission:
                From: {request.Email}
                Name: {request.Username}
                Message: {request.Message}
                
                """;

                email.Body = builder.ToMessageBody(); 

                // LOG EMAIL DETAILS
                _logger.LogInformation("Email Details:\nFrom: {From}\nTo: {To}\nSubject: {Subject}\nBody: {Body}", 
                    email.From.ToString(), 
                    email.To.ToString(), 
                    email.Subject,
                    builder.TextBody);

                // CONNECT TO SMTP SERVER
                var cancellationToken = CancellationToken.None;
                await _smtpClient.ConnectWithTokenAsync(config.Host, config.Port, SecureSocketOptions.SslOnConnect, cancellationToken);

                // AUTHENTICATE WITH SMTP SERVER
                await _smtpClient.AuthenticateWithTokenAsync(config.Email, GetSmtpPassword(config), cancellationToken);

                // SEND EMAIL
                await _smtpClient.SendWithTokenAsync(email, cancellationToken);

                // DISCONNECT FROM SMTP SERVER
                await _smtpClient.DisconnectWithTokenAsync(true, cancellationToken);

                // LOG SUCCESS
                _logger.LogInformation("");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Email sent successfully using SMTP_{config.Index} ({config.Email} -> {_smtpSettings.ReceptionEmail})");
                Console.ResetColor();

                // IF EMAIL SENT SUCCESSFULLY, TRACK IT
                await _emailTracker.TrackEmail(request.Email);
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // LOG ERROR
                _logger.LogError("");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to send email using SMTP_{smtpId}: {ex.Message}");
                Console.ResetColor();
                
                // TRY NEXT AVAILABLE SMTP IN SEQUENCE
                var nextConfig = _smtpSettings.Configurations
                    .Where(x => x.Index > smtpId)
                    .OrderBy(x => x.Index)
                    .FirstOrDefault();

                // TRY NEXT SMTP CONFIG
                if (nextConfig != null)
                {
                    _logger.LogInformation("");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Attempting to use next SMTP configuration (SMTP_{nextConfig.Index})");
                    Console.ResetColor();
                    return await SendEmailAsync(request, nextConfig.Index);
                }
                
                return false; // RETURN FALSE IF NO SMTP CONFIG IS AVAILABLE
            }
            finally
            {
                if (_smtpClient.IsConnected)
                {
                    await _smtpClient.DisconnectWithTokenAsync(true, CancellationToken.None);
                }
            }
        }
    }
}
