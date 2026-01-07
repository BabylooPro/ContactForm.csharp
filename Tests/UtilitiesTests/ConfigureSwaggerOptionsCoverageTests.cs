using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using API.Utilities;
using Microsoft.OpenApi.Models;

namespace Tests.UtilitiesTests
{
    // UNIT TESTS FOR CONFIGURESWAGGEROPTIONS (PRIVATE HELPERS)
    public class ConfigureSwaggerOptionsCoverageTests
    {
        // TEST FOR CREATEINFOFORAPIVERSION APPENDING DEPRECATED NOTICE
        [Fact]
        public void CreateInfoForApiVersion_WhenDeprecated_AppendsDeprecatedNotice()
        {
            // ARRANGE - GET PRIVATE METHOD VIA REFLECTION
            var method = typeof(ConfigureSwaggerOptions).GetMethod(
                "CreateInfoForApiVersion",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.NotNull(method);

            // ARRANGE - BUILD DEPRECATED VERSION DESCRIPTION
            var description = new ApiVersionDescription(new ApiVersion(1, 0), "v1", true);

            // ACT - INVOKE
            var info = (OpenApiInfo)method!.Invoke(null, [description])!;

            // ASSERT - DESCRIPTION CONTAINS DEPRECATED
            Assert.Contains("deprecated", info.Description, StringComparison.OrdinalIgnoreCase);
        }
    }
}
