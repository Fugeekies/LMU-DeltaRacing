using System.Globalization;
using LMU_DeltaRacing.Presentation.Localization;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Computes and labels the periods of the progression chart, in local time.
/// </summary>
public static class ProgressionPeriods
{
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
    /// Returns the label of a period in the interface language, for the summary, the detail panel and the period table.
    /// </summary>
    public static string Label(DateTime periodStart, ProgressionGranularity granularity) =>
        granularity switch
        {
            ProgressionGranularity.Day => DisplayFormat.Date(periodStart),
            ProgressionGranularity.Week => Texts.Format("Progression.WeekOf", DisplayFormat.Date(periodStart)),
            ProgressionGranularity.Month => $"{UiLanguage.Culture(UiLanguage.Current).DateTimeFormat.GetMonthName(periodStart.Month)} {periodStart.Year.ToString(CultureInfo.InvariantCulture)}",
            _ => periodStart.Year.ToString(CultureInfo.InvariantCulture)
        };

    /// <summary>
    /// Returns the label of a period on the date axis: the start date, in the date format.
    /// </summary>
    public static string AxisLabel(DateTime periodStart) => DisplayFormat.Date(periodStart);
}
