using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Localization;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Builds the driving profile of the selected driver from their qualifying and race sessions.
/// </summary>
/// <remarks>
/// Practice sessions are left out, like in the other result views: they are optional and a driver
/// tries things there. The driver is compared with the other drivers of the same sessions, who
/// faced the same tracks, grids and conditions.
/// </remarks>
public static class DrivingProfilePresentation
{
    private const int RecentRaceCount = 10;
    private const int MaximumOpponents = 5;
    private const int MinimumContactsForTakeaway = 5;
    private const int MinimumContactsForRiskSector = 3;

    /// <summary>
    /// Creates the profile of a driver, or returns <see langword="null"/> without a selected driver
    /// or without any qualifying or race session of theirs.
    /// </summary>
    public static DriverDrivingProfile? Create(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var allSessions = sessions as IReadOnlyCollection<Session> ?? sessions.ToArray();
        var results = allSessions
            .Where(session => session.Kind is SessionKind.Qualifying or SessionKind.Race)
            .Select(session => (Session: session, Driver: DriverProfile.FindDriver(session, userName)))
            .Where(result => result.Driver is not null)
            .Select(result => CreateResult(result.Session, result.Driver!))
            .ToArray();
        if (results.Length == 0)
        {
            return null;
        }

        var eventIds = RaceEventPresentation.CreateEvents(allSessions, userName)
            .Where(raceEvent => raceEvent.Race is not null)
            .ToDictionary(raceEvent => raceEvent.Race!.Id, raceEvent => raceEvent.Id);
        var incidents = results.SelectMany(result => result.Incidents).ToArray();
        var races = results.Where(result => result.Session.Kind == SessionKind.Race).ToArray();
        var qualifyings = results.Where(result => result.Session.Kind == SessionKind.Qualifying).ToArray();

        return new DriverDrivingProfile(
            results[0].Driver.Name,
            results.Length,
            CreateRates(results),
            SumOthers(results),
            CreateRow(Texts.Get("Progression.Race"), races),
            CreateRow(Texts.Get("Progression.Qualifying"), qualifyings),
            SumOthers(qualifyings),
            CreateRows(results, result => result.Driver.Category()),
            CreateRows(results, result => CircuitLabel(result.Session)),
            CreateCost(races, eventIds),
            races
                .OrderByDescending(result => result.Session.StartedAt)
                .Take(RecentRaceCount)
                .Select(result => new DrivingRaceEntry(
                    eventIds.GetValueOrDefault(result.Session.Id),
                    result.Session.StartedAt,
                    result.Session.TrackVenue,
                    result.Driver.Category(),
                    result.Conduct,
                    result.Story?.TimeLost ?? TimeSpan.Zero))
                .ToArray(),
            Count(incidents.Where(incident => incident.Kind == SessionEventKind.VehicleContact), incident => incident.Detail!)
                .Take(MaximumOpponents)
                .ToArray(),
            Count(incidents.Where(incident => incident.Kind == SessionEventKind.ObstacleContact), DrivingConductPresentation.Describe),
            Count(incidents.Where(incident => incident.Kind == SessionEventKind.Penalty), incident => incident.Detail!));
    }

