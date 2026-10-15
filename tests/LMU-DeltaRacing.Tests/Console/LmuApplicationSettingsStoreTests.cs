using FluentAssertions;
using LMU_DeltaRacing.ConsoleApp.Configuration;
using Microsoft.Extensions.Configuration;

namespace LMU_DeltaRacing.Tests.Console;

/// <summary>
/// Verifies persisting user configuration to the executable-side appsettings file.
/// </summary>
[TestClass]
public sealed class LmuApplicationSettingsStoreTests
{
    /// <summary>
    /// Verifies that saving validated settings updates appsettings and can be reloaded.
    /// </summary>
    [TestMethod]
    public void Save_persists_settings_to_appsettings_json()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "logs"));
        var configPath = Path.Combine(directory, "appsettings.json");
        File.WriteAllText(configPath, """{"LMU":{"ResultsDirectory":"","LogFilePath":"old.log","FileMinimumLogLevel":"Warning"}}""");

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
            var store = new LmuApplicationSettingsStore(
                configuration,
                configPath,
                new LmuApplicationOptionsValidator(directory));

            store.Save(new LmuApplicationOptions
            {
                ResultsDirectory = directory,
                LogFilePath = "logs/current.log",
                FileMinimumLogLevel = "Information"
            });

            configuration.Reload();
            configuration["LMU:ResultsDirectory"].Should().Be(directory);
            configuration["LMU:LogFilePath"].Should().Be("logs/current.log");
            configuration["LMU:FileMinimumLogLevel"].Should().Be("Information");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
