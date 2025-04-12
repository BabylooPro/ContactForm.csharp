using System.Net;
using System.Text.Json;
using ContactForm.Tests.TestConfiguration;

namespace ContactForm.Tests.ControllersTests
{
    public class VersionTestControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;

        public VersionTestControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

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
    }
}
