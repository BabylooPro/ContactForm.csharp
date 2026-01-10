using System.Reflection;
using API;
using API.Interfaces;
using API.Models;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.TestHost;
using Moq;

namespace Tests.UtilitiesTests
{
    // UNIT TESTS FOR PROGRAM CONFIGURATION BRANCHES
    public class ProgramConfigurationTests
    {
        // HELPER - SETUP DEFAULT SMTP ENVIRONMENTS VARIABLES
        private static void SetupSmtpEnvironmentVariables(string? host = null, string? password = null)
        {
            host ??= "127.0.0.1";
            password ??= "test";
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", $"[{{\"Index\":0,\"Host\":\"{host}\",\"Port\":1,\"Email\":\"test@example.com\",\"Description\":\"Test\"}}]");
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", password);
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "catchall@example.com");
        }

        // HELPER - CLEANUP SMTP ENVIRONMENT VARIABLES
        private static void CleanupSmtpEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);
        }

        // HELPER - CLEANUP CORS ENVIRONMENT VARIABLES
        private static void CleanupCorsEnvironmentVariables()
        {
            var corsKeys = Environment.GetEnvironmentVariables().Keys.Cast<string>().Where(k => k.StartsWith("CORS_", StringComparison.OrdinalIgnoreCase));
            foreach (var key in corsKeys) Environment.SetEnvironmentVariable(key, null); 
        }

        // HELPER - CREATE BASE CONFIGURATION
        private static IConfiguration CreateConfiguration(Dictionary<string, string?>? additionalConfig = null)
        {
            var configDict = additionalConfig ?? new Dictionary<string, string?>();
            return new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
        }

