using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace LMU_DeltaRacing.ConsoleApp;

/// <summary>
/// Writes OpenTelemetry log records to one UTF-8 file for the current run.
/// </summary>
public sealed class FileLogExporter : BaseExporter<LogRecord>
{
    private readonly object writeLock = new();
    private readonly FileStream stream;
    private readonly Utf8JsonWriter writer;
    private bool isClosed;

    /// <summary>
    /// Creates or truncates the log file at the specified path.
    /// </summary>
    /// <param name="filePath">The path of the current-run log file.</param>
    public FileLogExporter(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true
        });
        writer.WriteStartArray();
        writer.Flush();
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
            stream.Flush();
        }

        return ExportResult.Success;
    }

    /// <inheritdoc />
    protected override bool OnForceFlush(int timeoutMilliseconds)
    {
        lock (writeLock)
        {
            writer.Flush();
            stream.Flush();
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
        writer.WriteStartObject();
        writer.WriteString("timestamp", timestamp.ToUniversalTime());
        writer.WriteString("severity", record.LogLevel.ToString());
        writer.WriteString("category", record.CategoryName);
        writer.WriteString("message", record.FormattedMessage ?? record.Body?.ToString() ?? string.Empty);
        writer.WritePropertyName("attributes");
        writer.WriteStartArray();

        if (record.Attributes is not null)
        {
            foreach (var attribute in record.Attributes)
            {
                writer.WriteStartObject();
                writer.WriteString("key", attribute.Key);
                writer.WritePropertyName("value");
                JsonSerializer.Serialize(writer, attribute.Value);
                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
        writer.WriteString("traceId", record.TraceId.ToHexString());
        writer.WriteString("spanId", record.SpanId.ToHexString());
        writer.WriteString("exception", record.Exception?.ToString());
        writer.WriteEndObject();
    }

    private void Close()
    {
        if (isClosed)
        {
            return;
        }

        writer.WriteEndArray();
        writer.Flush();
        stream.Flush();
        writer.Dispose();
        stream.Dispose();
        isClosed = true;
    }
}
