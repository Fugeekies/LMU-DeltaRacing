using FluentAssertions;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Infrastructure.Persistence;
using LMU_DeltaRacing.Infrastructure.Watching;
using LMU_DeltaRacing.UI.Models;
using LMU_DeltaRacing.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies dashboard startup imports and live result-file updates.
/// </summary>
[TestClass]
public sealed class DashboardDataServiceTests
{
    /// <summary>
    /// Verifies that existing results load on startup and new results appear through the watcher.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_loads_existing_sessions_and_imports_new_files()
    {
        var sourceFile = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "Results",
            "2025_02_24_20_26_56-18R1.xml");
        var resultsDirectory = Path.Combine(Path.GetTempPath(), $"lmu-dashboard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(resultsDirectory);
        var initialFile = Path.Combine(resultsDirectory, Path.GetFileName(sourceFile));
        File.Copy(sourceFile, initialFile);

        var repository = new InMemorySessionRepository();
        var importer = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LMU:ResultsDirectory"] = resultsDirectory
            })
            .Build();
        var service = CreateService(configuration, importer, repository);

        try
        {
            await service.StartAsync(CancellationToken.None);

            var initialState = await WaitForStateAsync(
                service,
                state => !state.IsLoading && state.IsWatching);
            initialState.ErrorMessage.Should().BeNull();
            initialState.FilesToLoad.Should().Be(1);
            initialState.FilesLoaded.Should().Be(1);
            initialState.Sessions.Should().ContainSingle();
            initialState.UserName.Should().Be(initialState.DriverFrequencies[0].Name);
            initialState.DriverFrequencies[0].Percentage.Should().Be(1);

            var settingsPath = Path.Combine(resultsDirectory, "appsettings.json");
            var savedSettings = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(settingsPath));
            savedSettings?["LMU"]?["UserName"]?.GetValue<string>()
                .Should().Be(initialState.UserName);

            var selectedDriver = initialState.DriverFrequencies[1].Name;
            service.SelectUserName(selectedDriver);
            service.Snapshot.UserName.Should().Be(selectedDriver);
            System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(settingsPath))?["LMU"]?["UserName"]
                ?.GetValue<string>().Should().Be(selectedDriver);

            File.Copy(sourceFile, Path.Combine(resultsDirectory, "new-results.xml"));
            var updatedState = await WaitForStateAsync(service, state => state.Sessions.Count == 2);

            updatedState.Sessions.Should().HaveCount(2);
            updatedState.ImportFailures.Should().BeEmpty();
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
            Directory.Delete(resultsDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that reloading switches the watcher and replaces sessions from the old directory.
    /// </summary>
    [TestMethod]
    public async Task ReloadResultsAsync_replaces_the_active_results_and_watcher()
    {
        var sourceFile = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "Results",
            "2025_02_24_20_26_56-18R1.xml");
        var firstDirectory = CreateResultsDirectory(sourceFile, "first.xml");
        var secondDirectory = CreateResultsDirectory(sourceFile, "second.xml");
        var repository = new InMemorySessionRepository();
        var importer = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LMU:ResultsDirectory"] = firstDirectory
            })
            .Build();
        var service = CreateService(configuration, importer, repository);

        try
        {
            await service.StartAsync(CancellationToken.None);
            var firstState = await WaitForStateAsync(service, state => !state.IsLoading && state.IsWatching);
            firstState.Sessions.Should().ContainSingle();

            var invalidStatus = service.CheckResultsDirectory(Path.Combine(firstDirectory, "missing"));
            invalidStatus.IsValid.Should().BeFalse();
            service.Snapshot.Sessions.Should().ContainSingle();
            var invalidReload = await service.ReloadResultsAsync(Path.Combine(firstDirectory, "missing"));
            invalidReload.IsValid.Should().BeFalse();
            service.Snapshot.IsWatching.Should().BeTrue();

            var reloaded = await service.ReloadResultsAsync(secondDirectory);

            reloaded.IsValid.Should().BeTrue();
            reloaded.XmlFileCount.Should().Be(1);
            var secondState = service.Snapshot;
            secondState.ResultsDirectory.Should().Be(secondDirectory);
            secondState.IsWatching.Should().BeTrue();
            secondState.Sessions.Should().ContainSingle();

            File.Copy(sourceFile, Path.Combine(firstDirectory, "old-watcher-results.xml"));
            await Task.Delay(300);
            service.Snapshot.Sessions.Should().ContainSingle();

            File.Copy(sourceFile, Path.Combine(secondDirectory, "new-results.xml"));
            var updatedState = await WaitForStateAsync(service, state => state.Sessions.Count == 2);
            updatedState.Sessions.Should().HaveCount(2);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
            Directory.Delete(firstDirectory, recursive: true);
            Directory.Delete(secondDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that startup loads the configured sessions without watching when disabled.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_does_not_start_the_watcher_when_disabled_in_settings()
    {
        var sourceFile = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "Results",
            "2025_02_24_20_26_56-18R1.xml");
        var resultsDirectory = CreateResultsDirectory(sourceFile, "initial.xml");
        var repository = new InMemorySessionRepository();
        var importer = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LMU:ResultsDirectory"] = resultsDirectory,
                ["LMU:WatchResultsOnStartup"] = "false"
            })
            .Build();
        var service = CreateService(configuration, importer, repository);

        try
        {
            await service.StartAsync(CancellationToken.None);

            var state = await WaitForStateAsync(service, snapshot => !snapshot.IsLoading);
            state.Sessions.Should().ContainSingle();
            state.IsWatching.Should().BeFalse();
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
            Directory.Delete(resultsDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that driver merges rename drivers in memory only, and can be cancelled.
    /// </summary>
    [TestMethod]
    public async Task Driver_merges_rename_drivers_in_memory_and_can_be_cancelled()
    {
        var sourceFile = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "Results",
            "2025_02_24_20_26_56-18R1.xml");
        var resultsDirectory = CreateResultsDirectory(sourceFile, "initial.xml");
        var repository = new InMemorySessionRepository();
        var importer = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LMU:ResultsDirectory"] = resultsDirectory,
                ["LMU:WatchResultsOnStartup"] = "false",
                ["LMU:UserName"] = "Driver Gaspard",
                ["LMU:DriverMerges:0:From"] = "Driver Gaspard",
                ["LMU:DriverMerges:0:To"] = "Driver Renamed"
            })
            .Build();
        var service = CreateService(configuration, importer, repository);

        try
        {
            await service.StartAsync(CancellationToken.None);

            var state = await WaitForStateAsync(service, snapshot => !snapshot.IsLoading);
            var driverNames = state.Sessions.Single().Drivers.Select(driver => driver.Name).ToArray();
            driverNames.Should().Contain("Driver Renamed").And.NotContain("Driver Gaspard");
            state.UserName.Should().Be("Driver Renamed");
            state.DriverMerges.Should().ContainSingle();
            repository.GetAll().Single().Drivers.Should().Contain(driver => driver.Name == "Driver Gaspard");
            service.CountSharedSessions("Driver Juliette", "Driver Samuel").Should().Be(1);
            service.CountSharedSessions("Driver Juliette", "Driver Renamed").Should().Be(1);

            service.SetDriverMerges([]);

            service.Snapshot.Sessions.Single().Drivers.Should().Contain(driver => driver.Name == "Driver Gaspard");
            service.Snapshot.DriverMerges.Should().BeEmpty();
            File.ReadAllText(Path.Combine(resultsDirectory, "appsettings.json")).Should().Contain("\"DriverMerges\": []");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
            Directory.Delete(resultsDirectory, recursive: true);
        }
    }

    private static DashboardDataService CreateService(
        IConfiguration configuration,
        SessionImportService importer,
        InMemorySessionRepository repository) =>
        new(
            configuration,
            importer,
            repository,
            NullLogger<DashboardDataService>.Instance,
            NullLogger<ResultsDirectoryWatcher>.Instance,
            new DashboardSettingsStore(
                (IConfigurationRoot)configuration,
                Path.Combine(configuration["LMU:ResultsDirectory"]!, "appsettings.json")));

    private static string CreateResultsDirectory(string sourceFile, string fileName)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lmu-dashboard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.Copy(sourceFile, Path.Combine(directory, fileName));
        return directory;
    }

    private static async Task<DashboardState> WaitForStateAsync(
        DashboardDataService service,
        Func<DashboardState, bool> predicate)
    {
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < timeout)
        {
            var snapshot = service.Snapshot;
            if (predicate(snapshot))
            {
                return snapshot;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The dashboard did not reach the expected import state.");
    }
}
