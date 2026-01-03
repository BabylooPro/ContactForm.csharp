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
            
            // SAVE CORS ORIGIN VARIABLES (UP TO 10 FOR TESTING)
            for (int i = 1; i <= 10; i++) SaveEnvironmentVariable($"CORS_{i}_ORIGIN");
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
            foreach (var key in _originalEnvVars.Keys) RestoreEnvironmentVariable(key); 
            for (int i = 1; i <= 10; i++) RestoreEnvironmentVariable($"CORS_{i}_ORIGIN");

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
            // ARRANGE - SET ENV VARS
            Environment.SetEnvironmentVariable("VAR1", "value1");
            Environment.SetEnvironmentVariable("VAR2", "value2");

            // ACT - CHECK MISSING
            var result = EnvironmentUtils.CheckMissingEnvironmentVariables("VAR1", "VAR2");

            // ASSERT - SHOULD BE EMPTY
            Assert.Empty(result);
        }

        // TEST FOR CHECKING MISSING ENVIRONMENT VARIABLES WHEN SOME ARE MISSING
        [Fact]
        public void CheckMissingEnvironmentVariables_SomeMissing_ReturnsMissingVariables()
        {
            // ARRANGE - SET ENV VARS
            Environment.SetEnvironmentVariable("VAR1", "value1");
            Environment.SetEnvironmentVariable("VAR2", null);
            Environment.SetEnvironmentVariable("VAR3", "");

            // ACT - CHECK MISSING
            var result = EnvironmentUtils.CheckMissingEnvironmentVariables("VAR1", "VAR2", "VAR3");

            // ASSERT - VERIFY RESULT
            Assert.Equal(2, result.Count);
            Assert.Contains("VAR2", result);
            Assert.Contains("VAR3", result);
            Assert.DoesNotContain("VAR1", result);
        }

        // TEST FOR CHECKING MISSING ENVIRONMENT VARIABLES WHEN ALL ARE MISSING
        [Fact]
        public void CheckMissingEnvironmentVariables_AllMissing_ReturnsAllVariables()
        {
            // ARRANGE - CLEAR VARIABLES
            Environment.SetEnvironmentVariable("VAR1", null);
            Environment.SetEnvironmentVariable("VAR2", null);

            // ACT - CHECK MISSING
            var result = EnvironmentUtils.CheckMissingEnvironmentVariables("VAR1", "VAR2");

            // ASSERT - ALL MISSING
            Assert.Equal(2, result.Count);
            Assert.Contains("VAR1", result);
            Assert.Contains("VAR2", result);
        }

        // TEST FOR CHECKING MISSING ENVIRONMENT VARIABLES WHEN VALUE IS WHITESPACE ONLY
        [Fact]
        public void CheckMissingEnvironmentVariables_WhitespaceOnly_ReturnsVariable()
        {
            // ARRANGE - SET ENV VAR
            Environment.SetEnvironmentVariable("VAR1", "   ");

            // ACT - CHECK MISSING
            var result = EnvironmentUtils.CheckMissingEnvironmentVariables("VAR1");

            // ASSERT - RESULT CONTAINS VAR1
            Assert.Single(result);
            Assert.Contains("VAR1", result);
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH VALID JSON ARRAY
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_ValidJsonArray_ReturnsConfigurations()
        {
            // ARRANGE - BACKUP/SET VAR
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
                // ACT - LOAD CONFIGS
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECK VALUES
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
                // CLEANUP - RESTORE VAR
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH MULTIPLE CONFIGURATIONS
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_MultipleConfigurations_ReturnsAll()
        {
            // ARRANGE - BACKUP, SET VAR
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
                // ACT - LOAD CONFIGURATIONS
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECK VALUES
                Assert.Equal(2, result.Count);
                Assert.Equal("smtp1.example.com", result[0].Host);
                Assert.Equal("smtp2.example.com", result[1].Host);
            }
            finally
            {
                // ARRANGE - RESTORE VAR
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN VARIABLE IS MISSING
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_MissingVariable_ThrowsException()
        {
            // ARRANGE - BACKUP, UNSET
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

            try
            {
                // ACT & ASSERT - THROW EXCEPTION
                var exception = Assert.Throws<InvalidOperationException>(() => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment());
                Assert.Contains("Missing required configuration: SMTP_CONFIGURATIONS", exception.Message);
            }
            finally
            {
                // ARRANGE - RESTORE VAR
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN VARIABLE IS EMPTY STRING
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_EmptyString_ThrowsException()
        {
            // ARRANGE - BACKUP, CLEAR VAR
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "");

            try
            {
                // ACT - CALL METHOD, ASSERT - THROW EXCEPTION
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment()
                );
                Assert.Contains("Missing required configuration: SMTP_CONFIGURATIONS", exception.Message);
            }
            finally
            {
                // ARRANGE - RESTORE VAR
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN JSON DOES NOT START WITH BRACKET
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_NotStartingWithBracket_ThrowsException()
        {
            // ARRANGE - BACKUP, SET VAR
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "{ \"Host\": \"test\" }");

            try
            {
                // ACT/ASSERT - EXPECT EXCEPTION
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment()
                );
                Assert.Contains("Invalid configuration format: SMTP_CONFIGURATIONS", exception.Message);
            }
            finally
            {
                // ARRANGE - RESTORE VAR
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN JSON IS INVALID
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_InvalidJson_ThrowsException()
        {
            // ARRANGE - BACKUP, SET INVALID
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[ invalid json }");

            try
            {
                // ACT/ASSERT - THROW IF INVALID
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment()
                );
                Assert.Contains("Failed to parse configuration: SMTP_CONFIGURATIONS", exception.Message);
            }
            finally
            {
                // ARRANGE - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN JSON ARRAY IS EMPTY
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_EmptyArray_ThrowsException()
        {
            // ARRANGE - BACKUP, SET EMPTY
            var originalSmtpConfig = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[]");

            try
            {
                // ACT - CALL METHOD
                var exception = Assert.Throws<InvalidOperationException>(() => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment());

                // ASSERT - EXPECT EXCEPTION
                Assert.Contains("The JSON array was parsed but produced no SMTP configurations", exception.Message);
            }
            finally
            {
                // ARRANGE - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WHEN JSON IS WRAPPED IN QUOTES
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WithQuotes_RemovesQuotes()
        {
            // ARRANGE - BACKUP/SET ENV
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
                // ACT - LOAD CONFIG
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECK RESULT
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                // ARRANGE - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH MULTILINE WHITESPACE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WithMultilineWhitespace_Normalizes()
        {
            // ARRANGE - ENV VAR SETUP
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
                // ACT - LOAD CONFIGS
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - RESULT VALID
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                // ARRANGE - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH TRAILING COMMA
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WithTrailingComma_HandlesCorrectly()
        {
            // ARRANGE - SAVE/SET ENV
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
                // ACT - LOAD CONFIG
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - VERIFY RESULT
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                // ARRANGE - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT WITH CASE INSENSITIVE PROPERTIES
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_CaseInsensitiveProperties_HandlesCorrectly()
        {
            // ARRANGE - SAVE ORIGINAL, SET ENV
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
                // ACT - LOAD CONFIGURATIONS
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECK COUNT, VALUES
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
                Assert.Equal(587, result[0].Port);
            }
            finally
            {
                // ARRANGE - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT FILE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_FromEnvFile_LoadsCorrectly()
        {
            // ARRANGE - ENV VAR
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
                // ACT - LOAD CONFIG
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - CHECK RESULT
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                // CLEANUP - RESTORE ENV
                Directory.SetCurrentDirectory(originalDir);
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR LOADING SMTP CONFIGURATIONS FROM ENVIRONMENT FILE WITH MULTILINE VALUE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_FromEnvFileMultiline_LoadsCorrectly()
        {
            // ARRANGE - SET ENV VARS
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
                // ACT - LOAD CONFIGURATIONS
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - VALIDATE RESULT
                Assert.Single(result);
                Assert.Equal("smtp.example.com", result[0].Host);
            }
            finally
            {
                // CLEANUP - RESTORE ENV
                Directory.SetCurrentDirectory(originalDir);
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", originalSmtpConfig);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WITH VALID CONFIGURATION
        [Fact]
        public void ConfigureSmtpSettings_ValidConfiguration_ConfiguresServices()
        {
            // ARRANGE - SAVE ORIGINALS
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
                // ACT - CONFIGURE SERVICES
                EnvironmentUtils.ConfigureSmtpSettings(services, configurations);

                // ASSERT - VERIFY CONFIGURATION
                var serviceProvider = services.BuildServiceProvider();
                var smtpSettings = serviceProvider.GetRequiredService<IOptions<SmtpSettings>>().Value;
                Assert.Single(smtpSettings.Configurations);
                Assert.Equal("reception@example.com", smtpSettings.ReceptionEmail);
                Assert.Equal("catchall@example.com", smtpSettings.CatchAllEmail);
            }
            finally
            {
                // CLEANUP - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WHEN RECEPTION EMAIL IS MISSING
        [Fact]
        public void ConfigureSmtpSettings_MissingReceptionEmail_ThrowsException()
        {
            // ARRANGE - SAVE ORIGINALS
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>();
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT & ASSERT - THROW IF MISSING
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.ConfigureSmtpSettings(services, configurations)
                );
                Assert.Contains("Environment variable 'SMTP_RECEPTION_EMAIL' is required", exception.Message);
            }
            finally
            {
                // CLEANUP - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WHEN CATCHALL EMAIL IS MISSING
        [Fact]
        public void ConfigureSmtpSettings_MissingCatchAllEmail_ThrowsException()
        {
            // ARRANGE - ENV VARS
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>();
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);

            try
            {
                // ACT & ASSERT - THROW EXCEPTION
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.ConfigureSmtpSettings(services, configurations)
                );
                Assert.Contains("Environment variable 'SMTP_CATCHALL_EMAIL' is required", exception.Message);
            }
            finally
            {
                // CLEANUP - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WHEN RECEPTION EMAIL IS EMPTY
        [Fact]
        public void ConfigureSmtpSettings_EmptyReceptionEmail_ThrowsException()
        {
            // ARRANGE - ENV VARS
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>();
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT & ASSERT - THROW EXCEPTION
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.ConfigureSmtpSettings(services, configurations)
                );
                Assert.Contains("Environment variable 'SMTP_RECEPTION_EMAIL' is required", exception.Message);
            }
            finally
            {
                // CLEANUP - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WHEN CATCHALL EMAIL IS EMPTY
        [Fact]
        public void ConfigureSmtpSettings_EmptyCatchAllEmail_ThrowsException()
        {
            // ARRANGE - ENV VARS
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            var services = new ServiceCollection();
            var configurations = new List<SmtpConfig>();
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "");

            try
            {
                // ACT & ASSERT - THROW EXCEPTION
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentUtils.ConfigureSmtpSettings(services, configurations)
                );
                Assert.Contains("Environment variable 'SMTP_CATCHALL_EMAIL' is required", exception.Message);
            }
            finally
            {
                // CLEANUP - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR CONFIGURING SMTP SETTINGS WITH MULTIPLE CONFIGURATIONS
        [Fact]
        public void ConfigureSmtpSettings_MultipleConfigurations_ConfiguresAll()
        {
            // ARRANGE - SAVE ORIGINAL ENV
            var originalReceptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var originalCatchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            
            // ARRANGE - PREPARE SERVICES
            var services = new ServiceCollection();
            
            // ARRANGE - CREATE CONFIGS
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
            
            // ARRANGE - SET ENV VARS
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT - CONFIGURE SMTP
                EnvironmentUtils.ConfigureSmtpSettings(services, configurations);

                // ASSERT - CHECK CONFIGS
                var serviceProvider = services.BuildServiceProvider();
                var smtpSettings = serviceProvider.GetRequiredService<IOptions<SmtpSettings>>().Value;
                Assert.Equal(2, smtpSettings.Configurations.Count);
                Assert.Equal("smtp1.example.com", smtpSettings.Configurations[0].Host);
                Assert.Equal("smtp2.example.com", smtpSettings.Configurations[1].Host);
            }
            finally
            {
                // ARRANGE - RESTORE ENV
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", originalReceptionEmail);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", originalCatchAllEmail);
            }
        }

        // TEST FOR LOADING CORS ORIGINS FROM ENVIRONMENT VARIABLES WITH SINGLE ORIGIN
        [Fact]
        public void LoadCorsOriginsFromEnvironment_SingleOrigin_ReturnsOrigin()
        {
            // ARRANGE - SET CORS ORIGIN
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://example.com");

            try
            {
                // ACT - LOAD CORS ORIGINS
                var result = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

                // ASSERT - VERIFY RESULT
                Assert.Single(result);
                Assert.Contains("https://example.com", result);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", null);
            }
        }

        // TEST FOR LOADING CORS ORIGINS FROM ENVIRONMENT VARIABLES WITH MULTIPLE ORIGINS
        [Fact]
        public void LoadCorsOriginsFromEnvironment_MultipleOrigins_ReturnsAllOrigins()
        {
            // ARRANGE - SET MULTIPLE CORS ORIGINS
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://example.com");
            Environment.SetEnvironmentVariable("CORS_2_ORIGIN", "https://another-domain.com");
            Environment.SetEnvironmentVariable("CORS_3_ORIGIN", "https://third-domain.com");

            try
            {
                // ACT - LOAD CORS ORIGINS
                var result = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

                // ASSERT - VERIFY RESULT
                Assert.Equal(3, result.Count);
                Assert.Contains("https://example.com", result);
                Assert.Contains("https://another-domain.com", result);
                Assert.Contains("https://third-domain.com", result);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", null);
                Environment.SetEnvironmentVariable("CORS_2_ORIGIN", null);
                Environment.SetEnvironmentVariable("CORS_3_ORIGIN", null);
            }
        }

        // TEST FOR LOADING CORS ORIGINS FROM ENVIRONMENT VARIABLES WITH NO ORIGINS
        [Fact]
        public void LoadCorsOriginsFromEnvironment_NoOrigins_ReturnsEmptyList()
        {
            // ARRANGE - CLEAR ALL CORS ORIGINS
            for (int i = 1; i <= 10; i++)
            {
                Environment.SetEnvironmentVariable($"CORS_{i}_ORIGIN", null);
            }

            // ACT - LOAD CORS ORIGINS
            var result = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

            // ASSERT - VERIFY RESULT
            Assert.Empty(result);
        }

        // TEST FOR LOADING CORS ORIGINS FROM ENVIRONMENT VARIABLES WITH GAPS IN INDEXING
        [Fact]
        public void LoadCorsOriginsFromEnvironment_GapsInIndexing_StopsAtFirstGap()
        {
            // ARRANGE - SET ORIGINS WITH GAP
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://example.com");
            Environment.SetEnvironmentVariable("CORS_2_ORIGIN", "https://another-domain.com");
            Environment.SetEnvironmentVariable("CORS_3_ORIGIN", null);
            Environment.SetEnvironmentVariable("CORS_4_ORIGIN", "https://fourth-domain.com");

            try
            {
                // ACT - LOAD CORS ORIGINS
                var result = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

                // ASSERT - VERIFY RESULT (SHOULD STOP AT GAP)
                Assert.Equal(2, result.Count);
                Assert.Contains("https://example.com", result);
                Assert.Contains("https://another-domain.com", result);
                Assert.DoesNotContain("https://fourth-domain.com", result);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", null);
                Environment.SetEnvironmentVariable("CORS_2_ORIGIN", null);
                Environment.SetEnvironmentVariable("CORS_4_ORIGIN", null);
            }
        }

        // TEST FOR LOADING CORS ORIGINS FROM ENVIRONMENT VARIABLES WITH DUPLICATES
        [Fact]
        public void LoadCorsOriginsFromEnvironment_DuplicateOrigins_ReturnsUniqueOrigins()
        {
            // ARRANGE - SET DUPLICATE ORIGINS
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://example.com");
            Environment.SetEnvironmentVariable("CORS_2_ORIGIN", "https://example.com");

            try
            {
                // ACT - LOAD CORS ORIGINS
                var result = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

                // ASSERT - VERIFY RESULT (SHOULD CONTAIN ONLY ONE)
                Assert.Single(result);
                Assert.Contains("https://example.com", result);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", null);
                Environment.SetEnvironmentVariable("CORS_2_ORIGIN", null);
            }
        }

        // TEST FOR LOADING CORS ORIGINS FROM ENVIRONMENT VARIABLES WITH WHITESPACE
        [Fact]
        public void LoadCorsOriginsFromEnvironment_WhitespaceInOrigin_TrimsWhitespace()
        {
            // ARRANGE - SET ORIGIN WITH WHITESPACE
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "  https://example.com  ");

            try
            {
                // ACT - LOAD CORS ORIGINS
                var result = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

                // ASSERT - VERIFY RESULT (SHOULD BE TRIMMED)
                Assert.Single(result);
                Assert.Contains("https://example.com", result);
                Assert.DoesNotContain("  https://example.com  ", result);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", null);
            }
        }

        // TEST FOR LOADING CORS ORIGINS FROM ENVIRONMENT VARIABLES WITH EMPTY STRING
        [Fact]
        public void LoadCorsOriginsFromEnvironment_EmptyString_StopsAtEmpty()
        {
            // ARRANGE - SET EMPTY ORIGIN (FUNCTION STOPS AT FIRST EMPTY/NULL)
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "");
            Environment.SetEnvironmentVariable("CORS_2_ORIGIN", "https://example.com");

            try
            {
                // ACT - LOAD CORS ORIGINS
                var result = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

                // ASSERT - VERIFY RESULT
                Assert.Empty(result);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", null);
                Environment.SetEnvironmentVariable("CORS_2_ORIGIN", null);
            }
        }

        // TEST FOR LOADING CORS ORIGINS FROM ENVIRONMENT VARIABLES WITH WHITESPACE ONLY
        [Fact]
        public void LoadCorsOriginsFromEnvironment_WhitespaceOnly_StopsAtWhitespace()
        {
            // ARRANGE - SET WHITESPACE ORIGIN
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "   ");
            Environment.SetEnvironmentVariable("CORS_2_ORIGIN", "https://example.com");

            try
            {
                // ACT - LOAD CORS ORIGINS
                var result = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

                // ASSERT - VERIFY RESULT
                Assert.Empty(result);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", null);
                Environment.SetEnvironmentVariable("CORS_2_ORIGIN", null);
            }
        }

        // TEST FOR ISLOCALHOSTORIGIN WITH HTTP LOCALHOST
        [Fact]
        public void IsLocalhostOrigin_HttpLocalhost_ReturnsTrue()
        {
            // ACT & ASSERT - CHECK IF LOCALHOST ORIGIN IS TRUE
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("http://localhost"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("http://localhost:3000"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("http://localhost:8080"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("http://localhost:5000"));
        }

        // TEST FOR ISLOCALHOSTORIGIN WITH HTTPS LOCALHOST
        [Fact]
        public void IsLocalhostOrigin_HttpsLocalhost_ReturnsTrue()
        {
            // ACT & ASSERT - CHECK IF LOCALHOST ORIGIN IS TRUE
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("https://localhost"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("https://localhost:3000"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("https://localhost:8443"));
        }

        // TEST FOR ISLOCALHOSTORIGIN WITH 127.0.0.1
        [Fact]
        public void IsLocalhostOrigin_127001_ReturnsTrue()
        {
            // ACT & ASSERT - CHECK IF LOCALHOST ORIGIN IS TRUE
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("http://127.0.0.1"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("http://127.0.0.1:3000"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("https://127.0.0.1"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("https://127.0.0.1:8443"));
        }

        // TEST FOR ISLOCALHOSTORIGIN WITH CASE INSENSITIVITY
        [Fact]
        public void IsLocalhostOrigin_CaseInsensitive_ReturnsTrue()
        {
            // ACT & ASSERT - CHECK IF LOCALHOST ORIGIN IS TRUE
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("HTTP://LOCALHOST:3000"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("HTTPS://LOCALHOST:8080"));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("Http://Localhost:5000"));
        }

        // TEST FOR ISLOCALHOSTORIGIN WITH NON-LOCALHOST ORIGINS
        [Fact]
        public void IsLocalhostOrigin_NonLocalhost_ReturnsFalse()
        {
            // ACT & ASSERT - CHECK IF LOCALHOST ORIGIN IS FALSE
            Assert.False(EnvironmentUtils.IsLocalhostOrigin("https://example.com"));
            Assert.False(EnvironmentUtils.IsLocalhostOrigin("http://example.com:3000"));
            Assert.False(EnvironmentUtils.IsLocalhostOrigin("https://maxremy.dev"));
            Assert.False(EnvironmentUtils.IsLocalhostOrigin("https://keypops.app"));
            Assert.False(EnvironmentUtils.IsLocalhostOrigin("http://192.168.1.1"));
        }

        // TEST FOR ISLOCALHOSTORIGIN WITH NULL OR EMPTY
        [Fact]
        public void IsLocalhostOrigin_NullOrEmpty_ReturnsFalse()
        {
            // ACT & ASSERT - CHECK IF LOCALHOST ORIGIN IS FALSE
            Assert.False(EnvironmentUtils.IsLocalhostOrigin(null!));
            Assert.False(EnvironmentUtils.IsLocalhostOrigin(""));
            Assert.False(EnvironmentUtils.IsLocalhostOrigin("   "));
        }

        // TEST FOR ISLOCALHOSTORIGIN WITH WHITESPACE
        [Fact]
        public void IsLocalhostOrigin_Whitespace_TrimsAndReturnsTrue()
        {
            // ACT & ASSERT - CHECK IF LOCALHOST ORIGIN IS TRUE
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("  http://localhost:3000  "));
            Assert.True(EnvironmentUtils.IsLocalhostOrigin("  https://localhost  "));
        }
    }
}
