using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Builds the detailed classification for one imported session.
/// </summary>
/// <remarks>
/// Every comparison is made within the driver's vehicle class, as in WEC timing: a GT3 car is
/// compared with the best GT3 times, not with the prototypes. Only laps with a time count:
/// LMU blanks the time of laps invalidated for track limits, and incomplete laps have no time.
/// </remarks>
public static class RaceDetailsPresentation
{
    /// <summary>
    /// Creates a position-ordered classification with best sectors, ideal lap and gaps.
    /// </summary>
    public static IReadOnlyList<RaceDetailsDriverResult> CreateResults(Session session, string? userName)
    {
        ArgumentNullException.ThrowIfNull(session);

        var isRace = session.Kind == SessionKind.Race;
        var drivers = session.Drivers
            .Select(driver =>
            {
                var bestSectors = driver.Laps
                    .Where(lap => lap.Time.HasValue)
                    .SelectMany(lap => lap.Sectors)
                    .GroupBy(sector => sector.Number)
                    .ToDictionary(group => group.Key, group => group.Min(sector => sector.Time));
                TimeSpan? idealLap = Enumerable.Range(1, 3).All(bestSectors.ContainsKey)
                    ? bestSectors[1] + bestSectors[2] + bestSectors[3]
                    : null;
                return new DriverTiming(
                    driver,
                    driver.CarClass.Trim(),
                    driver.BestLap,
                    bestSectors,
                    idealLap);
            })
            .ToArray();

        var classes = drivers
            .GroupBy(driver => driver.CarClass, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new ClassReference(
                    group
                        .SelectMany(driver => driver.BestSectors)
                        .GroupBy(sector => sector.Key)
                        .ToDictionary(sector => sector.Key, sector => sector.Min(item => item.Value)),
                    group.Min(driver => driver.FastestLap),
                    group.Min(driver => driver.IdealLap),
                    group
                        .Where(driver => driver.Driver.ClassRank().HasValue)
                        .OrderBy(driver => driver.Driver.ClassRank())
                        .FirstOrDefault()),
                StringComparer.OrdinalIgnoreCase);

        return drivers
            .OrderBy(driver => driver.Driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.Driver.Name, StringComparer.OrdinalIgnoreCase)
            .Select(driver =>
            {
                var reference = classes[driver.CarClass];
                var sectors = Enumerable.Range(1, 3)
                    .Select(number => CreateSectorResult(
                        driver.BestSectors.TryGetValue(number, out var time) ? time : null,
                        reference.BestSectors.TryGetValue(number, out var classBest) ? classBest : null))
                    .ToArray();

                return new RaceDetailsDriverResult(
                    driver.Driver.Name,
                    driver.Driver.CarName(),
                    driver.Driver.Category(),
                    driver.Driver.Position,
                    driver.Driver.ClassPosition,
                    string.Equals(
                        driver.Driver.Name.Trim(),
                        userName?.Trim(),
                        StringComparison.OrdinalIgnoreCase),
                    driver.FastestLap,
                    sectors,
                    driver.IdealLap,
                    driver.IdealLap - reference.BestIdealLap,
                    driver.FastestLap - reference.BestLap,
                    isRace && !IsRetired(driver.Driver) ? driver.Driver.FinishTime : null,
                    isRace
                        ? GetRaceGap(driver.Driver, reference.Leader?.Driver)
                        : GetLapGap(driver, reference));
            })
            .ToArray();
    }

    private static RaceDetailsSectorResult CreateSectorResult(TimeSpan? time, TimeSpan? classBest) =>
        new(
            time,
            time - classBest,
            time is not null && time == classBest ? SectorPerformance.ClassBest : SectorPerformance.Standard);

    /// <summary>
    /// Race gap: retirement status first, then laps behind, then the total-time gap.
    /// </summary>
    private static LeaderGap GetRaceGap(Driver driver, Driver? leader)
    {
        if (leader is null)
        {
            return new LeaderGap(LeaderGapKind.Unavailable);
        }

        if (ReferenceEquals(driver, leader))
        {
            return new LeaderGap(LeaderGapKind.Leader);
        }

        if (IsRetired(driver))
        {
            return new LeaderGap(LeaderGapKind.Status, Status: driver.FinishStatus.ToUpperInvariant());
        }

        if (driver.LapsCompleted is { } laps
            && leader.LapsCompleted is { } leaderLaps
            && laps < leaderLaps)
        {
            return new LeaderGap(LeaderGapKind.Laps, Laps: leaderLaps - laps);
        }

        return driver.FinishTime is { } finishTime && leader.FinishTime is { } leaderFinishTime
            ? new LeaderGap(LeaderGapKind.Time, Time: finishTime - leaderFinishTime)
            : new LeaderGap(LeaderGapKind.Unavailable);
    }

