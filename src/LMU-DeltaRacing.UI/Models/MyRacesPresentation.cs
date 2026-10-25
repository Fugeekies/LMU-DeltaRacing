using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Builds the selected driver's race and qualifying histories from imported LMU sessions.
/// </summary>
public static class MyRacesPresentation
{
    /// <summary>
    /// Creates one row per race and qualifying session of the selected driver, most recent first,
    /// with the delta to the previous session of the same type on the same circuit and category.
    /// </summary>
    /// <remarks>
    /// Practice sessions are not part of the history: they can be displayed in the race-details page.
    /// The vehicle is intentionally ignored by the delta: a car change within a category is still
    /// a comparable measure.
    /// </remarks>
    public static IReadOnlyList<MyRaceSession> CreateSessions(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var rows = new List<RowBuilder>();
        foreach (var session in sessions)
        {
            MyRaceSessionKind kind;
            if (session.SessionType.Contains("Race", StringComparison.OrdinalIgnoreCase))
            {
                kind = MyRaceSessionKind.Race;
            }
            else if (session.SessionType.Contains("Qual", StringComparison.OrdinalIgnoreCase))
            {
                kind = MyRaceSessionKind.Qualifying;
            }
            else
            {
                continue;
            }

            var driver = DriverProfile.FindDriver(session, userName);
            if (driver is null)
            {
                continue;
            }

            rows.Add(new RowBuilder
            {
                TrackVenue = session.TrackVenue,
                TrackCourse = session.TrackCourse,
                TrackEvent = session.TrackEvent,
                StartedAt = session.StartedAt,
                Kind = kind,
                Category = CarCategory.FromCarClass(driver.CarClass),
                Car = GetCarName(driver),
                BestLap = driver.Laps.Where(lap => lap.Time.HasValue).Select(lap => lap.Time).Min(),
                GridPosition = kind == MyRaceSessionKind.Race ? driver.GridPosition : null,
                LapsCompleted = driver.LapsCompleted,
                FinishTime = kind == MyRaceSessionKind.Race ? driver.FinishTime : null
            });
        }

        foreach (var history in rows.GroupBy(row => new { row.TrackVenue, row.TrackCourse, row.Category, row.Kind }))
        {
            TimeSpan? previousBestLap = null;
            foreach (var row in history.OrderBy(row => row.StartedAt))
            {
                if (row.BestLap is { } bestLap)
                {
                    row.Delta = previousBestLap is { } previous ? bestLap - previous : null;
                    previousBestLap = bestLap;
                }
            }
        }

        return rows
            .OrderByDescending(row => row.StartedAt)
            .Select(row => new MyRaceSession(
                row.TrackVenue,
                row.TrackCourse,
                row.TrackEvent,
                row.StartedAt,
                row.Kind,
                row.Category,
                row.Car,
                row.BestLap,
                row.Delta,
                row.GridPosition,
                row.LapsCompleted,
                row.FinishTime))
            .ToArray();
    }

    private static string GetCarName(Driver driver) =>
        !string.IsNullOrWhiteSpace(driver.CarType)
            ? driver.CarType
            : string.IsNullOrWhiteSpace(driver.CarNumber)
                ? "—"
                : $"#{driver.CarNumber}";

    private sealed class RowBuilder
    {
        public required string TrackVenue { get; init; }
        public required string TrackCourse { get; init; }
        public required string TrackEvent { get; init; }
        public required DateTimeOffset StartedAt { get; init; }
        public required MyRaceSessionKind Kind { get; init; }
        public required string Category { get; init; }
        public required string Car { get; init; }
        public TimeSpan? BestLap { get; init; }
        public TimeSpan? Delta { get; set; }
        public int? GridPosition { get; init; }
        public int? LapsCompleted { get; init; }
        public TimeSpan? FinishTime { get; init; }
    }
}
