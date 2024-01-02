using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

public static class StartupHelper
{
    // GETS USER CHOICES FOR ENVIRONMENT AND LOGGING LEVEL (environnement = launchsettings.json / type of logging = appsetting.json)
    public static (string? Environment, string? LoggingLevel) GetStartupOptions()
    {
        Console.WriteLine("Choose the environment (Development/Production):");
        var environment = Console.ReadLine();

        Console.WriteLine("Choose the type of logging (Normal/Detailed):");
        var loggingType = Console.ReadLine();

        return (environment, loggingType);
    }

    // CONFIGURES APPLICATION BASED ON USER CHOICES
    public static void ConfigureApp(WebApplicationBuilder builder, string environment, string loggingType)
    {
        // LOADS APPSETTINGS FILE BASED ON SELECTED ENVIRONMENT AND CONFIGURES LOGGING LEVEL BASED ON USER CHOICE
        builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: true);
        builder.Logging.ClearProviders();



         if (loggingType.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            // CONFIGURES LOGGING LEVEL FOR "DEVELOPMENT" CHOICE
            builder.Logging.AddConsole();
            //TODO: ADDITIONAL LOG PROVIDERS CAN BE CONFIGURED HERE
        }
        else if (loggingType.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            // CONFIGURES LOGGING LEVEL FOR "PRODUCTION" CHOICE
            builder.Logging.AddConsole();
            //TODO: ADDITIONAL LOG PROVIDERS CAN BE CONFIGURED HERE
        }
        else if (loggingType.Equals("Detailed", StringComparison.OrdinalIgnoreCase))
        {
            // CONFIGURES LOGGING LEVEL FOR "DETAILED" CHOICE
            builder.Logging.AddConsole();
            //TODO: ADDITIONAL LOG PROVIDERS CAN BE CONFIGURED HERE
        }
        else if (loggingType.Equals("Normal", StringComparison.OrdinalIgnoreCase))
        {
            // CONFIGURES LOGGING LEVEL FOR "NORMAL" CHOICE
            builder.Logging.AddConsole();
            //TODO: ADDITIONAL LOG PROVIDERS CAN BE CONFIGURED HERE
        }
    }
}
