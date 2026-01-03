using System.Net;
using System.Text.Json;
using ContactForm.Tests.TestConfiguration;
using Xunit;

namespace ContactForm.Tests.ControllersTests
{
    public class EmailControllerVersioningTest(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory = factory;

        // TEST FOR GET CONFIGS ENDPOINT WITHOUT VERSION
        [Fact]
        public async Task GetConfigs_WithoutVersion_ReturnsBadRequest()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUEST
            var client = _factory.CreateClient();

            // ACT - SEND A GET ERQUEST TO CONGIS ENDPOINT
            var response = await client.GetAsync("/api/email/configs");

            // ASSERT - CHECK RESPOSNE STATUS CODE AND CONTENT
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("title", out var titleProp), "title property missing in error response");

            var title = titleProp.GetString();

            Assert.Contains("API", title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("version", title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("required", title, StringComparison.OrdinalIgnoreCase);
        }

        // TEST FOR GET CONFIGS ENDPOINT WITH PATH VERSION RETURN SUCCESS
        [Fact]
        public async Task GetConfigs_WithPathVersion_ReturnsSuccess()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUEST
            var client = _factory.CreateClient();

            // ACT - SEND A GET ERQUEST TO CONGIS ENDPOINT
            var response = await client.GetAsync("/api/v1/email/configs");

            // ASSERT - CHECK RESPOSNE STATUS CODE AND CONTENT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET CONFIGS ENDPOINT WITH QUERY STRING VERSION RETURN SUCCESS
        [Fact]
        public async Task GetConfigs_WithQueryStringVersion_ReturnsSuccess()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUEST
            var client = _factory.CreateClient();

            // ACT - SEND A GET ERQUEST TO CONGIS ENDPOINT
            var response = await client.GetAsync("/api/email/configs?api-version=1.0");

            // ASSERT - CHECK RESPOSNE STATUS CODE AND CONTENT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET CONFIGS ENDPOINT WITH HEADER VERSION RETURN SUCCESS
        [Fact]
        public async Task GetConfigs_WithHeaderVersion_ReturnsSuccess()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUEST
            var client = _factory.CreateClient();

            // ACT - SEND A GET ERQUEST TO CONGIS ENDPOINT
            var request = new HttpRequestMessage(HttpMethod.Get, "api/email/configs");
            request.Headers.Add("X-Version", "1.0");

            // ASSERT - CHECK RESPOSNE STATUS CODE AND CONTENT
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET CONFIGS ENDPOINT WITH BOTH QUERY STRING AND HEADER VERSION - QUERY STRING TAKES PRIORITY
        [Fact]
        public async Task GetConfigs_WithBothQueryAndHeaderVersion_QueryStringTakesPriority()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUEST
            var client = _factory.CreateClient();

            // ACT - SEND A GET REQUEST WITH BOTH QUERY STRING AND HEADER VERSION
            var request = new HttpRequestMessage(HttpMethod.Get, "api/email/configs?api-version=1.0");
            request.Headers.Add("X-Version", "2.0");

            // ASSERT - CHECK RESPONSE STATUS CODE - SHOULD SUCCEED USING QUERY STRING VERSION (1.0)
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET CONFIGS ENDPOINT WITH INVALID PATH VERSION RETURNS NOT FOUND
        [Fact]
        public async Task GetConfigs_WithInvalidPathVersion_ReturnsNotFound()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUEST
            var client = _factory.CreateClient();

            // ACT - SEND A GET REQUEST TO CONFIGS ENDPOINT WITH INVALID PATH VERSION
            var response = await client.GetAsync("/api/v2.0/email/configs");

            // ASSERT - CHECK RESPONSE STATUS CODE
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // TEST FOR GET CONFIGS ENDPOINT WITH INVALID QUERY STRING VERSION RETURNS NOT FOUND
        [Fact]
        public async Task GetConfigs_WithInvalidQueryStringVersion_ReturnsNotFound()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUEST
            var client = _factory.CreateClient();

            // ACT - SEND A GET REQUEST TO CONFIGS ENDPOINT WITH INVALID QUERY STRING VERSION
            var response = await client.GetAsync("/api/email/configs?api-version=2.0");

            // ASSERT - CHECK RESPONSE STATUS CODE
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // TEST FOR GET CONFIGS ENDPOINT WITH INVALID HEADER VERSION RETURNS NOT FOUND
        [Fact]
        public async Task GetConfigs_WithInvalidHeaderVersion_ReturnsNotFound()
        {
            // ARRANGE - CREATE A CLIENT TO MAKE HTTP REQUEST
            var client = _factory.CreateClient();

            // ACT - SEND A GET REQUEST TO CONFIGS ENDPOINT WITH INVALID HEADER VERSION
            var request = new HttpRequestMessage(HttpMethod.Get, "api/email/configs");
            request.Headers.Add("X-Version", "2.0");

            // ASSERT - CHECK RESPONSE STATUS CODE
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
