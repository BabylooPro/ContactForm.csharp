using System.Runtime.CompilerServices;
using System.Text.Json;
using API.Models;

namespace Tests.TestConfiguration
{
    // MODULE INITIALIZER TO SET ENVIRONMENT VARIABLES BEFORE ANY CODE RUNS
    public static class TestEnvironmentInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // SET DEFAULT SMTP ENVIRONMENT VARIABLES FOR TESTS
            var testConfigurations = new List<SmtpConfig>
            {
                new()
                {
                    Host = "smtp.example.com",
                    Port = 465,
                    Email = "test@example.com",
                    Description = "Test SMTP",
                    Index = 0,
                },
            };

            // PREPARE THE SMTP CONFIGURATION AND DETERMINE IF AN EXISTING ENVIRONMENT VARIABLE SHOULD BE OVERRIDDEN
            var smtpConfigurationsJson = JsonSerializer.Serialize(testConfigurations);
            var existingValue = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            var shouldOverride = string.IsNullOrWhiteSpace(existingValue);

            // DETERMINE IF SMTP_CONFIGURATIONS SHOULD BE OVERRIDDEN
            if (!shouldOverride && existingValue != null)
            {
                var trimmedValue = existingValue.Trim();
                if (trimmedValue.StartsWith('"') && trimmedValue.EndsWith('"') && trimmedValue.Length > 1)
                {
                    trimmedValue = trimmedValue[1..^1].Trim();
                }

                shouldOverride = !trimmedValue.StartsWith('[') || trimmedValue == "***" || trimmedValue.Length < 3;
            }
            
            // SET DEFAULT TEST ENVIRONMENT VARIABLES
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", smtpConfigurationsJson);
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test-password");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");
        }
    }
}
