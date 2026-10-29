using System.Collections.Concurrent;
using System.Threading.Channels;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using Microsoft.Extensions.Logging;

namespace LMU_DeltaRacing.Infrastructure.Watching;

/// <summary>
/// Watches a results directory and imports each XML path once during the current process.
/// </summary>
public sealed class ResultsDirectoryWatcher : IAsyncDisposable
{
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly string directoryPath;
    private readonly ISessionImportService importer;
    private readonly ILogger<ResultsDirectoryWatcher> logger;
    private readonly ConcurrentDictionary<string, byte> registeredFiles = new(PathComparer);
    private readonly Channel<string> pendingFiles = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    private readonly CancellationTokenSource cancellation = new();
    private FileSystemWatcher? fileSystemWatcher;
    private Task? processingTask;
    private int started;
    private int disposed;
    private int importedSessionCount;

    /// <summary>
    /// Creates a watcher for the specified results directory.
    /// </summary>
    /// <param name="directoryPath">The directory containing LMU result XML files.</param>
    /// <param name="importer">The service used to parse and store result files.</param>
    /// <param name="logger">The logger used to report watcher activity.</param>
    public ResultsDirectoryWatcher(
        string directoryPath,
        ISessionImportService importer,
        ILogger<ResultsDirectoryWatcher> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(logger);

        this.directoryPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(this.directoryPath))
        {
            throw new DirectoryNotFoundException(
                $"The results directory '{this.directoryPath}' does not exist.");
        }

        this.importer = importer;
        this.logger = logger;
    }

    /// <summary>
    /// Occurs after a newly observed file has either imported or failed.
    /// </summary>
    public event Action<SessionImportResult>? ImportCompleted;

    /// <summary>
    /// Gets the number of sessions successfully imported by this watcher.
    /// </summary>
    public int ImportedSessionCount => Volatile.Read(ref importedSessionCount);

    /// <summary>
    /// Starts watching the directory and queues existing XML files not already imported.
    /// </summary>
    /// <param name="alreadyProcessedFiles">Files handled by the initial application load.</param>
    /// <param name="cancellationToken">A token used to cancel startup.</param>
    public Task StartAsync(
        IEnumerable<string>? alreadyProcessedFiles = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (Interlocked.Exchange(ref started, 1) != 0)
        {
            throw new InvalidOperationException("The results directory watcher has already been started.");
        }

        if (alreadyProcessedFiles is not null)
        {
            foreach (var filePath in alreadyProcessedFiles)
            {
                registeredFiles.TryAdd(Path.GetFullPath(filePath), 0);
            }
        }

        var watcher = new FileSystemWatcher(directoryPath, "*.xml")
        {
            IncludeSubdirectories = false,
            InternalBufferSize = 64 * 1024,
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
                | NotifyFilters.CreationTime
        };
        watcher.Created += OnFileChanged;
        watcher.Changed += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
        watcher.Error += OnWatcherError;
        fileSystemWatcher = watcher;
        processingTask = ProcessPendingFilesAsync(cancellation.Token);
        watcher.EnableRaisingEvents = true;

        logger.LogInformation("Watching LMU results directory {ResultsDirectory}", directoryPath);
        QueueExistingFiles();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops file notifications and waits for the active import to finish.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        fileSystemWatcher?.Dispose();
        pendingFiles.Writer.TryComplete();
        cancellation.Cancel();

        if (processingTask is not null)
        {
            try
            {
                await processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Results directory watcher stopped");
            }
        }

        cancellation.Dispose();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs eventArgs) =>
        QueueFile(eventArgs.FullPath);

    private void OnFileRenamed(object sender, RenamedEventArgs eventArgs) =>
        QueueFile(eventArgs.FullPath);

    private void OnWatcherError(object sender, ErrorEventArgs eventArgs)
    {
        logger.LogError(
            eventArgs.GetException(),
            "File-system watcher reported an error for {ResultsDirectory}; rescanning the directory",
            directoryPath);
        QueueExistingFiles();
    }

    private void QueueExistingFiles()
    {
        try
        {
            foreach (var filePath in Directory.EnumerateFiles(
                         directoryPath,
                         "*.xml",
                         SearchOption.TopDirectoryOnly))
            {
                QueueFile(filePath);
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(
                exception,
                "Could not scan results directory {ResultsDirectory}",
                directoryPath);
        }
    }

    private void QueueFile(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!registeredFiles.TryAdd(fullPath, 0))
        {
            logger.LogDebug("Ignoring duplicate result-file notification for {SourceFile}", Path.GetFileName(fullPath));
            return;
        }

        if (!pendingFiles.Writer.TryWrite(fullPath))
        {
            logger.LogError("Could not queue result file {SourceFile}", Path.GetFileName(fullPath));
        }
    }

    private async Task ProcessPendingFilesAsync(CancellationToken cancellationToken)
    {
        await foreach (var filePath in pendingFiles.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = ImportFile(filePath);

            if (result.Status == SessionImportStatus.Imported)
            {
                Interlocked.Increment(ref importedSessionCount);
            }

            NotifyImportCompleted(result);
        }
    }

    /// <summary>
    /// Imports one file. The importer reports expected failures itself; any other error is reported
    /// as a failure of this file, because an exception would end the loop and stop the watcher.
    /// </summary>
    private SessionImportResult ImportFile(string filePath)
    {
        try
        {
            return importer.ImportFile(filePath);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var sourceFile = Path.GetFileName(filePath);
            logger.LogError(exception, "Unexpected error while importing {SourceFile}", sourceFile);
            return new SessionImportResult(sourceFile, SessionImportStatus.Failed, null, exception.Message);
        }
    }

    private void NotifyImportCompleted(SessionImportResult result)
    {
        var handlers = ImportCompleted;
        if (handlers is null)
        {
            return;
        }

        foreach (Action<SessionImportResult> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(result);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "An import-completion listener failed for {SourceFile}",
                    result.SourceFile);
            }
        }
    }
}
