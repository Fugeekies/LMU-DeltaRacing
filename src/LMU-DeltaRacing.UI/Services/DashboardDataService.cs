using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Watching;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.UI.Services;

/// <summary>
/// Loads configured results and keeps the dashboard in sync with new imports.
/// </summary>
public sealed class DashboardDataService : IHostedService, IAsyncDisposable
{
    private readonly object stateLock = new();
    private readonly object userNameLock = new();
    private readonly SemaphoreSlim reloadLock = new(1, 1);
    private readonly IConfiguration configuration;
    private readonly ISessionImportService importer;
    private readonly ISessionRepository repository;
    private readonly DashboardSettingsStore settingsStore;
    private readonly ILogger<DashboardDataService> logger;
    private readonly ILogger<ResultsDirectoryWatcher> watcherLogger;
    private readonly CancellationTokenSource cancellation = new();
    private IReadOnlyList<Session> sessions = [];
    private IReadOnlyList<DriverFrequency> driverFrequencies = [];
    private IReadOnlyList<SessionImportResult> failures = [];
    private ResultsDirectoryWatcher? watcher;
    private Task? initializationTask;
    private bool isLoading = true;
    private bool isWatching;
    private int filesToLoad;
    private int filesLoaded;
    private string? errorMessage;
    private string? resultsDirectory;
    private string? userName;
    private IReadOnlyList<DriverMerge> driverMerges;

