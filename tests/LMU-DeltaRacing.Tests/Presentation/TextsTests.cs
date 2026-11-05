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

    [TestMethod]
    public void Every_text_is_a_valid_composite_format()
    {
        var arguments = Enumerable.Range(0, 10).Select(index => (object)index).ToArray();
        var invalid = new List<string>();
        var files = Directory.EnumerateFiles(Path.GetDirectoryName(ResourcePath())!, "Texts*.resx");
        foreach (var data in files.SelectMany(file => XDocument.Load(file).Root!.Elements("data")))
        {
            try
            {
                _ = string.Format(System.Globalization.CultureInfo.InvariantCulture, (string)data.Element("value")!, arguments);
            }
            catch (FormatException)
            {
                invalid.Add((string)data.Attribute("name")!);
            }
        }

        invalid.Should().BeEmpty("a text with a stray brace fails when it is shown");
    }

    [TestMethod]
    [DataRow("en", "Settings")]
    [DataRow("de", "Einstellungen")]
    [DataRow("it", "Impostazioni")]
    [DataRow("es", "Ajustes")]
    [DataRow("fr", "Options")]
    public void Get_reads_the_text_of_the_interface_language(string language, string expected)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = UiLanguage.Culture(language);

            Texts.Get("Nav.Settings").Should().Be(expected);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    [TestMethod]
    public void Every_translation_has_the_keys_and_placeholders_of_the_french_texts()
    {
        var french = Values(ResourcePath());

        foreach (var language in UiLanguage.Supported.Where(language => language != UiLanguage.French))
        {
            var translation = Values(ResourcePath().Replace("Texts.resx", $"Texts.{language}.resx", StringComparison.Ordinal));

            translation.Keys.Should().BeEquivalentTo(french.Keys, "Texts.{0}.resx translates every text", language);
            translation
                .Where(text => !Placeholders(text.Value).SetEquals(Placeholders(french[text.Key])))
                .Select(text => $"{language}: {text.Key}")
                .Should().BeEmpty("a translation keeps the values of its French text");
            translation.Where(text => string.IsNullOrWhiteSpace(text.Value)).Should().BeEmpty();
        }
    }

    private static Dictionary<string, string> Values(string path) =>
        XDocument.Load(path).Root!.Elements("data").ToDictionary(
            data => (string)data.Attribute("name")!,
            data => (string?)data.Element("value") ?? string.Empty,
            StringComparer.Ordinal);

    private static HashSet<string> Placeholders(string text) =>
        Placeholder().Matches(text).Select(match => match.Value).ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

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
