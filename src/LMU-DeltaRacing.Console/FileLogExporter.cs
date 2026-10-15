using System.Text.Json;
using LMU_DeltaRacing.ConsoleApp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace LMU_DeltaRacing.ConsoleApp;

/// <summary>
/// Writes OpenTelemetry log records to one UTF-8 file for the current run, keeping it available after shutdown.
/// </summary>
public sealed class FileLogExporter : BaseExporter<LogRecord>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly object writeLock = new();
    private readonly Func<LmuApplicationOptions> getOptions;
    private readonly LmuApplicationOptionsValidator pathResolver;
    private StreamWriter writer;
    private string activeFilePath;
    private bool isClosed;

    /// <summary>
    /// Creates or truncates the log file at the specified path when the application starts.
    /// </summary>
    /// <param name="filePath">The path of the current-run log file.</param>
    public FileLogExporter(string filePath)
        : this(
            () => new LmuApplicationOptions { LogFilePath = filePath },
            AppContext.BaseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
    }

    /// <summary>
    /// Creates a log exporter that follows dynamically reloaded application settings.
    /// </summary>
    /// <param name="optionsMonitor">The monitor providing the current log path and severity.</param>
    public FileLogExporter(IOptionsMonitor<LmuApplicationOptions> optionsMonitor)
        : this(CreateOptionsProvider(optionsMonitor), AppContext.BaseDirectory)
    {
    }

    /// <summary>
    /// Creates an exporter using a settings provider and a base directory for relative log paths.
    /// </summary>
    /// <param name="getOptions">Gets the latest application settings.</param>
    /// <param name="baseDirectory">The executable directory used to resolve relative paths.</param>
    public FileLogExporter(Func<LmuApplicationOptions> getOptions, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(getOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        this.getOptions = getOptions;
        pathResolver = new LmuApplicationOptionsValidator(baseDirectory);
        activeFilePath = pathResolver.ResolveLogFilePath(getOptions());
        writer = OpenWriter(activeFilePath);
    }

    /// <inheritdoc />
    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        lock (writeLock)
        {
            if (isClosed)
            {
                return ExportResult.Failure;
            }

            var options = getOptions();
            if (!Enum.TryParse<LogLevel>(options.FileMinimumLogLevel, ignoreCase: true, out var minimumLevel)
                || !Enum.IsDefined(minimumLevel))
            {
                return ExportResult.Failure;
            }

            var configuredPath = pathResolver.ResolveLogFilePath(options);
            if (!string.Equals(activeFilePath, configuredPath, StringComparison.OrdinalIgnoreCase))
            {
                writer.Dispose();
                activeFilePath = configuredPath;
                writer = OpenWriter(activeFilePath);
            }

            foreach (var record in batch)
            {
                if (minimumLevel != LogLevel.None && record.LogLevel >= minimumLevel)
                {
                    WriteRecord(record);
                }
            }

            writer.Flush();
        }

        return ExportResult.Success;
    }

    /// <inheritdoc />
    protected override bool OnForceFlush(int timeoutMilliseconds)
    {
        lock (writeLock)
        {
            if (!isClosed)
            {
                writer.Flush();
            }
        }

        return true;
    }

    /// <inheritdoc />
    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        lock (writeLock)
        {
            Close();
        }

        return true;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (writeLock)
            {
                Close();
            }
        }

        base.Dispose(disposing);
    }

    private void WriteRecord(LogRecord record)
    {
        var timestamp = record.Timestamp == default ? record.ObservedTimestamp : record.Timestamp;
        var logEntry = new
        {
            Timestamp = timestamp.ToUniversalTime(),
            Severity = record.LogLevel.ToString(),
            Category = record.CategoryName,
            Message = record.FormattedMessage ?? record.Body?.ToString() ?? string.Empty,
            Attributes = record.Attributes?.Select(attribute => new
            {
                attribute.Key,
                attribute.Value
            }),
            TraceId = record.TraceId.ToHexString(),
            SpanId = record.SpanId.ToHexString(),
            Exception = record.Exception?.ToString()
        };

        writer.WriteLine(JsonSerializer.Serialize(logEntry, JsonOptions));
    }

    private void Close()
    {
        if (isClosed)
        {
            return;
        }

        writer.Flush();
        writer.Dispose();
        isClosed = true;
    }

    private static StreamWriter OpenWriter(string filePath) =>
        new(
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };

    private static Func<LmuApplicationOptions> CreateOptionsProvider(
        IOptionsMonitor<LmuApplicationOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        return () => optionsMonitor.CurrentValue;
    }
}
