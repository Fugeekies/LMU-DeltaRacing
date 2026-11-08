using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Builds the personal records of the selected driver on one circuit and in one category, and the
/// comparison of their sessions there.
/// </summary>
/// <remarks>
/// As for the progression, only qualifying sessions and races count, with driver merges applied.
/// A record belongs to a car (<c>CarType</c>): a variant is a car of its own. A session breaks a
/// record when it beats the record of the sessions before it with the same car; the first session
/// of a car sets its record without breaking one.
/// </remarks>
public static class PersonalRecordsPresentation
{
    /// <summary>
    /// Creates the records and the sessions of a circuit and a category.
    /// </summary>
    /// <param name="sessions">The imported sessions, with driver merges applied.</param>
    /// <param name="userName">The selected driver.</param>
    /// <param name="trackVenue">The track venue.</param>
    /// <param name="trackCourse">The track layout.</param>
    /// <param name="category">The category, as returned by <see cref="CarCategory.FromCarClass"/>.</param>
    public static PersonalRecords Create(
        IEnumerable<Session> sessions,
        string? userName,
        string trackVenue,
        string trackCourse,
        string category)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(trackVenue);
        ArgumentNullException.ThrowIfNull(trackCourse);
        ArgumentNullException.ThrowIfNull(category);

        var results = sessions
            .Where(session => session.Kind is SessionKind.Qualifying or SessionKind.Race
                && string.Equals(session.TrackVenue.Trim(), trackVenue.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(session.TrackCourse.Trim(), trackCourse.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(session => (Session: session, Driver: DriverProfile.FindDriver(session, userName)))
            .Where(result => result.Driver is not null && string.Equals(result.Driver.Category(), category, StringComparison.Ordinal))
            .Select(result => (result.Session, Driver: result.Driver!))
            .OrderBy(result => result.Session.StartedAt)
            .ToArray();

        var lapRecords = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
        var speedRecords = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<SessionComparison>(results.Length);
        foreach (var (session, driver) in results)
        {
            var car = driver.CarName();
            var bestLap = driver.BestLap;
            var topSpeed = LapAnalysisPresentation.TopSpeed(driver);
            var isRace = session.Kind == SessionKind.Race;
            var brokeLap = bestLap is { } lap && lapRecords.TryGetValue(car, out var lapRecord) && lap < lapRecord;
            var brokeSpeed = topSpeed is { } speed && speedRecords.TryGetValue(car, out var speedRecord) && speed > speedRecord;
            if (bestLap is { } newLap && (!lapRecords.TryGetValue(car, out var oldLap) || newLap < oldLap))
            {
                lapRecords[car] = newLap;
            }

            if (topSpeed is { } newSpeed && (!speedRecords.TryGetValue(car, out var oldSpeed) || newSpeed > oldSpeed))
            {
                speedRecords[car] = newSpeed;
            }

            rows.Add(new SessionComparison(
                session.Id,
                session.StartedAt,
                session.Kind,
                car,
                driver.ClassRank(),
                bestLap,
                null,
                isRace ? LapAnalysisPresentation.Pace(driver) : null,
                isRace ? LapAnalysisPresentation.Deviation(driver) : null,
                topSpeed,
                isRace ? driver.PitStops : null,
                brokeLap,
                brokeSpeed));
        }

        // The gap is measured against the record of today, once every session is known.
        var comparisons = rows
            .Select(row => row with
            {
                GapToRecord = row.BestLap is { } lap && lapRecords.TryGetValue(row.Car, out var record) ? lap - record : null
            })
            .OrderByDescending(row => row.StartedAt)
            .ToArray();

        var cars = results
            .GroupBy(result => result.Driver.CarName(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var best = group
                    .Where(result => result.Driver.BestLap.HasValue)
                    .OrderBy(result => result.Driver.BestLap)
                    .ThenBy(result => result.Session.StartedAt)
                    .FirstOrDefault();
                var fastest = group
                    .Where(result => LapAnalysisPresentation.TopSpeed(result.Driver).HasValue)
                    .OrderByDescending(result => LapAnalysisPresentation.TopSpeed(result.Driver))
                    .ThenBy(result => result.Session.StartedAt)
                    .FirstOrDefault();
                return new CarRecord(
                    group.Key,
                    group.Count(),
                    best.Session is null ? null : new LapRecord(best.Driver.BestLap!.Value, best.Session.StartedAt, best.Session.Kind),
                    fastest.Session is null
                        ? null
                        : new SpeedRecord(LapAnalysisPresentation.TopSpeed(fastest.Driver)!.Value, fastest.Session.StartedAt, fastest.Session.Kind));
            })
            .OrderBy(record => record.BestLap?.Time ?? TimeSpan.MaxValue)
            .ThenBy(record => record.Car, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PersonalRecords(cars, comparisons);
    }
}

/// <summary>
/// The personal records of a circuit and a category, and the sessions driven there.
/// </summary>
/// <param name="Cars">The records of each car, fastest lap first.</param>
/// <param name="Sessions">The sessions, most recent first.</param>
public sealed record PersonalRecords(IReadOnlyList<CarRecord> Cars, IReadOnlyList<SessionComparison> Sessions);

/// <summary>
/// The records of one car on a circuit.
/// </summary>
/// <param name="Car">The car name.</param>
/// <param name="Sessions">The number of sessions with this car.</param>
/// <param name="BestLap">The best lap, when one was timed.</param>
/// <param name="TopSpeed">The highest top speed, when known.</param>
public sealed record CarRecord(string Car, int Sessions, LapRecord? BestLap, SpeedRecord? TopSpeed);

/// <summary>
/// A lap time record.
/// </summary>
/// <param name="Time">The lap time.</param>
/// <param name="StartedAt">The start of the session of the record.</param>
/// <param name="Kind">The kind of the session of the record.</param>
public sealed record LapRecord(TimeSpan Time, DateTimeOffset StartedAt, SessionKind Kind);

/// <summary>
/// A top speed record.
/// </summary>
/// <param name="Speed">The top speed in km/h.</param>
/// <param name="StartedAt">The start of the session of the record.</param>
/// <param name="Kind">The kind of the session of the record.</param>
public sealed record SpeedRecord(double Speed, DateTimeOffset StartedAt, SessionKind Kind);

/// <summary>
/// One session of the selected driver, compared with the others of the same circuit and category.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="StartedAt">The start of the session.</param>
/// <param name="Kind">The kind of session, qualifying or race.</param>
/// <param name="Car">The car name.</param>
/// <param name="ClassPosition">The finishing position in the class.</param>
/// <param name="BestLap">The best timed lap.</param>
/// <param name="GapToRecord">The best lap minus the record of the car today.</param>
/// <param name="Pace">The median of the representative laps, in a race.</param>
/// <param name="Deviation">The standard deviation of the representative laps, in a race.</param>
/// <param name="TopSpeed">The top speed in km/h, when known.</param>
/// <param name="PitStops">The pit stops counted by LMU, in a race.</param>
/// <param name="BrokeLapRecord">Whether the best lap beat the record of the earlier sessions with this car.</param>
/// <param name="BrokeSpeedRecord">Whether the top speed beat the record of the earlier sessions with this car.</param>
public sealed record SessionComparison(
    Guid SessionId,
    DateTimeOffset StartedAt,
    SessionKind Kind,
    string Car,
    int? ClassPosition,
    TimeSpan? BestLap,
    TimeSpan? GapToRecord,
    TimeSpan? Pace,
    TimeSpan? Deviation,
    double? TopSpeed,
    int? PitStops,
    bool BrokeLapRecord,
    bool BrokeSpeedRecord);
