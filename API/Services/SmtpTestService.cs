using API.Interfaces;
using API.Models;
using API.Utilities;

namespace API.Services
{
    public class SmtpTestService(ILogger<SmtpTestService> logger, IServiceProvider serviceProvider) : ISmtpTestService
    {
        private readonly ILogger<SmtpTestService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly List<SmtpConfig> _smtpConfigs = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();

        // TEST SMTP CONNECTIONS
        public async Task TestSmtpConnections()
        {
            // CANCEL TOKEN
            using var cts = new CancellationTokenSource();
            var results = new List<(bool success, string message, long duration)>();

            // LOADING ANIMATION
            var loadingTask = Task.Run(
                async () =>
                {
                    var counter = 0;
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var dots = new string('.', (counter % 3) + 1).PadRight(3);
                        Console.Write($"\rSMTP TESTING IN PROGRESS{dots}   ");
                        counter++;
                        await Task.Delay(500, cts.Token);
                    }
                },
                cts.Token
            );

            // TESTING CONNECTIONS WITH SCOPE
            try
            {
                // CREATE SCOPE
                using var scope = _serviceProvider.CreateScope();
                var hasErrors = false;
                var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var shuffledConfigs = new List<(SmtpConfig config, bool isTest)>();

                // TESTING CONNECTIONS FOR BOTH REGULAR AND TEST EMAILS
                foreach (var smtp in _smtpConfigs)
                {
                    shuffledConfigs.Add((smtp, false)); // REGULAR EMAIL

                    if (!string.IsNullOrEmpty(smtp.TestEmail))
                    {
                        shuffledConfigs.Add((smtp, true)); // TEST EMAIL
                    }
                }

                // SHUFFLE CONFIG TO AVOID PATTERNS IN TESTING
                shuffledConfigs = [.. shuffledConfigs.OrderBy(_ => Guid.NewGuid())];

                // TEST EACH CONFIG INDEPENDENTLY
                foreach (var (config, isTest) in shuffledConfigs)
                {
                    // CREATE NEW CLIENT FOR EACH TEST
                    using var smtpClient = new Services.SmtpClientWrapper();
                    await TestSmtpConnection(config, smtpClient, results, isTest);
                }

                // COMPLETE TESTS
                totalStopwatch.Stop();

                // CANCEL LOADING ANIMATION
                cts.Cancel();
                try
                {
                    await loadingTask;
                }
                catch {
                    // IGNORE ERROR
                }

                Console.Write("\r".PadRight(50) + "\r");

                results.Sort(
                    (a, b) =>
                    {
                        if (a.success == b.success) return a.duration.CompareTo(b.duration);
                        return a.success ? -1 : 1;
                    }
                );

                // PRINT RESULTS
                Console.WriteLine("\n---------- SMTP TEST RESULTS ----------");

                foreach (var (success, message, _) in results)
                {
                    if (success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(message);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(message);
                        hasErrors = true;
                    }
                    Console.ResetColor();
                }
                Console.WriteLine($"Total test duration: {totalStopwatch.ElapsedMilliseconds}ms\n");
                Console.WriteLine("---------------------------------------\n");

                // THROW ERROR IF TEST FAILED
                if (hasErrors)
                {
                    throw new InvalidOperationException("SMTP test failed");
                }
            }
            finally
            {
                cts.Cancel();
                try
                {
                    await loadingTask;
                }
                catch
                {
                    // IGNORE ERROR
                }
                Console.Write("\r".PadRight(50) + "\r"); // CLEAR LOADING LINE
            }
        }

