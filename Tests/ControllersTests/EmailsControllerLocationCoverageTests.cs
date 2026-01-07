using System.Net;
using System.Text;
using System.Text.Json;
using Tests.TestConfiguration;

namespace Tests.ControllersTests
{
    // INTEGRATION TESTS FOR EMAILS CONTROLLER LOCATION BEHAVIOR
    public class EmailsControllerLocationCoverageTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory = factory;

        // TEST FOR CREATING EMAIL WITH QUERY STRING VERSION
        [Fact]
        public async Task CreateEmail_WithQueryStringVersion_SetsLocation()
        {
            // ARRANGE - CREATE CLIENT
            var client = _factory.CreateClient();
            var json = JsonSerializer.Serialize(new { Email = "sender@example.com", Message = "Hello" });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // ACT - SEND REQUEST
            var response = await client.PostAsync("/api/emails?smtpId=1&api-version=1.0", content);

            // ASSERT - CHECK RESULT
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Contains("?api-version=1.0", response.Headers.Location!.ToString());
        }
    }
}
