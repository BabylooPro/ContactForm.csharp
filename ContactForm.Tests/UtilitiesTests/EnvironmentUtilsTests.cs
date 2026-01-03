using ContactForm.MinimalAPI.Models;
using ContactForm.MinimalAPI.Utilities;
using Microsoft.Extensions.Options;

namespace ContactForm.Tests.UtilitiesTests
{
    // UNIT TESTS FOR ENVIRONMENT UTILS
    public class EnvironmentUtilsTests : IDisposable
    {
        // DEPENDENCY INJECTION
        private readonly Dictionary<string, string?> _originalEnvVars = [];
        private readonly List<string> _tempFiles = [];

        // CONSTRUCTOR INITIALIZING ENVIRONMENT VARIABLES
        public EnvironmentUtilsTests()
        {
            SaveEnvironmentVariable("SMTP_CONFIGURATIONS");
            SaveEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            SaveEnvironmentVariable("SMTP_CATCHALL_EMAIL");
        }

        // SAVE ENVIRONMENT VARIABLE VALUE
        private void SaveEnvironmentVariable(string name)
        {
            _originalEnvVars[name] = Environment.GetEnvironmentVariable(name);
        }

        // RESTORE ENVIRONMENT VARIABLE VALUE
        private void RestoreEnvironmentVariable(string name)
        {
            var originalValue = _originalEnvVars.GetValueOrDefault(name);
            if (originalValue == null)
            {
                Environment.SetEnvironmentVariable(name, null);
            }
            else
            {
                Environment.SetEnvironmentVariable(name, originalValue);
            }
        }

        // CLEANUP - RESTORE ORIGINAL ENVIRONMENT VARIABLES AND DELETE TEMP FILES
        public void Dispose()
        {
            foreach (var key in _originalEnvVars.Keys)
            {
                RestoreEnvironmentVariable(key);
            }

            foreach (var tempFile in _tempFiles)
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                    else if (Directory.Exists(tempFile))
                    {
                        Directory.Delete(tempFile, true);
                    }
                }
                catch
                {
                    // IGNORE ERRORS DURING CLEANUP
                }
            }

