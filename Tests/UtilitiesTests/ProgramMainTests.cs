using System.Text.Json;
using API;
using API.Models;

namespace Tests.UtilitiesTests
{
    // UNIT TESTS FOR PROGRAM MAIN (PROCESS EXIT PATHS)
    public class ProgramMainTests
    {
        // SNAPSHOT ENVIRONMENT VARIABLES FOR TEST ISOLATION
        private static Dictionary<string, string?> SnapshotEnv(params string[] keys)
        {
            var snapshot = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var key in keys) snapshot[key] = Environment.GetEnvironmentVariable(key);
            return snapshot;
        }

        // RESTORE ENVIRONMENT VARIABLES FOR TEST ISOLATION
        private static void RestoreEnv(Dictionary<string, string?> snapshot)
        {
            foreach (var (key, value) in snapshot) Environment.SetEnvironmentVariable(key, value);
        }

        // TEST FOR MAIN EXITING WHEN SMTP PASSWORD IS MISSING
        [Fact]
        public void Main_WhenMissingSmtpPassword_ExitsViaGenericInvalidOperationExceptionCatch()
        {
            // ARRANGE - OVERRIDE EXIT AND SNAPSHOT ENVIRONMENT
            var originalExit = Program.Exit;
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS", "SMTP_0_PASSWORD");

            try
            {
                // ARRANGE - CAPTURE EXIT CODE INSTEAD OF KILLING TEST RUNNER
                int? exitCode = null;
                Program.Exit = code => exitCode = code;

                // ARRANGE - SMTP CONFIG IS PRESENT, BUT PASSWORD IS MISSING
                var configs = new List<SmtpConfig>
                {
                    new()
                    {
                        Index = 0,
                        Host = "127.0.0.1",
                        Port = 1,
                        Email = "test@example.com",
                        Description = "Test SMTP"
                    }
                };

                // ARRANGE - SET ENVIRONMENT VARIABLES
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", JsonSerializer.Serialize(configs));
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);

                // ACT - RUN PROGRAM MAIN
                Program.Main(Array.Empty<string>());

                // ASSERT - EXIT CODE WAS REQUESTED
                Assert.Equal(1, exitCode);
            }
            finally
            {
                // CLEANUP - RESTORE GLOBALS
                Program.Exit = originalExit;
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR MAIN EXITING WHEN SMTP CONNECTION TEST FAILS
        [Fact]
        public void Main_WhenSmtpConnectionTestFails_ExitsViaFilteredCatch()
        {
            // ARRANGE - OVERRIDE EXIT AND SNAPSHOT ENVIRONMENT
            var originalExit = Program.Exit;
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS", "SMTP_0_PASSWORD", "SMTP_RECEPTION_EMAIL", "SMTP_CATCHALL_EMAIL");

            try
            {
                // ARRANGE - CAPTURE EXIT CODE INSTEAD OF KILLING TEST RUNNER
                int? exitCode = null;
                Program.Exit = code => exitCode = code;

                // ARRANGE - VALID CONFIG/PASSWORDS BUT APP WILL FAIL IN SMTP CONNECTION TEST PATH
                var configs = new List<SmtpConfig>
                {
                    new()
                    {
                        Index = 0,
                        Host = "127.0.0.1",
                        Port = 1,
                        Email = "test@example.com",
                        Description = "Test SMTP"
                    }
                };

                // ARRANGE - SET ENVIRONMENT VARIABLES
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", JsonSerializer.Serialize(configs));
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test-password");
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

                // ACT - RUN PROGRAM MAIN
                Program.Main(Array.Empty<string>());

                // ASSERT - EXIT CODE WAS REQUESTED
                Assert.Equal(1, exitCode);
            }
            finally
            {
                // CLEANUP - RESTORE GLOBALS
                Program.Exit = originalExit;
                RestoreEnv(envSnapshot);
            }
        }
    }
}
