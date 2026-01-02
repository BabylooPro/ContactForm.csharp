using System.Runtime.CompilerServices;
using System.Text.Json;
using ContactForm.MinimalAPI.Models;

namespace ContactForm.Tests.TestConfiguration
{
    // MODULE INITIALIZER TO SET ENVIRONMENT VARIABLES BEFORE ANY CODE RUNS
    public static class TestEnvironmentInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // SET DEFAULT SMTP CONFIGURATIONS FOR ALL TESTS
            // THIS ENSURES ENVIRONMENT VARIABLES ARE SET BEFORE PROGRAM.CONFIGURESERVICES IS CALLED
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

            // SETTING ENVIRONMENT VARIABLES FOR TESTING
            var smtpConfigurationsJson = JsonSerializer.Serialize(testConfigurations);
            
            // ONLY SET IF NOT ALREADY SET (ALLOWS TEST FACTORIES TO OVERRIDE)
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS")))
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", smtpConfigurationsJson);
            }
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMTP_0_PASSWORD")))
            {
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test-password");
            }
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL")))
            {
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            }
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL")))
            {
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");
            }
        }
    }
}
