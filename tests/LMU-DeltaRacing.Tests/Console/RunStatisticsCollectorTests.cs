using System.Text.Json;
using FluentAssertions;
using LMU_DeltaRacing.Application.Telemetry;
using LMU_DeltaRacing.ConsoleApp;

namespace LMU_DeltaRacing.Tests.Console;

/// <summary>
/// Verifies per-execution OpenTelemetry statistics.
/// </summary>
[TestClass]
public sealed class RunStatisticsCollectorTests
{
    /// <summary>
    /// Verifies that import measurements are aggregated and the previous run is replaced.
    /// </summary>
    [TestMethod]
    public void Complete_writes_averages_to_a_separate_latest_run_file()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var statisticsPath = Path.Combine(directory, "lmu-deltaracing-stats.json");
        File.WriteAllText(statisticsPath, "previous run");

        try
        {
            using var collector = new RunStatisticsCollector(statisticsPath);
            ApplicationTelemetry.ImportDuration.Record(10);
            ApplicationTelemetry.ImportDuration.Record(30);
            ApplicationTelemetry.Imports.Add(
                2,
                new KeyValuePair<string, object?>("import.status", "imported"));

            var statistics = collector.Complete();

            statistics.TotalImports.Should().Be(2);
            statistics.SuccessfulImports.Should().Be(2);
            statistics.FailedImports.Should().Be(0);
            statistics.AverageImportDurationMilliseconds.Should().Be(20);
            statistics.AverageImportsPerSecond.Should().BeGreaterThan(0);
            statistics.AverageWorkingSetBytes.Should().BeGreaterThan(0);
            statistics.AverageManagedHeapBytes.Should().BeGreaterThan(0);

            using var document = JsonDocument.Parse(File.ReadAllText(statisticsPath));
            document.RootElement.GetProperty("totalImports").GetInt64().Should().Be(2);
            File.ReadAllText(statisticsPath).Should().NotContain("previous run");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that measurements recorded by parallel imports are all counted.
    /// </summary>
    [TestMethod]
    public void Measurements_recorded_from_several_threads_are_all_counted()
    {
        var statisticsPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-stats.json");

        try
        {
            using var collector = new RunStatisticsCollector(statisticsPath);
            Parallel.For(0, 20_000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, _ =>
            {
                ApplicationTelemetry.ImportDuration.Record(2);
                ApplicationTelemetry.Imports.Add(1, new KeyValuePair<string, object?>("import.status", "imported"));
            });

            var statistics = collector.Complete();

            statistics.TotalImports.Should().Be(20_000);
            statistics.SuccessfulImports.Should().Be(20_000);
            statistics.AverageImportDurationMilliseconds.Should().Be(2);
        }
        finally
        {
            File.Delete(statisticsPath);
        }
    }
}
