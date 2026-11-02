using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Turns the events of session logs into the conduct of each driver: contacts, track limits and
/// penalties.
/// </summary>
/// <remarks>
/// An event is not a contact: each car reports its own, often several times in one accident. A
/// contact therefore counts once for each driver involved, whichever car reported it.
/// </remarks>
public static class DrivingConductPresentation
{
    // Reports with the same target closer than this belong to one accident. On the fixtures, 89% of
    // the reports of a pair of drivers follow within 5 s, and only 2% between 5 and 10 s.
    private static readonly TimeSpan SameContactWindow = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Creates the chronological incidents of each driver of a session, by driver name. Reports of
    /// a same contact are merged into the first one.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<DrivingIncident>> CreateIncidents(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var incidents = new Dictionary<string, List<DrivingIncident>>(StringComparer.OrdinalIgnoreCase);
        var lastReports = new Dictionary<(string Driver, SessionEventKind Kind, string Target), TimeSpan>();

        void Add(string driver, SessionEventKind kind, string? detail, int? lap, TimeSpan elapsed)
        {
            var isContact = kind is SessionEventKind.VehicleContact or SessionEventKind.ObstacleContact or SessionEventKind.OtherContact;
            if (isContact)
            {
                var key = (driver, kind, (detail ?? string.Empty).ToUpperInvariant());
                var isSameContact = lastReports.TryGetValue(key, out var lastReport) && elapsed - lastReport < SameContactWindow;
                lastReports[key] = elapsed;
                if (isSameContact)
                {
                    return;
                }
            }

            if (!incidents.TryGetValue(driver, out var driverIncidents))
            {
                incidents[driver] = driverIncidents = [];
            }

            driverIncidents.Add(new DrivingIncident(elapsed, kind, detail, lap));
        }

        foreach (var sessionEvent in session.Events.OrderBy(sessionEvent => sessionEvent.Elapsed))
        {
            var driver = sessionEvent.DriverName.Trim();
            Add(driver, sessionEvent.Kind, sessionEvent.Detail, sessionEvent.Lap, sessionEvent.Elapsed);

            // The opponent lives the same contact, even when their car did not report it.
            if (sessionEvent.Kind == SessionEventKind.VehicleContact && sessionEvent.Detail is { } opponent)
            {
                Add(opponent.Trim(), sessionEvent.Kind, driver, sessionEvent.Lap, sessionEvent.Elapsed);
            }
        }

        return incidents.ToDictionary(
            driver => driver.Key,
            driver => (IReadOnlyList<DrivingIncident>)driver.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Counts incidents by family. Contacts whose meaning is not established are not counted.
    /// </summary>
    public static DrivingConduct Summarize(IEnumerable<DrivingIncident> incidents)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        var kinds = incidents.Select(incident => incident.Kind).ToArray();
        int Count(SessionEventKind kind) => kinds.Count(value => value == kind);
        return new DrivingConduct(
            Count(SessionEventKind.VehicleContact),
            Count(SessionEventKind.ObstacleContact),
            Count(SessionEventKind.TrackLimitWarning),
            Count(SessionEventKind.InvalidLap),
            Count(SessionEventKind.Penalty));
    }

