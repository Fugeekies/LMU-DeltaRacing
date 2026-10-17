using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Builds vehicle performance history from imported LMU sessions.
/// </summary>
public static class MyRacesPresentation
{
    /// <summary>
    /// Combines qualifying and race results and calculates deltas against the previous
    /// matching session for each circuit, category, and vehicle.
    /// </summary>
    public static IReadOnlyList<MyRaceRecord> CreateRecords(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var results = new List<SessionResult>();
        foreach (var session in sessions)
        {
            var isQualifying = IsQualifying(session.SessionType);
            if (!isQualifying && !IsRace(session.SessionType))
            {
                continue;
            }

            var driver = session.Drivers.FirstOrDefault(driver => driver.IsPlayer);
            if (driver is null)
            {
                continue;
            }

            var category = GetCategory(driver.CarClass);
            if (category is null)
            {
                continue;
            }

            var vehicleKey = string.Join(
                "|",
                category,
                string.IsNullOrWhiteSpace(driver.CarNumber) ? driver.Name : driver.CarNumber,
                driver.TeamName);
            var car = string.IsNullOrWhiteSpace(driver.CarNumber)
                ? driver.TeamName
                : $"#{driver.CarNumber}{(string.IsNullOrWhiteSpace(driver.TeamName) ? string.Empty : $" · {driver.TeamName}")}";

            results.Add(new SessionResult(
                session.TrackVenue,
                session.TrackCourse,
                session.TrackEvent,
                DateOnly.FromDateTime(session.StartedAt.Date),
                session.StartedAt,
                category,
                vehicleKey,
                car,
                isQualifying,
                driver.Laps.Where(lap => lap.Time.HasValue).Select(lap => lap.Time).Min()));
        }

        var records = results
            .GroupBy(result => new
            {
                result.TrackVenue,
                result.TrackCourse,
                result.TrackEvent,
                result.Date
            })
            .Select(group =>
            {
                var player = group.First();
                var qualifyingTime = group
                    .Where(result => result.IsQualifying)
                    .Select(result => result.BestLap)
                    .Where(time => time.HasValue)
                    .Min();
                var raceTime = group
                    .Where(result => !result.IsQualifying)
                    .Select(result => result.BestLap)
                    .Where(time => time.HasValue)
                    .Min();

                return new RecordBuilder(
                    group.Key.TrackVenue,
                    group.Key.TrackCourse,
                    group.Key.TrackEvent,
                    group.Min(result => result.StartedAt),
                    player.Category,
                    player.VehicleKey,
                    player.Car,
                    qualifyingTime,
                    raceTime);
            })
            .ToArray();

        foreach (var history in records
            .GroupBy(record => new
            {
                record.TrackVenue,
                record.TrackCourse,
                record.Category,
                record.VehicleKey
            }))
        {
            TimeSpan? previousQualifyingTime = null;
            TimeSpan? previousRaceTime = null;

            foreach (var record in history.OrderBy(record => record.StartedAt))
            {
                if (record.QualifyingTime is { } qualifyingTime)
                {
                    record.QualifyingDelta = previousQualifyingTime is { } previous
                        ? qualifyingTime - previous
                        : null;
                    previousQualifyingTime = qualifyingTime;
                }

                if (record.RaceTime is { } raceTime)
                {
                    record.RaceDelta = previousRaceTime is { } previous
                        ? raceTime - previous
                        : null;
                    previousRaceTime = raceTime;
                }
            }
        }

        return records
            .OrderByDescending(record => record.StartedAt)
            .Select(record => new MyRaceRecord(
                record.TrackVenue,
                record.TrackCourse,
                record.TrackEvent,
                record.StartedAt,
                record.Category,
                record.VehicleKey,
                record.Car,
                record.QualifyingTime,
                record.QualifyingDelta,
                record.RaceTime,
                record.RaceDelta))
            .ToArray();
    }

    private static string? GetCategory(string carClass)
    {
        if (carClass.Contains("Hyper", StringComparison.OrdinalIgnoreCase))
        {
            return "Hypercar";
        }

        if (carClass.Contains("LMP2", StringComparison.OrdinalIgnoreCase)
            || carClass.Equals("P2", StringComparison.OrdinalIgnoreCase))
        {
            return "LMP2";
        }

        if (carClass.Contains("GT3", StringComparison.OrdinalIgnoreCase))
        {
            return "LMGT3";
        }

        return null;
    }

    private static bool IsQualifying(string sessionType) =>
        sessionType.Contains("Qual", StringComparison.OrdinalIgnoreCase);

    private static bool IsRace(string sessionType) =>
        sessionType.Contains("Race", StringComparison.OrdinalIgnoreCase);

    private sealed record SessionResult(
        string TrackVenue,
        string TrackCourse,
        string TrackEvent,
        DateOnly Date,
        DateTimeOffset StartedAt,
        string Category,
        string VehicleKey,
        string Car,
        bool IsQualifying,
        TimeSpan? BestLap);

    private sealed class RecordBuilder(
        string trackVenue,
        string trackCourse,
        string trackEvent,
        DateTimeOffset startedAt,
        string category,
        string vehicleKey,
        string car,
        TimeSpan? qualifyingTime,
        TimeSpan? raceTime)
    {
        public string TrackVenue { get; } = trackVenue;
        public string TrackCourse { get; } = trackCourse;
        public string TrackEvent { get; } = trackEvent;
        public DateTimeOffset StartedAt { get; } = startedAt;
        public string Category { get; } = category;
        public string VehicleKey { get; } = vehicleKey;
        public string Car { get; } = car;
        public TimeSpan? QualifyingTime { get; } = qualifyingTime;
        public TimeSpan? QualifyingDelta { get; set; }
        public TimeSpan? RaceTime { get; } = raceTime;
        public TimeSpan? RaceDelta { get; set; }
    }
}