    /// <summary>
    /// Qualifying and practice gap: fastest lap compared with the best lap of the class.
    /// </summary>
    private static LeaderGap GetLapGap(DriverTiming driver, ClassReference reference)
    {
        if (ReferenceEquals(driver, reference.Leader))
        {
            return new LeaderGap(LeaderGapKind.Leader);
        }

        return driver.FastestLap is { } lap && reference.BestLap is { } bestLap
            ? new LeaderGap(LeaderGapKind.Time, Time: lap - bestLap)
            : new LeaderGap(LeaderGapKind.Unavailable);
    }

    /// <summary>
    /// Whether the driver retired or was disqualified: their total time is not a race time.
    /// </summary>
    private static bool IsRetired(Driver driver) =>
        driver.FinishStatus.Equals("DNF", StringComparison.OrdinalIgnoreCase)
        || driver.FinishStatus.Equals("DQ", StringComparison.OrdinalIgnoreCase);

    private sealed record DriverTiming(
        Driver Driver,
        string CarClass,
        TimeSpan? FastestLap,
        IReadOnlyDictionary<int, TimeSpan> BestSectors,
        TimeSpan? IdealLap);

    private sealed record ClassReference(
        IReadOnlyDictionary<int, TimeSpan> BestSectors,
        TimeSpan? BestLap,
        TimeSpan? BestIdealLap,
        DriverTiming? Leader);
}

/// <summary>
/// A driver's result in a detailed session classification.
/// </summary>
/// <param name="Name">The driver name.</param>
/// <param name="Car">The vehicle model.</param>
/// <param name="Category">The WEC category of the vehicle class.</param>
/// <param name="Position">The overall position.</param>
/// <param name="ClassPosition">The position within the vehicle class.</param>
/// <param name="IsPlayer">Whether the driver is the selected driver.</param>
/// <param name="FastestLap">The fastest timed lap.</param>
/// <param name="Sectors">The driver's best time for each sector, across timed laps.</param>
/// <param name="IdealLap">The sum of the best sectors, when all three are known.</param>
/// <param name="DeltaToBestIdealLap">The gap between the ideal lap and the best ideal lap of the class.</param>
/// <param name="DeltaToBestLap">The gap between the fastest lap and the best lap of the class.</param>
/// <param name="TotalTime">The total race time, for classified race finishers.</param>
/// <param name="GapToLeader">The gap to the class leader.</param>
public sealed record RaceDetailsDriverResult(
    string Name,
    string Car,
    string Category,
    int? Position,
    int? ClassPosition,
    bool IsPlayer,
    TimeSpan? FastestLap,
    IReadOnlyList<RaceDetailsSectorResult> Sectors,
    TimeSpan? IdealLap,
    TimeSpan? DeltaToBestIdealLap,
    TimeSpan? DeltaToBestLap,
    TimeSpan? TotalTime,
    LeaderGap GapToLeader);

/// <summary>
/// A sector time and its WEC-style classification.
/// </summary>
/// <param name="Time">The driver's best time for the sector.</param>
/// <param name="Delta">The gap to the best time of the class for the sector.</param>
/// <param name="Performance">Whether the time is the best of the class.</param>
public sealed record RaceDetailsSectorResult(
    TimeSpan? Time,
    TimeSpan? Delta,
    SectorPerformance Performance);

/// <summary>
/// Describes a sector time relative to the best time of the class.
/// </summary>
public enum SectorPerformance
{
    /// <summary>The sector time is slower than the best of the class (yellow).</summary>
    Standard,

    /// <summary>The sector time is the best of the class (purple).</summary>
    ClassBest
}

/// <summary>
/// The gap between a driver and the leader of their class.
/// </summary>
/// <param name="Kind">How the gap is expressed.</param>
/// <param name="Time">The time gap, for <see cref="LeaderGapKind.Time"/>.</param>
/// <param name="Laps">The number of laps behind, for <see cref="LeaderGapKind.Laps"/>.</param>
/// <param name="Status">The finish status, for <see cref="LeaderGapKind.Status"/>.</param>
public sealed record LeaderGap(
    LeaderGapKind Kind,
    TimeSpan? Time = null,
    int? Laps = null,
    string? Status = null);

/// <summary>
/// Describes how a gap to the class leader is expressed.
/// </summary>
public enum LeaderGapKind
{
    /// <summary>No gap can be calculated.</summary>
    Unavailable,

    /// <summary>The driver leads the class.</summary>
    Leader,

    /// <summary>A time gap.</summary>
    Time,

    /// <summary>The driver is one or more laps behind the leader.</summary>
    Laps,

    /// <summary>The driver retired or was disqualified.</summary>
    Status
}
