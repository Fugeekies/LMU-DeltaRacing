using System.Text.Json;
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
    private readonly StreamWriter writer;
    private bool isClosed;

    /// <summary>
    /// Creates or truncates the log file at the specified path when the application starts.
    /// </summary>
    /// <param name="filePath">The path of the current-run log file.</param>
    public FileLogExporter(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        writer = new StreamWriter(
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
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

            foreach (var record in batch)
            {
                WriteRecord(record);
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
            writer.Flush();
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
}
