namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// How dates are written (<c>LMU:DateFormat</c>). The time is always written on 24 hours.
/// </summary>
public enum DateFormatOption
{
    /// <summary>The format of the interface language: <c>05/11/2026</c>, or <c>05.11.2026</c> in German.</summary>
    Language,

    /// <summary>Day first, with slashes: <c>05/11/2026</c>.</summary>
    DayMonthYear,

    /// <summary>Month first, with slashes, as in the United States: <c>11/05/2026</c>.</summary>
    MonthDayYear,

    /// <summary>ISO 8601: <c>2026-11-05</c>.</summary>
    Iso,

    /// <summary>Day first, with dots: <c>05.11.2026</c>.</summary>
    DotDayMonthYear
}

/// <summary>
/// Reads and describes the <see cref="DateFormatOption"/> values.
/// </summary>
public static class DateFormatOptions
{
    /// <summary>
    /// Gets the options in the order of the Options list.
    /// </summary>
    public static IReadOnlyList<DateFormatOption> All { get; } = Enum.GetValues<DateFormatOption>();

    /// <summary>
    /// Reads a setting, ignoring case. A missing or unknown value follows the language.
    /// </summary>
    /// <param name="value">The value of <c>LMU:DateFormat</c>.</param>
    public static DateFormatOption Parse(string? value) =>
        Enum.TryParse<DateFormatOption>(value?.Trim(), ignoreCase: true, out var option) && Enum.IsDefined(option)
            ? option
            : DateFormatOption.Language;

    /// <summary>
    /// Gets the .NET pattern of a date (without time) for an option and a language.
    /// </summary>
    /// <param name="option">The date format.</param>
    /// <param name="language">The two-letter code of the interface language.</param>
    public static string Pattern(DateFormatOption option, string language) => option switch
    {
        DateFormatOption.MonthDayYear => "MM/dd/yyyy",
        DateFormatOption.Iso => "yyyy-MM-dd",
        DateFormatOption.DotDayMonthYear => "dd.MM.yyyy",
        DateFormatOption.DayMonthYear => "dd/MM/yyyy",
        // Written here rather than read from the culture, whose patterns change with the system.
        _ => language == "de" ? "dd.MM.yyyy" : "dd/MM/yyyy"
    };
}
