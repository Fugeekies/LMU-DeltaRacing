using FluentAssertions;
using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.Presentation.Services;
using Microsoft.Extensions.Configuration;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies atomic persistence of dashboard settings.
/// </summary>
[TestClass]
public sealed class DashboardSettingsStoreTests
{
    /// <summary>
    /// Verifies that the update check preference is saved and visible through the configuration.
    /// </summary>
    [TestMethod]
    public void Update_check_preference_is_saved()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lmu-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var settingsPath = Path.Combine(directory, "appsettings.json");
        File.WriteAllText(settingsPath, """{ "LMU": { "OpenBrowser": false } }""");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsPath, optional: false, reloadOnChange: false)
            .Build();

        try
        {
            new DashboardSettingsStore(configuration, settingsPath).SaveCheckForUpdates(false);

            configuration["LMU:CheckForUpdates"].Should().Be("False");
            configuration["LMU:OpenBrowser"].Should().Be("False");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that results settings are saved while unrelated application settings are preserved.
    /// </summary>
    [TestMethod]
    public void Save_methods_update_lmu_values_independently_and_preserve_other_settings()
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
            store.SaveResultsDirectory(@"C:\LMU\Results");
            store.SaveWatchOnStartup(false);
            store.SaveLoadPracticeSessions(true);
            store.SaveDriverMerges([new DriverMerge("Old Name", "New Name")]);
            store.SaveUserName("Driver One");
            store.SaveProgressionSettings(new ProgressionSettings(
                ProgressionGranularity.Week,
                ProgressionAggregate.Max,
                ProgressionMode.PerCar,
                ProgressionSession.Qualifying));

            configuration["LMU:ResultsDirectory"].Should().Be(@"C:\LMU\Results");
            configuration["LMU:UserName"].Should().Be("Driver One");
            configuration.GetValue<bool>("LMU:WatchResultsOnStartup").Should().BeFalse();
            configuration.GetValue<bool>("LMU:LoadPracticeSessions").Should().BeTrue();
            configuration["LMU:DriverMerges:0:From"].Should().Be("Old Name");
            configuration["LMU:DriverMerges:0:To"].Should().Be("New Name");
            ProgressionSettings.Load(configuration).Should().Be(new ProgressionSettings(
                ProgressionGranularity.Week,
                ProgressionAggregate.Max,
                ProgressionMode.PerCar,
                ProgressionSession.Qualifying));
            configuration["LMU:Progression:Granularity"].Should().Be("Week");
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
