using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ContactForm.MinimalAPI.Utilities
{
    // SWAGGER OPERATION FILLTER TO APPLY CORRECT VERSIONING VALUES FOR OPENAPI
    public class SwaggerDefaultValuesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            // GET API VERSION FROM ROUTE
            var apiVersion = apiDescription.GetApiVersion();
            if (apiVersion == null) return;

            // REPLACE VERSION PALCEHOLDER IN ROUTE TEMPLATE
            foreach (var parameter in operation.Parameters)
            {
                // API VERSION PARAMETER DESCRIPTION
                var description = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Name == parameter.Name);

                if (description == null) continue;

                // API VERSION PARAMETER INTERFACE TYPE INFO
                var routeInfo = description.RouteInfo;
                if (routeInfo == null) continue;

                // SET DEFAULT PARAMETER VALUES
                if (parameter.Schema.Default == null && routeInfo.DefaultValue != null && parameter.Name == "api-version")
                {
                    var defaultValue = routeInfo.DefaultValue.ToString();
                    parameter.Schema.Default = new OpenApiString(defaultValue);
                }

                parameter.Required |= !routeInfo.IsOptional;
            }
        }
    }
}
