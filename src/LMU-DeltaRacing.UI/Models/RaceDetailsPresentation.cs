using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Builds the detailed classification for one imported session.
/// </summary>
public static class RaceDetailsPresentation
{
    /// <summary>
    /// Creates a position-ordered classification with sector and fastest-lap deltas.
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
        var sessionBestLap = drivers
            .Where(driver => driver.FastestLap is not null)
            .Min(driver => driver.FastestLap!.Time);
        var orderedDrivers = drivers
            .OrderBy(driver => driver.Driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.Driver.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

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
                TimeSpan? lapDelta = sessionBestLap is { } bestLap && driver.FastestLap?.Time is { } lapTime
                    ? lapTime - bestLap
                    : null;

                return new RaceDetailsDriverResult(
                    driver.Driver.Name,
                    GetCarName(driver.Driver),
                    driver.Driver.Position,
                    driver.Driver.IsPlayer,
                    driver.FastestLap?.Time,
                    sectors,
                    lapDelta);
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

        TimeSpan? delta = time is { } sectorTime && sessionBest is { } bestTime
            ? sectorTime - bestTime
            : null;

        return new RaceDetailsSectorResult(number, time, delta, performance);
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
    TimeSpan? DeltaToBestLap);

/// <summary>
/// A sector time and its WEC-style performance classification.
/// </summary>
public sealed record RaceDetailsSectorResult(
    int Number,
    TimeSpan? Time,
    TimeSpan? Delta,
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
