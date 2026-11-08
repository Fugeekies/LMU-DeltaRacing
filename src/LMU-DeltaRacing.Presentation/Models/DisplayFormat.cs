using System.Globalization;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Formats times, numbers and dates the same way in the dashboard pages and the console views.
/// A missing value is always shown as "—".
/// </summary>
public static class DisplayFormat
{
    /// <summary>
    /// The text shown for a missing or non-computable value.
    /// </summary>
    public const string Missing = "—";

    // Thousands are separated by a narrow no-break space (U+202F), as in French typography.
    private static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = " "
    };

    /// <summary>
    /// Gets or sets how dates are written (<c>LMU:DateFormat</c>), for the whole application: the
    /// dashboard has a single user. The console keeps the default, which follows the language.
    /// </summary>
    public static DateFormatOption DateFormat { get; set; } = DateFormatOption.Language;

    /// <summary>
    /// Formats a position as an ordinal of the interface language: <c>1er</c> / <c>2e</c> in French,
    /// <c>1st</c> / <c>2nd</c> in English, <c>1.</c> in German, <c>1º</c> in Italian, <c>1.º</c> in Spanish.
    /// </summary>
    public static string Position(int? position)
    {
        if (position is not { } value)
        {
            return Missing;
        }

        var number = value.ToString(CultureInfo.InvariantCulture);
        return Localization.UiLanguage.Current switch
        {
            "en" => number + EnglishSuffix(value),
            "de" => number + ".",
            "it" => number + "º",
            "es" => number + ".º",
            _ => value == 1 ? "1er" : number + "e"
        };
    }

    // 11th, 12th and 13th, but 1st, 2nd, 3rd, 21st, 22nd, 23rd...
    private static string EnglishSuffix(int value) =>
        (value % 100) is 11 or 12 or 13
            ? "th"
            : (value % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };

    /// <summary>
    /// Formats a lap time as <c>m:ss.fff</c>.
    /// </summary>
    public static string Lap(TimeSpan? time) =>
        time?.ToString(@"m\:ss\.fff", CultureInfo.InvariantCulture) ?? Missing;

    /// <summary>
    /// Formats a race time as <c>h:mm:ss.fff</c> from one hour, <c>m:ss.fff</c> below.
    /// </summary>
    public static string TotalTime(TimeSpan? time)
    {
        if (time is not { } value)
        {
            return Missing;
        }

        return value.TotalHours >= 1
            ? value.ToString(@"h\:mm\:ss\.fff", CultureInfo.InvariantCulture)
            : value.ToString(@"m\:ss\.fff", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a time difference in seconds with three decimals, signed when it is a loss.
    /// </summary>
    public static string Delta(TimeSpan? delta) =>
        delta is { } value
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"{(value > TimeSpan.Zero ? "+" : string.Empty)}{value.TotalSeconds:0.000}s")
            : Missing;

    /// <summary>
    /// Formats a spread of lap times in seconds with three decimals: <c>±0.412s</c>.
    /// </summary>
    public static string Deviation(TimeSpan? deviation) =>
        deviation is { } value
            ? string.Create(CultureInfo.InvariantCulture, $"±{value.TotalSeconds:0.000}s")
            : Missing;

    /// <summary>
    /// Formats a speed in km/h with one decimal: <c>263.1 km/h</c>.
    /// </summary>
    public static string Speed(double? kilometersPerHour) =>
        kilometersPerHour is { } value ? $"{value.ToString("0.0", NumberFormat)} km/h" : Missing;

    /// <summary>
    /// Formats a count with a narrow no-break space as thousands separator.
    /// </summary>
    public static string Count(double? value) =>
        value?.ToString("#,0", NumberFormat) ?? Missing;

    /// <summary>
    /// Formats an average position with one decimal.
    /// </summary>
    public static string Average(double? value) =>
        value?.ToString("0.0", NumberFormat) ?? Missing;

    /// <summary>
    /// Formats a gain with a leading "+" and a loss with a typographic minus sign.
    /// </summary>
    public static string Signed(double? value, string format) =>
        value is { } number
            ? (number > 0 ? "+" : string.Empty)
                + number.ToString(format, NumberFormat).Replace("-", "−", StringComparison.Ordinal)
            : Missing;

    /// <summary>
    /// Formats a driver rating in whole points: <c>1 568</c>.
    /// </summary>
    public static string Rating(double? rating) =>
        Count(rating is { } value ? Math.Round(value, MidpointRounding.AwayFromZero) : null);

    /// <summary>
    /// Formats a rating change in whole points: <c>+13</c>, <c>−4</c>, or <c>0</c> under half a point.
    /// </summary>
    public static string RatingChange(double change) =>
        // Adding 0.0 turns the -0 of a small loss into 0, which .NET would otherwise print "-0".
        Signed(Math.Round(change, MidpointRounding.AwayFromZero) + 0.0, "0");

    /// <summary>
    /// Formats a distance in kilometers, without decimals.
    /// </summary>
    public static string Distance(double? kilometers) =>
        kilometers is { } value ? $"{value.ToString("#,0", NumberFormat)} km" : Missing;

    /// <summary>
    /// Formats a time as minutes and seconds: <c>8 min 52 s</c>, or <c>42 s</c> under a minute.
    /// </summary>
    public static string Minutes(TimeSpan time)
    {
        var minutes = (int)time.TotalMinutes;
        return minutes == 0 ? $"{time.Seconds} s" : $"{minutes} min {time.Seconds:00} s";
    }

    /// <summary>
    /// Formats a duration in seconds as <c>h:mm</c>, hours not wrapping at 24.
    /// </summary>
    public static string Duration(double? seconds)
    {
        if (seconds is not { } value)
        {
            return Missing;
        }

        var duration = TimeSpan.FromSeconds(value);
        return $"{(int)duration.TotalHours}:{duration.Minutes:00}";
    }

    /// <summary>
    /// Formats a share between 0 and 1 as a percentage with one decimal.
    /// </summary>
    public static string Percent(double share) =>
        $"{(share * 100).ToString("0.0", NumberFormat)} %";

    /// <summary>
    /// Formats a date and time in local time with the date format (<c>05/11/2026 21:00</c>).
    /// </summary>
    public static string DateTime(DateTimeOffset date) => Date(date.LocalDateTime, withTime: true);

    /// <summary>
    /// Formats a day in local time with the date format (<c>05/11/2026</c>).
    /// </summary>
    public static string Day(DateTimeOffset date) => Date(date.LocalDateTime);

    /// <summary>
    /// Formats a date already in local time with the date format, and its time on 24 hours when asked.
    /// </summary>
    /// <param name="date">The local date.</param>
    /// <param name="withTime">Whether to add the time (<c>21:00</c>).</param>
    /// <param name="shortYear">Whether to write the year on two digits, for the chart axes.</param>
    public static string Date(System.DateTime date, bool withTime = false, bool shortYear = false)
    {
        var pattern = DateFormatOptions.Pattern(DateFormat, Localization.UiLanguage.Current);
        if (shortYear)
        {
            pattern = pattern.Replace("yyyy", "yy", StringComparison.Ordinal);
        }

        return date.ToString(withTime ? $"{pattern} HH:mm" : pattern, CultureInfo.InvariantCulture);
    }
}
