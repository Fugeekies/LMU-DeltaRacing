using System.Diagnostics;
using System.Globalization;
using System.Text;
using LMU_DeltaRacing.Infrastructure.Parsing;

namespace LMU_DeltaRacing.Benchmarks;

internal sealed class LoadBenchmarkRunner(XmlResultParser parser)
{
    private static readonly int[] DiskBatchSizes = [10, 100, 500, 1000, 2000, 5000];

    public string Run(int years)
    {
        var phases = SyntheticResultXml.CreatePhaseStreams();
        var inMemoryResult = RunInMemory(phases, years);
        var diskResults = DiskBatchSizes
            .Select(count => RunDiskBatch(phases, count))
            .ToArray();
        var output = new StringBuilder();

        output.AppendLine("# Rapport de performance — parsing XML");
        output.AppendLine();
        output.AppendLine($"- Date UTC : {DateTimeOffset.UtcNow:O}");
        output.AppendLine($"- Runtime : {Environment.Version}");
        output.AppendLine($"- Système : {Environment.OSVersion}");
        output.AppendLine($"- Processeurs logiques : {Environment.ProcessorCount}");
        output.AppendLine();
        output.AppendLine("## Charge en mémoire");
        output.AppendLine();
        output.AppendLine(
            $"Simulation : 10 courses/jour × 3 phases × 365 jours × {years} ans = " +
            $"{inMemoryResult.SessionCount.ToString("N0", CultureInfo.InvariantCulture)} sessions.");
        output.AppendLine(
            "Les XML sont conservés dans des streams en mémoire ; chaque résultat analysé est libéré avant le suivant.");
        output.AppendLine(
            "Chaque XML synthétique contient 10 pilotes, leurs métadonnées, les tours et trois secteurs par tour " +
            "(Practice : 3 tours, Qualifying : 2, Race : 8).");
        output.AppendLine();
        output.AppendLine("| Durée | Débit | Allocations cumulées | Pic mémoire observé* |");
        output.AppendLine("|---:|---:|---:|---:|");
        output.AppendLine(
            $"| {FormatDuration(inMemoryResult.Elapsed)} | " +
            $"{inMemoryResult.SessionsPerSecond.ToString("N0", CultureInfo.InvariantCulture)} sessions/s | " +
            $"{FormatBytes(inMemoryResult.AllocatedBytes)} | " +
            $"{FormatBytes(inMemoryResult.PeakWorkingSetBytes)} |");
        output.AppendLine();
        output.AppendLine("* Pic du working set échantillonné toutes les 300 sessions.");
        output.AppendLine();
        output.AppendLine("## Lots générés sur disque");
        output.AppendLine();
        output.AppendLine("Les fichiers sont écrits dans un répertoire temporaire, analysés puis supprimés.");
        output.AppendLine();
        output.AppendLine("| Fichiers | Génération | Parsing | Débit parsing |");
        output.AppendLine("|---:|---:|---:|---:|");

        foreach (var result in diskResults)
        {
            output.AppendLine(
                $"| {result.FileCount} | {FormatDuration(result.GenerationElapsed)} | " +
                $"{FormatDuration(result.ParseElapsed)} | " +
                $"{result.FilesPerSecond.ToString("N0", CultureInfo.InvariantCulture)} fichiers/s |");
        }

        output.AppendLine();
        output.Append(MyRacesBenchmark.Run());
        return output.ToString();
    }

    private InMemoryBenchmarkResult RunInMemory(IReadOnlyList<PhaseStream> phases, int years)
    {
        var targetSessions = checked(10L * 3 * 365 * years);
        var sessionCount = 0L;
        var peakWorkingSetBytes = Process.GetCurrentProcess().WorkingSet64;
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
        var stopwatch = Stopwatch.StartNew();

        while (sessionCount < targetSessions)
        {
            var phase = phases[(int)(sessionCount % phases.Count)];
            phase.Stream.Position = 0;
            _ = parser.Parse(phase.Stream);
            sessionCount++;

            if (sessionCount % 300 == 0)
            {
                using var process = Process.GetCurrentProcess();
                process.Refresh();
                peakWorkingSetBytes = Math.Max(peakWorkingSetBytes, process.WorkingSet64);
            }
        }

        stopwatch.Stop();
        var allocatedBytes = GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore;
        var sessionsPerSecond = sessionCount / stopwatch.Elapsed.TotalSeconds;
        return new InMemoryBenchmarkResult(
            sessionCount,
            stopwatch.Elapsed,
            allocatedBytes,
            peakWorkingSetBytes,
            sessionsPerSecond);
    }

    private DiskBenchmarkResult RunDiskBatch(IReadOnlyList<PhaseStream> phases, int fileCount)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lmu-load-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(directory);
            var generationTimer = Stopwatch.StartNew();
            for (var index = 0; index < fileCount; index++)
            {
                var phase = phases[index % phases.Count];
                File.WriteAllBytes(Path.Combine(directory, $"{index:D4}.xml"), phase.Bytes);
            }

            generationTimer.Stop();

            var parsingTimer = Stopwatch.StartNew();
            foreach (var filePath in Directory.EnumerateFiles(directory, "*.xml"))
            {
                using var stream = File.OpenRead(filePath);
                _ = parser.Parse(stream);
            }

            parsingTimer.Stop();
            return new DiskBenchmarkResult(
                fileCount,
                generationTimer.Elapsed,
                parsingTimer.Elapsed,
                fileCount / parsingTimer.Elapsed.TotalSeconds);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        var megabytes = bytes / 1024d / 1024d;
        return $"{megabytes.ToString("N1", CultureInfo.InvariantCulture)} MiB";
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

    private sealed record InMemoryBenchmarkResult(
        long SessionCount,
        TimeSpan Elapsed,
        long AllocatedBytes,
        long PeakWorkingSetBytes,
        double SessionsPerSecond);

    private sealed record DiskBenchmarkResult(
        int FileCount,
        TimeSpan GenerationElapsed,
        TimeSpan ParseElapsed,
        double FilesPerSecond);

    internal sealed record PhaseStream(byte[] Bytes, MemoryStream Stream);
}
