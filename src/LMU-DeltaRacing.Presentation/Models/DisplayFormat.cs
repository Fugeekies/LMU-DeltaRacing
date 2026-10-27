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
    /// Formats a position as a French ordinal: <c>1er</c>, then <c>2e</c>, <c>3e</c>…
    /// </summary>
    public static string Position(int? position) => position switch
    {
        null => Missing,
        1 => "1er",
        { } value => value.ToString(CultureInfo.InvariantCulture) + "e"
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
    /// Formats a distance in kilometers, without decimals.
    /// </summary>
    public static string Distance(double? kilometers) =>
        kilometers is { } value ? $"{value.ToString("#,0", NumberFormat)} km" : Missing;

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
    /// Formats a date and time in local time as <c>dd/MM/yyyy HH:mm</c>.
    /// </summary>
    public static string DateTime(DateTimeOffset date) =>
        date.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a day in local time as <c>dd/MM/yyyy</c>.
    /// </summary>
    public static string Day(DateTimeOffset date) =>
        date.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
