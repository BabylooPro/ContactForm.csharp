using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ContactForm.MinimalAPI.Models
{
    // MODEL FOR EMAIL REQUEST WITH DYNAMIC FIELDS
    public class EmailRequest
    {
        // RECEPTION EMAIL
        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "The Email field is not a valid e-mail address.")]
        public string? Email { get; set; }

        // RECEPTION USERNAME
        public string? Username { get; set; }

        // RECEPTION MESSAGE
        [Required(ErrorMessage = "The Message field is required.")]
        public string? Message { get; set; }

        // ADDITIONAL CUSTOM FIELDS
        public Dictionary<string, string>? CustomFields { get; set; }

        // CUSTOM EMAIL TEMPLATE - USE {PropertyName} FOR PLACEHOLDERS
        // EXAMPLE: "FROM: {Email}\nNAME: {Username}\nMESSAGE: {Message}"
        public string? EmailTemplate { get; set; }

        // PREDEFINED TEMPLATE - OVERRIDES EmailTemplate IF SET
        public PredefinedTemplate? Template { get; set; }

        // HTML EMAIL SUPPORT
        public bool IsHtml { get; set; } = false;

        // ATTACHMENTS SUPPORT
        public List<EmailAttachment>? Attachments { get; set; }

        // CUSTOM SUBJECT TEMPLATE - USE {PropertyName} FOR PLACEHOLDERS
        // EXAMPLE: "New message from {Username} at {company}"
        public string? SubjectTemplate { get; set; }

        // EMAIL PRIORITY
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    }

    public class EmailAttachment
    {
        [Required(ErrorMessage = "The FileName field is required.")]
        public string? FileName { get; set; }

        [Required(ErrorMessage = "The Base64Content field is required.")]
        public string? Base64Content { get; set; }

        // OPTIONAL MIME TYPE (WILL BE GUESSED FROM FILENAME IF NOT PROVIDED)
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
