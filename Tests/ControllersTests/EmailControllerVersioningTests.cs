using System.Net;
using System.Text.Json;
using Tests.TestConfiguration;

namespace Tests.ControllersTests
{
    public class EmailControllerVersioningTest(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory = factory;

        // TEST FOR GET SMTP CONFIGURATIONS ENDPOINT WITHOUT VERSION
        [Fact]
        public async Task GetConfigs_WithoutVersion_ReturnsBadRequest()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - GET REQUEST
            var response = await client.GetAsync("/api/smtp-configurations");

            // ASSERT - CHECK STATUS CODE
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // ASSERT - ERROR BODY
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("title", out var titleProp), "title property missing in error response");

            // ASSERT - ERROR TITLE CONTENT
            var title = titleProp.GetString();
            Assert.Contains("API", title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("version", title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("required", title, StringComparison.OrdinalIgnoreCase);
        }

        // TEST FOR GET SMTP CONFIGURATIONS ENDPOINT WITH PATH VERSION RETURN SUCCESS
        [Fact]
        public async Task GetConfigs_WithPathVersion_ReturnsSuccess()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - SEND REQUEST
            var response = await client.GetAsync("/api/v1/smtp-configurations");

            // ASSERT - CHECK STATUS
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET SMTP CONFIGURATIONS ENDPOINT WITH QUERY STRING VERSION RETURN SUCCESS
        [Fact]
        public async Task GetConfigs_WithQueryStringVersion_ReturnsSuccess()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - GET CONFIGS
            var response = await client.GetAsync("/api/smtp-configurations?api-version=1.0");

            // ASSERT - STATUS OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET SMTP CONFIGURATIONS ENDPOINT WITH HEADER VERSION RETURN SUCCESS
        [Fact]
        public async Task GetConfigs_WithHeaderVersion_ReturnsSuccess()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - PREPARE REQUEST
            var request = new HttpRequestMessage(HttpMethod.Get, "api/smtp-configurations");
            request.Headers.Add("X-Version", "1.0");

            // ASSERT - CHECK STATUS
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET CONFIGS ENDPOINT WITH BOTH QUERY STRING AND HEADER VERSION - QUERY STRING TAKES PRIORITY
        [Fact]
        public async Task GetConfigs_WithBothQueryAndHeaderVersion_QueryStringTakesPriority()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - PREPARE REQUEST
            var request = new HttpRequestMessage(HttpMethod.Get, "api/smtp-configurations?api-version=1.0");
            request.Headers.Add("X-Version", "2.0");

            // ASSERT - STATUS OK
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST FOR GET SMTP CONFIGURATIONS ENDPOINT WITH INVALID PATH VERSION RETURNS NOT FOUND
        [Fact]
        public async Task GetConfigs_WithInvalidPathVersion_ReturnsNotFound()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - GET REQUEST
            var response = await client.GetAsync("/api/v2.0/smtp-configurations");

            // ASSERT - STATUS NOTFOUND
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // TEST FOR GET SMTP CONFIGURATIONS ENDPOINT WITH INVALID QUERY STRING VERSION RETURNS NOT FOUND
        [Fact]
        public async Task GetConfigs_WithInvalidQueryStringVersion_ReturnsNotFound()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - GET RESPONSE
            var response = await client.GetAsync("/api/smtp-configurations?api-version=2.0");

            // ASSERT - STATUS NOTFOUND
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // TEST FOR GET SMTP CONFIGURATIONS ENDPOINT WITH INVALID HEADER VERSION RETURNS NOT FOUND
        [Fact]
        public async Task GetConfigs_WithInvalidHeaderVersion_ReturnsNotFound()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();

            // ACT - PREPARE REQUEST
            var request = new HttpRequestMessage(HttpMethod.Get, "api/smtp-configurations");
            request.Headers.Add("X-Version", "2.0");

            // ASSERT - CHECK STATUS
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
