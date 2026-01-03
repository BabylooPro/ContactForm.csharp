using System.Net;
using System.Text.Json;
using Tests.TestConfiguration;

namespace Tests.ControllersTests
{
    public class VersionTestControllerTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory = factory;

        // TEST FOR GET V1 ENDPOINT WITH PATH VERSION TO RETURN V1 DATA
        [Fact]
        public async Task GetV1_WithPahtVersion_ReturnV1Data()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - GET V1
            var response = await client.GetAsync("/api/v1/versiontest");

            // ASSERT - STATUS OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET V2 ENDPOINT WITH PATH VERSION TO RETURN V2 DATA
        [Fact]
        public async Task GetV2_WithPahtVersion_ReturnV1Data()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - SEND REQUEST
            var response = await client.GetAsync("/api/v2/versiontest");

            // ASSERT - CHECK STATUS
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET ENDPOINT WITHOUT SPECIFIED VERSION TO RETURN MIDDLEWARE WARNING
        [Fact]
        public async Task Get_WithoutVersion_ReturnBadRequest()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - SEND REQUEST
            var response = await client.GetAsync("/api/versiontest");

            // ASSERT - EXPECT BADREQUEST
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // TEST FOR GET ENDPOINT WITH BOTH QUERY STRING AND HEADER VERSION - QUERY STRING TAKES PRIORITY
        [Fact]
        public async Task Get_WithBothQueryAndHeaderVersion_QueryStringTakesPriority()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ARRANGE - BUILD REQUEST
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/versiontest?api-version=1.0");
            request.Headers.Add("X-Version", "2.0");

            // ACT - SEND REQUEST
            var response = await client.SendAsync(request);

            // ASSERT - STATUS OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // ASSERT - VERSION 1.0
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("version", out var versionProp));
            Assert.Equal("1.0", versionProp.GetString());
        }
    }
}
