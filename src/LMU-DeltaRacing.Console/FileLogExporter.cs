using System.Globalization;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace LMU_DeltaRacing.ConsoleApp;

/// <summary>
/// Writes OpenTelemetry log records to one UTF-8 file for the current run.
/// </summary>
public sealed class FileLogExporter : BaseExporter<LogRecord>
{
    private readonly object writeLock = new();
    private readonly StreamWriter writer;

    /// <summary>
    /// Creates or truncates the log file at the specified path.
    /// </summary>
    /// <param name="filePath">The path of the current-run log file.</param>
    public FileLogExporter(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
    }

    /// <inheritdoc />
    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        lock (writeLock)
        {
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
            writer.Dispose();
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
                writer.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void WriteRecord(LogRecord record)
    {
        var timestamp = record.Timestamp == default ? record.ObservedTimestamp : record.Timestamp;
        var severity = record.LogLevel.ToString();
        var message = record.FormattedMessage ?? record.Body?.ToString() ?? string.Empty;

        writer.Write(timestamp.ToString("O", CultureInfo.InvariantCulture));
        writer.Write(" [");
        writer.Write(severity);
        writer.Write("] ");
        writer.Write(record.CategoryName);
        writer.Write(": ");
        writer.WriteLine(message);

        if (record.Attributes is not null)
        {
            foreach (var attribute in record.Attributes)
            {
                writer.Write("  ");
                writer.Write(attribute.Key);
                writer.Write('=');
                writer.WriteLine(Convert.ToString(attribute.Value, CultureInfo.InvariantCulture));
            }
        }

        if (record.Exception is not null)
        {
            writer.WriteLine(record.Exception);
        }
    }
}
