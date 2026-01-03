using System.Text.Json;
using System.Text.RegularExpressions;
using ContactForm.MinimalAPI.Models;

namespace ContactForm.MinimalAPI.Utilities
{
    // UTILITY CLASS FOR ENVIRONMENT VARIABLES
    public static partial class EnvironmentUtils
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private static readonly char[] SplitSeparator = ['='];

        // CHECK FOR MISSING ENVIRONMENT VARIABLES
        public static List<string> CheckMissingEnvironmentVariables(params string[] variableNames)
        {
            var missingVariables = new List<string>(); // LIST FOR STORING MISSING VARIABLES

            // CHECKING FOR MISSING ENVIRONMENT VARIABLES
            foreach (var name in variableNames)
            {
                var variableValue = Environment.GetEnvironmentVariable(name); // GETTING ENVIRONMENT VARIABLE VALUE

                // ADDING MISSING VARIABLE TO THE LIST
                if (string.IsNullOrWhiteSpace(variableValue))
                {
                    missingVariables.Add(name);
                }
            }

            return missingVariables; // RETURNING MISSING VARIABLES
        }

        // CREATE MISSING SMTP CONFIGURATIONS EXCEPTION
        private static InvalidOperationException CreateMissingSmtpConfigurationsException(string envVarName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var baseDirectory = AppContext.BaseDirectory;
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var searchedEnvFiles = new[]
            {
                Path.Combine(currentDirectory, ".env"),
                Path.Combine(baseDirectory, ".env"),
                Path.Combine(userProfile, ".env")
            };

            var existingEnvFiles = searchedEnvFiles.Where(File.Exists).ToList();

            var nl = Environment.NewLine;

            var locations = string.Join(nl, new List<string>
            {
                "Fix it by defining the variable in one of these places:",
                $"- launchSettings.json: ContactForm.MinimalAPI/Properties/launchSettings.json (environmentVariables section)",
                $"- .env file: {currentDirectory}{Path.DirectorySeparatorChar}.env",
                $"- OS environment: set it in your shell/session before running the app"
            });

            var searched = existingEnvFiles.Count > 0
                ? $"Detected .env file(s):{nl}- {string.Join($"{nl}- ", existingEnvFiles)}"
                : $"No .env file found in:{nl}- {string.Join($"{nl}- ", searchedEnvFiles)}";

            var message =
                $"Missing required configuration: {envVarName}{nl}{nl}" +
                "What is expected:" + nl +
                "- A JSON array of SMTP configuration objects" + nl +
                "- The value must be non-empty and start with '['" + nl + nl +
                locations + nl + nl +
                "Notes:" + nl +
                "- If you changed environment variables, restart the process so it picks them up" + nl +
                "- If you store JSON in a .env file, ensure it is a valid JSON array and not truncated" + nl + nl +
                "Diagnostics:" + nl +
                $"- Current working directory: {currentDirectory}{nl}" +
                $"- {searched}";

            return new InvalidOperationException(message);
        }

        // CREATE INVALID SMTP CONFIGURATIONS FORMAT EXCEPTION
        private static InvalidOperationException CreateInvalidSmtpConfigurationsFormatException(string envVarName, string originalValue, string processedValue)
        {
            var preview = processedValue.Length > 80 ? string.Concat(processedValue.AsSpan(0, 80), "...") : processedValue;
            var nl = Environment.NewLine;

            var message =
                $"Invalid configuration format: {envVarName}{nl}{nl}" +
                "What happened:" + nl +
                "- The value does not look like a JSON array (it must start with '[')" + nl + nl +
                "How to fix:" + nl +
                "- Ensure the value is a JSON array (not a single object, not wrapped in extra quotes)" + nl +
                "- Ensure the value is not truncated when stored in launchSettings.json or .env" + nl + nl +
                "Diagnostics:" + nl +
                $"- Value preview: {preview}{nl}" +
                $"- Original length: {originalValue.Length}{nl}" +
                $"- Processed length: {processedValue.Length}";

            return new InvalidOperationException(message);
        }

        // CREATE SMTP CONFIGURATIONS JSON PARSE EXCEPTION
        private static InvalidOperationException CreateSmtpConfigurationsJsonParseException(string envVarName, string processedValue, JsonException ex)
        {
            var preview = processedValue.Length > 140 ? string.Concat(processedValue.AsSpan(0, 140), "...") : processedValue;
            var nl = Environment.NewLine;

            var message =
                $"Failed to parse configuration: {envVarName}{nl}{nl}" +
                "What happened:" + nl +
                "- The value was found but could not be parsed as JSON" + nl + nl +
                "How to fix:" + nl +
                "- Validate the JSON is a valid array and uses double quotes for property names/strings" + nl +
                "- Check escaping/quoting if storing JSON inside a .env or launchSettings.json" + nl + nl +
                "Diagnostics:" + nl +
                $"- JSON preview: {preview}{nl}" +
                $"- Parser error: {ex.Message}";

            return new InvalidOperationException(message, ex);
        }

