using System.Globalization;
using FluentAssertions;
using LMU_DeltaRacing.Presentation.Localization;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the choice of the interface language and the formats that depend on it.
/// </summary>
[TestClass]
public sealed class LanguageFormatsTests
{
    private CultureInfo? uiCulture;
    private DateFormatOption dateFormat;

    [TestInitialize]
    public void SaveCulture()
    {
        uiCulture = CultureInfo.CurrentUICulture;
        dateFormat = DisplayFormat.DateFormat;
    }

    [TestCleanup]
    public void RestoreCulture()
    {
        CultureInfo.CurrentUICulture = uiCulture!;
        DisplayFormat.DateFormat = dateFormat;
    }

    [TestMethod]
    [DataRow("de", "fr-FR", "de")]
    [DataRow("", "it-IT", "it")]
    [DataRow(null, "es-MX", "es")]
    [DataRow("", "pt-BR", "en")]
    [DataRow("nl", "fr-CA", "fr")]
    public void Resolve_prefers_the_setting_then_the_language_of_windows_then_english(
        string? configured,
        string windowsCulture,
        string expected)
    {
        UiLanguage.Resolve(configured, CultureInfo.GetCultureInfo(windowsCulture)).Should().Be(expected);
    }

    [TestMethod]
    [DataRow("fr", "1er", "2e", "11e", "21e")]
    [DataRow("en", "1st", "2nd", "11th", "21st")]
    [DataRow("de", "1.", "2.", "11.", "21.")]
    [DataRow("it", "1º", "2º", "11º", "21º")]
    [DataRow("es", "1.º", "2.º", "11.º", "21.º")]
    public void Position_writes_the_ordinal_of_the_language(string language, string first, string second, string eleventh, string twentyFirst)
    {
        CultureInfo.CurrentUICulture = UiLanguage.Culture(language);

        DisplayFormat.Position(1).Should().Be(first);
        DisplayFormat.Position(2).Should().Be(second);
        DisplayFormat.Position(11).Should().Be(eleventh);
        DisplayFormat.Position(21).Should().Be(twentyFirst);
    }

    [TestMethod]
    public void Position_writes_the_english_ordinals_of_3_12_and_13()
    {
        CultureInfo.CurrentUICulture = UiLanguage.Culture("en");

        DisplayFormat.Position(3).Should().Be("3rd");
        DisplayFormat.Position(12).Should().Be("12th");
        DisplayFormat.Position(13).Should().Be("13th");
        DisplayFormat.Position(22).Should().Be("22nd");
        DisplayFormat.Position(113).Should().Be("113th");
    }

    [TestMethod]
    [DataRow("fr", DateFormatOption.Language, "05/11/2026")]
    [DataRow("en", DateFormatOption.Language, "05/11/2026")]
    [DataRow("de", DateFormatOption.Language, "05.11.2026")]
    [DataRow("fr", DateFormatOption.MonthDayYear, "11/05/2026")]
    [DataRow("de", DateFormatOption.Iso, "2026-11-05")]
    [DataRow("en", DateFormatOption.DotDayMonthYear, "05.11.2026")]
    [DataRow("de", DateFormatOption.DayMonthYear, "05/11/2026")]
    public void Date_follows_the_date_format_option(string language, DateFormatOption option, string expected)
    {
        CultureInfo.CurrentUICulture = UiLanguage.Culture(language);
        DisplayFormat.DateFormat = option;

        DisplayFormat.Date(new DateTime(2026, 11, 5, 21, 0, 0)).Should().Be(expected);
        DisplayFormat.Date(new DateTime(2026, 11, 5, 21, 0, 0), withTime: true).Should().Be($"{expected} 21:00");
    }

    [TestMethod]
    [DataRow("fr", "novembre 2026")]
    [DataRow("en", "November 2026")]
    [DataRow("de", "November 2026")]
    [DataRow("it", "novembre 2026")]
    [DataRow("es", "noviembre 2026")]
    public void Month_periods_are_named_in_the_language(string language, string expected)
    {
        CultureInfo.CurrentUICulture = UiLanguage.Culture(language);

        ProgressionPeriods.Label(new DateTime(2026, 11, 1), ProgressionGranularity.Month).Should().Be(expected);
    }

    [TestMethod]
    public void Parse_reads_a_date_format_and_defaults_to_the_language()
    {
        DateFormatOptions.Parse("Iso").Should().Be(DateFormatOption.Iso);
        DateFormatOptions.Parse("monthdayyear").Should().Be(DateFormatOption.MonthDayYear);
        DateFormatOptions.Parse(null).Should().Be(DateFormatOption.Language);
        DateFormatOptions.Parse("unknown").Should().Be(DateFormatOption.Language);
    }
}
