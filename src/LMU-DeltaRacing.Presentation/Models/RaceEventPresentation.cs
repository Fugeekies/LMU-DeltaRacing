using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Groups imported sessions into race events (practice, qualifying and race) for the
/// race-details page.
/// </summary>
/// <remarks>
/// LMU writes one file per session. On a given circuit, the sessions of one event follow each
/// other in the order practice → qualifying → race, a few minutes apart, while two events are
/// separated by at least half an hour after a race. A session therefore joins the current event
/// when it keeps that order and starts at most two hours after the previous session.
/// Several practice sessions of one event are merged into a single classification.
/// </remarks>
public static class RaceEventPresentation
{
    private static readonly TimeSpan MaximumGapInsideEvent = TimeSpan.FromHours(2);

    /// <summary>
    /// Creates the events in which the selected driver took part, most recent first.
    /// </summary>
    public static IReadOnlyList<RaceEvent> CreateEvents(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var events = new List<RaceEvent>();
        foreach (var circuit in sessions
            .Where(session => GetOrder(session.SessionType) is not null)
            .GroupBy(session => new { session.TrackVenue, session.TrackCourse, session.TrackEvent }))
        {
            var current = new List<Session>();
            foreach (var session in circuit
                .OrderBy(session => session.StartedAt)
                .ThenBy(session => GetOrder(session.SessionType)))
            {
                if (current.Count > 0 && StartsNewEvent(current[^1], session))
                {
                    events.Add(CreateEvent(current));
                    current = [];
                }

                current.Add(session);
            }

            if (current.Count > 0)
            {
                events.Add(CreateEvent(current));
            }
        }

        return events
            .Where(raceEvent => raceEvent.Sessions.Any(session => DriverProfile.FindDriver(session, userName) is not null))
            .OrderByDescending(raceEvent => raceEvent.StartedAt)
            .ToArray();
    }

    private static bool StartsNewEvent(Session previous, Session next)
    {
        var previousOrder = GetOrder(previous.SessionType)!.Value;
        var nextOrder = GetOrder(next.SessionType)!.Value;

        // Only practice sessions can follow one another inside an event.
        var keepsOrder = nextOrder > previousOrder || (nextOrder == previousOrder && nextOrder == PracticeOrder);
        return !keepsOrder || next.StartedAt - previous.StartedAt > MaximumGapInsideEvent;
    }

    private const int PracticeOrder = 0;

    private static int? GetOrder(string sessionType) =>
        sessionType.StartsWith("Practice", StringComparison.OrdinalIgnoreCase) ? PracticeOrder
        : sessionType.Contains("Qual", StringComparison.OrdinalIgnoreCase) ? 1
        : sessionType.Contains("Race", StringComparison.OrdinalIgnoreCase) ? 2
        : null;

    private static RaceEvent CreateEvent(IReadOnlyList<Session> sessions)
    {
        var practices = sessions.Where(session => GetOrder(session.SessionType) == PracticeOrder).ToArray();
        var first = sessions[0];

        return new RaceEvent(
            first.Id.ToString(),
            first.TrackVenue,
            first.TrackCourse,
            first.TrackEvent,
            first.StartedAt,
            sessions.FirstOrDefault(session => GetOrder(session.SessionType) == 2),
            sessions.FirstOrDefault(session => GetOrder(session.SessionType) == 1),
            practices.Length switch
            {
                0 => null,
                1 => practices[0],
                _ => MergePractices(practices)
            },
            practices.Length);
    }

    /// <summary>
    /// Combines several practice sessions into one classification: every driver keeps all their
    /// laps, and drivers are ranked by best timed lap, overall and within their class.
    /// </summary>
    private static Session MergePractices(IReadOnlyList<Session> practices)
    {
        var drivers = practices
            .SelectMany(session => session.Drivers)
            .GroupBy(driver => driver.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var entries = group.ToArray();
                var laps = entries.SelectMany(driver => driver.Laps).ToArray();
                return new
                {
                    Template = entries[^1],
                    Laps = laps.Select((lap, index) => new Lap(index + 1, lap.Time, lap.Sectors)).ToArray(),
                    LapsCompleted = entries.Sum(driver => driver.LapsCompleted ?? driver.Laps.Count),
                    BestLap = laps.Where(lap => lap.Time.HasValue).Min(lap => lap.Time)
                };
            })
            .OrderBy(driver => driver.BestLap ?? TimeSpan.MaxValue)
            .ThenBy(driver => driver.Template.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var classRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var mergedDrivers = drivers.Select((driver, index) =>
        {
            var carClass = driver.Template.CarClass.Trim();
            classRanks[carClass] = classRanks.TryGetValue(carClass, out var rank) ? rank + 1 : 1;
            return new Driver(
                driver.Template.Name,
                driver.Template.CarNumber,
                driver.Template.CarClass,
                driver.Template.TeamName,
                index + 1,
                driver.Laps,
                driver.Template.CarType,
                classPosition: classRanks[carClass],
                lapsCompleted: driver.LapsCompleted);
        });

        var first = practices[0];
        return new Session(
            first.Id,
            first.StartedAt,
            first.TrackVenue,
            first.TrackCourse,
            first.TrackEvent,
            "Practice",
            mergedDrivers,
            drivers.Length == 0 ? null : drivers.Max(driver => driver.LapsCompleted),
            first.TrackLength);
    }
}

/// <summary>
/// The practice, qualifying and race sessions of one event on one circuit.
/// </summary>
/// <param name="Id">A stable identifier, taken from the first session of the event.</param>
/// <param name="TrackVenue">The track venue.</param>
/// <param name="TrackCourse">The track layout.</param>
/// <param name="TrackEvent">The event name.</param>
/// <param name="StartedAt">The start time of the first session.</param>
/// <param name="Race">The race, when the event has one.</param>
/// <param name="Qualifying">The qualifying session, when the event has one.</param>
/// <param name="Practice">The practice classification, merged when there were several sessions.</param>
/// <param name="PracticeSessionCount">The number of practice sessions merged into <paramref name="Practice"/>.</param>
public sealed record RaceEvent(
    string Id,
    string TrackVenue,
    string TrackCourse,
    string TrackEvent,
    DateTimeOffset StartedAt,
    Session? Race,
    Session? Qualifying,
    Session? Practice,
    int PracticeSessionCount)
{
    /// <summary>
    /// Gets the sessions of the event, race first.
    /// </summary>
    public IEnumerable<Session> Sessions =>
        new[] { Race, Qualifying, Practice }.OfType<Session>();
}
