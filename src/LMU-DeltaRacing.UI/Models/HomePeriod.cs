using System.Globalization;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The kinds of period offered by the home dashboard filter.
/// </summary>
public enum HomePeriodKind
{
    /// <summary>The whole career.</summary>
    All,

    /// <summary>The last 12 months.</summary>
    Last12Months,

    /// <summary>The last 30 days.</summary>
    Last30Days,

    /// <summary>One calendar year, in local time.</summary>
    Year
}

/// <summary>
/// A period of the home dashboard filter.
/// </summary>
/// <param name="Kind">The kind of period.</param>
/// <param name="Year">The calendar year, for <see cref="HomePeriodKind.Year"/>.</param>
public sealed record HomePeriod(HomePeriodKind Kind, int Year = 0)
{
    /// <summary>
    /// Gets the whole career.
    /// </summary>
    public static HomePeriod All { get; } = new(HomePeriodKind.All);

    /// <summary>
    /// Gets the last 12 months.
    /// </summary>
    public static HomePeriod Last12Months { get; } = new(HomePeriodKind.Last12Months);

    /// <summary>
    /// Gets the last 30 days.
    /// </summary>
    public static HomePeriod Last30Days { get; } = new(HomePeriodKind.Last30Days);

    /// <summary>
    /// Returns one calendar year.
    /// </summary>
    public static HomePeriod ForYear(int year) => new(HomePeriodKind.Year, year);

    /// <summary>
    /// Gets the value used by the filter drop-down list.
    /// </summary>
    public string Key => Kind switch
    {
        HomePeriodKind.Last12Months => "12m",
        HomePeriodKind.Last30Days => "30d",
        HomePeriodKind.Year => string.Create(CultureInfo.InvariantCulture, $"y{Year}"),
        _ => "all"
    };

    /// <summary>
    /// Gets the French label of the period.
    /// </summary>
    public string Label => Kind switch
    {
        HomePeriodKind.Last12Months => "12 derniers mois",
        HomePeriodKind.Last30Days => "30 derniers jours",
        HomePeriodKind.Year => Year.ToString(CultureInfo.InvariantCulture),
        _ => "Toute la carrière"
    };

    /// <summary>
    /// Reads a period from its <see cref="Key"/>; an unknown key means the whole career.
    /// </summary>
    public static HomePeriod Parse(string? key) => key switch
    {
        "12m" => Last12Months,
        "30d" => Last30Days,
        ['y', .. var year] when int.TryParse(year, NumberStyles.None, CultureInfo.InvariantCulture, out var value) => ForYear(value),
        _ => All
    };

    /// <summary>
    /// Returns whether a session started within the period.
    /// </summary>
    /// <param name="startedAt">The session start time.</param>
    /// <param name="now">The current time, which ends the rolling periods.</param>
    public bool Contains(DateTimeOffset startedAt, DateTimeOffset now) => Kind switch
    {
        HomePeriodKind.Last12Months => startedAt >= now.AddMonths(-12),
        HomePeriodKind.Last30Days => startedAt >= now.AddDays(-30),
        HomePeriodKind.Year => startedAt.ToLocalTime().Year == Year,
        _ => true
    };
}
