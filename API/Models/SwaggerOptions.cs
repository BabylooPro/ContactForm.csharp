namespace API.Models
{
    public class SwaggerOptions
    {
        public const string SectionName = "Swagger";
        
        public bool Enabled { get; set; } = true;
        public string RoutePrefix { get; set; } = "";
    }
}