        // METHODE TO TEST AN SMPT CONNECTIONS
        private async Task TestSmtpConnection(SmtpConfig smtp, SmtpClientWrapper smtpClient, List<(bool success, string message, long duration)> results, bool isTestEmail)
        {
            var testStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var emailToUse = isTestEmail ? smtp.TestEmail : smtp.Email;
            var emailType = isTestEmail ? "TEST" : "REGULAR";
            var passwordVar = isTestEmail ? $"SMTP_{smtp.Index}_PASSWORD_TEST" : $"SMTP_{smtp.Index}_PASSWORD";

            try
            {
                // CONNECT TO SMTP
                await smtpClient.ConnectWithTokenAsync(smtp.Host, smtp.Port, MailKit.Security.SecureSocketOptions.SslOnConnect, CancellationToken.None);

                // GET PASSWORD FROM ENVIRONMENT VARIABLE
                var password = Environment.GetEnvironmentVariable(passwordVar);
                if (string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException($"{passwordVar} environment variable is missing");
                }

                // AUTHENTICATE WITH PASSWORD
                await smtpClient.AuthenticateWithTokenAsync(emailToUse, password, CancellationToken.None);

                // DISCONNECT FROM SMTP
                await smtpClient.DisconnectWithTokenAsync(true, CancellationToken.None);
                testStopwatch.Stop();
                results.Add((
                    true,
                    $"SMTP_{smtp.Index} {emailType}: {smtp.Description} ({emailToUse})\nSMTP_{smtp.Index} {emailType} connection test: SUCCESS ({testStopwatch.ElapsedMilliseconds}ms)\n",
                    testStopwatch.ElapsedMilliseconds
                ));
            }
            catch (Exception ex)
            {
                // ENSURE DISCONNECTION IF CONNECTED
                try
                {
                    if (smtpClient.IsConnected)
                    {
                        await smtpClient.DisconnectWithTokenAsync(true, CancellationToken.None);
                    }
                }
                catch
                {
                    // IGNORE DISCONNECTION ERRORS DURING CLEANUP
                }

                // STOP STOPWATCH
                testStopwatch.Stop();

                // CREATE USER-FRIENDLY ERROR MESSAGE
                string userFriendlyError = GetUserFriendlyErrorMessage(ex, smtp, emailToUse);

                results.Add((
                    false,
                    $"SMTP_{smtp.Index} {emailType}: {smtp.Description} ({emailToUse})\nSMTP_{smtp.Index} {emailType} connection test: FAILED ({testStopwatch.ElapsedMilliseconds}ms)\n Error: {userFriendlyError}\n",
                    testStopwatch.ElapsedMilliseconds
                ));

                // LOG DETAILED ERROR FOR DEBUGGING
                _logger.LogDebug(ex, "SMTP connection test failed for {EmailToUse} on {Host}:{Port}", emailToUse, smtp.Host, smtp.Port);
            }
        }

        // GET USER-FRIENDLY ERROR MESSAGE BASED ON EXECPETION TYPE AND CONTENT
        private static string GetUserFriendlyErrorMessage(Exception ex, SmtpConfig config, string emailAdress)
        {
            string exMessage = ex.Message;

            // AUTHENTICATION FAILURE (535)
            if (exMessage.Contains("535") && exMessage.Contains("authentication failed"))
            {
                return $"Authentication failed. Email address ({emailAdress}) or password incorrect. Password environment variable should be verified.";
            }

            // AUTHENTICATION FAILURE (GENERAL)
            if (exMessage.Contains("authentication") || exMessage.Contains("535") || exMessage.Contains("530") || exMessage.Contains("534"))
            {
                return $"Authentication failed. Email address ({emailAdress}) or password incorrect. Verify credentials and password environment variable.";
            }

            // CONNECTION REFUSED OR TIMEOUT
            if (ex is System.Net.Sockets.SocketException || exMessage.Contains("connection refused") || exMessage.Contains("timeout") || exMessage.Contains("timed out"))
            {
                return $"Connection failed to {config.Host}:{config.Port}. Check if host and port are correct, firewall settings, and network connectivity.";
            }

            // SSL/TLS CERTIFICATE ERRORS
            if (exMessage.Contains("certificate") || exMessage.Contains("SSL") || exMessage.Contains("TLS") || exMessage.Contains("handshake"))
            {
                return $"SSL/TLS connection error. Verify SSL/TLS settings for {config.Host}:{config.Port}. Certificate validation may be required.";
            }

            // SERVICE NOT AVAILABLE
            if (exMessage.Contains("service not available") || exMessage.Contains("550") || exMessage.Contains("421"))
            {
                return $"SMTP service not available on {config.Host}:{config.Port}. Server may be down or rejecting connections.";
            }

            // INVALID HOST OR PORT
            if (exMessage.Contains("host") && (exMessage.Contains("invalid") || exMessage.Contains("not found") || exMessage.Contains("unreachable")))
            {
                return $"Invalid host or port configuration. Verify SMTP_{config.Index}_HOST and SMTP_{config.Index}_PORT environment variables.";
            }

            // NETWORK ERRORS
            if (exMessage.Contains("network") || exMessage.Contains("unreachable") || exMessage.Contains("no route"))
            {
                return $"Network error connecting to {config.Host}:{config.Port}. Check network connectivity and DNS resolution.";
            }

            // MISSING ENVIRONMENT VARIABLE (ALREADY HANDLED IN CALLER, BUT ADD AS FALLBACK)
            if (ex is InvalidOperationException && exMessage.Contains("environment variable"))
            {
                return exMessage;
            }

            return $"{exMessage} - Server settings and network connection should be checked.";
        }
    }
}
