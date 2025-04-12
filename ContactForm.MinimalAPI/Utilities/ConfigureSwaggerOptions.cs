using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ContactForm.MinimalAPI.Utilities
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        // INITIALIZES A NEW INSTANCE OF CONFIGURE SWAGGER OPTIONS
        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        // CONFIGURE SWAGGER OPTIONS FOR EACH API VERSION
        public void Configure(SwaggerGenOptions options)
        {
            // ADD SWAGGER DOCUMENT FR EARCH DISCOVERED API VERSION
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
        }

        // CREATE INFORMATION ABOUT THE VERSION OF API
        private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo
            {
                Version = description.ApiVersion.ToString(),
                Title = "Contact Form API",
                Description = "An API for handling contact form submissions",
            };

            if (description.IsDeprecated)
            {
                info.Description += "This API version has been deprecated.";
            }

            return info;
        }
    }
}
