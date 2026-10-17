using System.Diagnostics;
using System.Globalization;
using System.Text;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Benchmarks;

internal static class MyRacesBenchmark
{
    private static readonly int[] RecordCounts = [1_000, 10_000, 50_000];
    private const int WarmupIterations = 2;
    private const int MeasurementIterations = 7;

    public static string Run()
    {
        var output = new StringBuilder();
        output.AppendLine("## Opérations de la vue Mes courses");
        output.AppendLine();
        output.AppendLine(
            "Mesures sur des historiques synthétiques de 20 circuits et trois catégories, " +
            "avec une distribution uniforme des événements. Le jeu de données est généré avant les mesures.");
        output.AppendLine(
            $"Chaque opération est préchauffée {WarmupIterations} fois ; les résultats sont la médiane de " +
            $"{MeasurementIterations} exécutions. Les allocations mesurées sont celles du thread courant.");
        output.AppendLine();
        output.AppendLine("| Courses | Opération | Médiane | Allocations médianes | Éléments traités |");
        output.AppendLine("|---:|---|---:|---:|---:|");

        foreach (var recordCount in RecordCounts)
        {
            var records = CreateRecords(recordCount);
            AppendMeasurement(output, recordCount, "Tri par date décroissante", records, Sort);
            AppendMeasurement(output, recordCount, "Groupement circuit / catégorie", records, Group);
            AppendMeasurement(output, recordCount, "Filtre par circuit", records, FilterTrack);
            AppendMeasurement(output, recordCount, "Filtre par catégorie", records, FilterCategory);
            AppendMeasurement(output, recordCount, "Filtres circuit + catégorie", records, FilterTrackAndCategory);
        }

        return output.ToString();
    }

    private static MyRaceRecord[] CreateRecords(int count)
    {
        var records = new MyRaceRecord[count];
        var start = DateTimeOffset.UnixEpoch;
        for (var index = 0; index < count; index++)
        {
            var track = index % 20;
            var category = index % 3;
            var startedAt = start.AddSeconds(index);
            records[index] = new MyRaceRecord(
                $"Circuit {track:D2}",
                $"Layout {track % 4}",
                $"Event {index / 60:D5}",
                startedAt,
                Categories[category],
                $"#{index % 200:D3} · Team",
                TimeSpan.FromSeconds(100 + index % 20),
                null,
                null,
                TimeSpan.FromSeconds(110 + index % 25),
                null,
                TimeSpan.FromSeconds(1320 + index % 90));
        }

        return records;
    }

    private static readonly string[] Categories = ["Hypercar", "LMP2", "LMGT3"];

    private static void AppendMeasurement(
        StringBuilder output,
        int recordCount,
        string operation,
        MyRaceRecord[] records,
        Func<MyRaceRecord[], int> action)
    {
        for (var iteration = 0; iteration < WarmupIterations; iteration++)
        {
            _ = action(records);
        }

        var elapsedTicks = new long[MeasurementIterations];
        var allocatedBytes = new long[MeasurementIterations];
        var processedCount = 0;
        for (var iteration = 0; iteration < MeasurementIterations; iteration++)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            processedCount = action(records);
            timer.Stop();
            elapsedTicks[iteration] = timer.ElapsedTicks;
            allocatedBytes[iteration] = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        }

        Array.Sort(elapsedTicks);
        Array.Sort(allocatedBytes);
        var median = TimeSpan.FromSeconds(
            elapsedTicks[MeasurementIterations / 2] / (double)Stopwatch.Frequency);
        output.AppendLine(
            $"| {recordCount.ToString("N0", CultureInfo.InvariantCulture)} | {operation} | " +
            $"{median.TotalMilliseconds.ToString("N3", CultureInfo.InvariantCulture)} ms | " +
            $"{allocatedBytes[MeasurementIterations / 2].ToString("N0", CultureInfo.InvariantCulture)} B | " +
            $"{processedCount.ToString("N0", CultureInfo.InvariantCulture)} |");
    }

    private static int Sort(MyRaceRecord[] records) =>
        records.OrderByDescending(record => record.StartedAt).ToArray().Length;

    private static int Group(MyRaceRecord[] records) =>
        records
            .GroupBy(record => new { record.TrackVenue, record.Category })
            .Select(group => group.Count())
            .ToArray()
            .Length;

    private static int FilterTrack(MyRaceRecord[] records) =>
        records.Where(record => record.TrackVenue == "Circuit 03").ToArray().Length;

    private static int FilterCategory(MyRaceRecord[] records) =>
        records.Where(record => record.Category == "LMGT3").ToArray().Length;

    private static int FilterTrackAndCategory(MyRaceRecord[] records) =>
        records
            .Where(record => record.TrackVenue == "Circuit 03" && record.Category == "LMGT3")
            .ToArray()
            .Length;
}
