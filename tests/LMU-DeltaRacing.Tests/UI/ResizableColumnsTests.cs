using System.Text.RegularExpressions;
using FluentAssertions;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies that every table of the dashboard lets the user resize its columns.
/// </summary>
[TestClass]
public sealed partial class ResizableColumnsTests
{
    [TestMethod]
    public void Every_razor_table_is_marked_as_resizable()
    {
        var components = Path.Combine(FindRepositoryRoot(), "src", "LMU-DeltaRacing.UI", "Components");

        var tables = Directory
            .EnumerateFiles(components, "*.razor", SearchOption.AllDirectories)
            .SelectMany(file => TableTag().Matches(File.ReadAllText(file))
                .Select(match => (File: Path.GetFileName(file), Tag: match.Value)))
            .ToArray();

        tables.Should().NotBeEmpty();
        tables.Where(table => !table.Tag.Contains("data-resizable-columns", StringComparison.Ordinal))
            .Should().BeEmpty("resizable-columns.js only handles tables marked with data-resizable-columns");
    }

    // The sources are not copied next to the tests: walk up to the solution.
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LMU-DeltaRacing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("LMU-DeltaRacing.slnx not found.");
    }

    [GeneratedRegex(@"<table\b[^>]*>")]
    private static partial Regex TableTag();
}
