using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace ContactForm.Tests
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        // METHOD FOR CONFIGURING TEST WEB SERVER, PARTICULARLY FOR HANDLING ENVIRONMENT VARIABLES
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                // Determining path to .env file used for configuration
                // Assuming that test executable is located in "ContactForm.tests/bin/debug/net8.0/" folder
                // This path uses a series of ".." to go up in folder hierarchy to reach root of "ContactForm.minimalapi" project
                // This allows access to .env file located at this root, using a relative path suitable for project structure
                var envFilePath = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..",
                    "ContactForm.MinimalAPI", ".env"
                ));

                // LOADING ENVIRONMENT VARIABLES IF .ENV FILE EXISTS
                if (File.Exists(envFilePath))
                {
                    var envVars = File.ReadAllLines(envFilePath)
                                      .Where(line => line.Contains('='))
                                      .Select(line => line.Split('='))
                                      .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    foreach (var envVar in envVars)
                    {
                        Environment.SetEnvironmentVariable(envVar.Key, envVar.Value);
                    }
                }
                else
                {
                    throw new FileNotFoundException($".ENV FILE WAS NOT FOUND IN FOLLOWING LOCATION : {envFilePath}");
                }
            });
        }
    }

    // INTEGRATION TEST CLASS FOR EMAIL API
    public class EmailApiIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public EmailApiIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        // TEST VERIFYING THAT SENDING A VALID EMAIL RETURNS A SUCCESS STATUS
        [Fact]
        public async Task Post_ContactForm_Endpoint_Returns_Success_With_Valid_EmailRequest()
        {
            // ARRANGE
            var emailRequest = new EmailRequest
            {
                Email = "user@example.com",
                Username = "Test User",
                Message = "Test Message"
            };
            var content = new StringContent(JsonSerializer.Serialize(emailRequest), Encoding.UTF8, "application/json");

            // ACT & ASSERT: SENDING REQUEST AND RECEIVING RESPONSE & VERIFYING THAT RESPONSE STATUS IS OK
            var response = await _client.PostAsync("/send-email", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST VERIFYING THAT SENDING AN INVALID EMAIL RETURNS AN INCORRECT REQUEST STATUS
        [Fact]
        public async Task Post_ContactForm_Endpoint_Returns_BadRequest_With_Invalid_EmailRequest()
        {
            // ARRANGE
            var emailRequest = new EmailRequest
            {
                Email = "not-valid",
                Username = "Test User",
                Message = "Test Message"
            };
            var content = new StringContent(JsonSerializer.Serialize(emailRequest), Encoding.UTF8, "application/json");

            // ACT & ASSERT: SENDING REQUEST AND RECEIVING RESPONSE & VERIFYING THAT RESPONSE STATUS IS BADREQUEST
            var response = await _client.PostAsync("/send-email", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
