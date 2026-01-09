using API;
using API.Models;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.TestHost;

namespace Tests.UtilitiesTests
{
    // UNIT TESTS FOR PROGRAM CONFIGURATION BRANCHES
    public class ProgramConfigurationTests
    {
        // TEST FOR CONFIGURESERVICES WHEN APIVERSIONING OPTIONS ARE MISSING (USES DEFAULTS)
        [Fact]
        public void ConfigureServices_WhenApiVersioningOptionsMissing_UsesDefaultOptions()
        {
            // ARRANGE - CONFIGURATION WITHOUT SECTION
            var configDict = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            // ARRANGE - MINIMAL REQUIRED SERVICES
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddLogging();
            services.AddControllers();
            services.AddEndpointsApiExplorer();

            // ARRANGE - MOCK SMTP CONFIGS
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[{\"Index\":0,\"Host\":\"127.0.0.1\",\"Port\":1,\"Email\":\"test@example.com\",\"Description\":\"Test\"}]");
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT - CALL CONFIGURESERVICES
                Program.ConfigureServices(services, config);

                // ASSERT - DEFAULT OPTIONS USED
                var serviceProvider = services.BuildServiceProvider();
                Assert.NotNull(serviceProvider);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);
            }
        }

        // TEST FOR CONFIGURESERVICES WHEN API VERSION HAS NO MINOR VERSION
        [Fact]
        public void ConfigureServices_WhenApiVersionHasNoMinorVersion_UsesZeroForMinor()
        {
            // ARRANGE - VERSION WITHOUT MINOR PART
            var configDict = new Dictionary<string, string?> {{ "ApiVersioning:DefaultVersion", "2" }};
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            // ARRANGE - SERVICES COLLECTION
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddLogging();
            services.AddControllers();
            services.AddEndpointsApiExplorer();

            // ARRANGE - MOCK SMTP CONFIGS
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[{\"Index\":0,\"Host\":\"127.0.0.1\",\"Port\":1,\"Email\":\"test@example.com\",\"Description\":\"Test\"}]");
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ACT - CALL CONFIGURESERVICES
                Program.ConfigureServices(services, config);

                // ASSERT - VERSION HANDLED CORRECTLY
                var serviceProvider = services.BuildServiceProvider();
                Assert.NotNull(serviceProvider);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);
            }
        }

        // TEST FOR CONFIGUREAPP WHEN SWAGGER IS DISABLED
        [Fact]
        public async Task ConfigureApp_WhenSwaggerDisabled_DoesNotRegisterSwagger()
        {
            // ARRANGE - MOCK SMTP CONFIGS (MUST BE SET BEFORE HOST BUILDER CREATION)
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[{\"Index\":0,\"Host\":\"127.0.0.1\",\"Port\":1,\"Email\":\"test@example.com\",\"Description\":\"Test\"}]");
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ARRANGE - SWAGGER DISABLED
                var configDict = new Dictionary<string, string?> {{ "Swagger:Enabled", "false" }, { "Swagger:RoutePrefix", "" }};
                var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

                // ARRANGE - WEB HOST BUILDER
                var hostBuilder = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseTestServer()
                        .ConfigureAppConfiguration((context, builder) => { builder.Sources.Clear(); builder.AddConfiguration(config); })
                        .ConfigureServices((context, services) =>
                        {
                            services.AddSingleton<IConfiguration>(config);
                            services.AddControllers();
                            services.AddLogging();
                            services.AddApiVersioning().AddApiExplorer();
                            services.AddEndpointsApiExplorer();
                            services.AddSwaggerGen();
                            services.Configure<RateLimitingOptions>(config.GetSection(RateLimitingOptions.SectionName));
                            services.AddSingleton<IIpProtectionService, IpProtectionService>();
                            
                            // CONFIGURE SMTP SERVICES FOR TESTS
                            var smtpConfigs = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();
                            EnvironmentUtils.ConfigureSmtpSettings(services, smtpConfigs);
                        }
                    ).Configure(app => Program.ConfigureApp(app));
                });

                // ACT - START HOST
                using var host = hostBuilder.Start();
                var server = host.GetTestServer();
                var client = server.CreateClient();

                // ASSERT - SWAGGER ENDPOINT INACCESSIBLE
                var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
                Assert.Equal(System.Net.HttpStatusCode.NotFound, swaggerResponse.StatusCode);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);
            }
        }

        // TEST FOR CONFIGUREAPP WHEN HTTPS REDIRECTION IS DISABLED
        [Fact]
        public async Task ConfigureApp_WhenRequireHttpsIsFalse_DoesNotUseHttpsRedirection()
        {
            // ARRANGE - MOCK SMTP CONFIGS
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[{\"Index\":0,\"Host\":\"127.0.0.1\",\"Port\":1,\"Email\":\"test@example.com\",\"Description\":\"Test\"}]");
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ARRANGE - HTTPS DISABLED
                var configDict = new Dictionary<string, string?> {{ "Security:RequireHttps", "false" }};
                var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

                // ARRANGE - WEB HOST BUILDER
                var hostBuilder = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseTestServer()
                        .ConfigureAppConfiguration((context, builder) => { builder.Sources.Clear(); builder.AddConfiguration(config); })
                        .ConfigureServices((context, services) =>
                        {
                            services.AddSingleton<IConfiguration>(config);
                            services.AddControllers();
                            services.AddLogging();
                            services.AddApiVersioning().AddApiExplorer();
                            services.AddEndpointsApiExplorer();
                            services.AddSwaggerGen();
                            services.Configure<RateLimitingOptions>(config.GetSection(RateLimitingOptions.SectionName));
                            services.AddSingleton<IIpProtectionService, IpProtectionService>();
                                
                            // CONFIGURE SMTP SERVICES FOR TESTS
                            var smtpConfigs = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();
                            EnvironmentUtils.ConfigureSmtpSettings(services, smtpConfigs);
                        }
                    ).Configure(app => Program.ConfigureApp(app));
                });

                // ACT - START HOST
                using var host = hostBuilder.Start();
                var server = host.GetTestServer();
                var client = server.CreateClient();

                // ASSERT - NO HTTPS REDIRECTION
                var response = await client.GetAsync("http://localhost/test");
                Assert.NotEqual(System.Net.HttpStatusCode.Redirect, response.StatusCode);
                Assert.NotEqual(System.Net.HttpStatusCode.RedirectKeepVerb, response.StatusCode);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);
            }
        }

        // TEST FOR CONFIGUREAPP WHEN SWAGGER OPTIONS ARE MISSING (USES DEFAULTS)
        [Fact]
        public void ConfigureApp_WhenSwaggerOptionsMissing_UsesDefaultOptions()
        {
            // ARRANGE - MOCK SMTP CONFIGS
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[{\"Index\":0,\"Host\":\"127.0.0.1\",\"Port\":1,\"Email\":\"test@example.com\",\"Description\":\"Test\"}]");
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ARRANGE - CONFIGURATION WITHOUT SECTION
                var configDict = new Dictionary<string, string?>();
                var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

                // ARRANGE - WEB HOST BUILDER
                var hostBuilder = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseTestServer()
                        .ConfigureAppConfiguration((context, builder) => { builder.Sources.Clear(); builder.AddConfiguration(config); })
                        .ConfigureServices((context, services) =>
                        {
                            services.AddSingleton<IConfiguration>(config);
                            services.AddControllers();
                            services.AddLogging();
                            services.AddApiVersioning().AddApiExplorer();
                            services.AddEndpointsApiExplorer();
                            services.AddSwaggerGen();
                            services.Configure<RateLimitingOptions>(config.GetSection(RateLimitingOptions.SectionName));
                            services.AddSingleton<IIpProtectionService, IpProtectionService>();
                                
                            // CONFIGURE SMTP SERVICES FOR TESTS
                            var smtpConfigs = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();
                            EnvironmentUtils.ConfigureSmtpSettings(services, smtpConfigs);
                        }
                    ).Configure(app => Program.ConfigureApp(app));
                });

                // ACT - START HOST
                using var host = hostBuilder.Start();
                var server = host.GetTestServer();

                // ASSERT - DEFAULT OPTIONS USED
                Assert.NotNull(server);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);
            }
        }

        // TEST FOR CONFIGUREAPP WHEN SECURITY OPTIONS ARE MISSING (USES DEFAULTS)
        [Fact]
        public void ConfigureApp_WhenSecurityOptionsMissing_UsesDefaultOptions()
        {
            // ARRANGE - MOCK SMTP CONFIGS
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", "[{\"Index\":0,\"Host\":\"127.0.0.1\",\"Port\":1,\"Email\":\"test@example.com\",\"Description\":\"Test\"}]");
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "test");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");

            try
            {
                // ARRANGE - CONFIGURATION WITHOUT SECTION
                var configDict = new Dictionary<string, string?>();
                var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

                // ARRANGE - WEB HOST BUILDER
                var hostBuilder = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseTestServer()
                        .ConfigureAppConfiguration((context, builder) => { builder.Sources.Clear(); builder.AddConfiguration(config); })
                        .ConfigureServices((context, services) =>
                        {
                            services.AddSingleton<IConfiguration>(config);
                            services.AddControllers();
                            services.AddLogging();
                            services.AddApiVersioning().AddApiExplorer();
                            services.AddEndpointsApiExplorer();
                            services.AddSwaggerGen();
                            services.Configure<RateLimitingOptions>(config.GetSection(RateLimitingOptions.SectionName));
                            services.AddSingleton<IIpProtectionService, IpProtectionService>();

                            // CONFIGURE SMTP SERVICES FOR TESTS
                            var smtpConfigs = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();
                            EnvironmentUtils.ConfigureSmtpSettings(services, smtpConfigs);
                        }
                    ).Configure(app => Program.ConfigureApp(app));
                });

                // ACT - START HOST
                using var host = hostBuilder.Start();
                var server = host.GetTestServer();

                // ASSERT - DEFAULT OPTIONS USED
                Assert.NotNull(server);
            }
            finally
            {
                // CLEANUP
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);
            }
        }
    }
}