    /// <summary>
    /// Creates the startup loader and live results monitor.
    /// </summary>
    public DashboardDataService(
        IConfiguration configuration,
        ISessionImportService importer,
        ISessionRepository repository,
        ILogger<DashboardDataService> logger,
        ILogger<ResultsDirectoryWatcher> watcherLogger,
        DashboardSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(watcherLogger);
        ArgumentNullException.ThrowIfNull(settingsStore);

        this.configuration = configuration;
        this.importer = importer;
        this.repository = repository;
        this.logger = logger;
        this.watcherLogger = watcherLogger;
        this.settingsStore = settingsStore;
        userName = configuration["LMU:UserName"]?.Trim();
        driverMerges = LoadDriverMerges(configuration);
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
                    failures,
                    resultsDirectory)
                {
                    UserName = DriverMerges.Resolve(userName, driverMerges),
                    DriverMerges = driverMerges,
                    DriverFrequencies = driverFrequencies
                };
            }
        }
    }

    /// <summary>
    /// Saves the selected driver and refreshes all views using the new profile.
    /// </summary>
    public void SelectUserName(string selectedUserName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedUserName);

        lock (userNameLock)
        {
            var normalizedName = selectedUserName.Trim();
            lock (stateLock)
            {
                var candidate = driverFrequencies.FirstOrDefault(driver =>
                    string.Equals(driver.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
                if (candidate is null)
                {
                    throw new ArgumentException(
                        "The selected driver is not present in the imported results.",
                        nameof(selectedUserName));
                }

                normalizedName = candidate.Name;
            }

            settingsStore.SaveUserName(normalizedName);
            lock (stateLock)
            {
                userName = normalizedName;
            }
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Saves the driver merges and applies them to the loaded sessions without reloading files:
    /// the repository keeps the original names, so a cancelled merge restores them.
    /// </summary>
    public void SetDriverMerges(IReadOnlyList<DriverMerge> merges)
    {
        ArgumentNullException.ThrowIfNull(merges);

        settingsStore.SaveDriverMerges(merges);
        lock (stateLock)
        {
            driverMerges = [.. merges];
            sessions = DriverMerges.Apply(repository.GetAll(), driverMerges);
            if (!isLoading)
            {
                driverFrequencies = DriverProfile.CalculateFrequencies(sessions);
            }
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Counts the original sessions in which both drivers took part, including the names already
    /// merged into each of them. One person cannot drive twice in a session, so a non-zero count
    /// warns that the two names probably belong to different people.
    /// </summary>
    public int CountSharedSessions(string firstName, string secondName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(secondName);

        IReadOnlyList<DriverMerge> merges;
        lock (stateLock)
        {
            merges = driverMerges;
        }

        return DriverMerges.CountSharedSessions(repository.GetAll(), firstName, secondName, merges);
    }

    private static IReadOnlyList<DriverMerge> LoadDriverMerges(IConfiguration configuration) =>
        configuration.GetSection("LMU:DriverMerges")
            .GetChildren()
            .Select(merge => new DriverMerge(merge["From"]?.Trim() ?? string.Empty, merge["To"]?.Trim() ?? string.Empty))
            .Where(merge => merge.From.Length > 0 && merge.To.Length > 0)
            .ToArray();

    /// <summary>
    /// Resolves and checks a results directory without changing the active import or watcher.
    /// </summary>
    public ResultsDirectoryStatus CheckResultsDirectory(string? configuredDirectory)
    {
        try
        {
            var resolvedDirectory = ResultsDirectoryResolver.Resolve(
                configuredDirectory,
                AppContext.BaseDirectory);
            var fileCount = Directory.EnumerateFiles(
                resolvedDirectory,
                "*.xml",
                SearchOption.TopDirectoryOnly).Count();
            return new ResultsDirectoryStatus(true, resolvedDirectory, fileCount, null);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException
                or NotSupportedException or InvalidOperationException or System.Security.SecurityException)
        {
            return new ResultsDirectoryStatus(
                false,
                string.IsNullOrWhiteSpace(configuredDirectory) ? null : configuredDirectory,
                0,
                exception.Message);
        }
    }

    /// <summary>
    /// Validates, reloads results from the selected directory, and starts its file watcher.
    /// </summary>
    public async Task<ResultsDirectoryStatus> ReloadResultsAsync(
        string? configuredDirectory,
        CancellationToken cancellationToken = default)
    {
        var status = CheckResultsDirectory(configuredDirectory);
        if (!status.IsValid)
        {
            return status;
        }

        await reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReloadResolvedResultsAsync(status, startWatcher: true, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            reloadLock.Release();
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
            watcher.ImportCompleted -= OnImportCompleted;
            await watcher.DisposeAsync().ConfigureAwait(false);
            watcher = null;
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
        reloadLock.Dispose();
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var status = CheckResultsDirectory(configuration["LMU:ResultsDirectory"]);
        if (!status.IsValid)
        {
            SetInitializationError(status.ErrorMessage ?? "The results directory is unavailable.");
            return;
        }

        await reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var watchOnStartup = configuration.GetValue("LMU:WatchResultsOnStartup", true);
            await ReloadResolvedResultsAsync(status, watchOnStartup, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Dashboard initialization cancelled during shutdown");
        }
        finally
        {
            reloadLock.Release();
        }
    }

    private async Task<ResultsDirectoryStatus> ReloadResolvedResultsAsync(
        ResultsDirectoryStatus status,
        bool startWatcher,
        CancellationToken cancellationToken)
    {
        ResultsDirectoryWatcher? nextWatcher = null;
        try
        {
            if (watcher is not null)
            {
                watcher.ImportCompleted -= OnImportCompleted;
                await watcher.DisposeAsync().ConfigureAwait(false);
                watcher = null;
            }

            var files = Directory.EnumerateFiles(
                    status.DirectoryPath!,
                    "*.xml",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(filePath => Path.GetFileName(filePath), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            repository.Clear();
            lock (stateLock)
            {
                isLoading = true;
                isWatching = false;
                filesToLoad = files.Length;
                filesLoaded = 0;
                errorMessage = null;
                resultsDirectory = status.DirectoryPath;
                sessions = [];
                driverFrequencies = [];
                failures = [];
            }

            NotifyStateChanged();

            if (startWatcher)
            {
                nextWatcher = new ResultsDirectoryWatcher(status.DirectoryPath!, importer, watcherLogger);
                nextWatcher.ImportCompleted += OnImportCompleted;
                await nextWatcher.StartAsync(files, cancellationToken).ConfigureAwait(false);
                watcher = nextWatcher;
                nextWatcher = null;
                lock (stateLock)
                {
                    isWatching = true;
                }

                NotifyStateChanged();
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ApplyImportResult(await Task.Run(
                        () => importer.ImportFile(file),
                        cancellationToken)
                    .ConfigureAwait(false));

                lock (stateLock)
                {
                    filesLoaded++;
                }

                NotifyStateChanged();
            }

            lock (stateLock)
            {
                sessions = DriverMerges.Apply(repository.GetAll(), driverMerges);
                driverFrequencies = DriverProfile.CalculateFrequencies(sessions);
            }

            EnsureUserNameSelected();
            lock (stateLock)
            {
                isLoading = false;
            }

            NotifyStateChanged();
            return status with { XmlFileCount = files.Length };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (nextWatcher is not null)
            {
                nextWatcher.ImportCompleted -= OnImportCompleted;
                await nextWatcher.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException
                or NotSupportedException or InvalidOperationException or System.Security.SecurityException
                or System.Text.Json.JsonException)
        {
            if (nextWatcher is not null)
            {
                nextWatcher.ImportCompleted -= OnImportCompleted;
                await nextWatcher.DisposeAsync().ConfigureAwait(false);
            }

            if (watcher is not null)
            {
                watcher.ImportCompleted -= OnImportCompleted;
                await watcher.DisposeAsync().ConfigureAwait(false);
                watcher = null;
            }

            logger.LogError(exception, "Could not reload dashboard results from {ResultsDirectory}", status.DirectoryPath);
            lock (stateLock)
            {
                isLoading = false;
                isWatching = false;
                errorMessage = exception.Message;
                sessions = DriverMerges.Apply(repository.GetAll(), driverMerges);
            }

            NotifyStateChanged();
            return status with { IsValid = false, ErrorMessage = exception.Message };
        }
    }

    private void SetInitializationError(string message)
    {
        lock (stateLock)
        {
            isLoading = false;
            errorMessage = message;
        }

        logger.LogError("Could not resolve the dashboard results directory: {ErrorMessage}", message);
        NotifyStateChanged();
    }

    private void OnImportCompleted(SessionImportResult result)
    {
        ApplyImportResult(result);
        EnsureUserNameSelected();
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

            sessions = DriverMerges.Apply(repository.GetAll(), driverMerges);
            if (!isLoading)
            {
                driverFrequencies = DriverProfile.CalculateFrequencies(sessions);
            }
        }
    }

    private void EnsureUserNameSelected()
    {
        lock (userNameLock)
        {
            string detectedName;
            lock (stateLock)
            {
                if (!string.IsNullOrWhiteSpace(userName) || driverFrequencies.Count == 0)
                {
                    return;
                }

                detectedName = driverFrequencies[0].Name;
            }

            settingsStore.SaveUserName(detectedName);
            lock (stateLock)
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    userName = detectedName;
                }
            }
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
