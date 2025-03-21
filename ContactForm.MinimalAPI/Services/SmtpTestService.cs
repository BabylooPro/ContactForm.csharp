using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContactForm.MinimalAPI.Services
{
    public class SmtpTestService : ISmtpTestService
    {
        private readonly ILogger<SmtpTestService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<SmtpConfig> _smtpConfigs;

        // CONSTRUCTOR
        public SmtpTestService(
            ILogger<SmtpTestService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider =
                serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            var configs = configuration
                .GetSection("SmtpSettings:Configurations")
                .Get<List<SmtpConfig>>();
            _smtpConfigs =
                configs
                ?? throw new InvalidOperationException(
                    "Failed to load SMTP configurations from settings"
                );
        }

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
                var smtpClient = scope.ServiceProvider.GetRequiredService<ISmtpClientWrapper>();
                var hasErrors = false;
                var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

                // TESTING CONNECTIONS FOR BOTH REGULAR AND TEST EMAILS
                foreach (var smtp in _smtpConfigs)
                {
                    // TEST REGULAR EMAIL CONFIGURATIONS
                    await TestSmtpConnection(smtp, smtpClient, results, false);

                    // TEST TEST EMAIL CONFIGURATIONS IF TEST EMAIL IS SPECIFIED
                    if (!string.IsNullOrEmpty(smtp.TestEmail))
                    {
                        await TestSmtpConnection(smtp, smtpClient, results, true);
                    }
                }

                // COMPLETE TESTS
                totalStopwatch.Stop();

                // CANCEL LOADING ANIMATION
                cts.Cancel();
                try
                {
                    await loadingTask;
                }
                catch { }

                Console.Write("\r".PadRight(50) + "\r");

                results.Sort(
                    (a, b) =>
                    {
                        if (a.success == b.success)
                            return a.duration.CompareTo(b.duration);
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
                catch { }
                Console.Write("\r".PadRight(50) + "\r"); // CLEAR LOADING LINE
            }
        }

        // METHODE TO TEST AN SMPT CONNECTIONS
        private async Task TestSmtpConnection(
            SmtpConfig smtp,
            ISmtpClientWrapper smtpClient,
            List<(bool success, string message, long duration)> results,
            bool isTestEmail
        )
        {
            var testStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var emailToUse = isTestEmail ? smtp.TestEmail : smtp.Email;
            var emailType = isTestEmail ? "TEST" : "REGULAR";
            var passwordVar = isTestEmail
                ? $"SMTP_{smtp.Index}_PASSWORD_TEST"
                : $"SMTP_{smtp.Index}_PASSWORD";

            try
            {
                // CONNECT TO SMTP
                await smtpClient.ConnectWithTokenAsync(
                    smtp.Host,
                    smtp.Port,
                    MailKit.Security.SecureSocketOptions.SslOnConnect,
                    CancellationToken.None
                );

                // GET PASSWORD FROM ENVIRONMENT VARIABLE
                var password = Environment.GetEnvironmentVariable(passwordVar);
                if (string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException(
                        $"{passwordVar} environment variable is missing"
                    );
                }

                // AUTHENTICATE WITH PASSWORD
                await smtpClient.AuthenticateWithTokenAsync(
                    emailToUse,
                    password,
                    CancellationToken.None
                );

                // DISCONNECT FROM SMTP
                await smtpClient.DisconnectWithTokenAsync(true, CancellationToken.None);
                testStopwatch.Stop();
                results.Add(
                    (
                        true,
                        $"SMTP_{smtp.Index} {emailType}: {smtp.Description} ({emailToUse})\nSMTP_{smtp.Index} {emailType} connection test: SUCCESS ({testStopwatch.ElapsedMilliseconds}ms)\n",
                        testStopwatch.ElapsedMilliseconds
                    )
                );
            }
            catch (Exception ex)
            {
                testStopwatch.Stop();
                results.Add(
                    (
                        false,
                        $"SMTP_{smtp.Index} {emailType}: {smtp.Description} ({emailToUse})\nSMTP_{smtp.Index} {emailType} connection test: FAILED ({testStopwatch.ElapsedMilliseconds}ms) - {ex.Message}\n",
                        testStopwatch.ElapsedMilliseconds
                    )
                );
            }
        }
    }
}
