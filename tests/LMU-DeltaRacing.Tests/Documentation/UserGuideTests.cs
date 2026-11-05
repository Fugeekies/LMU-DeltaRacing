using System.Text.RegularExpressions;
using FluentAssertions;

namespace LMU_DeltaRacing.Tests.Documentation;

/// <summary>
/// Keeps the user guide (README.md and docs/guide/&lt;language&gt;) consistent across its languages.
/// </summary>
[TestClass]
public sealed partial class UserGuideTests
{
    private static readonly string[] Languages = ["en", "fr", "de", "it", "es"];

    [TestMethod]
    public void Every_language_has_the_same_pages()
    {
        var reference = Pages("fr");

        reference.Should().Contain("README.md");
        foreach (var language in Languages)
        {
            Pages(language).Should().BeEquivalentTo(reference, "the {0} guide must have one page per topic, like the others", language);
        }
    }

    [TestMethod]
    public void Every_page_opens_on_links_to_the_same_page_in_the_other_languages()
    {
        foreach (var language in Languages)
        {
            foreach (var page in Pages(language))
            {
                var firstLine = File.ReadLines(Path.Combine(GuideRoot(), language, page)).First();
                foreach (var other in Languages.Where(other => other != language))
                {
                    firstLine.Should().Contain($"(../{other}/{page})", "{0}/{1} links to its {2} version", language, page, other);
                }
            }
        }
    }

    [TestMethod]
    public void Relative_links_of_the_guide_and_the_readme_lead_to_existing_files()
    {
        var root = FindRepositoryRoot();
        var files = Directory.EnumerateFiles(GuideRoot(), "*.md", SearchOption.AllDirectories)
            .Append(Path.Combine(root, "README.md"));

        var links = files
            .SelectMany(file => MarkdownLink().Matches(File.ReadAllText(file))
                .Select(match => match.Groups["target"].Value)
                .Where(target => !target.StartsWith("http", StringComparison.Ordinal) && !target.StartsWith('#'))
                .Select(target => (File: Path.GetRelativePath(root, file), Target: target.Split('#')[0])))
            .ToArray();

        links.Should().HaveCountGreaterThan(Languages.Length * 10);
        links.Where(link => !File.Exists(Path.Combine(Path.GetDirectoryName(Path.Combine(root, link.File))!, link.Target)))
            .Should().BeEmpty();
    }

    private static string[] Pages(string language) =>
        Directory.EnumerateFiles(Path.Combine(GuideRoot(), language), "*.md")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string GuideRoot() => Path.Combine(FindRepositoryRoot(), "docs", "guide");

    // The documentation is not copied next to the tests: walk up to the solution.
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LMU-DeltaRacing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("LMU-DeltaRacing.slnx not found.");
    }

    [GeneratedRegex(@"\]\((?<target>[^)\s]+)\)")]
    private static partial Regex MarkdownLink();
}
