namespace API.Models
{
    public class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";
        
        public int PermitLimit { get; set; } = 10;
        public int WindowMinutes { get; set; } = 1;
        public int QueueLimit { get; set; } = 0;
    }
}