    private static SessionResult CreateResult(Session session, Driver driver)
    {
        var name = driver.Name.Trim();
        var incidents = DrivingConductPresentation.CreateIncidents(session);
        var others = session.Drivers
            .Where(other => !string.Equals(other.Name.Trim(), name, StringComparison.OrdinalIgnoreCase))
            .GroupBy(other => other.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var othersConduct = new DrivingConduct(0, 0, 0, 0, 0);
        foreach (var other in others)
        {
            if (incidents.TryGetValue(other.Key, out var otherIncidents))
            {
                othersConduct += DrivingConductPresentation.Summarize(otherIncidents);
            }
        }

        var driverIncidents = incidents.TryGetValue(name, out var known) ? known : [];
        return new SessionResult(
            session,
            driver,
            driverIncidents,
            DrivingConductPresentation.Summarize(driverIncidents),
            others.Sum(other => other.Sum(result => Laps(result))),
            othersConduct,
            RaceStoryPresentation.Create(session, driver, driverIncidents));
    }

    private static DrivingCost CreateCost(
        IReadOnlyCollection<SessionResult> races,
        IReadOnlyDictionary<Guid, string> eventIds)
    {
        var told = races.Where(race => race.Story is not null).ToArray();
        var stories = told.Select(race => race.Story!).ToArray();
        var contacts = stories.SelectMany(story => story.Contacts).ToArray();
        var losses = stories
            .SelectMany(story => story.Laps)
            .Where(lap => lap.Loss.HasValue)
            .Select(lap => lap.Loss!.Value)
            .Order()
            .ToArray();
        var circuits = told
            .GroupBy(race => CircuitLabel(race.Session), StringComparer.OrdinalIgnoreCase)
            .Select(circuit => (Label: circuit.Key, Lost: circuit.Aggregate(TimeSpan.Zero, (total, race) => total + race.Story!.TimeLost)))
            .Where(circuit => circuit.Lost > TimeSpan.Zero)
            .OrderByDescending(circuit => circuit.Lost)
            .ToArray();
        var costliestRace = told
            .Where(race => race.Story!.TimeLost > TimeSpan.Zero)
            .MaxBy(race => race.Story!.TimeLost);
        var retirements = told.Where(race => race.Driver.FinishStatus.Equals("DNF", StringComparison.OrdinalIgnoreCase)).ToArray();
        var cost = new DrivingCost(
            stories.Aggregate(TimeSpan.Zero, (total, story) => total + story.TimeLost),
            stories.Length,
            contacts.Length,
            contacts.Count(contact => contact.Lap == 1),
            contacts.Count(contact => contact.CausedDamage),
            stories.Sum(story => story.PositionsLost),
            retirements.Length,
            retirements.Count(race => race.Story!.Contacts.Any(contact => contact.Lap == race.Story.Laps[^1].Number)),
            losses.Length == 0 ? null : losses[losses.Length / 2],
            circuits.Length == 0 ? null : circuits[0].Label,
            circuits.Length == 0 ? TimeSpan.Zero : circuits[0].Lost,
            costliestRace is null
                ? null
                : new DrivingRaceEntry(
                    eventIds.GetValueOrDefault(costliestRace.Session.Id),
                    costliestRace.Session.StartedAt,
                    costliestRace.Session.TrackVenue,
                    costliestRace.Driver.Category(),
                    costliestRace.Conduct,
                    costliestRace.Story!.TimeLost));
        return cost with { Takeaways = CreateTakeaways(cost) };
    }

    private static IReadOnlyList<string> CreateTakeaways(DrivingCost cost)
    {
        var takeaways = new List<string>();
        if (cost.Contacts >= MinimumContactsForTakeaway && cost.FirstLapContacts > 0)
        {
            takeaways.Add(Texts.Format("DrivingProfile.TakeawayFirstLap", Math.Round(cost.FirstLapContacts * 100d / cost.Contacts)));
        }

        if (cost.MedianLossPerContactLap is { } medianLoss)
        {
            takeaways.Add(Texts.Format("DrivingProfile.TakeawayMedianLoss", FormatSeconds(medianLoss)));
        }

        if (cost.CostliestCircuit is { } circuit)
        {
            takeaways.Add(Texts.Format("DrivingProfile.TakeawayCircuit", circuit, FormatSeconds(cost.CostliestCircuitLost)));
        }

        if (cost.ContactsWithDamage > 0)
        {
            takeaways.Add(Texts.Plural("DrivingProfile.TakeawayDamage", cost.ContactsWithDamage));
        }

        if (cost.Retirements > 0)
        {
            takeaways.Add(Texts.Format("DrivingProfile.TakeawayRetirements", cost.RetirementsAfterContact, cost.Retirements));
        }

        return takeaways;
    }

    private static string FormatSeconds(TimeSpan time) =>
        time.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the sector holding most contacts away from the start, when there are enough of them
    /// and that sector holds at least half: otherwise it would point at chance.
    /// </summary>
    private static int? FindRiskSector(IReadOnlyCollection<SessionResult> results)
    {
        var sectors = results
            .Where(result => result.Story is not null)
            .SelectMany(result => result.Story!.Contacts)
            .Where(contact => contact.Sector.HasValue)
            .Select(contact => contact.Sector!.Value)
            .ToArray();
        if (sectors.Length < MinimumContactsForRiskSector)
        {
            return null;
        }

        var top = sectors.GroupBy(sector => sector).MaxBy(sector => sector.Count())!;
        return top.Count() * 2 >= sectors.Length ? top.Key : null;
    }

    private static int Laps(Driver driver) => driver.LapsCompleted ?? driver.Laps.Count;

    private static DrivingRates CreateRates(IReadOnlyCollection<SessionResult> results) =>
        new(
            results.Sum(result => Laps(result.Driver)),
            results.Aggregate(new DrivingConduct(0, 0, 0, 0, 0), (total, result) => total + result.Conduct));

    private static DrivingRates SumOthers(IReadOnlyCollection<SessionResult> results) =>
        new(
            results.Sum(result => result.OthersLaps),
            results.Aggregate(new DrivingConduct(0, 0, 0, 0, 0), (total, result) => total + result.OthersConduct));

    private static DrivingRow CreateRow(string label, IReadOnlyCollection<SessionResult> results) =>
        new(
            label,
            results.Count,
            CreateRates(results),
            results.Aggregate(TimeSpan.Zero, (total, result) => total + (result.Story?.TimeLost ?? TimeSpan.Zero)),
            FindRiskSector(results));

    private static IReadOnlyList<DrivingRow> CreateRows(
        IEnumerable<SessionResult> results,
        Func<SessionResult, string> label) =>
        results
            .GroupBy(label, StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateRow(group.Key, group.ToArray()))
            .OrderByDescending(row => row.Rates.Laps)
            .ThenBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<DrivingCount> Count(
        IEnumerable<DrivingIncident> incidents,
        Func<DrivingIncident, string> label) =>
        incidents
            .GroupBy(label, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DrivingCount(group.Key, group.Count()))
            .OrderByDescending(count => count.Count)
            .ThenBy(count => count.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string CircuitLabel(Session session) =>
        string.Equals(session.TrackVenue, session.TrackCourse, StringComparison.OrdinalIgnoreCase)
            ? session.TrackVenue
            : $"{session.TrackVenue} · {session.TrackCourse}";

    private sealed record SessionResult(
        Session Session,
        Driver Driver,
        IReadOnlyList<DrivingIncident> Incidents,
        DrivingConduct Conduct,
        int OthersLaps,
        DrivingConduct OthersConduct,
        RaceStory? Story);
}

/// <summary>
/// How the selected driver drives, over their qualifying and race sessions.
/// </summary>
/// <param name="Name">The driver name.</param>
/// <param name="Sessions">The number of qualifying and race sessions.</param>
/// <param name="Player">The incidents and laps of the driver.</param>
/// <param name="Others">The incidents and laps of the other drivers of these sessions.</param>
/// <param name="Race">The driver's races.</param>
/// <param name="Qualifying">The driver's qualifying sessions.</param>
/// <param name="OthersQualifying">The other drivers of these qualifying sessions.</param>
/// <param name="ByCategory">The driver's sessions by category, most driven first.</param>
/// <param name="ByCircuit">The driver's sessions by circuit, most driven first.</param>
/// <param name="Cost">What the incidents of the driver's races cost them.</param>
/// <param name="RecentRaces">The latest races, most recent first.</param>
/// <param name="Opponents">The opponents most often touched, most frequent first.</param>
/// <param name="Obstacles">The obstacles touched, most frequent first.</param>
/// <param name="PenaltyReasons">The reasons of the penalties, most frequent first, as written by LMU.</param>
public sealed record DriverDrivingProfile(
    string Name,
    int Sessions,
    DrivingRates Player,
    DrivingRates Others,
    DrivingRow Race,
    DrivingRow Qualifying,
    DrivingRates OthersQualifying,
    IReadOnlyList<DrivingRow> ByCategory,
    IReadOnlyList<DrivingRow> ByCircuit,
    DrivingCost Cost,
    IReadOnlyList<DrivingRaceEntry> RecentRaces,
    IReadOnlyList<DrivingCount> Opponents,
    IReadOnlyList<DrivingCount> Obstacles,
    IReadOnlyList<DrivingCount> PenaltyReasons);

/// <summary>
/// Incidents and the laps they happened in, to compare histories of different lengths.
/// </summary>
/// <param name="Laps">The number of laps.</param>
/// <param name="Conduct">The incidents.</param>
public sealed record DrivingRates(int Laps, DrivingConduct Conduct)
{
    /// <summary>Gets the contacts with a car per hundred laps, when there are laps.</summary>
    public double? ContactsPer100Laps => Per100Laps(Conduct.VehicleContacts);

    /// <summary>Gets the contacts with an obstacle per hundred laps, when there are laps.</summary>
    public double? ObstaclesPer100Laps => Per100Laps(Conduct.ObstacleContacts);

    /// <summary>Gets the track-limit warnings per hundred laps, when there are laps.</summary>
    public double? WarningsPer100Laps => Per100Laps(Conduct.Warnings);

    /// <summary>Gets the invalidated laps per hundred laps, when there are laps.</summary>
    public double? InvalidLapsPer100Laps => Per100Laps(Conduct.InvalidLaps);

    /// <summary>Gets the penalties per hundred laps, when there are laps.</summary>
    public double? PenaltiesPer100Laps => Per100Laps(Conduct.Penalties);

    private double? Per100Laps(int count) => Laps == 0 ? null : count * 100d / Laps;
}

/// <summary>
/// A line of a breakdown of the driver's sessions.
/// </summary>
/// <param name="Label">The session kind, category or circuit.</param>
/// <param name="Sessions">The number of sessions.</param>
/// <param name="Rates">The incidents and laps of these sessions.</param>
/// <param name="TimeLost">The time lost on laps with a contact, in the races of these sessions.</param>
/// <param name="RiskSector">The sector holding most contacts, when one stands out.</param>
public sealed record DrivingRow(
    string Label,
    int Sessions,
    DrivingRates Rates,
    TimeSpan TimeLost = default,
    int? RiskSector = null)
{
    private const int MinimumLapsForRates = 20;

    /// <summary>
    /// Gets whether the line has enough laps for its rates to mean something: one contact on one
    /// lap would read as 100 contacts per 100 laps.
    /// </summary>
    public bool HasEnoughLaps => Rates.Laps >= MinimumLapsForRates;
}

/// <summary>
/// The conduct of the driver in one race.
/// </summary>
/// <param name="EventId">The event of the race on the race-details page.</param>
/// <param name="StartedAt">The race start time.</param>
/// <param name="TrackVenue">The track venue.</param>
/// <param name="Category">The category of the driver.</param>
/// <param name="Conduct">The incidents of the race.</param>
/// <param name="TimeLost">The time lost on laps with a contact.</param>
public sealed record DrivingRaceEntry(
    string? EventId,
    DateTimeOffset StartedAt,
    string TrackVenue,
    string Category,
    DrivingConduct Conduct,
    TimeSpan TimeLost);

/// <summary>
/// What the incidents of the driver's races cost them.
/// </summary>
/// <param name="TimeLost">The time lost on laps with a contact, first and pit laps excluded.</param>
/// <param name="Races">The number of races with lap data.</param>
/// <param name="Contacts">The contacts with a car or an obstacle.</param>
/// <param name="FirstLapContacts">The contacts at the start or on the first lap.</param>
/// <param name="ContactsWithDamage">The contacts followed by damage to the car.</param>
/// <param name="PositionsLost">The positions lost on laps with a contact.</param>
/// <param name="Retirements">The races not finished.</param>
/// <param name="RetirementsAfterContact">The retirements with a contact on the last lap driven.</param>
/// <param name="MedianLossPerContactLap">The median time lost on a lap with a contact.</param>
/// <param name="CostliestCircuit">The circuit where contacts cost the most time.</param>
/// <param name="CostliestCircuitLost">The time lost on that circuit.</param>
/// <param name="CostliestRace">The race where contacts cost the most time.</param>
public sealed record DrivingCost(
    TimeSpan TimeLost,
    int Races,
    int Contacts,
    int FirstLapContacts,
    int ContactsWithDamage,
    int PositionsLost,
    int Retirements,
    int RetirementsAfterContact,
    TimeSpan? MedianLossPerContactLap,
    string? CostliestCircuit,
    TimeSpan CostliestCircuitLost,
    DrivingRaceEntry? CostliestRace)
{
    /// <summary>
    /// Gets what stands out, in plain sentences.
    /// </summary>
    public IReadOnlyList<string> Takeaways { get; init; } = [];

    /// <summary>
    /// Gets the time lost per race, when there are races.
    /// </summary>
    public TimeSpan? TimeLostPerRace => Races == 0 ? null : TimeLost / Races;
}

/// <summary>
/// A number of occurrences.
/// </summary>
/// <param name="Label">What is counted.</param>
/// <param name="Count">The number of occurrences.</param>
public sealed record DrivingCount(string Label, int Count);
