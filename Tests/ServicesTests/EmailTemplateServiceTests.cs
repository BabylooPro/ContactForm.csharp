using API.Models;
using API.Services;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR EMAIL TEMPLATE SERVICE
    public class EmailTemplateServiceTests
    {
        // TEST FOR GETTEMPLATE RETURNING A TEMPLATE FOR EACH KNOWN VALUE
        [Fact]
        public void GetTemplate_ReturnsTemplateForEachKnownValue()
        {
            // ARRANGE - INIT SERVICE
            var service = new EmailTemplateService();

            // ACT & ASSERT - EACH TEMPLATE RETURNS A VALID RESULT
            foreach (var template in Enum.GetValues<PredefinedTemplate>())
            {
                var result = service.GetTemplate(template);
                Assert.NotNull(result);
                Assert.False(string.IsNullOrWhiteSpace(result.Name));
                Assert.False(string.IsNullOrWhiteSpace(result.Subject));
                Assert.False(string.IsNullOrWhiteSpace(result.Body));
            }
        }

        // TEST FOR GETTEMPLATE RETURNING DEFAULT WHEN TEMPLATE IS UNKNOWN
        [Fact]
        public void GetTemplate_WhenUnknownTemplate_ReturnsDefault()
        {
            // ARRANGE - INIT SERVICE
            var service = new EmailTemplateService();

            // ACT - REQUEST UNKNOWN TEMPLATE
            var result = service.GetTemplate((PredefinedTemplate)999);

            // ASSERT - FALL BACK TO DEFAULT TEMPLATE
            Assert.Equal("Default", result.Name);
        }
    }
}