            GC.SuppressFinalize(this);
        }

        // CREATE TEMPORARY DIRECTORY WITH .ENV FILE
        private string CreateTempDirWithEnvFile(string content)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_env_dir_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            var envFile = Path.Combine(tempDir, ".env");
            File.WriteAllText(envFile, content);
            _tempFiles.Add(tempDir);
            return tempDir;
        }

        // TEST FOR CHECKING MISSING ENVIRONMENT VARIABLES WHEN ALL ARE PRESENT
        [Fact]
        public void CheckMissingEnvironmentVariables_AllPresent_ReturnsEmptyList()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            Environment.SetEnvironmentVariable("VAR1", "value1");
            Environment.SetEnvironmentVariable("VAR2", "value2");

            // ACT - CHECKING MISSING ENVIRONMENT VARIABLES
            var result = EnvironmentUtils.CheckMissingEnvironmentVariables("VAR1", "VAR2");

            // ASSERT - CHECKING IF THE RESULT IS EMPTY
            Assert.Empty(result);
        }

        // TEST FOR CHECKING MISSING ENVIRONMENT VARIABLES WHEN SOME ARE MISSING
        [Fact]
        public void CheckMissingEnvironmentVariables_SomeMissing_ReturnsMissingVariables()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            Environment.SetEnvironmentVariable("VAR1", "value1");
            Environment.SetEnvironmentVariable("VAR2", null);
            Environment.SetEnvironmentVariable("VAR3", "");

            // ACT - CHECKING MISSING ENVIRONMENT VARIABLES
            var result = EnvironmentUtils.CheckMissingEnvironmentVariables("VAR1", "VAR2", "VAR3");

            // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
            Assert.Equal(2, result.Count);
            Assert.Contains("VAR2", result);
            Assert.Contains("VAR3", result);
            Assert.DoesNotContain("VAR1", result);
        }

        // TEST FOR CHECKING MISSING ENVIRONMENT VARIABLES WHEN ALL ARE MISSING
        [Fact]
        public void CheckMissingEnvironmentVariables_AllMissing_ReturnsAllVariables()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            Environment.SetEnvironmentVariable("VAR1", null);
            Environment.SetEnvironmentVariable("VAR2", null);

            // ACT - CHECKING MISSING ENVIRONMENT VARIABLES
            var result = EnvironmentUtils.CheckMissingEnvironmentVariables("VAR1", "VAR2");

            // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
            Assert.Equal(2, result.Count);
            Assert.Contains("VAR1", result);
            Assert.Contains("VAR2", result);
        }

        // TEST FOR CHECKING MISSING ENVIRONMENT VARIABLES WHEN VALUE IS WHITESPACE ONLY
        [Fact]
        public void CheckMissingEnvironmentVariables_WhitespaceOnly_ReturnsVariable()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            Environment.SetEnvironmentVariable("VAR1", "   ");

            // ACT - CHECKING MISSING ENVIRONMENT VARIABLES
            var result = EnvironmentUtils.CheckMissingEnvironmentVariables("VAR1");

            // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
            Assert.Single(result);
            Assert.Contains("VAR1", result);
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH VALID JSON ARRAY
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_ValidJsonArray_ReturnsConfigurations()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            var json = @"[
                {
                    ""Host"": ""smtp.example.com"",
                    ""Port"": 587,
                    ""Email"": ""test@example.com"",
                    ""TestEmail"": ""test-email@example.com"",
                    ""Description"": ""Test SMTP"",
                    ""Index"": 0
                }
            ]";
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", json);

            try
            {
                // ACT - LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
                Assert.Equal(587, result[0].Port);
                Assert.Equal("test@example.com", result[0].Email);
                Assert.Equal("test-email@example.com", result[0].TestEmail);
                Assert.Equal("Test SMTP", result[0].Description);
                Assert.Equal(0, result[0].Index);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH MULTIPLE CONFIGURATIONS
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_MultipleConfigurations_ReturnsAll()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            var json = @"[
                {
                    ""Host"": ""smtp1.example.com"",
                    ""Port"": 587,
                    ""Email"": ""test1@example.com"",
                    ""TestEmail"": ""test1-email@example.com"",
                    ""Description"": ""SMTP 1"",
                    ""Index"": 0
                },
                {
                    ""Host"": ""smtp2.example.com"",
                    ""Port"": 465,
                    ""Email"": ""test2@example.com"",
                    ""TestEmail"": ""test2-email@example.com"",
                    ""Description"": ""SMTP 2"",
                    ""Index"": 1
                }
            ]";
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", json);

            try
            {
                // ACT - LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
                Assert.Equal(2, result.Count);
                Assert.Equal("smtp1.example.com", result[0].Host);
                Assert.Equal("smtp2.example.com", result[1].Host);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN VARIABLE IS MISSING
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_MissingVariable_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN VARIABLE IS MISSING
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment()
                );
                Assert.Contains("Missing required configuration: SMTP_CONFIGURATIONS", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN VARIABLE IS EMPTY STRING
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_EmptyString_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "");

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN VARIABLE IS EMPTY
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment()
                );
                Assert.Contains("Missing required configuration: SMTP_CONFIGURATIONS", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN JSON DOES NOT START WITH BRACKET
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_NotStartingWithBracket_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "{ \"Host\": \"test\" }");

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN JSON DOES NOT START WITH BRACKET
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment()
                );
                Assert.Contains("Invalid configuration format: SMTP_CONFIGURATIONS", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN JSON IS INVALID
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_InvalidJson_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[ invalid json }");

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN JSON IS INVALID
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment()
                );
                Assert.Contains("Failed to parse configuration: SMTP_CONFIGURATIONS", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN JSON ARRAY IS EMPTY
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_EmptyArray_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[]");

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN JSON ARRAY IS EMPTY
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment()
                );
                Assert.Contains("The JSON array was parsed but produced no SMTP configurations", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN JSON IS WRAPPED IN QUOTES
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WithQuotes_RemovesQuotes()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            var json = @"[
                {
                    ""Host"": ""smtp.example.com"",
                    ""Port"": 587,
                    ""Email"": ""test@example.com"",
                    ""TestEmail"": ""test-email@example.com"",
                    ""Description"": ""Test SMTP"",
                    ""Index"": 0
                }
            ]";
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", $"\"{json}\"");

            try
            {
                // ACT - LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH MULTILINE WHITESPACE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WithMultilineWhitespace_Normalizes()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            var json = @"[
                {
                    ""Host"": ""smtp.example.com"",
                    ""Port"": 587,
                    ""Email"": ""test@example.com"",
                    ""TestEmail"": ""test-email@example.com"",
                    ""Description"": ""Test SMTP"",
                    ""Index"": 0
                }
            ]";
            var multilineJson = json.Replace("\n", "\r\n").Replace(" ", "  ");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", multilineJson);

            try
            {
                // ACT - LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH TRAILING COMMA
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WithTrailingComma_HandlesCorrectly()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            var json = @"[
                {
                    ""Host"": ""smtp.example.com"",
                    ""Port"": 587,
                    ""Email"": ""test@example.com"",
                    ""TestEmail"": ""test-email@example.com"",
                    ""Description"": ""Test SMTP"",
                    ""Index"": 0,
                }
            ]";
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", json);

            try
            {
                // ACT - LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH CASE INSENSITIVE PROPERTIES
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_CaseInsensitiveProperties_HandlesCorrectly()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            var json = @"[
                {
                    ""host"": ""smtp.example.com"",
                    ""port"": 587,
                    ""email"": ""test@example.com"",
                    ""testEmail"": ""test-email@example.com"",
                    ""description"": ""Test SMTP"",
                    ""index"": 0
                }
            ]";
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", json);

            try
            {
                // ACT - LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
                Assert.Equal(587, result[0].Port);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT FILE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_FromEnvFile_LoadsCorrectly()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var json = @"[
                {
                    ""Host"": ""smtp.example.com"",
                    ""Port"": 587,
                    ""Email"": ""test@example.com"",
                    ""TestEmail"": ""test-email@example.com"",
                    ""Description"": ""Test SMTP"",
                    ""Index"": 0
                }
            ]";
            var tempDir = CreateTempDirWithEnvFile($"SMTP_CONFIGURATIONS=\"{json}\"");
            var originalDir = Directory.GetCurrentDirectory();
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Directory.SetCurrentDirectory(tempDir);
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

            try
            {
                // ACT - LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT FILE WITH MULTILINE VALUE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_FromEnvFileMultiline_LoadsCorrectly()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var json = @"[
                {
                    ""Host"": ""smtp.example.com"",
                    ""Port"": 587,
                    ""Email"": ""test@example.com"",
                    ""TestEmail"": ""test-email@example.com"",
                    ""Description"": ""Test SMTP"",
                    ""Index"": 0
                }
            ]";
            var tempDir = CreateTempDirWithEnvFile($"SMTP_CONFIGURATIONS=\"{json}\"");
            var originalDir = Directory.GetCurrentDirectory();
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Directory.SetCurrentDirectory(tempDir);
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

            try
            {
                // ACT - LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECKING IF THE RESULT IS NOT EMPTY
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WITH VALID CONFIGURATION
        [Fact]
        public void ConfigureSmtpSettings_ValidConfiguration_ConfiguresServices()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>
            {
                new()
                {
                    Host = "smtp.example.com",
                    Port = 587,
                    Email = "test@example.com",
                    TestEmail = "test-email@example.com",
                    Description = "Test SMTP",
                    Index = 0
                }
            };
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT - CONFIGURING SMTP SETTINGS
                EnvironmentUtils.ConfigureSmtpSettings(services, configurations);

                // ASSERT - CHECKING IF THE SMTP SETTINGS ARE CONFIGURED CORRECTLY
                var serviceProvider = services.BuildServiceProvider();
                var smtpSettings = serviceProvider.GetRequiredService<IOptions<SmtpSettings>>().Value;
                Assert.Single(smtpSettings.Configurations);
                Assert.Equal("reception@example.com", smtpSettings.ReceptionEmail);
                Assert.Equal("catchall@example.com", smtpSettings.CatchAllEmail);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WHEN RECEPTION EMAIL IS MISSING
        [Fact]
        public void ConfigureSmtpSettings_MissingReceptionEmail_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>();
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN RECEPTION EMAIL IS MISSING
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.ConfigureSmtpSettings(services, configurations)
                );
                Assert.Contains("Environment variable 'SMTP_RECEPTION_EMAIL' is required", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WHEN CATCHALL EMAIL IS MISSING
        [Fact]
        public void ConfigureSmtpSettings_MissingCatchAllEmail_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>();
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN CATCHALL EMAIL IS MISSING
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.ConfigureSmtpSettings(services, configurations)
                );
                Assert.Contains("Environment variable 'SMTP_CATCHALL_EMAIL' is required", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WHEN RECEPTION EMAIL IS EMPTY
        [Fact]
        public void ConfigureSmtpSettings_EmptyReceptionEmail_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>();
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN RECEPTION EMAIL IS EMPTY
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.ConfigureSmtpSettings(services, configurations)
                );
                Assert.Contains("Environment variable 'SMTP_RECEPTION_EMAIL' is required", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WHEN CATCHALL EMAIL IS EMPTY
        [Fact]
        public void ConfigureSmtpSettings_EmptyCatchAllEmail_ThrowsException()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>();
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "");

            try
            {
                // ACT & ASSERT - CHECKING IF EXCEPTION IS THROWN WHEN CATCHALL EMAIL IS EMPTY
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.ConfigureSmtpSettings(services, configurations)
                );
                Assert.Contains("Environment variable 'SMTP_CATCHALL_EMAIL' is required", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WITH MULTIPLE CONFIGURATIONS
        [Fact]
        public void ConfigureSmtpSettings_MultipleConfigurations_ConfiguresAll()
        {
            // ARRANGE - SETTING ENVIRONMENT VARIABLES
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>
            {
                new()
                {
                    Host = "smtp1.example.com",
                    Port = 587,
                    Email = "test1@example.com",
                    TestEmail = "test1-email@example.com",
                    Description = "SMTP 1",
                    Index = 0
                },
                new()
                {
                    Host = "smtp2.example.com",
                    Port = 465,
                    Email = "test2@example.com",
                    TestEmail = "test2-email@example.com",
                    Description = "SMTP 2",
                    Index = 1
                }
            };
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT - CONFIGURING SMTP SETTINGS
                EnvironmentUtils.ConfigureSmtpSettings(services, configurations);

                // ASSERT - CHECKING IF THE SMTP SETTINGS ARE CONFIGURED CORRECTLY
                var serviceProvider = services.BuildServiceProvider();
                var smtpSettings = serviceProvider.GetRequiredService<IOptions<SmtpSettings>>().Value;
                Assert.Equal(2, smtpSettings.Configurations.Count);
                Assert.Equal("smtp1.example.com", smtpSettings.Configurations[0].Host);
                Assert.Equal("smtp2.example.com", smtpSettings.Configurations[1].Host);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }
    }
}