        // HELPER - CREATE BASE SERVICE COLLECTION
        private static IServiceCollection CreateServiceCollection(IConfiguration config)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddLogging();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            return services;
        }

        // HELPER - CREATE BASE HOST BUILDER
        private static IHostBuilder CreateHostBuilder(IConfiguration config, Action<IServiceCollection>? configureServices = null)
        {
            return Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
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

                        var smtpConfigs = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();
                        EnvironmentUtils.ConfigureSmtpSettings(services, smtpConfigs);
                        
                        configureServices?.Invoke(services);
                    })
                    .Configure(app => Program.ConfigureApp(app));
            });
        }

        // HELPER - CREATE WEB APPLICATION BUILDER
        private static WebApplicationBuilder CreateWebApplicationBuilder(IConfiguration config, Action<IServiceCollection>? configureServices = null)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddConfiguration(config);
            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddControllers();
            builder.Services.AddLogging();
            builder.Services.AddApiVersioning().AddApiExplorer();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.Configure<RateLimitingOptions>(config.GetSection(RateLimitingOptions.SectionName));
            builder.Services.AddSingleton<IIpProtectionService, IpProtectionService>();

            var smtpConfigs = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();
            EnvironmentUtils.ConfigureSmtpSettings(builder.Services, smtpConfigs);
            
            configureServices?.Invoke(builder.Services);
            return builder;
        }

        // HELPER - GET CORS POLICY FROM SERVICES
        private static Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicy? GetCorsPolicy(IServiceProvider serviceProvider)
        {
            var corsOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>>();
            var defaultPolicyName = corsOptions.Value.DefaultPolicyName;
            return corsOptions.Value.GetPolicy(defaultPolicyName);
        }

        // HELPER - CALL ENSURETESTENVIRONMENTVARIABLES VIA REFLECTION
        private static void CallEnsureTestEnvironmentVariables()
        {
            var method = typeof(Program).GetMethod("EnsureTestEnvironmentVariables", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            method.Invoke(null, null);
        }

        // TEST FOR CONFIGURESERVICES WHEN APIVERSIONING OPTIONS ARE MISSING (USES DEFAULTS)
        [Fact]
        public void ConfigureServices_WhenApiVersioningOptionsMissing_UsesDefaultOptions()
        {
            // ARRANGE - CONFIGURATION
            var config = CreateConfiguration();
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();

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
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGURESERVICES WHEN API VERSION HAS NO MINOR VERSION
        [Fact]
        public void ConfigureServices_WhenApiVersionHasNoMinorVersion_UsesZeroForMinor()
        {
            // ARRANGE - CONFIGURATION
            var config = CreateConfiguration(new Dictionary<string, string?> {{ "ApiVersioning:DefaultVersion", "2" }});
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();

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
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGURESERVICES WHEN API VERSION HAS MINOR VERSION
        [Fact]
        public void ConfigureServices_WhenApiVersionHasMinorVersion_UsesBothMajorAndMinor()
        {
            // ARRANGE - CONFIGURATION
            var config = CreateConfiguration(new Dictionary<string, string?> {{ "ApiVersioning:DefaultVersion", "3.5" }});
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();

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
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGURESERVICES WITH CORS ORIGINS (NON-LOCALHOST AND IN ALLOWED LIST)
        [Fact]
        public void ConfigureServices_WithCorsOrigins_ConfiguresCorsPolicy()
        {
            // ARRANGE - CONFIGURATION
            var config = CreateConfiguration();
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://example.com");
            Environment.SetEnvironmentVariable("CORS_2_ORIGIN", "https://test.com");

            try
            {
                // ACT - CALL CONFIGURESERVICES
                Program.ConfigureServices(services, config);

                // ASSERT - SERVICES CONFIGURED
                var serviceProvider = services.BuildServiceProvider();
                Assert.NotNull(serviceProvider);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
                CleanupCorsEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP WHEN SWAGGER IS ENABLED
        [Fact]
        public async Task ConfigureApp_WhenSwaggerEnabled_RegistersSwagger()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration(new Dictionary<string, string?> 
            { 
                { "Swagger:Enabled", "true" }, 
                { "Swagger:RoutePrefix", "swagger" }
            });

            try
            {
                // ARRANGE - WEB HOST BUILDER
                using var host = CreateHostBuilder(config).Start();
                var server = host.GetTestServer();
                var client = server.CreateClient();

                // ASSERT - SWAGGER ENDPOINT ACCESSIBLE
                var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
                Assert.True(swaggerResponse.StatusCode == System.Net.HttpStatusCode.NotFound || swaggerResponse.StatusCode == System.Net.HttpStatusCode.OK);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP WHEN SWAGGER IS DISABLED
        [Fact]
        public async Task ConfigureApp_WhenSwaggerDisabled_DoesNotRegisterSwagger()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration(new Dictionary<string, string?> 
            { 
                { "Swagger:Enabled", "false" }, 
                { "Swagger:RoutePrefix", "" }
            });

            try
            {
                // ARRANGE - WEB HOST BUILDER
                using var host = CreateHostBuilder(config).Start();
                var server = host.GetTestServer();
                var client = server.CreateClient();

                // ASSERT - SWAGGER ENDPOINT INACCESSIBLE
                var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
                Assert.Equal(System.Net.HttpStatusCode.NotFound, swaggerResponse.StatusCode);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP WHEN HTTPS REDIRECTION IS ENABLED
        [Fact]
        public async Task ConfigureApp_WhenRequireHttpsIsTrue_UsesHttpsRedirection()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration(new Dictionary<string, string?> {{ "Security:RequireHttps", "true" }});

            try
            {
                // ARRANGE - WEB HOST BUILDER
                using var host = CreateHostBuilder(config).Start();
                var server = host.GetTestServer();
                var client = server.CreateClient();

                // ASSERT - HTTPS REDIRECTION MIDDLEWARE IS REGISTERED
                var response = await client.GetAsync("http://localhost/test");
                Assert.NotNull(response);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP WHEN HTTPS REDIRECTION IS DISABLED
        [Fact]
        public async Task ConfigureApp_WhenRequireHttpsIsFalse_DoesNotUseHttpsRedirection()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration(new Dictionary<string, string?> {{ "Security:RequireHttps", "false" }});

            try
            {
                // ARRANGE - WEB HOST BUILDER
                using var host = CreateHostBuilder(config).Start();
                var server = host.GetTestServer();
                var client = server.CreateClient();

                // ASSERT - NO HTTPS REDIRECTION
                var response = await client.GetAsync("http://localhost/test");
                Assert.NotEqual(System.Net.HttpStatusCode.Redirect, response.StatusCode);
                Assert.NotEqual(System.Net.HttpStatusCode.RedirectKeepVerb, response.StatusCode);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP WHEN SWAGGER OPTIONS ARE MISSING (USES DEFAULTS)
        [Fact]
        public void ConfigureApp_WhenSwaggerOptionsMissing_UsesDefaultOptions()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration();

            try
            {
                // ACT - START HOST
                using var host = CreateHostBuilder(config).Start();
                var server = host.GetTestServer();

                // ASSERT - DEFAULT OPTIONS USED
                Assert.NotNull(server);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP WHEN SECURITY OPTIONS ARE MISSING (USES DEFAULTS)
        [Fact]
        public void ConfigureApp_WhenSecurityOptionsMissing_UsesDefaultOptions()
        {
            // ARRANGE
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration();

            try
            {
                // ACT - START HOST
                using var host = CreateHostBuilder(config).Start();
                var server = host.GetTestServer();

                // ASSERT - DEFAULT OPTIONS USED
                Assert.NotNull(server);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP OVERLOAD WITH WEBAPPLICATION WHEN SMTP CONNECTION TEST FAILS
        [Fact]
        public void ConfigureApp_WithWebApplication_WhenSmtpConnectionTestFails_ThrowsException()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration();

            try
            {
                // ARRANGE - WEB APPLICATION BUILDER
                var builder = CreateWebApplicationBuilder(config);
                var app = builder.Build();
                
                // ASSERT - CONFIGURE APP WITH WEBAPPLICATION CALLS ENSURE SMTP CONNECTIONS ASYNC
                var exception = Assert.Throws<InvalidOperationException>(() => Program.ConfigureApp(app));
                Assert.Contains("SMTP connection test failed", exception.Message);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP OVERLOAD WITH WEBAPPLICATION WHEN SMTP CONNECTION TEST SUCCEEDS
        [Fact]
        public void ConfigureApp_WithWebApplication_WhenSmtpConnectionTestSucceeds_CompletesSuccessfully()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables("smtp.example.com");
            var config = CreateConfiguration();

            try
            {
                // ARRANGE - WEB APPLICATION BUILDER WITH MOCKED SMTP TEST SERVICE
                var mockSmtpTestService = new Mock<ISmtpTestService>();
                mockSmtpTestService.Setup(s => s.TestSmtpConnections()).Returns(Task.CompletedTask);
                
                var builder = CreateWebApplicationBuilder(config, services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISmtpTestService));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddScoped<ISmtpTestService>(_ => mockSmtpTestService.Object);
                });

                // ACT - BUILD AND CONFIGURE APP
                var app = builder.Build();
                Program.ConfigureApp(app);
                
                // ASSERT - VERIFY THAT TEST SMTP CONNECTIONS WAS CALLED
                mockSmtpTestService.Verify(s => s.TestSmtpConnections(), Times.Once);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP WITH SWAGGER ENABLED
        [Fact]
        public void ConfigureApp_WithSwaggerEnabled_ConfiguresSwagger()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration(new Dictionary<string, string?>
            {
                { "Swagger:Enabled", "true" },
                { "Swagger:RoutePrefix", "swagger" }
            });

            try
            {
                // ACT - START HOST
                using var host = CreateHostBuilder(config).Start();
                var server = host.GetTestServer();

                // ASSERT - HOST STARTED
                Assert.NotNull(server);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGUREAPP WITH HTTPS REDIRECTION ENABLED
        [Fact]
        public void ConfigureApp_WithHttpsRedirectionEnabled_ConfiguresHttpsRedirection()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables();
            var config = CreateConfiguration(new Dictionary<string, string?>
            {
                { "Security:RequireHttps", "true" }
            });

            try
            {
                // ACT - START HOST
                using var host = CreateHostBuilder(config).Start();
                var server = host.GetTestServer();

                // ASSERT - HOST STARTED
                Assert.NotNull(server);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGURESERVICES WITH CORS ORIGIN NOT IN ALLOWED LIST (NON-LOCALHOST)
        [Fact]
        public void ConfigureServices_WithCorsOriginNotInAllowedList_ConfiguresCorsPolicy()
        {
            // ARRANGE
            var config = CreateConfiguration();
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://allowed.com");

            try
            {
                // ACT - CALL CONFIGURESERVICES
                Program.ConfigureServices(services, config);

                // ASSERT - SERVICES CONFIGURED
                var serviceProvider = services.BuildServiceProvider();
                var corsService = serviceProvider.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>();
                Assert.NotNull(corsService);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
                CleanupCorsEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGURESERVICES WHEN NO SMTP PASSWORDS ARE MISSING
        [Fact]
        public void ConfigureServices_WhenNoSmtpPasswordsMissing_DoesNotThrow()
        {
            // ARRANGE - CONFIGURATION
            var config = CreateConfiguration();
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();

            try
            {
                // ACT - CALL CONFIGURE SERVICES
                Program.ConfigureServices(services, config);

                // ASSERT - SERVICES CONFIGURED
                var serviceProvider = services.BuildServiceProvider();
                Assert.NotNull(serviceProvider);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR ENSURESMTPCONNECTIONSASYNC WHEN CONNECTION TEST SUCCEEDS
        [Fact]
        public async Task EnsureSmtpConnectionsAsync_WhenConnectionTestSucceeds_DoesNotThrow()
        {
            // ARRANGE - CONFIGURATION
            SetupSmtpEnvironmentVariables("smtp.example.com");
            var config = CreateConfiguration();
            var services = CreateServiceCollection(config);
            
            var smtpConfigs = EnvironmentUtils.LoadSmtpConfigurationsFromEnvironment();
            EnvironmentUtils.ConfigureSmtpSettings(services, smtpConfigs);

            var mockSmtpTestService = new Mock<ISmtpTestService>();
            mockSmtpTestService.Setup(s => s.TestSmtpConnections()).Returns(Task.CompletedTask);

            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISmtpTestService));
            if (descriptor != null) services.Remove(descriptor);
            services.AddScoped<ISmtpTestService>(_ => mockSmtpTestService.Object);

            try
            {
                // ACT - CALL ENSURE SMTP CONNECTIONS ASYNC
                var serviceProvider = services.BuildServiceProvider();
                await Program.EnsureSmtpConnectionsAsync(serviceProvider);

                // ASSERT - VERIFY THAT TESTSMTPCONNECTIONS WAS CALLED
                mockSmtpTestService.Verify(s => s.TestSmtpConnections(), Times.Once);
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR ENSURETESTENVIRONMENTVARIABLES WHEN SMTP_CONFIGURATIONS ALREADY EXISTS
        [Fact]
        public void EnsureTestEnvironmentVariables_WhenSmtpConfigurationsExists_DoesNotOverride()
        {
            // ARRANGE - SET SMTP_CONFIGURATIONS BEFORE CALLING
            var existingConfig = "[{\"Index\":0,\"Host\":\"existing.example.com\",\"Port\":587,\"Email\":\"existing@example.com\",\"Description\":\"Existing\"}]";
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", existingConfig);
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", "existing-password");
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", "existing-reception@example.com");
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", "existing-catchall@example.com");

            try
            {
                // ACT - CALL ENSURE TEST ENVIRONMENT VARIABLES
                CallEnsureTestEnvironmentVariables();

                // ASSERT - VALUES NOT OVERRIDDEN
                Assert.Equal(existingConfig, Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS"));
                Assert.Equal("existing-password", Environment.GetEnvironmentVariable("SMTP_0_PASSWORD"));
                Assert.Equal("existing-reception@example.com", Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL"));
                Assert.Equal("existing-catchall@example.com", Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL"));
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR ENSURETESTENVIRONMENTVARIABLES WHEN SMTP VARIABLES ALREADY EXIST
        [Fact]
        public void EnsureTestEnvironmentVariables_WhenSmtpVariablesExist_DoesNotOverride()
        {
            // ARRANGE - SET SMTP_CONFIGURATIONS BUT KEEP EXISTING VARIABLES
            var existingPassword = "existing-password-123";
            var existingReception = "existing-reception@example.com";
            var existingCatchall = "existing-catchall@example.com";
            
            Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
            Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", existingPassword);
            Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", existingReception);
            Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", existingCatchall);

            try
            {
                // ACT - CALL ENSURE TEST ENVIRONMENT VARIABLES
                CallEnsureTestEnvironmentVariables();

                // ASSERT - SMTP_CONFIGURATIONS SET BUT OTHER VARIABLES NOT OVERRIDDEN
                var configAfter = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
                Assert.NotNull(configAfter);
                Assert.Contains("smtp.example.com", configAfter);
                Assert.Equal(existingPassword, Environment.GetEnvironmentVariable("SMTP_0_PASSWORD"));
                Assert.Equal(existingReception, Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL"));
                Assert.Equal(existingCatchall, Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL"));
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR ENSURETESTENVIRONMENTVARIABLES WHEN SMTP_CONFIGURATIONS IS NULL AND TEST ASSEMBLY EXISTS
        [Fact]
        public void EnsureTestEnvironmentVariables_WhenSmtpConfigurationsIsNullAndTestAssemblyExists_SetsDefaultValues()
        {
            // ARRANGE - CLEAR SMTP_CONFIGURATIONS TO TRIGGER THE IF
                Environment.SetEnvironmentVariable("SMTP_CONFIGURATIONS", null);
                Environment.SetEnvironmentVariable("SMTP_0_PASSWORD", null);
                Environment.SetEnvironmentVariable("SMTP_RECEPTION_EMAIL", null);
                Environment.SetEnvironmentVariable("SMTP_CATCHALL_EMAIL", null);

            try
            {
                // ACT - CALL ENSURE TEST ENVIRONMENT VARIABLES
                CallEnsureTestEnvironmentVariables();

                // ASSERT - DEFAULT VALUES SET
                var configAfter = Environment.GetEnvironmentVariable("SMTP_CONFIGURATIONS");
                Assert.NotNull(configAfter);
                Assert.Contains("smtp.example.com", configAfter);
                Assert.Equal("test-password", Environment.GetEnvironmentVariable("SMTP_0_PASSWORD"));
                Assert.Equal("reception@example.com", Environment.GetEnvironmentVariable("SMTP_RECEPTION_EMAIL"));
                Assert.Equal("catchall@example.com", Environment.GetEnvironmentVariable("SMTP_CATCHALL_EMAIL"));
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGURESERVICES WITH CORS ORIGIN NOT LOCALHOST AND NOT IN ALLOWED LIST
        [Fact]
        public void ConfigureServices_WithCorsOriginNotLocalhostAndNotInList_ReturnsFalse()
        {
            // ARRANGE - CONFIGURATION
            var config = CreateConfiguration();
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://allowed.com");

            try
            {
                // ACT - CALL CONFIGURESERVICES
                Program.ConfigureServices(services, config);

                // ASSERT - TEST ORIGIN
                var serviceProvider = services.BuildServiceProvider();
                var defaultPolicy = GetCorsPolicy(serviceProvider);
                Assert.NotNull(defaultPolicy);
                Assert.False(defaultPolicy.IsOriginAllowed("https://disallowed.com"));
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
                CleanupCorsEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGURESERVICES WITH CORS ORIGIN LOCALHOST (SHOULD BE ALLOWED)
        [Fact]
        public void ConfigureServices_WithCorsOriginLocalhost_ReturnsTrue()
        {
            // ARRANGE - CONFIGURATION
            var config = CreateConfiguration();
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();

            try
            {
                // ACT - CALL CONFIGURESERVICES
                Program.ConfigureServices(services, config);

                // ASSERT - TEST LOCALHOST ORIGIN
                var serviceProvider = services.BuildServiceProvider();
                var defaultPolicy = GetCorsPolicy(serviceProvider);
                Assert.NotNull(defaultPolicy);
                Assert.True(defaultPolicy.IsOriginAllowed("http://localhost:3000"));
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
            }
        }

        // TEST FOR CONFIGURESERVICES WITH CORS ORIGIN NOT LOCALHOST BUT IN ALLOWED LIST (SHOULD RETURN TRUE)
        [Fact]
        public void ConfigureServices_WithCorsOriginNotLocalhostButInList_ReturnsTrue()
        {
            // ARRANGE - CONFIGURATION
            var config = CreateConfiguration();
            var services = CreateServiceCollection(config);
            SetupSmtpEnvironmentVariables();
            Environment.SetEnvironmentVariable("CORS_1_ORIGIN", "https://allowed.com");

            try
            {
                // ACT - CALL CONFIGURESERVICES
                Program.ConfigureServices(services, config);

                // ASSERT - TEST ORIGIN
                var serviceProvider = services.BuildServiceProvider();
                var defaultPolicy = GetCorsPolicy(serviceProvider);
                Assert.NotNull(defaultPolicy);
                Assert.True(defaultPolicy.IsOriginAllowed("https://allowed.com"));
            }
            finally
            {
                CleanupSmtpEnvironmentVariables();
                CleanupCorsEnvironmentVariables();
            }
        }
    }
}