    /// <summary>
    /// Creates the driving profile of each driver over their qualifying and race sessions.
    /// </summary>
    /// <remarks>
    /// Practice sessions are left out, like in the other result views: they are optional and a
    /// driver tries things there, so they would make profiles depend on the practice option.
    /// </remarks>
    public static IReadOnlyList<DrivingProfile> CreateProfiles(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var totals = new Dictionary<string, ProfileTotals>(StringComparer.OrdinalIgnoreCase);
        foreach (var session in sessions.Where(session => session.Kind is SessionKind.Qualifying or SessionKind.Race))
        {
            var incidents = CreateIncidents(session);
            foreach (var driver in session.Drivers.GroupBy(driver => driver.Name.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                if (!totals.TryGetValue(driver.Key, out var total))
                {
                    totals[driver.Key] = total = new ProfileTotals(driver.Key);
                }

                total.Sessions++;
                total.Laps += driver.Sum(result => result.LapsCompleted ?? result.Laps.Count);
                if (incidents.TryGetValue(driver.Key, out var driverIncidents))
                {
                    total.Conduct += Summarize(driverIncidents);
                }
            }
        }

        return totals.Values
            .Select(total => new DrivingProfile(
                total.Name,
                string.Equals(total.Name, userName?.Trim(), StringComparison.OrdinalIgnoreCase),
                total.Sessions,
                total.Laps,
                total.Conduct))
            .ToArray();
    }

    /// <summary>
    /// Words an incident for the timeline of a driver. LMU reasons stay in English, as written.
    /// </summary>
    public static string Describe(DrivingIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        return incident.Kind switch
        {
            SessionEventKind.VehicleContact => $"Contact avec {incident.Detail}",
            SessionEventKind.ObstacleContact => incident.Detail switch
            {
                "Immovable" => "Contact avec un obstacle fixe",
                "Post" => "Contact avec un poteau",
                "Sign" => "Contact avec un panneau",
                "Cone" => "Contact avec un cône",
                _ => $"Contact avec {incident.Detail}"
            },
            SessionEventKind.OtherContact => $"Contact signalé : {incident.Detail}",
            SessionEventKind.TrackLimitWarning => "Avertissement de limites de piste",
            SessionEventKind.InvalidLap => "Tour invalidé",
            _ => $"Pénalité : {incident.Detail}"
        };
    }

    private sealed class ProfileTotals(string name)
    {
        public string Name { get; } = name;
        public int Sessions { get; set; }
        public int Laps { get; set; }
        public DrivingConduct Conduct { get; set; } = new(0, 0, 0, 0, 0);
    }
}

/// <summary>
/// One moment of a driver's session: a contact, a track-limit event or a penalty.
/// </summary>
/// <param name="Elapsed">The time elapsed since the start of the session.</param>
/// <param name="Kind">The kind of incident.</param>
/// <param name="Detail">The opponent, the object of the contact or the reason of the penalty.</param>
/// <param name="Lap">The lap of the incident (from 1), when LMU gives it.</param>
public sealed record DrivingIncident(TimeSpan Elapsed, SessionEventKind Kind, string? Detail, int? Lap);

/// <summary>
/// The number of incidents of a driver, by family.
/// </summary>
/// <param name="VehicleContacts">The contacts with another car.</param>
/// <param name="ObstacleContacts">The contacts with an obstacle of the track.</param>
/// <param name="Warnings">The track-limit warnings.</param>
/// <param name="InvalidLaps">The laps invalidated for cutting the track.</param>
/// <param name="Penalties">The penalties received.</param>
public sealed record DrivingConduct(
    int VehicleContacts,
    int ObstacleContacts,
    int Warnings,
    int InvalidLaps,
    int Penalties)
{
    /// <summary>
    /// Adds up two sets of incidents.
    /// </summary>
    public static DrivingConduct operator +(DrivingConduct left, DrivingConduct right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return new DrivingConduct(
            left.VehicleContacts + right.VehicleContacts,
            left.ObstacleContacts + right.ObstacleContacts,
            left.Warnings + right.Warnings,
            left.InvalidLaps + right.InvalidLaps,
            left.Penalties + right.Penalties);
    }
}

/// <summary>
/// How a driver drives, over their qualifying and race sessions.
/// </summary>
/// <param name="Name">The driver name.</param>
/// <param name="IsPlayer">Whether the driver is the selected driver.</param>
/// <param name="Sessions">The number of qualifying and race sessions.</param>
/// <param name="Laps">The number of laps driven in these sessions.</param>
/// <param name="Conduct">The incidents of these sessions.</param>
public sealed record DrivingProfile(string Name, bool IsPlayer, int Sessions, int Laps, DrivingConduct Conduct)
{
    /// <summary>
    /// Gets the contacts per hundred laps, to compare drivers with different histories, or
    /// <see langword="null"/> for a driver without any lap.
    /// </summary>
    public double? ContactsPer100Laps =>
        Laps == 0 ? null : (Conduct.VehicleContacts + Conduct.ObstacleContacts) * 100d / Laps;
}
