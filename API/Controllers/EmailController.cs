using Asp.Versioning;
using API.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    // RESOURCE CONTROLLER FOR EMAILS
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/emails")]
    [Route("api/emails")]
    public class EmailsController(IEmailService emailService, IEmailStore emailStore, IOptions<SmtpSettings> smtpSettings) : ControllerBase
    {
        // DEPENDENCY INJECTION
        private readonly IEmailService _emailService = emailService;
        private readonly IEmailStore _emailStore = emailStore;
        private readonly SmtpSettings _smtpSettings = smtpSettings.Value;

        // CREATE A NEW EMAIL RESOURCE (SERVER SENDS EMAIL SYNCHRONOUSLY)
        // - POST /api/v1/emails?smtpId=1
        // - POST /api/v1/emails?smtpId=1&test=true
        [HttpPost]
        public async Task<IActionResult> CreateEmail([FromBody] EmailRequest request, [FromQuery] int? smtpId = null, [FromQuery] bool test = false)
        {
            // RESOLVE SMTP ID (DEFAULT: LOWEST CONFIGURED INDEX)
            var resolvedSmtpId = smtpId ?? ResolveDefaultSmtpId();

            try
            {
                // SENDING EMAIL
                var success = await _emailService.SendEmailAsync(request, resolvedSmtpId, test);

                // ENSURE EMAIL ID EXISTS (EMAILSERVICE SHOULD SET IT)
                var emailId = string.IsNullOrWhiteSpace(request.EmailId) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : request.EmailId!;

                // STORE RESOURCE STATE FOR GET /emails/{id}
                var resource = new EmailResource
                {
                    Id = emailId,
                    Status = success ? EmailStatus.Sent : EmailStatus.Failed,
                    RequestedSmtpId = resolvedSmtpId,
                    IsTest = test,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ReceptionEmail = _smtpSettings.ReceptionEmail
                };

                _emailStore.Upsert(resource);

                if (!success)
                {
                    // DELIVERY FAILURE IS A GATEWAY-LIKE ERROR (UPSTREAM SMTP)
                    return Problem(
                        title: "Email delivery failed",
                        detail: "Failed to send email after trying all available SMTP configurations.",
                        statusCode: StatusCodes.Status502BadGateway
                    );
                }

                var location = BuildEmailLocation(emailId);
                return Created(location, resource);
            }
            catch (InvalidOperationException ex)
            {
                return Problem(title: "Bad request", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        // RETRIEVE AN EMAIL RESOURCE BY ID
        [HttpGet("{id}")]
        public IActionResult GetById(string id)
        {
            if (_emailStore.TryGet(id, out var resource)) return Ok(resource); 
            return NotFound(new { message = $"Email '{id}' was not found." });
        }

        // HELPER METHOD TO RESOLVE DEFAULT SMTP ID
        private int ResolveDefaultSmtpId()
        {
            var configs = _emailService.GetAllSmtpConfigs();
            if (configs.Count == 0) throw new InvalidOperationException("No SMTP configurations are available.");

            return configs.Min(c => c.Index);
        }

        // HELPER METHOD TO BUILD EMAIL LOCATION
        private string BuildEmailLocation(string emailId)
        {
            // KEEP LOCATION CONSISTENT WITH THE CALLER'S VERSIONING METHOD
            var path = HttpContext?.Request.Path.Value;

            if (string.IsNullOrWhiteSpace(path)) path = "/api/v1/emails";

            // URL SEGMENT VERSIONING (EX: /api/v1/emails)
            if (path.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase)) return $"{path.TrimEnd('/')}/{emailId}";

            // QUERY/HEADER VERSIONING (EX: /api/emails?api-version=1.0 OR X-Version: 1.0)
            var version = HttpContext!.GetRequestedApiVersion()?.ToString() ?? "1.0";
            return $"/api/emails/{emailId}?api-version={version}";
        }
    }
}
