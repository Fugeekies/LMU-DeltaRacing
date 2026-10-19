using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Builds the detailed classification for one imported session.
/// </summary>
public static class RaceDetailsPresentation
{
    /// <summary>
    /// Creates a position-ordered classification with fastest-lap sectors and leader gaps.
    /// </summary>
    public static IReadOnlyList<RaceDetailsDriverResult> CreateResults(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var drivers = session.Drivers
            .Select(driver => new DriverTiming(
                driver,
                driver.Laps
                    .Where(lap => lap.Time.HasValue)
                    .OrderBy(lap => lap.Time)
                    .FirstOrDefault(),
                driver.Laps
                    .SelectMany(lap => lap.Sectors)
                    .GroupBy(sector => sector.Number)
                    .ToDictionary(group => group.Key, group => group.Min(sector => sector.Time))))
            .ToArray();
        var sessionBestSectors = drivers
            .SelectMany(driver => driver.PersonalBestSectors)
            .GroupBy(sector => sector.Key)
            .ToDictionary(group => group.Key, group => group.Min(sector => sector.Value));
        var orderedDrivers = drivers
            .OrderBy(driver => driver.Driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.Driver.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var leader = orderedDrivers.FirstOrDefault();
        var compareFinishTimes = leader?.Driver.FinishTime.HasValue == true;
        var leaderTime = compareFinishTimes ? leader!.Driver.FinishTime : leader?.FastestLap?.Time;

        return orderedDrivers
            .Select(driver =>
            {
                var sectors = Enumerable.Range(1, 3)
                    .Select(number => CreateSectorResult(
                        number,
                        driver,
                        sessionBestSectors.TryGetValue(number, out var bestSector)
                            ? bestSector
                            : null))
                    .ToArray();
                var comparisonTime = compareFinishTimes
                    ? driver.Driver.FinishTime
                    : driver.FastestLap?.Time;
                TimeSpan? gap = leaderTime is { } firstTime && comparisonTime is { } time
                    ? time - firstTime
                    : null;

                return new RaceDetailsDriverResult(
                    driver.Driver.Name,
                    GetCarName(driver.Driver),
                    driver.Driver.Position,
                    driver.Driver.IsPlayer,
                    driver.FastestLap?.Time,
                    sectors,
                    gap);
            })
            .ToArray();
    }

    private static RaceDetailsSectorResult CreateSectorResult(
        int number,
        DriverTiming driver,
        TimeSpan? sessionBest)
    {
        var sector = driver.FastestLap?.Sectors.FirstOrDefault(sector => sector.Number == number);
        var time = sector?.Time;
        TimeSpan? personalBest = driver.PersonalBestSectors.TryGetValue(number, out var bestSector)
            ? bestSector
            : null;
        var performance = time is null
            ? SectorPerformance.Standard
            : time == sessionBest
                ? SectorPerformance.SessionBest
                : time == personalBest
                    ? SectorPerformance.PersonalBest
                    : SectorPerformance.Standard;

        return new RaceDetailsSectorResult(number, time, performance);
    }

    private static string GetCarName(Driver driver) =>
        !string.IsNullOrWhiteSpace(driver.CarType)
            ? driver.CarType
            : !string.IsNullOrWhiteSpace(driver.CarNumber)
                ? $"#{driver.CarNumber}"
                : "—";

    private sealed record DriverTiming(
        Driver Driver,
        Lap? FastestLap,
        IReadOnlyDictionary<int, TimeSpan> PersonalBestSectors);
}

/// <summary>
/// A driver's result in a detailed session classification.
/// </summary>
public sealed record RaceDetailsDriverResult(
    string Name,
    string Car,
    int? Position,
    bool IsPlayer,
    TimeSpan? FastestLap,
    IReadOnlyList<RaceDetailsSectorResult> Sectors,
    TimeSpan? GapToLeader);

/// <summary>
/// A sector time and its WEC-style performance classification.
/// </summary>
public sealed record RaceDetailsSectorResult(
    int Number,
    TimeSpan? Time,
    SectorPerformance Performance);

/// <summary>
/// Describes a sector time relative to the driver's and session's best.
/// </summary>
public enum SectorPerformance
{
    Standard,
    PersonalBest,
    SessionBest
}
