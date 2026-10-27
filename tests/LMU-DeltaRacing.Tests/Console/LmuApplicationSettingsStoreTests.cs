using System.Text.Json.Nodes;
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
    /// Verifies that saving the log settings updates appsettings, can be reloaded and keeps the
    /// other LMU settings (results directory, driver, merges) written by the shared settings store.
    /// </summary>
    [TestMethod]
    public void Save_persists_log_settings_and_keeps_other_lmu_settings()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "logs"));
        var configPath = Path.Combine(directory, "appsettings.json");
        File.WriteAllText(configPath, """
            {"LMU":{"ResultsDirectory":"C:\\Results","UserName":"Driver Tom","DriverMerges":[{"From":"Driver Paul","To":"Driver Iris"}],"LogFilePath":"old.log","FileMinimumLogLevel":"Warning"}}
            """);

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
                LogFilePath = "logs/current.log",
                FileMinimumLogLevel = "Information"
            });

            configuration["LMU:LogFilePath"].Should().Be("logs/current.log");
            configuration["LMU:FileMinimumLogLevel"].Should().Be("Information");
            configuration["LMU:ResultsDirectory"].Should().Be(@"C:\Results");
            configuration["LMU:UserName"].Should().Be("Driver Tom");
            configuration["LMU:DriverMerges:0:From"].Should().Be("Driver Paul");

            var lmu = JsonNode.Parse(File.ReadAllText(configPath))!["LMU"]!.AsObject();
            lmu.Select(property => property.Key).Should().BeEquivalentTo(
                "ResultsDirectory", "UserName", "DriverMerges", "LogFilePath", "FileMinimumLogLevel");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
