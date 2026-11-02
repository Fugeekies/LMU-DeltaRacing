using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies how the events of a session become contacts and driving profiles.
/// </summary>
[TestClass]
public sealed class DrivingConductPresentationTests
{
    private const string Player = "Player";
    private const string Rival = "Rival";

    private static readonly Lazy<Session[]> Fixtures = new(LoadFixtures);

    /// <summary>
    /// Verifies that the reports of both cars, a few seconds apart, are one contact for each driver.
    /// </summary>
    [TestMethod]
    public void CreateIncidents_merges_the_reports_of_one_accident_for_both_drivers()
    {
        var session = CreateSession("Race", [
            Contact(100, Player, Rival),
            Contact(100.5, Rival, Player),
            Contact(104, Player, Rival),
            Contact(120, Rival, Player)]);

        var incidents = DrivingConductPresentation.CreateIncidents(session);

        DrivingConductPresentation.Summarize(incidents[Player]).VehicleContacts.Should().Be(2);
        DrivingConductPresentation.Summarize(incidents[Rival]).VehicleContacts.Should().Be(2);
        incidents[Player].Select(incident => incident.Elapsed.TotalSeconds).Should().Equal(100, 120);
        incidents[Player][0].Detail.Should().Be(Rival);
    }

    /// <summary>
    /// Verifies that two opponents or two obstacles at the same moment are separate contacts.
    /// </summary>
    [TestMethod]
    public void CreateIncidents_keeps_contacts_with_different_targets_apart()
    {
        var session = CreateSession("Race", [
            Contact(100, Player, Rival),
            Contact(101, Player, "Other"),
            new SessionEvent(TimeSpan.FromSeconds(102), SessionEventKind.ObstacleContact, Player, "Post"),
            new SessionEvent(TimeSpan.FromSeconds(103), SessionEventKind.ObstacleContact, Player, "Sign"),
            new SessionEvent(TimeSpan.FromSeconds(103.5), SessionEventKind.ObstacleContact, Player, "Sign")]);

        var conduct = DrivingConductPresentation.Summarize(DrivingConductPresentation.CreateIncidents(session)[Player]);

        conduct.VehicleContacts.Should().Be(2);
        conduct.ObstacleContacts.Should().Be(2);
    }

    /// <summary>
    /// Verifies that warnings, invalid laps and penalties are counted one by one, and that contacts
    /// whose meaning is not established are listed but not counted.
    /// </summary>
    [TestMethod]
    public void Summarize_counts_every_track_limit_event_but_not_unknown_contacts()
    {
        var session = CreateSession("Qualify", [
            new SessionEvent(TimeSpan.FromSeconds(10), SessionEventKind.TrackLimitWarning, Player, Lap: 2),
            new SessionEvent(TimeSpan.FromSeconds(11), SessionEventKind.TrackLimitWarning, Player, Lap: 2),
            new SessionEvent(TimeSpan.FromSeconds(12), SessionEventKind.InvalidLap, Player, Lap: 3),
            new SessionEvent(TimeSpan.FromSeconds(13), SessionEventKind.Penalty, Player, "Speeding"),
            new SessionEvent(TimeSpan.FromSeconds(14), SessionEventKind.OtherContact, Player, "Wing")]);

        var incidents = DrivingConductPresentation.CreateIncidents(session)[Player];

        DrivingConductPresentation.Summarize(incidents).Should().Be(new DrivingConduct(0, 0, 2, 1, 1));
        incidents.Should().HaveCount(5);
    }

    /// <summary>
    /// Verifies that incidents are worded for the timeline of the race-details page.
    /// </summary>
    [TestMethod]
    public void Describe_words_each_kind_of_incident_in_french()
    {
        static string Describe(SessionEventKind kind, string? detail = null) =>
            DrivingConductPresentation.Describe(new DrivingIncident(TimeSpan.Zero, kind, detail, null));

        Describe(SessionEventKind.VehicleContact, Rival).Should().Be("Contact avec Rival");
        Describe(SessionEventKind.ObstacleContact, "Post").Should().Be("Contact avec un poteau");
        Describe(SessionEventKind.ObstacleContact, "Immovable").Should().Be("Contact avec un obstacle fixe");
        Describe(SessionEventKind.OtherContact, "Wing").Should().Be("Contact signalé : Wing");
        Describe(SessionEventKind.TrackLimitWarning).Should().Be("Avertissement de limites de piste");
        Describe(SessionEventKind.InvalidLap).Should().Be("Tour invalidé");
        Describe(SessionEventKind.Penalty, "Speeding").Should().Be("Pénalité : Speeding");
    }

    /// <summary>
    /// Verifies the classification columns: conduct of each driver, unknown without any event.
    /// </summary>
    [TestMethod]
    public void CreateResults_adds_the_conduct_of_each_driver()
    {
        var session = CreateSession("Race", [Contact(100, Player, Rival)]);
        var withoutEvents = CreateSession("Race", []);

        var result = RaceDetailsPresentation.CreateResults(session, Player).Single(driver => driver.Name == Player);
        var rival = RaceDetailsPresentation.CreateResults(session, Player).Single(driver => driver.Name == Rival);

        result.Conduct!.VehicleContacts.Should().Be(1);
        result.Incidents.Should().ContainSingle();
        rival.Conduct!.VehicleContacts.Should().Be(1);
        RaceDetailsPresentation.CreateResults(withoutEvents, Player).Should().OnlyContain(driver => driver.Conduct == null);
    }

