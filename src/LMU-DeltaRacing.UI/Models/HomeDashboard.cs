namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The career statistics of the selected driver, as shown on the home dashboard.
/// </summary>
/// <param name="Races">The number of races.</param>
/// <param name="Wins">Races finished first in class.</param>
/// <param name="Podiums">Races finished in the class top 3.</param>
/// <param name="Top5">Races finished in the class top 5.</param>
/// <param name="Top10">Races finished in the class top 10.</param>
/// <param name="AveragePosition">The average class position of classified races.</param>
/// <param name="Poles">Qualifying sessions finished first in class.</param>
/// <param name="AverageQualifyingPosition">The average class position in qualifying.</param>
/// <param name="AverageGridPosition">The average class starting position of classified races.</param>
/// <param name="AveragePositionsGained">The average class positions gained between the grid and the finish.</param>
/// <param name="TotalPositionsGained">The class positions gained over all races, when at least one race has a class grid position.</param>
/// <param name="Retirements">Races ended with a DNF status.</param>
/// <param name="Disqualifications">Races ended with a DQ status.</param>
/// <param name="Unclassified">Races ended without a finish status ("None").</param>
/// <param name="RetirementCauses">The retirements grouped by cause, most frequent first.</param>
/// <param name="Laps">Laps completed in races and qualifying sessions.</param>
/// <param name="DistanceKm">The distance driven in races and qualifying sessions, in kilometers.</param>
/// <param name="TimeOnTrack">The sum of timed laps in races and qualifying sessions, in seconds.</param>
/// <param name="Circuits">The number of distinct circuits (venue and layout).</param>
/// <param name="RecentForm">The last ten races, most recent first.</param>
/// <param name="LastRace">The most recent race.</param>
/// <param name="FavoriteCircuit">The circuit with the most races.</param>
/// <param name="FavoriteCar">The car with the most races.</param>
/// <param name="CategoryShares">The share of races per category, most driven first.</param>
public sealed record HomeDashboard(
    HomeStat Races,
    HomeStat Wins,
    HomeStat Podiums,
    HomeStat Top5,
    HomeStat Top10,
    HomeStat AveragePosition,
    HomeStat Poles,
    HomeStat AverageQualifyingPosition,
    HomeStat AverageGridPosition,
    HomeStat AveragePositionsGained,
    int? TotalPositionsGained,
    HomeStat Retirements,
    HomeStat Disqualifications,
    HomeStat Unclassified,
    IReadOnlyList<HomeShare> RetirementCauses,
    HomeStat Laps,
    HomeStat DistanceKm,
    HomeStat TimeOnTrack,
    int Circuits,
    IReadOnlyList<HomeFormEntry> RecentForm,
    HomeFormEntry? LastRace,
    HomeFavorite? FavoriteCircuit,
    HomeFavorite? FavoriteCar,
    IReadOnlyList<HomeShare> CategoryShares)
{
    /// <summary>
    /// Gets whether the selection contains neither a race nor a qualifying session.
    /// </summary>
    public bool IsEmpty { get; init; }
}

/// <summary>
/// A dashboard value with its breakdown by category.
/// </summary>
/// <param name="Value">The total value, or <see langword="null"/> when it cannot be calculated.</param>
/// <param name="Rate">The share of the value in its reference count (races or qualifying sessions), when relevant.</param>
/// <param name="Categories">The value of each category, in display order.</param>
public sealed record HomeStat(double? Value, double? Rate, IReadOnlyList<HomeCategoryValue> Categories);

/// <summary>
/// The value of a dashboard statistic for one category.
/// </summary>
/// <param name="Category">The category name.</param>
/// <param name="Value">The value, or <see langword="null"/> when it cannot be calculated.</param>
public sealed record HomeCategoryValue(string Category, double? Value);

/// <summary>
/// One part of a distribution (a retirement cause, a category).
/// </summary>
/// <param name="Label">The displayed label.</param>
/// <param name="Count">The number of races.</param>
/// <param name="Share">The share of the whole, between 0 and 1.</param>
public sealed record HomeShare(string Label, int Count, double Share);

/// <summary>
/// The most driven circuit or car, with its results.
/// </summary>
/// <param name="Name">The circuit venue or the car model.</param>
/// <param name="Detail">The circuit layout when it differs from the venue.</param>
/// <param name="Races">The number of races.</param>
/// <param name="Wins">Races finished first in class.</param>
/// <param name="Podiums">Races finished in the class top 3.</param>
public sealed record HomeFavorite(string Name, string? Detail, int Races, int Wins, int Podiums);

/// <summary>
/// How a race ended for the selected driver.
/// </summary>
public enum HomeResultKind
{
    /// <summary>First in class.</summary>
    Win,

    /// <summary>Second or third in class.</summary>
    Podium,

    /// <summary>Classified outside the podium.</summary>
    Classified,

    /// <summary>Retired (DNF).</summary>
    Retired,

    /// <summary>Disqualified (DQ).</summary>
    Disqualified,

    /// <summary>Not classified (finish status "None").</summary>
    Unclassified
}

/// <summary>
/// One race of the recent form.
/// </summary>
/// <param name="EventId">The race-details event of the race, when it can be found.</param>
/// <param name="StartedAt">The race start time.</param>
/// <param name="TrackVenue">The track venue.</param>
/// <param name="TrackCourse">The track layout.</param>
/// <param name="Category">The category of the driver's car.</param>
/// <param name="Car">The car model.</param>
/// <param name="ClassPosition">The official class position.</param>
/// <param name="Kind">How the race ended.</param>
/// <param name="BestLap">The driver's best timed lap.</param>
public sealed record HomeFormEntry(
    string? EventId,
    DateTimeOffset StartedAt,
    string TrackVenue,
    string TrackCourse,
    string Category,
    string Car,
    int? ClassPosition,
    HomeResultKind Kind,
    TimeSpan? BestLap)
{
    /// <summary>
    /// Gets the short label of the result: "P3", "DNF", "DQ" or "NC".
    /// </summary>
    public string Label => Kind switch
    {
        HomeResultKind.Retired => "DNF",
        HomeResultKind.Disqualified => "DQ",
        HomeResultKind.Unclassified => "NC",
        _ => ClassPosition is { } position ? $"P{position}" : "—"
    };

    /// <summary>
    /// Gets the French label of the finish status.
    /// </summary>
    public string StatusLabel => Kind switch
    {
        HomeResultKind.Retired => "Abandon",
        HomeResultKind.Disqualified => "Disqualifié",
        HomeResultKind.Unclassified => "Non classé",
        _ => "Arrivé"
    };
}
