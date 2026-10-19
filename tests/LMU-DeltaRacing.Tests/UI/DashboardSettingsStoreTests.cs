using FluentAssertions;
using LMU_DeltaRacing.UI.Services;
using Microsoft.Extensions.Configuration;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies atomic persistence of dashboard settings.
/// </summary>
[TestClass]
public sealed class DashboardSettingsStoreTests
{
    /// <summary>
    /// Verifies that results settings are saved while unrelated application settings are preserved.
    /// </summary>
    [TestMethod]
    public void SaveResultsSettings_updates_lmu_values_and_preserves_other_settings()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lmu-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var settingsPath = Path.Combine(directory, "appsettings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "LMU": {
                "LogFilePath": "existing.log",
                "OpenBrowser": false
              },
              "Logging": {
                "LogLevel": {
                  "Default": "Warning"
                }
              }
            }
            """);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsPath, optional: false, reloadOnChange: false)
            .Build();
        var store = new DashboardSettingsStore(configuration, settingsPath);

        try
        {
            store.SaveResultsSettings(@"C:\LMU\Results", watchOnStartup: false);
            store.SaveUserName("Driver One");

            configuration["LMU:ResultsDirectory"].Should().Be(@"C:\LMU\Results");
            configuration["LMU:UserName"].Should().Be("Driver One");
            configuration.GetValue<bool>("LMU:WatchResultsOnStartup").Should().BeFalse();
            configuration["LMU:LogFilePath"].Should().Be("existing.log");
            configuration.GetValue<bool>("LMU:OpenBrowser").Should().BeFalse();
            configuration["Logging:LogLevel:Default"].Should().Be("Warning");
            Directory.GetFiles(directory, "*.tmp").Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
