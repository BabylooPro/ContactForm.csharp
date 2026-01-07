using Asp.Versioning;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // RESOURCE CONTROLLER FOR SMTP CONFIGURATIONS
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/smtp-configurations")]
    [Route("api/smtp-configurations")]
    public class SmtpConfigurationsController(IEmailService emailService) : ControllerBase
    {
        private readonly IEmailService _emailService = emailService;

        // GET ALL SMTP CONFIGURATIONS
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_emailService.GetAllSmtpConfigs());
        }

        // GET SMTP CONFIGURATION BY ID
        [HttpGet("{smtpId:int}")]
        public IActionResult GetById(int smtpId)
        {
            try
            {
                return Ok(_emailService.GetSmtpConfigById(smtpId));
            }
            catch (InvalidOperationException)
            {
                return NotFound(new { message = $"SMTP configuration '{smtpId}' was not found." });
            }
        }
    }
}
