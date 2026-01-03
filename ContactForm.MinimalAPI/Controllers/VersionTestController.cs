using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ContactForm.MinimalAPI.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")] // ROUTES: api/v{version}/versiontest
    [Route("api/[controller]")]
    [ApiVersion("1.0")] // api/v1/versiontest 
    [ApiVersion("2.0")] // api/v2/versiontest 
    public class VersionTestController : ControllerBase
    {
        // V1 TEST ENDPOINT
        [HttpGet]
        [MapToApiVersion("1.0")]
        public IActionResult GetV1()
        {
            return Ok(new
            {
                Version = "1.0",
                Message = "The is version 1.0 of this Endpoint",
                VersionSource = GetVersionSource()
            });
        }

        // V2 TEST ENDPOINT
        [HttpGet]
        [MapToApiVersion("2.0")]
        public IActionResult GetV2()
        {
            return Ok(new
            {
                Version = "2.0",
                Message = "The is version 2.0 of this Endpoint",
                VersionSource = GetVersionSource(),
                AdditionalInfo = "This field is only awaillable in this V2 Endpoint"
            });
        }

        // DETERMINE WHICH VERSIONING METHOD WAS USED TO ACCESS THE API
        private string GetVersionSource()
        {
            var request = HttpContext?.Request;
            if (request == null) return "Unknow";

            // CHECK URL PATH VERSIONING
            if (request.Path.ToString().Contains("/v1/") || request.Path.ToString().Contains("/v2/"))
            {
                return "URL Path";
            }

            // CHECK QUERY STRING VERSIONING
            if (request.Query.ContainsKey("api-version"))
            {
                return "Query String";
            }

            // CHECK HEADER VERSIONING
            if (request.Headers.ContainsKey("X-Version"))
            {
                return "Header";
            }

            // DEFAULT VERSION
            return "Default";
        }
    }
}
