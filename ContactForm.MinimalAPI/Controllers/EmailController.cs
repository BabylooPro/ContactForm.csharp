using Microsoft.AspNetCore.Mvc;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace ContactForm.MinimalAPI.Controllers
{
    // CONTROLLER FOR SENDING EMAILS (POST)
    [ApiController]
    [Route("api/[controller]")] // ROUTE: api/email
    public class EmailController : ControllerBase
    {
        // DEPENDENCY INJECTION
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;
        private readonly SmtpSettings _smtpSettings;

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailController(IEmailService emailService, ILogger<EmailController> logger, IOptions<SmtpSettings> smtpSettings)
        {
            _emailService = emailService;
            _logger = logger;
            _smtpSettings = smtpSettings.Value;
        }

        // POST METHOD FOR SENDING EMAIL (api/email/send-email)
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
                var smtpConfig = _smtpSettings.Configurations.FirstOrDefault(c => c.Index == smtpId);
                return Ok($"Email sent successfully using SMTP_{smtpId} ({smtpConfig?.Email} -> {_smtpSettings.ReceptionEmail})" );
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

        // GET METHOD FOR GETTING ALL SMTP CONFIGURATIONS
        [HttpGet("configs")]
        public IActionResult GetSmtpConfigs()
        {
            return Ok(_emailService.GetAllSmtpConfigs());
        }
    }
}
