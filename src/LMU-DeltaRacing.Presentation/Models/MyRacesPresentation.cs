using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Builds the selected driver's performance history from imported LMU sessions.
/// </summary>
public static class MyRacesPresentation
{
    /// <summary>
    /// Pairs each race with the qualifying session that precedes it on the same circuit, then
    /// calculates deltas against the previous record of the same circuit and category.
    /// </summary>
    /// <remarks>
    /// LMU online events run several qualifying/race pairs per evening on the same circuit, so
    /// sessions are paired chronologically instead of being grouped by day. A qualifying session
    /// followed by another qualifying session, or a race without a preceding qualifying session,
    /// produces a partial record.
    /// </remarks>
    public static IReadOnlyList<MyRaceRecord> CreateRecords(IEnumerable<Session> sessions, string? userName)
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

            var driver = DriverProfile.FindDriver(session, userName);
            if (driver is null)
            {
                continue;
            }

            results.Add(new SessionResult(
                session.TrackVenue,
                session.TrackCourse,
                session.TrackEvent,
                session.StartedAt,
                isQualifying,
                CarCategory.FromCarClass(driver.CarClass),
                GetCarName(driver),
                driver.Laps.Where(lap => lap.Time.HasValue).Select(lap => lap.Time).Min(),
                driver.GridPosition,
                driver.FinishTime,
                driver.LapsCompleted));
        }

        var records = new List<RecordBuilder>();
        foreach (var circuit in results
            .OrderBy(result => result.StartedAt)
            .ThenBy(result => result.IsQualifying ? 0 : 1)
            .GroupBy(result => new { result.TrackVenue, result.TrackCourse, result.TrackEvent }))
        {
            SessionResult? pendingQualifying = null;
            foreach (var result in circuit)
            {
                if (result.IsQualifying)
                {
                    // A second qualifying session before any race leaves the first one unpaired.
                    if (pendingQualifying is not null)
                    {
                        records.Add(RecordBuilder.Create(pendingQualifying, null));
                    }

                    pendingQualifying = result;
                }
                else
                {
                    records.Add(RecordBuilder.Create(pendingQualifying, result));
                    pendingQualifying = null;
                }
            }

            if (pendingQualifying is not null)
            {
                records.Add(RecordBuilder.Create(pendingQualifying, null));
            }
        }

        // The vehicle is intentionally ignored: a car change within a category is still a comparable measure.
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
                record.LapsCompleted,
                record.FinishTime))
            .ToArray();
    }

    private static string GetCarName(Driver driver) =>
        !string.IsNullOrWhiteSpace(driver.CarType)
            ? driver.CarType
            : string.IsNullOrWhiteSpace(driver.CarNumber)
                ? "—"
                : $"#{driver.CarNumber}";

    private static bool IsQualifying(string sessionType) =>
        sessionType.Contains("Qual", StringComparison.OrdinalIgnoreCase);

    private static bool IsRace(string sessionType) =>
        sessionType.Contains("Race", StringComparison.OrdinalIgnoreCase);

    private sealed record SessionResult(
        string TrackVenue,
        string TrackCourse,
        string TrackEvent,
        DateTimeOffset StartedAt,
        bool IsQualifying,
        string Category,
        string Car,
        TimeSpan? BestLap,
        int? GridPosition,
        TimeSpan? FinishTime,
        int? LapsCompleted);

    private sealed class RecordBuilder
    {
        public required string TrackVenue { get; init; }
        public required string TrackCourse { get; init; }
        public required string TrackEvent { get; init; }
        public required DateTimeOffset StartedAt { get; init; }
        public required string Category { get; init; }
        public required string Car { get; init; }
        public TimeSpan? QualifyingTime { get; init; }
        public TimeSpan? QualifyingDelta { get; set; }
        public int? GridPosition { get; init; }
        public TimeSpan? RaceTime { get; init; }
        public TimeSpan? RaceDelta { get; set; }
        public int? LapsCompleted { get; init; }
        public TimeSpan? FinishTime { get; init; }

        /// <summary>
        /// Creates a record from a qualifying session, a race, or both.
        /// The race defines the category and car because it is the result that counts.
        /// </summary>
        public static RecordBuilder Create(SessionResult? qualifying, SessionResult? race)
        {
            var reference = race ?? qualifying
                ?? throw new ArgumentException("A record needs a qualifying session or a race.");

            return new RecordBuilder
            {
                TrackVenue = reference.TrackVenue,
                TrackCourse = reference.TrackCourse,
                TrackEvent = reference.TrackEvent,
                StartedAt = (qualifying ?? race)!.StartedAt,
                Category = reference.Category,
                Car = reference.Car,
                QualifyingTime = qualifying?.BestLap,
                GridPosition = race?.GridPosition,
                RaceTime = race?.BestLap,
                LapsCompleted = race?.LapsCompleted,
                FinishTime = race?.FinishTime
            };
        }
    }
}
