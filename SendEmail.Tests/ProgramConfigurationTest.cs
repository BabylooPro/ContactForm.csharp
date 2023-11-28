using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;

public class ProgramConfigurationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
 
    public ProgramConfigurationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // TEST TO ENSURE APPLICATION STARTS SUCCESSFULLY
    [Fact]
    public async Task Test_Application_Starts_Successfully()
    {
        // ARRANGE - CREATE A CLIENT TO SEND REQUESTS TO APPLICATION
        var client = _factory.CreateClient();

        // ACT - SEND A GET REQUEST TO "/test" ENDPOINT
        var response = await client.GetAsync("/test");

        // ASSERT - VERIFY THAT RESPONSE INDICATES SUCCESS (E.G., HTTP 200 STATUS)
        response.EnsureSuccessStatusCode();
    }
}
