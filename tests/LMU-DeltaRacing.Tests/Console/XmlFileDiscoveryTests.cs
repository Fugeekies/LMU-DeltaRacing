using FluentAssertions;
using LMU_DeltaRacing.ConsoleApp;

namespace LMU_DeltaRacing.Tests.Console;

/// <summary>
/// Verifies discovery of XML result files for the console application.
/// </summary>
[TestClass]
public sealed class XmlFileDiscoveryTests
{
    /// <summary>
    /// Verifies that only top-level XML files are returned in deterministic order.
    /// </summary>
    [TestMethod]
    public void GetXmlFiles_returns_top_level_xml_files_in_name_order()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "nested"));

        try
        {
            var firstFile = Path.Combine(directory, "a.xml");
            var secondFile = Path.Combine(directory, "b.xml");
            File.WriteAllText(secondFile, string.Empty);
            File.WriteAllText(Path.Combine(directory, "notes.txt"), string.Empty);
            File.WriteAllText(Path.Combine(directory, "nested", "nested.xml"), string.Empty);
            File.WriteAllText(firstFile, string.Empty);

            var files = XmlFileDiscovery.GetXmlFiles(directory);

            files.Should().Equal(firstFile, secondFile);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the repository test-assets directory can be located from test output.
    /// </summary>
    [TestMethod]
    public void FindTestAssetsResultsDirectory_finds_the_repository_fixture_directory()
    {
        var directory = XmlFileDiscovery.FindTestAssetsResultsDirectory(AppContext.BaseDirectory);

        Directory.Exists(directory).Should().BeTrue();
        Path.GetFileName(directory).Should().Be("Results");
        Path.GetFileName(Path.GetDirectoryName(directory)!).Should().Be("testAssets");
    }

    /// <summary>
    /// Verifies that a missing results directory is reported explicitly.
    /// </summary>
    [TestMethod]
    public void GetXmlFiles_rejects_a_missing_directory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var act = () => XmlFileDiscovery.GetXmlFiles(path);

        act.Should().Throw<DirectoryNotFoundException>();
    }
}
