using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Tells the race of one driver lap by lap, and what each incident cost them.
/// </summary>
/// <remarks>
/// The cost of a contact is measured, not assumed: a lap with a contact is compared with the median
/// lap of the same race, on the same track and in the same conditions. First laps and pit laps are
/// left out, because their time has other causes: the time lost is therefore a minimum.
/// </remarks>
public static class RaceStoryPresentation
{
    // Damage is logged just after the contact; on the fixtures, 1 s before to 3 s after covers it.
    private static readonly TimeSpan DamageBefore = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DamageAfter = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Creates the story of a driver in a race, or returns <see langword="null"/> for another kind
    /// of session, an unknown driver or a driver without any lap position.
    /// </summary>
    public static RaceStory? Create(Session session, string? driverName)
    {
        ArgumentNullException.ThrowIfNull(session);

        var driver = DriverProfile.FindDriver(session, driverName);
        if (driver is null)
        {
            return null;
        }

        var incidents = DrivingConductPresentation.CreateIncidents(session);
        return Create(session, driver, incidents.TryGetValue(driver.Name.Trim(), out var known) ? known : []);
    }

    /// <summary>
    /// Creates the story of a driver from their incidents, already calculated for the session.
    /// </summary>
    public static RaceStory? Create(Session session, Driver driver, IReadOnlyList<DrivingIncident> incidents)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(incidents);

        if (session.Kind != SessionKind.Race || !driver.Laps.Any(lap => lap.Position.HasValue))
        {
            return null;
        }

        var laps = driver.Laps.OrderBy(lap => lap.Number).ToArray();
        var byLap = incidents
            .GroupBy(incident => FindLap(laps, incident).Number)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<DrivingIncident>)group.ToArray());
        var reference = FindReferenceLap(laps);

        var previousPosition = driver.GridPosition;
        var storyLaps = new List<RaceStoryLap>(laps.Length);
        var positionsLost = 0;
        foreach (var lap in laps)
        {
            var lapIncidents = byLap.GetValueOrDefault(lap.Number, []);
            var hasContact = lapIncidents.Any(IsContact);
            if (hasContact && previousPosition is { } before && lap.Position is { } after && after > before)
            {
                positionsLost += after - before;
            }

            storyLaps.Add(new RaceStoryLap(
                lap.Number,
                lap.Position,
                lap.Time,
                hasContact ? MeasureLoss(lap, reference) : null,
                lap.IsPitLap,
                lapIncidents));
            previousPosition = lap.Position ?? previousPosition;
        }

        return new RaceStory(
            driver.Name,
            driver.GridPosition,
            storyLaps,
            reference,
            storyLaps.Aggregate(TimeSpan.Zero, (total, lap) => total + (lap.Loss ?? TimeSpan.Zero)),
            positionsLost,
            CreateContacts(laps, incidents));
    }

    /// <summary>
    /// Whether an incident is a contact whose cost can be measured.
    /// </summary>
    private static bool IsContact(DrivingIncident incident) =>
        incident.Kind is SessionEventKind.VehicleContact or SessionEventKind.ObstacleContact;

    /// <summary>
    /// Finds the lap of an incident: the one LMU gives, otherwise from its time. Incidents before
    /// the first lap (the grid, the formation lap) belong to the start, so to lap 1.
    /// </summary>
    private static Lap FindLap(Lap[] laps, DrivingIncident incident) =>
        laps.FirstOrDefault(lap => lap.Number == incident.Lap)
        ?? laps.LastOrDefault(lap => lap.StartedAt <= incident.Elapsed)
        ?? laps[0];

    private static TimeSpan? FindReferenceLap(IEnumerable<Lap> laps)
    {
        var times = laps
            .Where(lap => lap.Time.HasValue && lap.Number > 1 && !lap.IsPitLap)
            .Select(lap => lap.Time!.Value)
            .Order()
            .ToArray();
        return times.Length == 0 ? null : times[times.Length / 2];
    }

    private static TimeSpan? MeasureLoss(Lap lap, TimeSpan? reference)
    {
        if (lap.Number == 1 || lap.IsPitLap || lap.Time is not { } time || reference is not { } median)
        {
            return null;
        }

        return time > median ? time - median : TimeSpan.Zero;
    }

    private static IReadOnlyList<RaceStoryContact> CreateContacts(Lap[] laps, IReadOnlyList<DrivingIncident> incidents)
    {
        var damages = incidents
            .Where(incident => incident.Kind == SessionEventKind.Damage)
            .Select(incident => incident.Elapsed)
            .ToArray();
        return incidents
            .Where(IsContact)
            .Select(incident =>
            {
                var lap = FindLap(laps, incident);
                return new RaceStoryContact(
                    lap.Number,
                    FindSector(lap, incident.Elapsed),
                    damages.Any(damage =>
                        damage >= incident.Elapsed - DamageBefore && damage <= incident.Elapsed + DamageAfter));
            })
            .ToArray();
    }

    /// <summary>
    /// Finds the sector of a moment from the times of its lap. The first lap has none: a contact
    /// at the start says nothing about a place on the track.
    /// </summary>
    private static int? FindSector(Lap lap, TimeSpan elapsed)
    {
        if (lap.Number == 1
            || lap.StartedAt is not { } start
            || lap.Sectors.FirstOrDefault(sector => sector.Number == 1) is not { } first)
        {
            return null;
        }

        var intoLap = elapsed - start;
        if (intoLap < first.Time)
        {
            return 1;
        }

        return lap.Sectors.FirstOrDefault(sector => sector.Number == 2) is { } second
            ? intoLap < first.Time + second.Time ? 2 : 3
            : null;
    }
}

/// <summary>
/// The race of one driver, lap by lap.
/// </summary>
/// <param name="DriverName">The driver name.</param>
/// <param name="GridPosition">The position on the grid.</param>
/// <param name="Laps">The laps, in order.</param>
/// <param name="ReferenceLap">The median lap of the race, first and pit laps excluded.</param>
/// <param name="TimeLost">The time lost on laps with a contact.</param>
/// <param name="PositionsLost">The positions lost on laps with a contact.</param>
/// <param name="Contacts">The contacts with a car or an obstacle.</param>
public sealed record RaceStory(
    string DriverName,
    int? GridPosition,
    IReadOnlyList<RaceStoryLap> Laps,
    TimeSpan? ReferenceLap,
    TimeSpan TimeLost,
    int PositionsLost,
    IReadOnlyList<RaceStoryContact> Contacts);

/// <summary>
/// One lap of a race story.
/// </summary>
/// <param name="Number">The lap number.</param>
/// <param name="Position">The overall position at the end of the lap.</param>
/// <param name="Time">The lap time.</param>
/// <param name="Loss">The time lost on a lap with a contact, when it can be measured.</param>
/// <param name="IsPit">Whether the driver stopped in the pits.</param>
/// <param name="Incidents">The incidents of the lap.</param>
public sealed record RaceStoryLap(
    int Number,
    int? Position,
    TimeSpan? Time,
    TimeSpan? Loss,
    bool IsPit,
    IReadOnlyList<DrivingIncident> Incidents);

/// <summary>
/// A contact placed in a race.
/// </summary>
/// <param name="Lap">The lap of the contact.</param>
/// <param name="Sector">The sector of the contact, except on the first lap.</param>
/// <param name="CausedDamage">Whether damage was reported just after the contact.</param>
public sealed record RaceStoryContact(int Lap, int? Sector, bool CausedDamage);
