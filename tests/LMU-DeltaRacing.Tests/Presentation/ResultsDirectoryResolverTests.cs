using FluentAssertions;
using LMU_DeltaRacing.Presentation.Services;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies results-directory selection for installed and development builds.
/// </summary>
[TestClass]
public sealed class ResultsDirectoryResolverTests
{
    /// <summary>
    /// Verifies that an existing LMU results folder takes precedence over bundled sample data.
    /// </summary>
    [TestMethod]
    public void Resolve_prefers_lmu_results_over_packaged_samples()
    {
        var root = CreateTemporaryDirectory();
        var documentsDirectory = Path.Combine(root, "Documents");
        var lmuResults = Path.Combine(
            documentsDirectory,
            "Le Mans Ultimate",
            "UserData",
            "Log",
            "Results");
        var packagedSamples = Path.Combine(root, "TestAssets", "Results");
        Directory.CreateDirectory(lmuResults);
        Directory.CreateDirectory(packagedSamples);
        File.WriteAllText(Path.Combine(lmuResults, "race.xml"), "<race />");
        File.WriteAllText(Path.Combine(packagedSamples, "sample.xml"), "<sample />");

        try
        {
            ResultsDirectoryResolver.Resolve(string.Empty, root, documentsDirectory)
                .Should()
                .Be(lmuResults);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that packaged sample data is selected when no LMU results are available.
    /// </summary>
    [TestMethod]
    public void Resolve_uses_packaged_samples_when_lmu_results_are_missing()
    {
        var root = CreateTemporaryDirectory();
        var packagedSamples = Path.Combine(root, "TestAssets", "Results");
        Directory.CreateDirectory(packagedSamples);
        File.WriteAllText(Path.Combine(packagedSamples, "sample.xml"), "<sample />");

        try
        {
            ResultsDirectoryResolver.Resolve(string.Empty, root, string.Empty)
                .Should()
                .Be(packagedSamples);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the anonymized repository assets are available as samples.
    /// </summary>
    [TestMethod]
    public void Resolve_uses_the_repository_anonymized_assets()
    {
        var packagedResults = Path.Combine(AppContext.BaseDirectory, "assets", "Results");

        ResultsDirectoryResolver.Resolve(string.Empty, AppContext.BaseDirectory, string.Empty)
            .Should()
            .Be(packagedResults);

        Directory.GetFiles(packagedResults, "*.xml", SearchOption.TopDirectoryOnly)
            .Should()
            .NotBeEmpty();
    }

    /// <summary>
    /// Verifies that an explicitly configured results folder is preserved.
    /// </summary>
    [TestMethod]
    public void Resolve_uses_the_explicit_results_directory()
    {
        var root = CreateTemporaryDirectory();
        var configuredDirectory = Path.Combine(root, "custom-results");
        Directory.CreateDirectory(configuredDirectory);

        try
        {
            ResultsDirectoryResolver.Resolve(configuredDirectory, root, string.Empty)
                .Should()
                .Be(configuredDirectory);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lmu-results-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
