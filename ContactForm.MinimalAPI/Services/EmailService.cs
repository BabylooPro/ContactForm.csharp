using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

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
        private readonly IEmailTemplateService _templateService;

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailService(
            ILogger<EmailService> logger,
            IOptions<SmtpSettings> smtpSettings,
            ISmtpClientWrapper smtpClient,
            IEmailTrackingService emailTracker,
            IEmailTemplateService templateService
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _smtpSettings = smtpSettings.Value;
            _smtpClient = smtpClient;
            _emailTracker = emailTracker;
            _templateService = templateService;
        }

        // METHOD FOR GETTING SMTP PASSWORD
        private string GetSmtpPassword(SmtpConfig config, bool useTestEmail = false)
        {
            var envVar = useTestEmail
                ? $"SMTP_{config.Index}_PASSWORD_TEST"
                : $"SMTP_{config.Index}_PASSWORD";

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
                Console.WriteLine(
                    $"\nERROR: SMTP_{id} configuration not found. Available SMTP indexes: {string.Join(", ", _smtpSettings.Configurations.Select(x => x.Index))}\n"
                );
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
        public async Task<bool> SendEmailAsync(
            EmailRequest request,
            int smtpId,
            bool useTestEmail = false
        )
        {
            try
            {
                // CHECK IF EMAIL IS NULL
                if (string.IsNullOrEmpty(request.Email))
                {
                    throw new ArgumentNullException(
                        nameof(request.Email),
                        "Email cannot be null or empty"
                    );
                }

                // CHECK IF EMAIL IS UNIQUE
                var uniqueResult = await _emailTracker.IsEmailUnique(request.Email, smtpId);
                if (!uniqueResult.IsAllowed)
                {
                    _logger.LogWarning("");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Duplicate email detected from: {request.Email}");
                    Console.WriteLine($"Usage: {uniqueResult.UsageCount}");
                    Console.WriteLine($"Time remaining: {uniqueResult.TimeRemaining}");
                    Console.ResetColor();
                    
                    // FORMAT TIME REMAINING IN HUMAN-READABLE FORMAT
                    string timeMessage = "";
                    if (uniqueResult.TimeRemaining.HasValue)
                    {
                        var timeSpan = uniqueResult.TimeRemaining.Value;
                        var hours = (int)timeSpan.TotalHours;
                        var minutes = timeSpan.Minutes;
                        var seconds = timeSpan.Seconds;
                        
                        // SHOW COMBINATION OF HOURS, MINUTES, AND SECONDS
                        if (hours > 0)
                        {
                            timeMessage = $"{hours} hour{(hours > 1 ? "s" : "")}";
                            if (minutes > 0)
                                timeMessage += $" {minutes} minute{(minutes > 1 ? "s" : "")}";
                        }
                        else if (minutes > 0)
                        {
                            timeMessage = $"{minutes} minute{(minutes > 1 ? "s" : "")}";
                            if (seconds > 0)
                                timeMessage += $" {seconds} second{(seconds > 1 ? "s" : "")}";
                        }
                        else
                        {
                            timeMessage = $"{seconds} second{(seconds > 1 ? "s" : "")}";
                        }
                    }
                    
                    throw new InvalidOperationException(
                        $"This email has already been used to send a message with this SMTP server. " +
                        $"You can send another message in {timeMessage} (Usage: {uniqueResult.UsageCount})"
                    );
                }

                // VALIDATE SMTP CONFIG FIRST
                var config = GetSmtpConfigById(smtpId);
                if (config == null)
                {
                    throw new InvalidOperationException(
                        $"SMTP configuration with ID {smtpId} not found"
                    );
                }

                // CREATE EMAIL MESSAGE
                var email = new MimeMessage();

                // SET EMAIL FROM AND TO
                var fromEmail = useTestEmail ? config.TestEmail : config.Email;
                email.From.Add(new MailboxAddress(fromEmail, fromEmail));
                email.To.Add(
                    new MailboxAddress(_smtpSettings.ReceptionEmail, _smtpSettings.ReceptionEmail)
                );

                // SET CUSTOM SUBJECT OR USE DEFAULT
                var subject = request.SubjectTemplate;
                if (string.IsNullOrEmpty(subject))
                {
                    subject = $"Message from {request.Username}";
                }
                else
                {
                    // REPLACE PLACEHOLDERS IN SUBJECT
                    subject = subject
                        .Replace("{Email}", request.Email)
                        .Replace("{Username}", request.Username)
                        .Replace("{Message}", request.Message);

                    // REPLACE CUSTOM FIELDS IN SUBJECT
                    if (request.CustomFields?.Any() == true)
                    {
                        foreach (var field in request.CustomFields)
                        {
                            subject = subject.Replace($"{{{field.Key}}}", field.Value);
                        }
                    }
                }
                email.Subject = subject;

                // SET EMAIL PRIORITY
                switch (request.Priority)
                {
                    case EmailPriority.Low:
                        email.Priority = MessagePriority.NonUrgent;
                        break;
                    case EmailPriority.High:
                        email.Priority = MessagePriority.Urgent;
                        break;
                    case EmailPriority.Urgent:
                        email.Priority = MessagePriority.Urgent;
                        email.Headers.Add("X-Priority", "1");
                        email.Headers.Add("X-MSMail-Priority", "High");
                        email.Headers.Add("Importance", "High");
                        break;
                    default:
                        email.Priority = MessagePriority.Normal;
                        break;
                }

                // SET EMAIL BODY
                var builder = new BodyBuilder();
                var bodyText = "";

                if (request.Template.HasValue)
                {
                    // USE PREDEFINED TEMPLATE
                    var template = _templateService.GetTemplate(request.Template.Value);
                    request.IsHtml = template.IsHtml;
                    request.EmailTemplate = template.Body;
                    if (string.IsNullOrEmpty(request.SubjectTemplate))
                    {
                        request.SubjectTemplate = template.Subject;
                    }
                }

                if (string.IsNullOrEmpty(request.EmailTemplate))
                {
                    // USE DEFAULT TEMPLATE
                    bodyText = request.IsHtml
                        ? $"""
                            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                                <h2>New contact form submission</h2>
                                <p><strong>From:</strong> {request.Email}</p>
                                {(string.IsNullOrEmpty(request.Username) ? "" : $"<p><strong>Name:</strong> {request.Username}</p>")}
                                <p><strong>Message:</strong><br>{request.Message}</p>
                            """
                        : $"""
                            New contact form submission:
                            From: {request.Email}{(string.IsNullOrEmpty(request.Username) ? "" : $"\nName: {request.Username}")}
                            Message: {request.Message}
                            """;

                    // ADD CUSTOM FIELDS IF ANY
                    if (request.CustomFields?.Any() == true)
                    {
                        bodyText += request.IsHtml
                            ? "<h3>Custom Fields:</h3><ul>"
                            : "\nCustom Fields:";
                        foreach (var field in request.CustomFields)
                        {
                            bodyText += request.IsHtml
                                ? $"<li><strong>{field.Key}:</strong> {field.Value}</li>"
                                : $"\n{field.Key}: {field.Value}";
                        }
                        bodyText += request.IsHtml ? "</ul>" : "";
                    }

                    if (request.IsHtml)
                    {
                        bodyText += "</div>";
                    }
                }
                else
                {
                    // USE CUSTOM TEMPLATE
                    bodyText = request
                        .EmailTemplate.Replace("{Email}", request.Email)
                        .Replace("{Username}", request.Username ?? "")
                        .Replace("{Message}", request.Message);

                    // REPLACE CUSTOM FIELDS IN TEMPLATE
                    if (request.CustomFields?.Any() == true)
                    {
                        foreach (var field in request.CustomFields)
                        {
                            bodyText = bodyText.Replace($"{{{field.Key}}}", field.Value);
                        }
                    }
                }

                // SET BODY BASED ON FORMAT
                if (request.IsHtml)
                {
                    builder.HtmlBody = bodyText;
                }
                else
                {
                    builder.TextBody = bodyText + "\n";
                }

                // ADD ATTACHMENTS IF ANY
                if (request.Attachments?.Any() == true)
                {
                    foreach (var attachment in request.Attachments)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(attachment.Base64Content))
                            {
                                throw new InvalidOperationException(
                                    $"Base64 content is missing for attachment {attachment.FileName}"
                                );
                            }

                            var content = Convert.FromBase64String(attachment.Base64Content);
                            var contentType =
                                attachment.ContentType
                                ?? MimeTypes.GetMimeType(attachment.FileName ?? "unknown");
                            builder.Attachments.Add(
                                attachment.FileName ?? "unnamed",
                                content,
                                ContentType.Parse(contentType)
                            );
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogError(
                                $"Invalid Base64 content for attachment {attachment.FileName}: {ex.Message}"
                            );
                            throw new InvalidOperationException(
                                $"Invalid Base64 content for attachment {attachment.FileName}"
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                $"Failed to add attachment {attachment.FileName}: {ex.Message}"
                            );
                            throw new InvalidOperationException(
                                $"Failed to process attachment {attachment.FileName}"
                            );
                        }
                    }
                }

                email.Body = builder.ToMessageBody();

                // LOG EMAIL DETAILS
                _logger.LogInformation(
                    "Email Details:\nFrom: {From}\nTo: {To}\nSubject: {Subject}\nBody: {Body}",
                    email.From.ToString(),
                    email.To.ToString(),
                    email.Subject,
                    builder.TextBody
                );

                // CONNECT TO SMTP SERVER
                var cancellationToken = CancellationToken.None;
                await _smtpClient.ConnectWithTokenAsync(
                    config.Host,
                    config.Port,
                    SecureSocketOptions.SslOnConnect,
                    cancellationToken
                );

                // AUTHENTICATE WITH SMTP SERVER
                await _smtpClient.AuthenticateWithTokenAsync(
                    fromEmail,
                    GetSmtpPassword(config, useTestEmail),
                    cancellationToken
                );

                // SEND EMAIL
                await _smtpClient.SendWithTokenAsync(email, cancellationToken);

                // DISCONNECT FROM SMTP SERVER
                await _smtpClient.DisconnectWithTokenAsync(true, cancellationToken);

                // LOG SUCCESS
                _logger.LogInformation("");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"Email sent successfully using SMTP_{config.Index} ({fromEmail} -> {_smtpSettings.ReceptionEmail})"
                );
                Console.ResetColor();

                // IF EMAIL SENT SUCCESSFULLY, TRACK IT
                await _emailTracker.TrackEmail(request.Email, config.Index);
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
                var nextConfig = _smtpSettings
                    .Configurations.Where(x => x.Index > smtpId)
                    .OrderBy(x => x.Index)
                    .FirstOrDefault();

                // TRY NEXT SMTP CONFIG
                if (nextConfig != null)
                {
                    _logger.LogInformation("");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(
                        $"Attempting to use next SMTP configuration (SMTP_{nextConfig.Index})"
                    );
                    Console.ResetColor();
                    return await SendEmailAsync(request, nextConfig.Index, useTestEmail);
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
