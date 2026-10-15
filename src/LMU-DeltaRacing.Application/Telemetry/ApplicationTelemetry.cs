using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LMU_DeltaRacing.Application.Telemetry;

/// <summary>
/// Defines the OpenTelemetry sources and instruments for the application layer.
/// </summary>
public static class ApplicationTelemetry
{
    /// <summary>
    /// Gets the activity source name used to trace application operations.
    /// </summary>
    public const string ActivitySourceName = "LMU-DeltaRacing.Application";

    /// <summary>
    /// Gets the meter name used to publish application metrics.
    /// </summary>
    public const string MeterName = "LMU-DeltaRacing.Application";

    /// <summary>
    /// Gets the activity source for application operations.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    /// <summary>
    /// Gets the meter for application metrics.
    /// </summary>
    public static Meter Meter { get; } = new(MeterName);

    /// <summary>
    /// Gets the number of result-file imports grouped by outcome.
    /// </summary>
    public static Counter<long> Imports { get; } =
        Meter.CreateCounter<long>("lmu.result.imports", unit: "{file}");

    /// <summary>
    /// Gets the duration of result-file imports.
    /// </summary>
    public static Histogram<double> ImportDuration { get; } =
        Meter.CreateHistogram<double>("lmu.result.import.duration", unit: "ms");

    /// <summary>
    /// Gets an observable gauge for the current process working set in bytes.
    /// </summary>
    public static ObservableGauge<long> WorkingSetBytes { get; } =
        Meter.CreateObservableGauge(
            "lmu.process.memory.working_set",
            () =>
            {
                using var process = Process.GetCurrentProcess();
                return process.WorkingSet64;
            },
            unit: "By");

    /// <summary>
    /// Gets an observable gauge for the current managed heap size in bytes.
    /// </summary>
    public static ObservableGauge<long> ManagedHeapBytes { get; } =
        Meter.CreateObservableGauge(
            "lmu.process.memory.managed_heap",
            () => GC.GetTotalMemory(forceFullCollection: false),
            unit: "By");
}
