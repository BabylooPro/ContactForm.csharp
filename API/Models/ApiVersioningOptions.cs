namespace API.Models
{
    public class ApiVersioningOptions
    {
        public const string SectionName = "ApiVersioning";

        public string DefaultVersion { get; set; } = "1.0";
        public bool AssumeDefaultVersionWhenUnspecified { get; set; } = false;
        public bool ReportApiVersions { get; set; } = true;
    }
}
