using FluentAssertions;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.ConsoleApp;
using LMU_DeltaRacing.ConsoleApp.Configuration;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Infrastructure.Persistence;
using LMU_DeltaRacing.Infrastructure.Watching;
using LMU_DeltaRacing.Presentation.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Spectre.Console.Testing;

namespace LMU_DeltaRacing.Tests.Console;

/// <summary>
/// Verifies the interactive console on a copy of real result files.
/// </summary>
[TestClass]
public sealed class ConsoleApplicationTests
{
    private static readonly string[] FixtureFiles =
    [
        "2025_08_06_20_58_25-65P1.xml",
        "2025_08_06_21_10_55-58Q1.xml"
    ];

    /// <summary>
    /// Verifies the start-up summary (practice ignored), the home view and quitting from the menu.
    /// </summary>
    [TestMethod]
    public async Task RunAsync_loads_results_shows_the_home_view_and_quits()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lmu-console-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        foreach (var file in FixtureFiles)
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, "assets", "Results", file), Path.Combine(directory, file));
        }

        var settingsPath = Path.Combine(directory, "appsettings.json");
        File.WriteAllText(settingsPath, $$$"""
            {"LMU":{"ResultsDirectory":{{{System.Text.Json.JsonSerializer.Serialize(directory)}}},"UserName":"Driver Tom","WatchResultsOnStartup":false}}
            """);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsPath, optional: false, reloadOnChange: false)
            .Build();
        var repository = new InMemorySessionRepository();
        var settingsStore = new DashboardSettingsStore(configuration, settingsPath);
        var data = new DashboardDataService(
            configuration,
            new SessionImportService(new XmlResultParser(), repository, NullLogger<SessionImportService>.Instance),
            repository,
            NullLogger<DashboardDataService>.Instance,
            NullLogger<ResultsDirectoryWatcher>.Instance,
            settingsStore);
        var validator = new LmuApplicationOptionsValidator(directory);
        var console = new TestConsole().Width(160).Interactive();
        // Main menu: "Accueil"; home view: "Retour"; main menu: down three times to "Quitter".
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);
        var application = new ConsoleApplication(
            console,
            data,
            settingsStore,
            new LmuApplicationSettingsStore(configuration, settingsPath, validator),
            configuration,
            new FixedOptionsMonitor(new LmuApplicationOptions()),
            NullLogger<ConsoleApplication>.Instance);

        try
        {
            await data.StartAsync(CancellationToken.None);

            var exitCode = await application.RunAsync();

            exitCode.Should().Be(0);
            console.Output.Should().Contain("1 session(s) importée(s) · 1 essai(s) ignoré(s) · 0 échec(s)")
                .And.Contain("Driver Tom")
                .And.Contain("Surveillance inactive")
                .And.Contain("Qualification & départ");
        }
        finally
        {
            await data.StopAsync(CancellationToken.None);
            await data.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FixedOptionsMonitor(LmuApplicationOptions value) : IOptionsMonitor<LmuApplicationOptions>
    {
        public LmuApplicationOptions CurrentValue => value;

        public LmuApplicationOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<LmuApplicationOptions, string?> listener) => null;
    }
}
