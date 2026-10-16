using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.ConsoleApp;

/// <summary>
/// Calculates lap-time statistics grouped by circuit and vehicle.
/// </summary>
public static class SessionStatistics
{
    /// <summary>
    /// Creates a chronological row for each session, grouped by venue and circuit layout.
    /// </summary>
    /// <param name="sessions">The imported sessions to analyze.</param>
    /// <returns>Session best laps and deltas from the previous measured session on that circuit.</returns>
    public static IReadOnlyList<CircuitStatisticsRow> ByCircuit(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var rows = new List<CircuitStatisticsRow>();
        foreach (var circuit in ExcludePracticeSessions(sessions)
                     .GroupBy(session => new CircuitKey(session.TrackVenue, session.TrackCourse))
                     .OrderBy(group => group.Key.TrackVenue, StringComparer.Ordinal)
                     .ThenBy(group => group.Key.TrackCourse, StringComparer.Ordinal))
        {
            TimeSpan? previousBestLap = null;
            foreach (var session in OrderChronologically(circuit))
            {
                var bestLap = GetBestLap(session.Drivers);
                rows.Add(new CircuitStatisticsRow(
                    session.TrackVenue,
                    session.TrackCourse,
                    session.StartedAt,
                    session.SessionType,
                    bestLap,
                    GetDelta(bestLap, previousBestLap)));

                if (bestLap is not null)
                {
                    previousBestLap = bestLap;
                }
            }
        }

        return rows.AsReadOnly();
    }

    /// <summary>
    /// Creates a chronological row for each session and vehicle group, using vehicle class and team.
    /// </summary>
    /// <param name="sessions">The imported sessions to analyze.</param>
    /// <returns>Vehicle best laps and deltas from the previous measured session for that vehicle and circuit.</returns>
    public static IReadOnlyList<CircuitVehicleStatisticsRow> ByCircuitAndCar(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var measurements = ExcludePracticeSessions(sessions)
            .SelectMany(session => session.Drivers
                .GroupBy(driver => new VehicleKey(
                    session.TrackVenue,
                    session.TrackCourse,
                    driver.CarClass,
                    driver.TeamName))
                .Select(group => new VehicleMeasurement(
                    group.Key,
                    session,
                    GetBestLap(group))))
            .GroupBy(measurement => measurement.Key)
            .OrderBy(group => group.Key.TrackVenue, StringComparer.Ordinal)
            .ThenBy(group => group.Key.TrackCourse, StringComparer.Ordinal)
            .ThenBy(group => group.Key.CarClass, StringComparer.Ordinal)
            .ThenBy(group => group.Key.TeamName, StringComparer.Ordinal);

        var rows = new List<CircuitVehicleStatisticsRow>();
        foreach (var vehicle in measurements)
        {
            TimeSpan? previousBestLap = null;
            foreach (var measurement in OrderChronologically(vehicle))
            {
                rows.Add(new CircuitVehicleStatisticsRow(
                    measurement.Key.TrackVenue,
                    measurement.Key.TrackCourse,
                    measurement.Key.CarClass,
                    measurement.Key.TeamName,
                    measurement.Session.StartedAt,
                    measurement.Session.SessionType,
                    measurement.BestLap,
                    GetDelta(measurement.BestLap, previousBestLap)));

                if (measurement.BestLap is not null)
                {
                    previousBestLap = measurement.BestLap;
                }
            }
        }

        return rows.AsReadOnly();
    }

    private static IEnumerable<Session> ExcludePracticeSessions(IEnumerable<Session> sessions) =>
        sessions.Where(session =>
            !session.SessionType.StartsWith("Practice", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<Session> OrderChronologically(IEnumerable<Session> sessions) =>
        sessions.OrderBy(session => session.StartedAt)
            .ThenBy(session => session.SessionType, StringComparer.Ordinal)
            .ThenBy(session => session.Id);

    private static IEnumerable<VehicleMeasurement> OrderChronologically(
        IEnumerable<VehicleMeasurement> measurements) =>
        measurements.OrderBy(measurement => measurement.Session.StartedAt)
            .ThenBy(measurement => measurement.Session.SessionType, StringComparer.Ordinal)
            .ThenBy(measurement => measurement.Session.Id);

    private static TimeSpan? GetBestLap(IEnumerable<Driver> drivers) =>
        drivers.SelectMany(driver => driver.Laps)
            .Where(lap => lap.Time is not null)
            .Select(lap => lap.Time)
            .Min();

    private static TimeSpan? GetDelta(TimeSpan? currentBestLap, TimeSpan? previousBestLap) =>
        currentBestLap is not null && previousBestLap is not null
            ? currentBestLap.Value - previousBestLap.Value
            : null;

    private sealed record CircuitKey(string TrackVenue, string TrackCourse);

    private sealed record VehicleKey(
        string TrackVenue,
        string TrackCourse,
        string CarClass,
        string TeamName);

    private sealed record VehicleMeasurement(VehicleKey Key, Session Session, TimeSpan? BestLap);
}

/// <summary>
/// Contains the best-lap measurement and delta for a session on one circuit.
/// </summary>
/// <param name="TrackVenue">The circuit venue.</param>
/// <param name="TrackCourse">The circuit layout.</param>
/// <param name="StartedAt">The session start time.</param>
/// <param name="SessionType">The session type.</param>
/// <param name="BestLap">The fastest valid lap, when available.</param>
/// <param name="Delta">The difference from the previous session's best lap, when available.</param>
public sealed record CircuitStatisticsRow(
    string TrackVenue,
    string TrackCourse,
    DateTimeOffset StartedAt,
    string SessionType,
    TimeSpan? BestLap,
    TimeSpan? Delta);

/// <summary>
/// Contains the best-lap measurement and delta for one vehicle group on one circuit.
/// </summary>
/// <param name="TrackVenue">The circuit venue.</param>
/// <param name="TrackCourse">The circuit layout.</param>
/// <param name="CarClass">The vehicle class.</param>
/// <param name="TeamName">The team name used to identify the vehicle group.</param>
/// <param name="StartedAt">The session start time.</param>
/// <param name="SessionType">The session type.</param>
/// <param name="BestLap">The fastest valid lap for the vehicle group, when available.</param>
/// <param name="Delta">The difference from the previous session's best lap for the group, when available.</param>
public sealed record CircuitVehicleStatisticsRow(
    string TrackVenue,
    string TrackCourse,
    string CarClass,
    string TeamName,
    DateTimeOffset StartedAt,
    string SessionType,
    TimeSpan? BestLap,
    TimeSpan? Delta);