        // LOAD SMTP CONFIGURATIONS FROM ENVIRONMENT VARIABLE
        public static List<SmtpConfig> LoadSmtpConfigurationsFromEnvironment()
        {
            const string envVarName = "SMTP_CONFIGURATIONS";
            var jsonValue = Environment.GetEnvironmentVariable(envVarName);

            // IF ENVIRONMENT VARIABLE IS MISSING OR APPEARS INCOMPLETE, TRY TO LOAD IT FROM .ENV FILE; OTHERWISE, THROW AN EXCEPTION IF IT IS STILL MISSING
            if (string.IsNullOrWhiteSpace(jsonValue) || (jsonValue.StartsWith('"') && !jsonValue.EndsWith('"') && jsonValue.Length < 10))
            {
                jsonValue = LoadMultilineValueFromEnvFile(envVarName) ?? jsonValue;

                if (string.IsNullOrWhiteSpace(jsonValue))
                {
                    throw CreateMissingSmtpConfigurationsException(envVarName);
                }
            }

            // CLEAN UP JSON: TRIM WHITESPACE AND HANDLE MULTILINE JSON
            var originalValue = jsonValue;
            jsonValue = jsonValue.Trim();

            if (jsonValue.Contains("\\\""))
            {
                jsonValue = jsonValue.Replace("\\\"", "\"");
            }

            if (jsonValue.StartsWith('"') && !jsonValue.EndsWith('"'))
            {
                jsonValue = jsonValue[1..];
            }
            else if (jsonValue.StartsWith('"') && jsonValue.EndsWith('"') && jsonValue.Length > 1)
            {
                jsonValue = jsonValue[1..^1];
                jsonValue = jsonValue.Replace("\\\"", "\"");
            }
            else if (jsonValue.StartsWith('\'') && jsonValue.EndsWith('\'') && jsonValue.Length > 1)
            {
                jsonValue = jsonValue[1..^1];
            }

            // NORMALIZE LINE ENDINGS (CONVERT ALL TO SPACES)
            jsonValue = jsonValue
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");

            // REMOVE EXTRA WHITESPACE (MULTIPLE SPACES/TABS TO SINGLE SPACE)
            jsonValue = WhitespaceRegex().Replace(jsonValue, " ");

            jsonValue = jsonValue.Trim();

            // VALIDATE THAT JSON STARTS WITH [ (ARRAY)
            if (!jsonValue.StartsWith('['))
            {
                throw CreateInvalidSmtpConfigurationsFormatException(envVarName, originalValue, jsonValue);
            }

            // TRY TO PARSE JSON VALUE AS LIST OF SMTP CONFIGURATIONS
            try
            {
                var configurations = JsonSerializer.Deserialize<List<SmtpConfig>>(jsonValue, JsonSerializerOptions);

                if (configurations == null || configurations.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid configuration: {envVarName}{Environment.NewLine}{Environment.NewLine}" +
                        "- The JSON array was parsed but produced no SMTP configurations" + Environment.NewLine +
                        "- Ensure the array is not empty and matches the expected schema"
                    );
                }

                return configurations;
            }
            catch (JsonException ex)
            {
                throw CreateSmtpConfigurationsJsonParseException(envVarName, jsonValue, ex);
            }
        }

        // LOAD MULTILINE VALUE FROM .ENV FILE DIRECTLY
        // THIS HANDLES CASES WHERE DOTENV.NET DOESN'T PROPERLY LOAD MULTILINE VALUES
        private static string? LoadMultilineValueFromEnvFile(string variableName)
        {
            try
            {
                var envFilePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                    Path.Combine(AppContext.BaseDirectory, ".env"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".env")
                };

                foreach (var envFilePath in envFilePaths)
                {
                    if (!File.Exists(envFilePath)) continue;

                    var lines = File.ReadAllLines(envFilePath);
                    var inMultilineValue = false;
                    var currentKey = string.Empty;
                    var valueBuilder = new System.Text.StringBuilder();

                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();

                        // SKIP COMMENTS AND EMPTY LINES
                        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
                        {
                            if (inMultilineValue) valueBuilder.AppendLine();
                            continue;
                        }

                        // CHECK IF THIS LINE DEFINES THE TARGET VARIABLE
                        if (!inMultilineValue && trimmedLine.StartsWith($"{variableName}=", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmedLine.Split(SplitSeparator, 2);
                            if (parts.Length == 2)
                            {
                                currentKey = parts[0];
                                var value = parts[1];

                                // CHECK IF VALUE STARTS WITH QUOTE (MULTILINE)
                                if (value.StartsWith('"'))
                                {
                                    inMultilineValue = true;
                                    valueBuilder.Append(value);

                                    // IF VALUE ENDS WITH QUOTE ON SAME LINE, IT'S NOT MULTILINE
                                    if (value.EndsWith('"') && value.Length > 1)
                                    {
                                        inMultilineValue = false;
                                        return valueBuilder.ToString();
                                    }
                                }
                                else
                                {
                                    return value; // SINGLE LINE VALUE
                                }
                            }
                        }
                        else if (inMultilineValue)
                        {
                            valueBuilder.Append(line); // CONTINUATION OF MULTILINE VALUE

                            // IF VALUE ENDS WITH QUOTE ON SAME LINE, IT'S NOT MULTILINE
                            if (line.TrimEnd().EndsWith('"'))
                            {
                                inMultilineValue = false;
                                return valueBuilder.ToString();
                            }
                            else
                            {
                                valueBuilder.AppendLine();
                            }
                        }
                    }

                    // IF MULTILINE VALUE WAS STARTED BUT NO CLOSING QUOTE WAS FOUND, RETURN ACCUMULATED VALUE
                    if (inMultilineValue && valueBuilder.Length > 0) return valueBuilder.ToString();
                }
            }
            catch
            {
                // IGNORE ERRORS AND FALL BACK TO ENVIRONMENT VARIABLE
            }

            return null;
        }

        // BUILD AND CONFIGURE SMTP SETTINGS FROM ENVIRONMENT VARIABLES
        public static void ConfigureSmtpSettings(IServiceCollection services, List<SmtpConfig> configurations)
        {
            // GET RECEPTION AND CATCHALL EMAILS FROM ENVIRONMENT VARIABLES
            var receptionEmail = Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL");
            var catchAllEmail = Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL");
            if (string.IsNullOrWhiteSpace(receptionEmail)) throw new InvalidOperationException("Environment variable 'SMTP_RECEPTION_EMAIL' is required");
            if (string.IsNullOrWhiteSpace(catchAllEmail)) throw new InvalidOperationException("Environment variable 'SMTP_CATCHALL_EMAIL' is required");

            // BUILD SMTP SETTINGS FROM ENVIRONMENT
            var smtpSettings = new SmtpSettings
            {
                Configurations = configurations,
                ReceptionEmail = receptionEmail,
                CatchAllEmail = catchAllEmail
            };

            // CONFIGURE SMTP SETTINGS
            services.Configure<SmtpSettings>(options =>
            {
                options.Configurations = smtpSettings.Configurations;
                options.ReceptionEmail = smtpSettings.ReceptionEmail;
                options.CatchAllEmail = smtpSettings.CatchAllEmail;
            });
        }

        // LOAD CORS ORIGINS FROM CORS_{INDEX}_ORIGIN ENVIRONMENT VARIABLES AND INCLUDE ALL LOCALHOST URLS
        public static List<string> LoadCorsOriginsFromEnvironment()
        {
            var origins = new List<string>();
            var index = 1;

            // LOAD ORIGINS FROM ENVIRONMENT VARIABLES USING INDEXED PATTERN
            while (true)
            {
                var envVarName = $"CORS_{index}_ORIGIN";
                var origin = Environment.GetEnvironmentVariable(envVarName);

                if (string.IsNullOrWhiteSpace(origin))
                {
                    break; // NO MORE ORIGINS FOUND - STOP LOOPING
                }

                // ADD ORIGIN IF NOT EMPTY AND NOT ALREADY IN LIST
                var trimmedOrigin = origin.Trim();
                if (!string.IsNullOrEmpty(trimmedOrigin) && !origins.Contains(trimmedOrigin))
                {
                    origins.Add(trimmedOrigin);
                }

                index++;
            }

            // ADD LOCALHOST ORIGINS AUTOMATICALLY IF NOT PRESENT
            var hasLocalhost = origins.Any(o => 
                o.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
                o.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase) ||
                o.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                o.StartsWith("https://127.0.0.1", StringComparison.OrdinalIgnoreCase));

            // IF NO LOCALHOST ORIGIN EXISTS, ADD A WILDCARD PATTERN FOR LOCALHOST
            // INFO: CORS DOESN'T SUPPORT WILDCARDS IN ORIGINS, SO USE SETISORIGINALLOWED
            // INFO: BUT FOR NOW, ADD COMMON LOCALHOST PATTERNS
            // INFO: THE ACTUAL IMPLEMENTATION WILL USE SETISORIGINALLOWED IN CORS CONFIGURATION

            return origins;
        }

        // CHECK IF AN ORIGIN IS A LOCALHOST ORIGIN (ANY PORT)
        public static bool IsLocalhostOrigin(string origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;

            var uri = origin.Trim();
            return uri.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
                   uri.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase) ||
                   uri.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   uri.StartsWith("https://127.0.0.1", StringComparison.OrdinalIgnoreCase);
        }

        [GeneratedRegex(@"[ \t]+")]
        private static partial Regex WhitespaceRegex();
    }
}
