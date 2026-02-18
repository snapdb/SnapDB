using System.IO;
using System.Runtime.CompilerServices;
using Gemstone.Configuration;
using Gemstone.Diagnostics;
using Microsoft.Extensions.Configuration;
using ConfigSettings = Gemstone.Configuration.Settings;

namespace SnapDB.UnitTests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // This method is called automatically by the runtime before any
        // static constructors or other code in this assembly executes.
        // Define settings for the application
        ConfigSettings settings = new()
        {
            SQLite = ConfigurationOperation.Disabled,
            INIFile = ConfigurationOperation.ReadWrite,
            ConfiguredINIPath = GetSourceDirectory()
        };

        // Define settings for service components
        DiagnosticsLogger.DefineSettings(settings);

        // Bind settings to configuration sources
        settings.Bind(new ConfigurationBuilder()
            .ConfigureGemstoneDefaults(settings));
    }

    private static string GetSourceDirectory([CallerFilePath] string callerFilePath = "")
    {
        return Path.GetDirectoryName(callerFilePath)!;
    }
}
