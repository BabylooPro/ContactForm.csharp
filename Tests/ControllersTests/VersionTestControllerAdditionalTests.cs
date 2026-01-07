using System.Net;
using System.Text.Json;
using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Tests.TestConfiguration;

namespace Tests.ControllersTests
{
    // UNIT TESTS FOR VERSIONTESTCONTROLLER
    public class VersionTestControllerAdditionalTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory = factory;

        // TEST FOR GET USING HEADER VERSIONING (X-VERSION)
        [Fact]
        public async Task Get_WithHeaderVersion_ReturnsHeaderVersionSource()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ARRANGE - BUILD REQUEST WITH HEADER VERSION
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/versiontest");
            request.Headers.Add("X-Version", "1.0");

            // ACT - SEND REQUEST
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // ASSERT - RESPONSE VERSION SOURCE IS HEADER
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            Assert.Equal("1.0", root.GetProperty("version").GetString());
            Assert.Equal("Header", root.GetProperty("versionSource").GetString());
        }

        // TEST FOR GETV1 RETURNING DEFAULT VERSION SOURCE WHEN NO VERSIONING SIGNALS ARE PRESENT
        [Fact]
        public void GetV1_WhenNoVersioningSignals_ReturnsDefaultVersionSource()
        {
            // ARRANGE - CONTROLLER WITH HTTP CONTEXT BUT NO VERSIONING SIGNALS
            var controller = new VersionTestController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            controller.HttpContext.Request.Path = "/api/versiontest";

            // ACT - CALL ENDPOINT
            var result = controller.GetV1();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            // ASSERT - VERSION SOURCE IS DEFAULT
            var versionSourceProp = ok.Value.GetType().GetProperty("VersionSource");
            Assert.NotNull(versionSourceProp);
            Assert.Equal("Default", versionSourceProp.GetValue(ok.Value)?.ToString());
        }

        // TEST FOR GETV1 RETURNING "UNKNOW" WHEN HTTP CONTEXT IS NULL
        [Fact]
        public void GetV1_WhenHttpContextIsNull_ReturnsUnknownVersionSource()
        {
            // ARRANGE - CONTROLLER WITHOUT HTTP CONTEXT
            var controller = new VersionTestController();

            // ACT - CALL ENDPOINT
            var result = controller.GetV1();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            // ASSERT - VERSION SOURCE IS "UNKNOW" (AS IMPLEMENTED)
            var versionSourceProp = ok.Value.GetType().GetProperty("VersionSource");
            Assert.NotNull(versionSourceProp);
            Assert.Equal("Unknow", versionSourceProp.GetValue(ok.Value)?.ToString());
        }
    }
}
