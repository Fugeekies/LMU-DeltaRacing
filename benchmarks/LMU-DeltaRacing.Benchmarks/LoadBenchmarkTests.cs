using System.Globalization;
using System.Reflection;
using LMU_DeltaRacing.Infrastructure.Parsing;

namespace LMU_DeltaRacing.Benchmarks;

[TestClass]
public sealed class LoadBenchmarkTests
{
    private static readonly IReadOnlyDictionary<string, string> BuildMetadata =
        Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Value is not null)
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value!, StringComparer.Ordinal);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("Load")]
    public void Generate_performance_report()
    {
        var years = GetBenchmarkYears();
        var reportDirectory = BuildMetadata["PerformanceReportDirectory"];
        var reportPath = Path.Combine(reportDirectory, "performance-report.md");
        var report = new LoadBenchmarkRunner(new XmlResultParser()).Run(years);
        var expectedSessionCount = (10L * 3 * 365 * years).ToString("N0", CultureInfo.InvariantCulture);

        Assert.IsTrue(
            report.Contains($"{expectedSessionCount} sessions.", StringComparison.Ordinal),
            $"The report must include the expected {expectedSessionCount} sessions.");
        Assert.IsTrue(
            report.Contains("## Opérations de la vue Mes courses", StringComparison.Ordinal)
            && report.Contains("Groupement circuit / catégorie", StringComparison.Ordinal)
            && report.Contains("Filtres circuit + catégorie", StringComparison.Ordinal),
            "The report must include My Races sorting, grouping, and filtering benchmarks.");
        Directory.CreateDirectory(reportDirectory);
        File.WriteAllText(reportPath, report);

        TestContext.WriteLine($"Rapport de performance : {reportPath}");
        TestContext.WriteLine(report);
    }

    /// <summary>
    /// Measures the import chain on the real result files only, without writing the report.
    /// </summary>
    [TestMethod]
    [TestCategory("Import")]
    public void Measure_import_pipeline() => TestContext.WriteLine(ImportPipelineBenchmark.Run());

    /// <summary>
    /// Measures the calculations of the dashboard pages only, without writing the report.
    /// </summary>
    [TestMethod]
    [TestCategory("Views")]
    public void Measure_view_calculations() => TestContext.WriteLine(PresentationBenchmark.Run());

    private static int GetBenchmarkYears()
    {
        var value = BuildMetadata["BenchmarkYears"];
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var years)
            || years is not (2 or 5 or 10))
        {
            throw new InvalidOperationException(
                $"BenchmarkYears must be one of 2, 5, or 10; received '{value}'.");
        }

        return years;
    }
}
