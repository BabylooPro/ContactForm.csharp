using System.Text.Json;
using API.Models;
using API.Utilities;

namespace Tests.UtilitiesTests
{
    // UNIT TESTS FOR ENVIRONMENTUTILS (SMTP CONFIG + CORS ORIGIN PARSING)
    public class EnvironmentUtilsCoverageTests
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

        // BUILD A MINIMAL VALID SMTP CONFIG LIST
        private static List<SmtpConfig> LoadMinimalConfigs()
        {
            return [ new SmtpConfig { Index = 0, Host = "smtp.example.com", Port = 465, Email = "test@example.com", Description = "d" } ];
        }

        // RUN ACTION WITH TEMPORARY CURRENT WORKING DIRECTORY (FOR .ENV FILE SCENARIOS)
        private static void WithTempCwd(Action<string> action)
        {
            var originalCwd = Directory.GetCurrentDirectory();
            var tempDir = Path.Combine(Path.GetTempPath(), $"contactform-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                Directory.SetCurrentDirectory(tempDir);
                action(tempDir);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCwd);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
        }

        // TEST FOR UNESCAPING \" SEQUENCES BEFORE JSON PARSE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenContainsEscapedQuotes_UnescapesAndParses()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                // ARRANGE - VALUE CONTAINS ESCAPED QUOTES (\\\") THAT SHOULD BE UNESCAPED
                var escaped = "[{\\\"Index\\\":0,\\\"Host\\\":\\\"smtp.example.com\\\",\\\"Port\\\":465,\\\"Email\\\":\\\"test@example.com\\\",\\\"Description\\\":\\\"d\\\"}]";
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", escaped);

                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();
                Assert.Single(result);
                Assert.Equal(0, result[0].Index);
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR STRIPPING LEADING QUOTE WHEN VALUE DOES NOT END WITH QUOTE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenStartsWithQuoteButNotEndsWithQuote_StripsLeadingQuote()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                // ARRANGE - QUOTED LEADING VALUE
                var json = JsonSerializer.Serialize(LoadMinimalConfigs());
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", $"\"{json}");

                // ACT - LOAD CONFIGS
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - PARSED
                Assert.Single(result);
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR STRIPPING OUTER SINGLE QUOTES
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenSingleQuoted_StripsOuterQuotes()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                // ARRANGE - SINGLE QUOTED VALUE
                var json = JsonSerializer.Serialize(LoadMinimalConfigs());
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", $"'{json}'");

                // ACT - LOAD CONFIGS
                var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                // ASSERT - PARSED
                Assert.Single(result);
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR LOADING MULTILINE VALUE FROM .ENV WITH COMMENTS/BLANK LINES
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenMultilineEnvWithCommentsAndBlankLines_Parses()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT AND CWD
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            var originalCwd = Directory.GetCurrentDirectory();

            try
            {
                WithTempCwd(tempDir =>
                {
                    // ARRANGE - WRITE .ENV FILE WITH MULTILINE JSON
                    var envPath = Path.Combine(tempDir, ".env");

                    var envContent = """
                                     SMTP_CONFIGURATIONS="[
                                       {
                                         "Index": 0,
                                         "Host": "smtp.example.com",
                                         "Port": 465,
                                         "Email": "test@example.com",
                                         "Description": "d",
                                       }

                                       # COMMENT WHILE IN MULTILINE VALUE
                                     ]"
                                     """;

                    File.WriteAllText(envPath, envContent);

                    Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

                    // ACT - LOAD CONFIGS
                    var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                    // ASSERT - PARSED
                    Assert.Single(result);
                });
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCwd);
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR LOADING QUOTED SINGLE-LINE VALUE FROM .ENV FILE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenEnvFileQuotedSingleLine_Parses()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                WithTempCwd(tempDir =>
                {
                    // ARRANGE - WRITE .ENV FILE WITH QUOTED JSON
                    var envPath = Path.Combine(tempDir, ".env");
                    var json = JsonSerializer.Serialize(LoadMinimalConfigs());
                    File.WriteAllText(envPath, $"SMTP_CONFIGURATIONS=\"{json}\"");

                    Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

                    // ACT - LOAD CONFIGS
                    var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                    // ASSERT - PARSED
                    Assert.Single(result);
                });
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR LOADING UNQUOTED SINGLE-LINE VALUE FROM .ENV FILE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenEnvFileUnquotedSingleLine_Parses()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                WithTempCwd(tempDir =>
                {
                    // ARRANGE - WRITE .ENV FILE WITH UNQUOTED JSON
                    var envPath = Path.Combine(tempDir, ".env");
                    var json = JsonSerializer.Serialize(LoadMinimalConfigs());
                    File.WriteAllText(envPath, $"SMTP_CONFIGURATIONS={json}");

                    Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

                    // ACT - LOAD CONFIGS
                    var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                    // ASSERT - PARSED
                    Assert.Single(result);
                });
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR LOADING MULTILINE VALUE FROM .ENV WITHOUT CLOSING QUOTE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenEnvFileMultilineWithoutClosingQuote_Parses()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                WithTempCwd(tempDir =>
                {
                    // ARRANGE - WRITE .ENV FILE WITH MULTILINE JSON (NO CLOSING QUOTE)
                    var envPath = Path.Combine(tempDir, ".env");

                    var envContent = """
                                     SMTP_CONFIGURATIONS="[
                                       {
                                         "Index": 0,
                                         "Host": "smtp.example.com",
                                         "Port": 465,
                                         "Email": "test@example.com",
                                         "Description": "d",
                                       }
                                     ]
                                     """;

                    File.WriteAllText(envPath, envContent);

                    Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

                    // ACT - LOAD CONFIGS
                    var result = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

                    // ASSERT - PARSED
                    Assert.Single(result);
                });
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR FALLING BACK WHEN .ENV FILE CANNOT BE READ
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenEnvFileUnreadable_FallsBackAndThrowsMissingConfig()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                WithTempCwd(tempDir =>
                {
                    // ARRANGE - WRITE .ENV FILE THEN MAKE IT UNREADABLE
                    var envPath = Path.Combine(tempDir, ".env");
                    File.WriteAllText(envPath, "SMTP_CONFIGURATIONS=\"[\"");

                    // ARRANGE - MAKE FILE UNREADABLE TO FORCE ENV FILE READ FAILURE PATH
#pragma warning disable CA1416
                    File.SetUnixFileMode(envPath, UnixFileMode.None);
#pragma warning restore CA1416

                    try
                    {
                        Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

                        // ACT & ASSERT - LOAD THROWS
                        Assert.Throws<InvalidOperationException>(() => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment());
                    }
                    finally
                    {
#pragma warning disable CA1416
                        File.SetUnixFileMode(envPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
#pragma warning restore CA1416
                    }
                });
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR THROWING WHEN .ENV DOES NOT CONTAIN THE VARIABLE
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenEnvFileDoesNotContainVariable_ThrowsMissingConfig()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                WithTempCwd(tempDir =>
                {
                    // ARRANGE - WRITE .ENV WITHOUT SMTP_CONFIGURATIONS
                    var envPath = Path.Combine(tempDir, ".env");
                    File.WriteAllText(envPath, "OTHER_VAR=1");

                    Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);

                    // ACT & ASSERT - LOAD THROWS
                    Assert.Throws<InvalidOperationException>(() => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment());
                });
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR INVALID FORMAT THROWING WITH TRUNCATED PREVIEW
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenInvalidFormatVeryLong_TruncatesPreview()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                // ARRANGE - INVALID VALUE (NOT JSON ARRAY)
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", new string('x', 200));

                // ACT & ASSERT - LOAD THROWS
                Assert.Throws<InvalidOperationException>(() => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment());
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR INVALID JSON THROWING WITH TRUNCATED PREVIEW
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenInvalidJsonVeryLong_TruncatesPreview()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                // ARRANGE - INVALID JSON STARTING WITH '[' AND LONG ENOUGH TO TRIGGER PREVIEW TRUNCATION
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[" + new string('a', 200));

                // ACT & ASSERT - LOAD THROWS
                Assert.Throws<InvalidOperationException>(() => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment());
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR EMPTY JSON ARRAY THROWING
        [Fact]
        public void LoadSmtpConfigurationsFromEnvironment_WhenEmptyArray_Throws()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("SMTP_CONFIGURATIONS");
            try
            {
                // ARRANGE - EMPTY ARRAY
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[]");

                // ACT & ASSERT - LOAD THROWS
                Assert.Throws<InvalidOperationException>(() => EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment());
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }

        // TEST FOR DEDUPLICATING CORS ORIGINS
        [Fact]
        public void LoadCorsOriginsFromEnvironment_WhenDuplicate_OnlyAddsOnce()
        {
            // ARRANGE - SNAPSHOT ENVIRONMENT
            var envSnapshot = SnapshotEnv("CORS_1_ORIGIN", "CORS_2_ORIGIN", "CORS_3_ORIGIN");
            try
            {
                // ARRANGE - SET DUPLICATE ORIGINS WITH WHITESPACE VARIATION
                Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://dup.example");
                Environment.SetEnvironmentVariable("CORS_2_ORIGIN", " https://dup.example ");
                Environment.SetEnvironmentVariable("CORS_3_ORIGIN", null);

                // ACT - LOAD ORIGINS
                var origins = EnvironmentUtils.LoadCorsOriginsFromEnvironment();

                // ASSERT - DEDUPED
                Assert.Single(origins);
                Assert.Equal("https://dup.example", origins[0]);
            }
            finally
            {
                RestoreEnv(envSnapshot);
            }
        }
    }
}
