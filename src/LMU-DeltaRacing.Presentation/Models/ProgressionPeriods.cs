using System.Globalization;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Computes and labels the periods of the progression chart, in local time.
/// </summary>
public static class ProgressionPeriods
{
    // French names are written here so that labels do not depend on the installed cultures.
    private static readonly string[] MonthNames =
    [
        "janvier", "février", "mars", "avril", "mai", "juin",
        "juillet", "août", "septembre", "octobre", "novembre", "décembre"
    ];

    /// <summary>
    /// Returns the start of the period that contains a local date and time.
    /// </summary>
    public static DateTime Start(DateTime localTime, ProgressionGranularity granularity)
    {
        var day = localTime.Date;
        return granularity switch
        {
            ProgressionGranularity.Day => day,
            // ISO weeks start on Monday.
            ProgressionGranularity.Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
            ProgressionGranularity.Month => new DateTime(day.Year, day.Month, 1),
            _ => new DateTime(day.Year, 1, 1)
        };
    }

    /// <summary>
    /// Returns the start of the period that follows the one starting at <paramref name="periodStart"/>.
    /// </summary>
    public static DateTime Next(DateTime periodStart, ProgressionGranularity granularity) =>
        granularity switch
        {
            ProgressionGranularity.Day => periodStart.AddDays(1),
            ProgressionGranularity.Week => periodStart.AddDays(7),
            ProgressionGranularity.Month => periodStart.AddMonths(1),
            _ => periodStart.AddYears(1)
        };

    /// <summary>
    /// Returns the full French label of a period, for the summary, the detail panel and the period table.
    /// </summary>
    public static string Label(DateTime periodStart, ProgressionGranularity granularity) =>
        granularity switch
        {
            ProgressionGranularity.Day => periodStart.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            ProgressionGranularity.Week => $"semaine du {periodStart.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}",
            ProgressionGranularity.Month => $"{MonthNames[periodStart.Month - 1]} {periodStart.Year.ToString(CultureInfo.InvariantCulture)}",
            _ => periodStart.Year.ToString(CultureInfo.InvariantCulture)
        };

    /// <summary>
    /// Returns the label of a period on the date axis: the start date as <c>dd/MM/yyyy</c>.
    /// </summary>
    public static string AxisLabel(DateTime periodStart) =>
        periodStart.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
