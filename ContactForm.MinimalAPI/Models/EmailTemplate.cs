using System.Text.Json.Serialization;

namespace ContactForm.MinimalAPI.Models
{
    public class EmailTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PredefinedTemplate
    {
        Default,
        Modern,
        Minimal,
        Professional,
        Alert
    }
} 
