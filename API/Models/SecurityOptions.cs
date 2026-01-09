namespace API.Models
{
    public class SecurityOptions
    {
        public const string SectionName = "Security";
        
        public bool RequireHttps { get; set; } = true;
        public int HstsMaxAge { get; set; } = 31536000; // 1 YEAR IN SECONDS
    }
}
