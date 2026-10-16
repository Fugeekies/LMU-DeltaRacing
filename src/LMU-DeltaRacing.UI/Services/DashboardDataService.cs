using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Watching;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.UI.Services;

/// <summary>
/// Loads the configured results on startup and keeps the dashboard in sync with new imports.
/// </summary>
public sealed class DashboardDataService : IHostedService, IAsyncDisposable
{
    private readonly object stateLock = new();
    private readonly IConfiguration configuration;
    private readonly ISessionImportService importer;
    private readonly ISessionRepository repository;
    private readonly ILogger<DashboardDataService> logger;
    private readonly ILogger<ResultsDirectoryWatcher> watcherLogger;
    private readonly CancellationTokenSource cancellation = new();
    private IReadOnlyList<Session> sessions = [];
    private IReadOnlyList<SessionImportResult> failures = [];
    private ResultsDirectoryWatcher? watcher;
    private Task? initializationTask;
    private bool isLoading = true;
    private bool isWatching;
    private int filesToLoad;
    private int filesLoaded;
    private string? errorMessage;

    /// <summary>
    /// Creates the startup loader and live results monitor.
    /// </summary>
    public DashboardDataService(
        IConfiguration configuration,
        ISessionImportService importer,
        ISessionRepository repository,
        ILogger<DashboardDataService> logger,
        ILogger<ResultsDirectoryWatcher> watcherLogger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(watcherLogger);

        this.configuration = configuration;
        this.importer = importer;
        this.repository = repository;
        this.logger = logger;
        this.watcherLogger = watcherLogger;
    }

    /// <summary>
    /// Raised whenever import progress or dashboard data changes.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Gets an immutable snapshot of the current dashboard state.
    /// </summary>
    public DashboardState Snapshot
    {
        get
        {
            lock (stateLock)
            {
                return new DashboardState(
                    isLoading,
                    filesToLoad,
                    filesLoaded,
                    isWatching,
                    errorMessage,
                    sessions,
                    failures);
            }
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        initializationTask = InitializeAsync(cancellation.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellation.Cancel();
        if (watcher is not null)
        {
            await watcher.DisposeAsync().ConfigureAwait(false);
        }

        if (initializationTask is not null)
        {
            try
            {
                await initializationTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                logger.LogDebug("Dashboard startup import stopped");
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        cancellation.Dispose();
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var resultsDirectory = ResolveResultsDirectory();
            var files = Directory.EnumerateFiles(resultsDirectory, "*.xml", SearchOption.TopDirectoryOnly)
                .OrderBy(filePath => Path.GetFileName(filePath), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            lock (stateLock)
            {
                filesToLoad = files.Length;
            }

            var resultsWatcher = new ResultsDirectoryWatcher(resultsDirectory, importer, watcherLogger);
            resultsWatcher.ImportCompleted += OnImportCompleted;
            watcher = resultsWatcher;
            await resultsWatcher.StartAsync(files, cancellationToken).ConfigureAwait(false);
            lock (stateLock)
            {
                isWatching = true;
            }

            NotifyStateChanged();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ApplyImportResult(await Task.Run(() => importer.ImportFile(file), cancellationToken)
                    .ConfigureAwait(false));

                lock (stateLock)
                {
                    filesLoaded++;
                }

                NotifyStateChanged();
            }

            lock (stateLock)
            {
                isLoading = false;
                sessions = repository.GetAll().ToArray();
            }

            NotifyStateChanged();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Dashboard initialization cancelled during shutdown");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException
                or NotSupportedException or InvalidOperationException)
        {
            logger.LogError(exception, "Could not initialize the dashboard results feed");
            lock (stateLock)
            {
                isLoading = false;
                errorMessage = exception.Message;
            }

            NotifyStateChanged();
        }
    }

    private string ResolveResultsDirectory()
    {
        var configuredDirectory = configuration["LMU:ResultsDirectory"];
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            var resolvedPath = Path.GetFullPath(configuredDirectory, AppContext.BaseDirectory);
            if (!Directory.Exists(resolvedPath))
            {
                throw new DirectoryNotFoundException(
                    $"The configured results directory does not exist: '{resolvedPath}'.");
            }

            return resolvedPath;
        }

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var defaultDirectory = Path.Combine(directory.FullName, "tests", "testAssets", "Results");
            if (Directory.Exists(defaultDirectory))
            {
                return defaultDirectory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "No results directory is configured. Set LMU:ResultsDirectory in appsettings.json.");
    }

    private void OnImportCompleted(SessionImportResult result)
    {
        ApplyImportResult(result);
        NotifyStateChanged();
    }

    private void ApplyImportResult(SessionImportResult result)
    {
        lock (stateLock)
        {
            if (result.Status == SessionImportStatus.Failed)
            {
                failures = [.. failures, result];
            }

            sessions = repository.GetAll().ToArray();
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
