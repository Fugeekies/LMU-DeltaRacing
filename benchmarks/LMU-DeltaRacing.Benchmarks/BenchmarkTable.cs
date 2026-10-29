using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LMU_DeltaRacing.Benchmarks;

/// <summary>
/// Measures operations (warm-up, then median of several runs) and writes them as a Markdown table.
/// </summary>
internal sealed class BenchmarkTable(string firstColumn)
{
    private readonly List<string> rows = [];

    /// <summary>
    /// Measures an operation and adds its row. The operation returns the number of processed items,
    /// which is shown in the table and keeps the JIT from discarding the work.
    /// </summary>
    public void Measure(string scale, string operation, Func<int> action, int warmupIterations = 2, int iterations = 7)
    {
        for (var iteration = 0; iteration < warmupIterations; iteration++)
        {
            _ = action();
        }

        var elapsed = new double[iterations];
        var allocated = new long[iterations];
        var processed = 0;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            // Total allocations (all threads), so that parallel operations are measured too.
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var timer = Stopwatch.StartNew();
            processed = action();
            timer.Stop();
            elapsed[iteration] = timer.Elapsed.TotalMilliseconds;
            allocated[iteration] = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        }

        Array.Sort(elapsed);
        Array.Sort(allocated);
        rows.Add(
            $"| {scale} | {operation} | {FormatMilliseconds(elapsed[iterations / 2])} | " +
            $"{FormatMilliseconds(elapsed[0])} | {FormatBytes(allocated[iterations / 2])} | " +
            $"{processed.ToString("N0", CultureInfo.InvariantCulture)} |");
    }

    /// <summary>
    /// Measures an asynchronous operation and adds its row.
    /// </summary>
    public void Measure(string scale, string operation, Func<Task<int>> action, int warmupIterations = 1, int iterations = 5) =>
        Measure(scale, operation, () => action().GetAwaiter().GetResult(), warmupIterations, iterations);

    /// <summary>
    /// Appends the table to a report.
    /// </summary>
    public void AppendTo(StringBuilder output)
    {
        output.AppendLine($"| {firstColumn} | Opération | Médiane | Meilleur | Allocations (médiane) | Éléments |");
        output.AppendLine("|---:|---|---:|---:|---:|---:|");
        foreach (var row in rows)
        {
            output.AppendLine(row);
        }

        output.AppendLine();
    }

    private static string FormatMilliseconds(double milliseconds) =>
        $"{milliseconds.ToString(milliseconds < 10 ? "N3" : "N1", CultureInfo.InvariantCulture)} ms";

    private static string FormatBytes(long bytes) =>
        bytes < 1024 * 1024
            ? $"{(bytes / 1024d).ToString("N1", CultureInfo.InvariantCulture)} KiB"
            : $"{(bytes / 1024d / 1024d).ToString("N1", CultureInfo.InvariantCulture)} MiB";
}
