using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using LMU_DeltaRacing.Application.Telemetry;

namespace LMU_DeltaRacing.ConsoleApp;

/// <summary>
/// Collects OpenTelemetry measurements and writes an aggregate summary for one application run.
/// </summary>
public sealed class RunStatisticsCollector : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string statisticsFilePath;
    private readonly MeterListener meterListener = new();
    private readonly Stopwatch runDuration = Stopwatch.StartNew();
    private double totalImportDurationMilliseconds;
    private long importDurationMeasurementCount;
    private long totalImports;
    private long successfulImports;
    private long failedImports;
    private double totalWorkingSetBytes;
    private long workingSetMeasurementCount;
    private double totalManagedHeapBytes;
    private long managedHeapMeasurementCount;
    private bool completed;
    private bool disposed;

    /// <summary>
    /// Creates a collector and resets the statistics file for this application run.
    /// </summary>
    /// <param name="statisticsFilePath">The path of the per-run statistics file.</param>
    public RunStatisticsCollector(string statisticsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statisticsFilePath);

        this.statisticsFilePath = statisticsFilePath;
        var directory = Path.GetDirectoryName(Path.GetFullPath(statisticsFilePath));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        WriteStatistics(new
        {
            Status = "running",
            StartedAtUtc = DateTimeOffset.UtcNow
        });

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ApplicationTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>(OnDoubleMeasurement);
        meterListener.SetMeasurementEventCallback<long>(OnLongMeasurement);
        meterListener.Start();
        SampleMemory();
    }

    /// <summary>
    /// Samples observable memory measurements at this point in the application run.
    /// </summary>
    public void SampleMemory()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        meterListener.RecordObservableInstruments();
    }

    /// <summary>
    /// Writes and returns the aggregate statistics collected for this run.
    /// </summary>
    /// <returns>The summary of imports, duration, throughput, and process memory.</returns>
    public RunStatistics Complete()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (completed)
        {
            throw new InvalidOperationException("Run statistics have already been completed.");
        }

        SampleMemory();
        runDuration.Stop();

        var elapsedSeconds = runDuration.Elapsed.TotalSeconds;
        var statistics = new RunStatistics(
            DateTimeOffset.UtcNow,
            elapsedSeconds,
            totalImports,
            successfulImports,
            failedImports,
            Average(totalImportDurationMilliseconds, importDurationMeasurementCount),
            elapsedSeconds > 0 ? totalImports / elapsedSeconds : null,
            Average(totalWorkingSetBytes, workingSetMeasurementCount),
            Average(totalManagedHeapBytes, managedHeapMeasurementCount),
            Math.Max(workingSetMeasurementCount, managedHeapMeasurementCount));

        WriteStatistics(statistics);
        completed = true;
        return statistics;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (!completed)
        {
            Complete();
        }

        meterListener.Dispose();
        disposed = true;
    }

    private void OnDoubleMeasurement(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        if (instrument.Name == "lmu.result.import.duration")
        {
            totalImportDurationMilliseconds += measurement;
            importDurationMeasurementCount++;
        }
    }

    private void OnLongMeasurement(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        switch (instrument.Name)
        {
            case "lmu.result.imports":
                totalImports += measurement;
                foreach (var tag in tags)
                {
                    if (tag.Key == "import.status" && tag.Value is string status)
                    {
                        if (status == "imported")
                        {
                            successfulImports += measurement;
                        }
                        else if (status == "failed")
                        {
                            failedImports += measurement;
                        }
                    }
                }

                break;
            case "lmu.process.memory.working_set":
                totalWorkingSetBytes += measurement;
                workingSetMeasurementCount++;
                break;
            case "lmu.process.memory.managed_heap":
                totalManagedHeapBytes += measurement;
                managedHeapMeasurementCount++;
                break;
        }
    }

    private void WriteStatistics<T>(T statistics)
    {
        using var stream = new FileStream(
            statisticsFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        JsonSerializer.Serialize(stream, statistics, JsonOptions);
        stream.Flush();
    }

    private static double? Average(double total, long count) => count > 0 ? total / count : null;
}

/// <summary>
/// Aggregate OpenTelemetry statistics collected during one application run.
/// </summary>
/// <param name="GeneratedAtUtc">The time the summary was generated.</param>
/// <param name="ExecutionDurationSeconds">The total application run duration in seconds.</param>
/// <param name="TotalImports">The total number of attempted imports.</param>
/// <param name="SuccessfulImports">The number of successful imports.</param>
/// <param name="FailedImports">The number of failed imports.</param>
/// <param name="AverageImportDurationMilliseconds">The average duration of an import, or null when no imports were measured.</param>
/// <param name="AverageImportsPerSecond">The attempted import throughput over the application run.</param>
/// <param name="AverageWorkingSetBytes">The average process working set, or null if it could not be sampled.</param>
/// <param name="AverageManagedHeapBytes">The average managed heap size, or null if it could not be sampled.</param>
/// <param name="MemorySampleCount">The number of observed process memory samples.</param>
public sealed record RunStatistics(
    DateTimeOffset GeneratedAtUtc,
    double ExecutionDurationSeconds,
    long TotalImports,
    long SuccessfulImports,
    long FailedImports,
    double? AverageImportDurationMilliseconds,
    double? AverageImportsPerSecond,
    double? AverageWorkingSetBytes,
    double? AverageManagedHeapBytes,
    long MemorySampleCount);
