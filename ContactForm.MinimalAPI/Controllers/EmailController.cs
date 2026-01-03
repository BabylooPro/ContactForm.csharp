using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Asp.Versioning;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactForm.MinimalAPI.Controllers
{
    // CONTROLLER FOR SENDING EMAILS (POST)
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")] // ROUTE: api/v1/email
    [Route("api/[controller]")]
    public class EmailController(IEmailService emailService, IOptions<SmtpSettings> smtpSettings) : ControllerBase
    {
        // DEPENDENCY INJECTION
        private readonly IEmailService _emailService = emailService;
        private readonly SmtpSettings _smtpSettings = smtpSettings.Value;

        // POST METHOD FOR SENDING EMAIL (api/email/{smtpId}) [example: (api/email/1) send regular email]
        [HttpPost("{smtpId}")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request, int smtpId)
        {
            try
            {
                // SENDING EMAIL
                var success = await _emailService.SendEmailAsync(request, smtpId);

                if (!success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nERROR: Failed to send email after trying all available SMTP configurations\n");
                    Console.ResetColor();
                    return StatusCode(500, "Failed to send email after trying all available SMTP configurations");
                }

                // RETURN EMAIL SENT SUCCESS MESSAGE WITH SMTP INDEX
                var smtpConfig = _smtpSettings.Configurations.FirstOrDefault(c => c.Index == smtpId );
                return Ok($"Email sent successfully using SMTP_{smtpId} ({smtpConfig?.Email} -> {_smtpSettings.ReceptionEmail})");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR: Unexpected error while sending email: {ex.Message}\n");
                Console.ResetColor();
                return StatusCode(500, "An unexpected error occurred while sending the email");
            }
        }

        // POST METHOD FOR SENDING TEST EMAIL (api/email/{smtpId}/test) [example: (api/email/1/test) send test email]
        [HttpPost("{smtpId}/test")]
        public async Task<IActionResult> SendTestEmail([FromBody] EmailRequest request, int smtpId)
        {
            try
            {
                // SENDING EMAIL USING TEST EMAIL
                var success = await _emailService.SendEmailAsync(request, smtpId, true);

                if (!success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nERROR: Failed to send test email after trying all available SMTP configurations\n");
                    Console.ResetColor();
                    return StatusCode(500, "Failed to send test email after trying all available SMTP configurations");
                }

                // RETURN TEST EMAIL SENT SUCCESS MESSAGE WITH SMTP INDEX
                var smtpConfig = _smtpSettings.Configurations.FirstOrDefault(c =>
                    c.Index == smtpId
                );
                return Ok($"Test Email sent successfully using SMTP_{smtpId} ({smtpConfig?.Email} -> {_smtpSettings.ReceptionEmail})");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR: Unexpected error while sending test email: {ex.Message}\n");
                Console.ResetColor();
                return StatusCode(500, "An unexpected error occurred while sending the test email");
            }
        }

        // GET METHOD FOR GETTING ALL SMTP CONFIGURATIONS
        [HttpGet("configs")]
        public IActionResult GetSmtpConfigs()
        {
            return Ok(_emailService.GetAllSmtpConfigs());
        }
    }
}
