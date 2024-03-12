using Microsoft.AspNetCore.Mvc;
using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Services;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

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

        // CONSTRUCTOR INRIAIALIZING DEPENDENCY INJECTION
        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        // POST METHOD FOR SENDING EMAIL (api/email/send-email)
        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            // VALIDATING MODEL STATE
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TRY-CATCH BLOCK FOR SENDING EMAIL
            try
            {
                // SENDING EMAIL
                var result = await _emailService.SendEmailAsync(request);
                if (!result.IsSuccess)
                {
                    return BadRequest(result.Errors); // RETURNING ERROR MESSAGES
                }

                // LOGGING EMAIL SENT SUCCESSFULLY
                _logger.LogInformation("Email sent successfully from {SenderEmail}", request.Email);
                return Ok(new { Message = "Email sent successfully." });
            }
            catch (Exception ex)
            {
                // LOGGING FAILED TO SEND EMAIL
                _logger.LogError(ex, "Failed to send email from {SenderEmail}", request.Email);
                return StatusCode(500, "Failed to send email.");
            }
        }
    }
}
