using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using LMU_DeltaRacing.Presentation.Localization;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the texts shown to the user (<c>Presentation/Resources/Texts.resx</c>) and their use.
/// </summary>
[TestClass]
public sealed partial class TextsTests
{
    [TestMethod]
    public void Get_reads_the_french_text_of_a_key()
    {
        Texts.Get("Nav.Settings").Should().Be("Options");
    }

    [TestMethod]
    public void Plural_puts_zero_and_one_in_the_singular_in_french()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");

            Texts.Plural("Common.Races", 0).Should().Be("0 course");
            Texts.Plural("Common.Races", 1).Should().Be("1 course");
            Texts.Plural("Common.Races", 3).Should().Be("3 courses");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    [TestMethod]
    public void Get_returns_an_unknown_key_as_is()
    {
        Texts.Get("Unknown.Key").Should().Be("Unknown.Key");
        Texts.Find("Unknown.Key").Should().BeNull();
    }

    [TestMethod]
    public void Every_key_used_in_the_code_exists_and_every_text_is_used()
    {
        var keys = ResourceKeys();
        var sources = SourceFiles().Select(File.ReadAllText).ToArray();
        var used = sources
            .SelectMany(source => UsedKey().Matches(source).Select(match => match.Groups["key"].Value)
                .Concat(UsedPlural().Matches(source).SelectMany(match => new[]
                {
                    $"{match.Groups["key"].Value}.One",
                    $"{match.Groups["key"].Value}.Other"
                })))
            .ToHashSet(StringComparer.Ordinal);
        // Keys built at run time ($"Category.{name}") cover every key of their prefix.
        var prefixes = sources
            .SelectMany(source => UsedPrefix().Matches(source).Select(match => match.Groups["prefix"].Value))
            .ToArray();

        used.Should().NotBeEmpty();
        used.Except(keys).Should().BeEmpty("every key read by the code exists in Texts.resx");
        keys.Where(key => !used.Contains(key) && !prefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal)))
            .Should().BeEmpty("every text of Texts.resx is shown somewhere");
    }

    [TestMethod]
    public void Every_text_has_a_value()
    {
        var resource = XDocument.Load(ResourcePath());

        resource.Root!.Elements("data")
            .Where(data => string.IsNullOrWhiteSpace((string?)data.Element("value")))
            .Select(data => (string?)data.Attribute("name"))
            .Should().BeEmpty();
    }

    private static HashSet<string> ResourceKeys() =>
        XDocument.Load(ResourcePath()).Root!.Elements("data")
            .Select(data => (string)data.Attribute("name")!)
            .ToHashSet(StringComparer.Ordinal);

    private static string ResourcePath() =>
        Path.Combine(FindRepositoryRoot(), "src", "LMU-DeltaRacing.Presentation", "Resources", "Texts.resx");

    private static IEnumerable<string> SourceFiles() =>
        new[] { "LMU-DeltaRacing.Presentation", "LMU-DeltaRacing.UI", "LMU-DeltaRacing.Console" }
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), "src", project), "*.*", SearchOption.AllDirectories))
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".razor", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

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

    [GeneratedRegex(@"(?:\bT\[|Texts\.(?:Get|Find|Format)\()""(?<key>[A-Za-z0-9_.]+)""")]
    private static partial Regex UsedKey();

    [GeneratedRegex(@"Texts\.Plural\(""(?<key>[A-Za-z0-9_.]+)""")]
    private static partial Regex UsedPlural();

    [GeneratedRegex(@"(?:\bT\[|Texts\.(?:Get|Find|Format)\()\$""(?<prefix>[A-Za-z0-9_.]+)\{")]
    private static partial Regex UsedPrefix();
}
