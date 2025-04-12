using System.Net;
using System.Text.Json;
using ContactForm.Tests.TestConfiguration;
using Xunit;

namespace ContactForm.Tests.ControllersTests
{
    public class EmailControllerVersioningTest : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;

        public EmailControllerVersioningTest(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

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

        // TODO: TEST FOR GET CONFIGS ENDPOINT WITH INVALID PATH VERSION RETURNS NOT FOUND
        // TODO: TEST FOR GET CONFIGS ENDPOINT WITH INVALID QUERY STRING VERSION RETURNS NOT FOUND
        // TODO: TEST FOR GET CONFIGS ENDPOINT WITH INVALID HEADER VERSION RETURNS NOT FOUND
    }
}
