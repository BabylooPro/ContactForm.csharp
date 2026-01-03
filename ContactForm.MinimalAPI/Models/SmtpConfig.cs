namespace ContactForm.MinimalAPI.Models
{
    public class SmtpConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Email { get; set; } = string.Empty;
        public string TestEmail { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Index { get; set; }
    }

    public class SmtpSettings
    {
        public List<SmtpConfig> Configurations { get; set; } = [];
        public string ReceptionEmail { get; set; } = string.Empty;
        public string CatchAllEmail { get; set; } = string.Empty;
    }
}
