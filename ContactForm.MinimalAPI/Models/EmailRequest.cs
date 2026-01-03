using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ContactForm.MinimalAPI.Models
{
    public class EmailRequest
    {

        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "The Email field is not a valid e-mail address.")]
        public string? Email { get; set; }

        public string? Username { get; set; }

        [Required(ErrorMessage = "The Message field is required.")]
        public string? Message { get; set; }

        public Dictionary<string, string>? CustomFields { get; set; }
        public string? EmailTemplate { get; set; }
        public PredefinedTemplate? Template { get; set; }
        public bool IsHtml { get; set; } = false;
        public List<EmailAttachment>? Attachments { get; set; }
        public string? SubjectTemplate { get; set; }
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    }

    public class EmailAttachment
    {
        public string? FileName { get; set; }
        public string? Base64Content { get; set; }
        public string? ContentType { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EmailPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }
}
