using System.Globalization;
using System.Text;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Infrastructure.Persistence;
using LMU_DeltaRacing.Infrastructure.Watching;
using LMU_DeltaRacing.Presentation.Services;
using LMU_DeltaRacing.UI.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMU_DeltaRacing.Benchmarks;

/// <summary>
/// Measures the import chain on the real result files: parsing, import from disk and the full
/// startup load of the dashboard data service.
/// </summary>
internal static class ImportPipelineBenchmark
{
    private static readonly int[] DirectoryCopies = [1, 5];

    public static string Run()
    {
        var output = new StringBuilder();
        var files = FixtureSessions.Files;
        output.AppendLine("## Chaîne d'import sur les fichiers réels");
        output.AppendLine();
        output.AppendLine(
            $"Mesures sur les {files.Count} fichiers XML anonymisés de `assets/Results` " +
            "(essais, qualifications et courses, jusqu'à 280 Ko par fichier). " +
            "Chaque opération est préchauffée, puis la médiane et le meilleur temps de plusieurs exécutions sont retenus. " +
            "Les allocations comptent tous les threads.");
        output.AppendLine();

        AppendParsing(output, files);
        AppendImport(output, files);
        return output.ToString();
    }

    private static void AppendParsing(StringBuilder output, IReadOnlyList<string> files)
    {
        var parser = new XmlResultParser();
        var contents = files.Select(File.ReadAllBytes).ToArray();
        var table = new BenchmarkTable("Fichier");

        foreach (var (label, suffix) in new[] { ("Essais", "P"), ("Qualification", "Q"), ("Course", "R") })
        {
            var ofType = files
                .Select((file, index) => (File: file, Bytes: contents[index]))
                .Where(entry => Path.GetFileNameWithoutExtension(entry.File)[^2] == suffix[0])
                .OrderBy(entry => entry.Bytes.Length)
                .ToArray();
            var median = ofType[ofType.Length / 2];
            var largest = ofType[^1];
            table.Measure(FormatSize(median.Bytes.Length), $"Parsing d'un fichier {label} médian", () => Parse(parser, median.Bytes), 20, 51);
            table.Measure(FormatSize(largest.Bytes.Length), $"Parsing du plus gros fichier {label}", () => Parse(parser, largest.Bytes), 20, 51);
        }

        table.Measure(
            FormatSize(contents.Sum(bytes => (long)bytes.Length)),
            $"Parsing des {files.Count} fichiers en mémoire (séquentiel)",
            () => contents.Sum(bytes => Parse(parser, bytes)));

        output.AppendLine("### Parsing XML");
        output.AppendLine();
        table.AppendTo(output);
    }

    private static void AppendImport(StringBuilder output, IReadOnlyList<string> files)
    {
        var table = new BenchmarkTable("Fichiers");
        foreach (var copies in DirectoryCopies)
        {
            var directory = CreateResultsDirectory(files, copies);
            try
            {
                var fileCount = files.Count * copies;
                var scale = fileCount.ToString("N0", CultureInfo.InvariantCulture);
                var paths = Directory.GetFiles(directory, "*.xml");
                table.Measure(scale, "Import disque fichier par fichier, sans essais", () => ImportSequentially(paths, importPractice: false), 1, 3);
                table.Measure(scale, "Chargement complet du service, sans essais", () => LoadDashboardAsync(directory, importPractice: false), 1, 3);
                table.Measure(scale, "Chargement complet du service, avec essais", () => LoadDashboardAsync(directory, importPractice: true), 1, 3);
                table.Measure(scale, "Chargement complet du service, sans essais, 2 fusions", () => LoadDashboardAsync(directory, importPractice: false, withMerges: true), 1, 3);
                table.Measure(scale, "Chargement complet du service, sans essais, journal JSON (Debug)", () => LoadDashboardAsync(directory, importPractice: false, withLog: true), 1, 3);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        output.AppendLine("### Import depuis le disque");
        output.AppendLine();
        output.AppendLine(
            "« Chargement complet » mesure `DashboardDataService` du démarrage jusqu'à la fin de l'import initial " +
            "(liste des fichiers, import, fusions, fréquences des pilotes), sans surveillance. " +
            "Au-delà d'une copie, les fichiers réels sont dupliqués sous d'autres noms pour simuler un historique plus long.");
        output.AppendLine();
        table.AppendTo(output);
    }

    private static int Parse(XmlResultParser parser, byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        return parser.Parse(stream).Drivers.Count;
    }

    private static int ImportSequentially(IEnumerable<string> paths, bool importPractice)
    {
        var repository = new InMemorySessionRepository();
        var importer = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance,
            () => importPractice);
        foreach (var path in paths)
        {
            importer.ImportFile(path);
        }

        return repository.GetAll().Count;
    }

    private static async Task<int> LoadDashboardAsync(string directory, bool importPractice, bool withMerges = false, bool withLog = false)
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), $"lmu-benchmark-{Guid.NewGuid():N}.json");
        var logPath = Path.ChangeExtension(settingsPath, ".log");
        // The log of the dashboard, at its default level, or no log at all.
        var logProvider = withLog ? new JsonFileLoggerProvider(logPath, LogLevel.Debug) : null;
        var loggerFactory = logProvider is null
            ? NullLoggerFactory.Instance
            : LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(logProvider));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LMU:ResultsDirectory"] = directory,
                ["LMU:UserName"] = FixtureSessions.UserName,
                ["LMU:WatchResultsOnStartup"] = "false",
                // Two merges of fixture drivers, so that merges are applied during the load.
                ["LMU:DriverMerges:0:From"] = withMerges ? "Driver Gina" : null,
                ["LMU:DriverMerges:0:To"] = withMerges ? "Driver Tristan" : null,
                ["LMU:DriverMerges:1:From"] = withMerges ? "Driver Katia" : null,
                ["LMU:DriverMerges:1:To"] = withMerges ? "Driver Zacharie" : null
            })
            .Build();
        var repository = new InMemorySessionRepository();
        var importer = new SessionImportService(
            new XmlResultParser(),
            repository,
            loggerFactory.CreateLogger<SessionImportService>(),
            () => importPractice);
        var service = new DashboardDataService(
            configuration,
            importer,
            repository,
            loggerFactory.CreateLogger<DashboardDataService>(),
            loggerFactory.CreateLogger<ResultsDirectoryWatcher>(),
            new DashboardSettingsStore(configuration, settingsPath));
        try
        {
            var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            service.StateChanged += () =>
            {
                if (!service.Snapshot.IsStartupImportRunning)
                {
                    loaded.TrySetResult();
                }
            };

            await service.StartAsync(CancellationToken.None);
            await loaded.Task;
            return service.Snapshot.Sessions.Count;
        }
        finally
        {
            await service.DisposeAsync();
            File.Delete(settingsPath);
            loggerFactory.Dispose();
            // A provider given as an instance is not disposed by the factory.
            logProvider?.Dispose();
            File.Delete(logPath);
        }
    }

    private static string CreateResultsDirectory(IReadOnlyList<string> files, int copies)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lmu-benchmark-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        for (var copy = 0; copy < copies; copy++)
        {
            foreach (var file in files)
            {
                File.Copy(file, Path.Combine(directory, $"{copy:D2}-{Path.GetFileName(file)}"));
            }
        }

        return directory;
    }

    private static string FormatSize(long bytes) =>
        bytes < 1024 * 1024
            ? $"{(bytes / 1024d).ToString("N0", CultureInfo.InvariantCulture)} Ko"
            : $"{(bytes / 1024d / 1024d).ToString("N1", CultureInfo.InvariantCulture)} Mo";
}
