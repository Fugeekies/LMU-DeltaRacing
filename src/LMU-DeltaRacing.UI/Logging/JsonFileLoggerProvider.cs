using System.Text;
using System.Text.Json;

namespace LMU_DeltaRacing.UI.Logging;

/// <summary>
/// Writes one structured JSON record per line to the local application log.
/// </summary>
public sealed class JsonFileLoggerProvider : ILoggerProvider
{
    private readonly object writeLock = new();
    private readonly StreamWriter writer;
    private readonly LogLevel minimumLevel;
    private bool disposed;

    /// <summary>
    /// Opens a new log file for the current application run.
    /// </summary>
    public JsonFileLoggerProvider(string filePath, LogLevel minimumLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.minimumLevel = minimumLevel;
        writer = new StreamWriter(
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new JsonFileLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
        lock (writeLock)
        {
            if (disposed)
            {
                return;
            }

            writer.Dispose();
            disposed = true;
        }
    }

    private bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= minimumLevel;

    private void Write(
        string category,
        LogLevel logLevel,
        EventId eventId,
        string message,
        Exception? exception)
    {
        lock (writeLock)
        {
            if (disposed)
            {
                return;
            }

            using var stream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(stream))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("timestamp", DateTimeOffset.UtcNow);
                jsonWriter.WriteString("severity", logLevel.ToString());
                jsonWriter.WriteString("category", category);
                jsonWriter.WriteNumber("eventId", eventId.Id);
                jsonWriter.WriteString("message", message);
                if (exception is null)
                {
                    jsonWriter.WriteNull("exception");
                }
                else
                {
                    jsonWriter.WriteString("exception", exception.ToString());
                }

                jsonWriter.WriteEndObject();
            }

            writer.WriteLine(Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length));
        }
    }

    private sealed class JsonFileLogger(JsonFileLoggerProvider provider, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (IsEnabled(logLevel))
            {
                provider.Write(categoryName, logLevel, eventId, formatter(state, exception), exception);
            }
        }
    }
}
