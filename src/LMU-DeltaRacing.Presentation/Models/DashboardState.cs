using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// A thread-safe snapshot of the dashboard's initial and ongoing import state.
/// </summary>
/// <param name="IsLoading">Whether startup imports are still being processed.</param>
/// <param name="FilesToLoad">The number of XML files found at startup.</param>
/// <param name="FilesLoaded">The number of startup files already processed.</param>
/// <param name="IsWatching">Whether new result files are being monitored.</param>
/// <param name="ErrorMessage">A startup error, when results could not be loaded.</param>
/// <param name="Sessions">The imported sessions.</param>
/// <param name="ImportFailures">The failures observed during this run.</param>
/// <param name="ResultsDirectory">The results directory currently being monitored.</param>
public sealed record DashboardState(
    bool IsLoading,
    int FilesToLoad,
    int FilesLoaded,
    bool IsWatching,
    string? ErrorMessage,
    IReadOnlyList<Session> Sessions,
    IReadOnlyList<SessionImportResult> ImportFailures,
    string? ResultsDirectory)
{
    /// <summary>
    /// Gets the selected driver name from application settings.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets driver frequencies calculated from the imported result files.
    /// </summary>
    public IReadOnlyList<DriverFrequency> DriverFrequencies { get; init; } = [];

    /// <summary>
    /// Gets the driver names merged in memory (after a name change).
    /// </summary>
    public IReadOnlyList<DriverMerge> DriverMerges { get; init; } = [];

    /// <summary>
    /// Gets whether the import run at application startup is still in progress. Unlike
    /// <see cref="IsLoading"/>, it stays false while a reload re-imports the results directory.
    /// </summary>
    public bool IsStartupImportRunning { get; init; }
}
