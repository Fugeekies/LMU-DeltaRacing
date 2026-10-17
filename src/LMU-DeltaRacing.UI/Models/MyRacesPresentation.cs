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

            var car = !string.IsNullOrWhiteSpace(driver.CarType)
                ? driver.CarType
                : string.IsNullOrWhiteSpace(driver.CarNumber)
                    ? "—"
                    : $"#{driver.CarNumber}";

            results.Add(new SessionResult(
                session.TrackVenue,
                session.TrackCourse,
                session.TrackEvent,
                DateOnly.FromDateTime(session.StartedAt.Date),
                session.StartedAt,
                category,
                car,
                isQualifying,
                driver.Laps.Where(lap => lap.Time.HasValue).Select(lap => lap.Time).Min(),
                driver.GridPosition,
                driver.FinishTime,
                session.MostLapsCompleted));
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
                var gridPosition = group
                    .Where(result => !result.IsQualifying)
                    .Select(result => result.GridPosition)
                    .FirstOrDefault(position => position.HasValue);
                var finishTime = group
                    .Where(result => !result.IsQualifying)
                    .Select(result => result.FinishTime)
                    .FirstOrDefault(time => time.HasValue);
                var mostLapsCompleted = group
                    .Where(result => !result.IsQualifying)
                    .Select(result => result.MostLapsCompleted)
                    .FirstOrDefault(laps => laps.HasValue);

                return new RecordBuilder(
                    group.Key.TrackVenue,
                    group.Key.TrackCourse,
                    group.Key.TrackEvent,
                    group.Min(result => result.StartedAt),
                    player.Category,
                    player.Car,
                    qualifyingTime,
                    gridPosition,
                    raceTime,
                    finishTime,
                    mostLapsCompleted);
            })
            .ToArray();

        foreach (var history in records
            .GroupBy(record => new
            {
                record.TrackVenue,
                record.TrackCourse,
                record.Category
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
                record.Car,
                record.QualifyingTime,
                record.QualifyingDelta,
                record.GridPosition,
                record.RaceTime,
                record.RaceDelta,
                record.MostLapsCompleted,
                record.FinishTime))
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
        string Car,
        bool IsQualifying,
        TimeSpan? BestLap,
        int? GridPosition,
        TimeSpan? FinishTime,
        int? MostLapsCompleted);

    private sealed class RecordBuilder(
        string trackVenue,
        string trackCourse,
        string trackEvent,
        DateTimeOffset startedAt,
        string category,
        string car,
        TimeSpan? qualifyingTime,
        int? gridPosition,
        TimeSpan? raceTime,
        TimeSpan? finishTime,
        int? mostLapsCompleted)
    {
        public string TrackVenue { get; } = trackVenue;
        public string TrackCourse { get; } = trackCourse;
        public string TrackEvent { get; } = trackEvent;
        public DateTimeOffset StartedAt { get; } = startedAt;
        public string Category { get; } = category;
        public string Car { get; } = car;
        public TimeSpan? QualifyingTime { get; } = qualifyingTime;
        public TimeSpan? QualifyingDelta { get; set; }
        public int? GridPosition { get; } = gridPosition;
        public TimeSpan? RaceTime { get; } = raceTime;
        public TimeSpan? RaceDelta { get; set; }
        public int? MostLapsCompleted { get; } = mostLapsCompleted;
        public TimeSpan? FinishTime { get; } = finishTime;
    }
}
