using System.Text.Json.Serialization;

namespace API.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EmailStatus { Sent = 0, Failed = 1 }

    // REPRESENTS A SERVER-SIDE EMAIL RESOURCE CREATED VIA POST /emails
    public class EmailResource
    {
        public string Id { get; set; } = string.Empty;
        public EmailStatus Status { get; set; } = EmailStatus.Sent;
        public int RequestedSmtpId { get; set; }
        public bool IsTest { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? ReceptionEmail { get; set; }
    }
}