    /// <summary>
    /// Verifies that a profile adds up qualifying and race sessions, without practice sessions, and
    /// gives a rate per hundred laps.
    /// </summary>
    [TestMethod]
    public void CreateProfiles_adds_up_qualifying_and_race_sessions()
    {
        var sessions = new[]
        {
            CreateSession("Race", [Contact(100, Player, Rival)], laps: 40),
            CreateSession("Qualify", [new SessionEvent(TimeSpan.FromSeconds(5), SessionEventKind.InvalidLap, Player, Lap: 1)], laps: 10),
            CreateSession("Practice1", [Contact(5, Player, Rival)], laps: 50)
        };

        var profiles = DrivingConductPresentation.CreateProfiles(sessions, Player);

        var player = profiles.Single(profile => profile.Name == Player);
        player.IsPlayer.Should().BeTrue();
        player.Sessions.Should().Be(2);
        player.Laps.Should().Be(50);
        player.Conduct.Should().Be(new DrivingConduct(1, 0, 0, 1, 0));
        player.ContactsPer100Laps.Should().Be(2);
        profiles.Single(profile => profile.Name == Rival).IsPlayer.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a driver without any lap has no rate, rather than a rate of zero.
    /// </summary>
    [TestMethod]
    public void CreateProfiles_gives_no_rate_without_laps()
    {
        var profiles = DrivingConductPresentation.CreateProfiles([CreateSession("Race", [], laps: 0)], Player);

        profiles.Should().OnlyContain(profile => profile.ContactsPer100Laps == null);
    }

    /// <summary>
    /// Verifies that merging a driver also renames them in the events, as the driver and as the
    /// opponent, so that the merged name keeps its contacts.
    /// </summary>
    [TestMethod]
    public void DriverMerges_rename_drivers_in_the_events()
    {
        var session = CreateSession("Race", [Contact(100, Player, Rival), Contact(120, Rival, Player)]);

        var merged = DriverMerges.Apply([session], [new DriverMerge(Rival, "Renamed")]).Single();

        merged.Events.Should().Equal(Contact(100, Player, "Renamed"), Contact(120, "Renamed", Player));
    }

    /// <summary>
    /// Verifies that merged practice sessions keep the events of every session.
    /// </summary>
    [TestMethod]
    public void CreateEvents_keeps_the_events_of_merged_practice_sessions()
    {
        var sessions = new[]
        {
            CreateSession("Practice1", [Contact(100, Player, Rival)], startedAt: 1760000000),
            CreateSession("Practice1", [Contact(50, Rival, Player)], startedAt: 1760000600)
        };

        var raceEvent = RaceEventPresentation.CreateEvents(sessions, Player).Single();

        raceEvent.Practice!.Events.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies on the real fixtures the profile of the main driver, and that every qualifying or
    /// race driver with events has them: this fails if the Stream names stop matching the drivers.
    /// </summary>
    [TestMethod]
    public void CreateProfiles_gives_the_main_driver_a_real_profile_on_the_fixtures()
    {
        var profiles = DrivingConductPresentation.CreateProfiles(Fixtures.Value, "Driver Tom");

        var tom = profiles.Single(profile => profile.IsPlayer);
        tom.Sessions.Should().Be(170);
        tom.Conduct.VehicleContacts.Should().BePositive();
        tom.Conduct.ObstacleContacts.Should().BePositive();
        tom.Conduct.Warnings.Should().BePositive();
        tom.Conduct.InvalidLaps.Should().BePositive();
    }

    /// <summary>
    /// Verifies on the real fixtures that a lap invalidated by LMU is never a timed lap of the driver
    /// the event is attached to: an independent proof that events reach the right driver.
    /// </summary>
    [TestMethod]
    public void Invalid_laps_of_the_fixtures_are_never_timed_laps()
    {
        var invalidLaps = Fixtures.Value
            .SelectMany(session => session.Events
                .Where(sessionEvent => sessionEvent.Kind == SessionEventKind.InvalidLap)
                .Select(sessionEvent => (Event: sessionEvent, Driver: session.Drivers.FirstOrDefault(driver => driver.Name == sessionEvent.DriverName))))
            .Where(item => item.Driver is not null)
            .ToArray();

        invalidLaps.Should().NotBeEmpty();
        invalidLaps.Should().NotContain(item => item.Driver!.Laps.Any(lap => lap.Number == item.Event.Lap && lap.Time.HasValue));
    }

    private static Session[] LoadFixtures()
    {
        var parser = new XmlResultParser();
        return Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(file =>
            {
                using var stream = File.OpenRead(file);
                return parser.Parse(stream);
            })
            .ToArray();
    }

    private static SessionEvent Contact(double seconds, string driver, string opponent) =>
        new(TimeSpan.FromSeconds(seconds), SessionEventKind.VehicleContact, driver, opponent);

    private static Session CreateSession(string type, IEnumerable<SessionEvent> events, int laps = 10, long startedAt = 1760000000) => new(
        Guid.NewGuid(),
        DateTimeOffset.FromUnixTimeSeconds(startedAt),
        "Spa",
        "Spa GP",
        "Spa GP",
        type,
        [CreateDriver(Player, 1, laps), CreateDriver(Rival, 2, laps)],
        events: events);

    private static Driver CreateDriver(string name, int position, int laps) =>
        new(name, position.ToString(System.Globalization.CultureInfo.InvariantCulture), "GT3", "Team", position, [], lapsCompleted: laps);
}
