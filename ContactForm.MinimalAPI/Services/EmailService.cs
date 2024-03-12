using ContactForm.MinimalAPI.Models;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MailKit.Security;

namespace ContactForm.MinimalAPI.Services
{
    // SERVICE FOR SENDING EMAILS
    public class EmailService : IEmailService
    {
        // DEPENDENCY INJECTION
        private readonly ILogger<EmailService> _logger;
        private readonly ISmtpClient _smtpClient;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpEmail;
        private readonly string _smtpPassword;
        private readonly string _receptionEmail;

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailService(ILogger<EmailService> logger, ISmtpClient smtpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _smtpClient = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));

            _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? throw new InvalidOperationException("SMTP_HOST environment variable is not set.");
            _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : throw new InvalidOperationException("SMTP_PORT environment variable is not set or is not a valid number.");
            _smtpEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? throw new InvalidOperationException("SMTP_EMAIL environment variable is not set.");
            _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? throw new InvalidOperationException("SMTP_PASSWORD environment variable is not set.");
            _receptionEmail = Environment.GetEnvironmentVariable("RECEPTION_EMAIL") ?? throw new InvalidOperationException("RECEPTION_EMAIL environment variable is not set.");
        }

        // METHOD FOR SENDING EMAIL
        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> SendEmailAsync(EmailRequest request)
        {
            var errors = new List<string>(); // LIST FOR STORING ERRORS

            // TRY-CATCH BLOCK FOR SENDING EMAIL
            try
            {
                // CREATING EMAIL MESSAGE
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("", _smtpEmail));
                emailMessage.To.Add(new MailboxAddress("", _receptionEmail));
                emailMessage.To.Add(new MailboxAddress("", request.Email));
                emailMessage.Subject = "New Message from Contact Form";
                emailMessage.Body = new TextPart("plain")
                {
                    Text = $"Name: {request.Username}\nEmail: {request.Email}\nMessage: {request.Message}"
                };

                // CONNECTING TO SMTP SERVER AND SENDING EMAIL
                await _smtpClient.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.SslOnConnect);
                await _smtpClient.AuthenticateAsync(_smtpEmail, _smtpPassword);
                await _smtpClient.SendAsync(emailMessage);
                await _smtpClient.DisconnectAsync(true);

                // LOGGING EMAIL SENT SUCCESSFULLY
                _logger.LogInformation("Email sent successfully.");
                return (true, errors);
            }
            catch (Exception ex)
            {
                // LOGGING FAILED TO SEND EMAIL
                _logger.LogError(ex, "Failed to send email.");
                errors.Add(ex.Message);
                return (false, errors);
            }
        }
    }
}
