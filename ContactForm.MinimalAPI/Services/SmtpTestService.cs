using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContactForm.MinimalAPI.Interfaces;
using ContactForm.MinimalAPI.Models;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            var configs = configuration.GetSection("SmtpSettings:Configurations").Get<List<SmtpConfig>>();
            _smtpConfigs = configs ?? throw new InvalidOperationException("Failed to load SMTP configurations from settings");
        }

        // TEST SMTP CONNECTIONS
        public async Task TestSmtpConnections()
        {
            // CANCEL TOKEN
            using var cts = new CancellationTokenSource();
            var results = new List<(bool success, string message, long duration)>();
            
            // LOADING ANIMATION
            var loadingTask = Task.Run(async () =>
            {
                var counter = 0;
                while (!cts.Token.IsCancellationRequested)
                {
                    var dots = new string('.', (counter % 3) + 1).PadRight(3);
                    Console.Write($"\rSMTP TESTING IN PROGRESS{dots}   ");
                    counter++;
                    await Task.Delay(500, cts.Token);
                }
            }, cts.Token);

            // TESTING CONNECTIONS WITH SCOPE
            try 
            {
                // CREATE SCOPE
                using var scope = _serviceProvider.CreateScope();
                var smtpClient = scope.ServiceProvider.GetRequiredService<ISmtpClientWrapper>();
                var hasErrors = false;
                var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

                // TESTING CONNECTIONS
                foreach (var smtp in _smtpConfigs)
                {
                    var testStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
                    try
                    {
                        // CONNECT TO SMTP
                        await smtpClient.ConnectWithTokenAsync(
                            smtp.Host, 
                            smtp.Port, 
                            SecureSocketOptions.SslOnConnect,
                            CancellationToken.None
                        );
                        
                        // GET PASSWORD FROM ENVIRONMENT VARIABLE
                        var password = Environment.GetEnvironmentVariable($"SMTP_{smtp.Index}_PASSWORD");
                        if (string.IsNullOrEmpty(password))
                        {
                            throw new InvalidOperationException($"SMTP_{smtp.Index}_PASSWORD environment variable is missing");
                        }
                        
                        // AUTHENTICATE WITH PASSWORD
                        await smtpClient.AuthenticateWithTokenAsync(
                            smtp.Email,
                            password,
                            CancellationToken.None
                        );
                        
                        // DISCONNECT FROM SMTP
                        await smtpClient.DisconnectWithTokenAsync(true, CancellationToken.None);
                        testStopwatch.Stop();
                        results.Add((true, $"SMTP_{smtp.Index}: {smtp.Description} ({smtp.Email})\nSMTP_{smtp.Index} connection test: SUCCESS ({testStopwatch.ElapsedMilliseconds}ms)\n", testStopwatch.ElapsedMilliseconds));
                    }
                    catch (Exception ex)
                    {
                        // STOP STOPWATCH
                        testStopwatch.Stop();
                        hasErrors = true;
                        results.Add((false, $"SMTP_{smtp.Index}: {smtp.Description} ({smtp.Email})\nSMTP_{smtp.Index} connection test: FAILED ({testStopwatch.ElapsedMilliseconds}ms) - {ex.Message}\n", testStopwatch.ElapsedMilliseconds));
                    }
                }

                totalStopwatch.Stop(); 
                
                // CLEAR LOADING LINE AND SHOW RESULTS
                Console.Write("\r" + new string(' ', 50) + "\r");
                Console.WriteLine("\nAVAILABLE SMTP CONFIGURATIONS WITH TESTING CONNECTIONS:\n");
                foreach (var result in results)
                {
                    if (result.success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    Console.WriteLine(result.message);
                    Console.ResetColor();
                }
                Console.WriteLine($"Total test duration: {totalStopwatch.ElapsedMilliseconds}ms\n");

                // THROW ERROR IF TEST FAILED
                if (hasErrors)
                {
                    throw new InvalidOperationException("SMTP test failed");
                }
            }
            finally
            {
                cts.Cancel();
                await Task.Delay(50); // GIVE TIME FOR LOADING ANIMATION TO CLEAR
                Console.Write("\r" + new string(' ', 50) + "\r"); // CLEAR LOADING LINE
            }
        }
    }
} 
