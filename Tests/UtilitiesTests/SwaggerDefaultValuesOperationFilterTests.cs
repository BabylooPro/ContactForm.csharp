using API.Controllers;
using API.Utilities;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tests.TestConfiguration;

namespace Tests.UtilitiesTests
{
    public class SwaggerDefaultValuesOperationFilterTests
    {
        // TEST FOR APPLY RETURNING EARLY WHEN API VERSION IS NULL
        [Fact]
        public void Apply_WhenApiVersionIsNull_ReturnsWithoutModifyingParameters()
        {
            // ARRANGE - INIT FILTER
            var filter = new SwaggerDefaultValuesOperationFilter();

            // ARRANGE - CREATE OPERATION WITH ONE PARAMETER
            var operation = new OpenApiOperation
            {
                Parameters =
                [
                    new OpenApiParameter
                    {
                        Name = "api-version",
                        Required = false,
                        Schema = new OpenApiSchema()
                    }
                ]
            };

            // ARRANGE - EMPTY APIDESCRIPTION (NO VERSION)
            var apiDescription = new ApiDescription();

            // ARRANGE - BUILD FILTER CONTEXT
            var ctx = new OperationFilterContext(
                apiDescription,
                null!,
                new SchemaRepository(),
                typeof(VersionTestController).GetMethod(nameof(VersionTestController.GetV1))!
            );

            // ACT - APPLY FILTER
            filter.Apply(operation, ctx);

            // ASSERT - PARAMETER NOT MODIFIED
            Assert.Null(operation.Parameters[0].Schema.Default);
            Assert.False(operation.Parameters[0].Required);
        }

        // TEST FOR APPLY COVERING NULL CHECKS AND DEFAULT SETTING LOGIC
        [Fact]
        public void Apply_WhenApiVersionPresent_CoversNullChecksAndDefaultLogic()
        {
            // ARRANGE - INIT FILTER
            var filter = new SwaggerDefaultValuesOperationFilter();

            // ARRANGE - GET A REAL APIDESCRIPTION WITH VERSIONING METADATA
            using var factory = new TestWebApplicationFactory();
            using var scope = factory.Services.CreateScope();

            var groupProvider = scope.ServiceProvider.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
            var apiDescription = groupProvider.ApiDescriptionGroups.Items.SelectMany(g => g.Items).First();

            // ASSERT - API VERSION EXISTS SO FILTER DOES NOT RETURN EARLY
            Assert.NotNull(apiDescription.GetApiVersion());

            // ARRANGE - INSERT API-VERSION PARAMETER DESCRIPTION
            apiDescription.ParameterDescriptions.Insert(0,
                new ApiParameterDescription
                {
                    Name = "api-version",
                    RouteInfo = new ApiParameterRouteInfo
                    {
                        DefaultValue = "1.0",
                        IsOptional = false
                    }
                }
            );

            // ARRANGE - ADD PARAMETER DESCRIPTION WITH NO ROUTE INFO
            apiDescription.ParameterDescriptions.Add(new ApiParameterDescription { Name = "no-routeinfo", RouteInfo = null });

            // ARRANGE - ADD PARAMETER DESCRIPTION WITH NULL DEFAULT VALUE
            apiDescription.ParameterDescriptions.Add(
                new ApiParameterDescription
                {
                    Name = "null-default",
                    RouteInfo = new ApiParameterRouteInfo { DefaultValue = null, IsOptional = true }
                }
            );

            // ARRANGE - ADD PARAMETER DESCRIPTION WHERE NAME IS NOT "api-version"
            apiDescription.ParameterDescriptions.Add(
                new ApiParameterDescription
                {
                    Name = "not-api-version",
                    RouteInfo = new ApiParameterRouteInfo { DefaultValue = "2.0", IsOptional = true }
                }
            );

            var operation = new OpenApiOperation
            {
                Parameters =
                [
                    // ARRANGE - DESCRIPTION == NULL -> CONTINUE
                    new OpenApiParameter { Name = "missing-description", Schema = new OpenApiSchema() },

                    // ARRANGE - ROUTEINFO == NULL -> CONTINUE
                    new OpenApiParameter { Name = "no-routeinfo", Schema = new OpenApiSchema() },

                    // ARRANGE - SET DEFAULT + REQUIRED
                    new OpenApiParameter { Name = "api-version", Required = false, Schema = new OpenApiSchema() },

                    // ARRANGE - DEFAULTVALUE == NULL
                    new OpenApiParameter { Name = "null-default", Required = false, Schema = new OpenApiSchema() },

                    // ARRANGE - NAME IS NOT api-version (THIRD CONDITION FALSE)
                    new OpenApiParameter { Name = "not-api-version", Required = false, Schema = new OpenApiSchema() },

                    // ARRANGE - SCHEMA.DEFAULT ALREADY SET (FIRST CONDITION FALSE)
                    new OpenApiParameter { Name = "api-version", Required = false, Schema = new OpenApiSchema { Default = new OpenApiString("already") } }
                ]
            };

            // ARRANGE - BUILD FILTER CONTEXT
            var ctx = new OperationFilterContext(
                apiDescription,
                null!,
                new SchemaRepository(),
                typeof(VersionTestController).GetMethod(nameof(VersionTestController.GetV1))!
            );

            // ACT - APPLY FILTER
            filter.Apply(operation, ctx);

            // ASSERT - API-VERSION PARAM DEFAULT IS SET AND MARKED REQUIRED
            var apiVersionParam = operation.Parameters.Single(p => p.Name == "api-version" && p.Schema.Default is OpenApiString { Value: "1.0" });
            Assert.True(apiVersionParam.Required);
        }
    }
}
