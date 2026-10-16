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
            "TestAssets",
            "Results",
            "2025_09_20_19_03_47-43P1.xml");
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
        var service = new DashboardDataService(
            configuration,
            importer,
            repository,
            NullLogger<DashboardDataService>.Instance,
            NullLogger<ResultsDirectoryWatcher>.Instance);

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
