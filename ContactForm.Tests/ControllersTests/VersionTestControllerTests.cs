using System.Net;
using System.Text.Json;
using ContactForm.Tests.TestConfiguration;

namespace ContactForm.Tests.ControllersTests
{
    public class VersionTestControllerTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory = factory;

        // TEST FOR GET V1 ENDPOINT WITH PATH VERSION TO RETURN V1 DATA
        [Fact]
        public async Task GetV1_WithPahtVersion_ReturnV1Data()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUESTS
            var client = _factory.CreateClient();

            // ACT - SEND A GET REQUEST TO THE V1 ENDPOINT WITH PATH VERSION
            var response = await client.GetAsync("/api/v1/versiontest");

            // ASSERT - CHECK THE RESPOSNE STATUS CODE AND CONTENT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        // TEST FOR GET V2 ENDPOINT WITH PATH VERSION TO RETURN V2 DATA
        [Fact]
        public async Task GetV2_WithPahtVersion_ReturnV1Data()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUESTS
            var client = _factory.CreateClient();

            // ACT - SEND A GET REQUEST TO THE V1 ENDPOINT WITH PATH VERSION
            var response = await client.GetAsync("/api/v2/versiontest");

            // ASSERT - CHECK THE RESPOSNE STATUS CODE AND CONTENT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET ENDPOINT WITHOUT SPECIFIED VERSION TO RETURN MIDDLEWARE WARNING
        [Fact]
        public async Task Get_WithoutVersion_ReturnBadRequest()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUESTS
            var client = _factory.CreateClient();

            // ACT - SEND A GET REQUEST TO THE V1 ENDPOINT WITH PATH VERSION
            var response = await client.GetAsync("/api/versiontest");

            // ASSERT - CHECK THE RESPOSNE STATUS CODE AND CONTENT
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // TEST FOR GET ENDPOINT WITH BOTH QUERY STRING AND HEADER VERSION - QUERY STRING TAKES PRIORITY
        [Fact]
        public async Task Get_WithBothQueryAndHeaderVersion_QueryStringTakesPriority()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUESTS
            var client = _factory.CreateClient();

            // ACT - SEND A GET REQUEST WITH BOTH QUERY STRING AND HEADER VERSION
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/versiontest?api-version=1.0");
            request.Headers.Add("X-Version", "2.0");

            // ASSERT - CHECK RESPONSE STATUS CODE - SHOULD SUCCEED USING QUERY STRING VERSION (1.0)
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // VERIFY THAT V1 WAS USED (NOT V2 FROM HEADER)
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            // JSON USES CAMELCASE (version) NOT PASCALCASE (Version)
            Assert.True(root.TryGetProperty("version", out var versionProp));
            Assert.Equal("1.0", versionProp.GetString());
        }
    }
}
