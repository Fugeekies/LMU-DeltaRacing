using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the driving profile of the selected driver.
/// </summary>
[TestClass]
public sealed class DrivingProfilePresentationTests
{
    private const string Player = "Player";
    private const string Rival = "Rival";
    private const string Stranger = "Stranger";

    private static SessionEvent Contact(double seconds, string driver, string opponent) =>
        new(TimeSpan.FromSeconds(seconds), SessionEventKind.VehicleContact, driver, opponent);

    /// <summary>
    /// Verifies the totals of the driver, without practice sessions nor sessions of other drivers.
    /// </summary>
    [TestMethod]
    public void Create_adds_up_the_qualifying_and_race_sessions_of_the_driver()
    {
        var sessions = new[]
        {
            CreateSession("Race", [Contact(100, Player, Rival)], [(Player, 40), (Rival, 40)]),
            CreateSession("Qualify", [new SessionEvent(TimeSpan.FromSeconds(5), SessionEventKind.InvalidLap, Player, Lap: 1)], [(Player, 10), (Rival, 10)]),
            CreateSession("Practice1", [Contact(5, Player, Rival)], [(Player, 50), (Rival, 50)]),
            CreateSession("Race", [Contact(5, Rival, Stranger)], [(Rival, 50), (Stranger, 50)])
        };

        var profile = DrivingProfilePresentation.Create(sessions, Player)!;

        profile.Name.Should().Be(Player);
        profile.Sessions.Should().Be(2);
        profile.Player.Laps.Should().Be(50);
        profile.Player.Conduct.Should().Be(new DrivingConduct(1, 0, 0, 1, 0));
        profile.Player.ContactsPer100Laps.Should().Be(2);
        profile.Race.Sessions.Should().Be(1);
        profile.Qualifying.Rates.InvalidLapsPer100Laps.Should().Be(10);
    }

    /// <summary>
    /// Verifies that the driver is compared with the other drivers of the same sessions only.
    /// </summary>
    [TestMethod]
    public void Create_compares_the_driver_with_the_other_drivers_of_their_sessions()
    {
        var sessions = new[]
        {
            CreateSession("Race", [Contact(100, Player, Rival), Contact(200, Rival, Stranger)], [(Player, 10), (Rival, 10), (Stranger, 10)]),
            CreateSession("Race", [Contact(5, Rival, Stranger)], [(Rival, 90), (Stranger, 90)])
        };

        var profile = DrivingProfilePresentation.Create(sessions, Player)!;

        profile.Others.Laps.Should().Be(20);
        profile.Others.Conduct.VehicleContacts.Should().Be(3);
        profile.Others.ContactsPer100Laps.Should().Be(15);
    }

    /// <summary>
    /// Verifies the breakdown by circuit and by category, most driven first.
    /// </summary>
    [TestMethod]
    public void Create_breaks_the_conduct_down_by_circuit_and_category()
    {
        var sessions = new[]
        {
            CreateSession("Race", [Contact(100, Player, Rival)], [(Player, 10), (Rival, 10)], venue: "Spa", course: "Spa GP", carClass: "GT3"),
            CreateSession("Race", [], [(Player, 30), (Rival, 30)], venue: "Monza", course: "Monza", carClass: "Hyper"),
            CreateSession("Qualify", [], [(Player, 5), (Rival, 5)], venue: "Monza", course: "Monza", carClass: "Hyper")
        };

        var profile = DrivingProfilePresentation.Create(sessions, Player)!;

        profile.ByCircuit.Select(row => row.Label).Should().Equal("Monza", "Spa · Spa GP");
        profile.ByCircuit[0].Sessions.Should().Be(2);
        profile.ByCircuit[1].Rates.Conduct.VehicleContacts.Should().Be(1);
        profile.ByCategory.Select(row => row.Label).Should().Equal("Hypercar", "LMGT3");
    }

    /// <summary>
    /// Verifies the opponents most touched, the obstacles and the penalty reasons, most frequent first.
    /// </summary>
    [TestMethod]
    public void Create_lists_opponents_obstacles_and_penalty_reasons_by_frequency()
    {
        var session = CreateSession(
            "Race",
            [
                Contact(100, Player, Rival),
                Contact(200, Stranger, Player),
                Contact(300, Player, Stranger),
                new SessionEvent(TimeSpan.FromSeconds(400), SessionEventKind.ObstacleContact, Player, "Post"),
                new SessionEvent(TimeSpan.FromSeconds(500), SessionEventKind.Penalty, Player, "Speeding")
            ],
            [(Player, 10), (Rival, 10), (Stranger, 10)]);

        var profile = DrivingProfilePresentation.Create([session], Player)!;

        profile.Opponents.Should().Equal(new DrivingCount(Stranger, 2), new DrivingCount(Rival, 1));
        profile.Obstacles.Should().Equal(new DrivingCount("Contact avec un poteau", 1));
        profile.PenaltyReasons.Should().Equal(new DrivingCount("Speeding", 1));
    }

    /// <summary>
    /// Verifies that the ten latest races come most recent first, with a link to their event.
    /// </summary>
    [TestMethod]
    public void Create_lists_the_ten_latest_races_with_their_event()
    {
        var sessions = Enumerable.Range(0, 12)
            .Select(index => CreateSession("Race", [], [(Player, 10), (Rival, 10)], startedAt: 1760000000 + (index * 86400)))
            .ToArray();

        var profile = DrivingProfilePresentation.Create(sessions, Player)!;

        profile.RecentRaces.Should().HaveCount(10);
        profile.RecentRaces[0].StartedAt.Should().Be(sessions[^1].StartedAt);
        profile.RecentRaces[0].EventId.Should().Be(sessions[^1].Id.ToString());
    }

    /// <summary>
    /// Verifies that a line with too few laps gives no meaningful rate: one contact on one lap
    /// would read as 100 contacts per 100 laps.
    /// </summary>
    [TestMethod]
    public void A_line_needs_twenty_laps_to_give_a_meaningful_rate()
    {
        var conduct = new DrivingConduct(1, 0, 0, 0, 0);

        new DrivingRow("Spa", 1, new DrivingRates(19, conduct)).HasEnoughLaps.Should().BeFalse();
        new DrivingRow("Spa", 1, new DrivingRates(20, conduct)).HasEnoughLaps.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a rate does not exist without laps: it is unknown, not zero.
    /// </summary>
    [TestMethod]
    public void Create_gives_no_rate_without_laps()
    {
        var profile = DrivingProfilePresentation.Create([CreateSession("Race", [], [(Player, 0)])], Player)!;

        profile.Player.ContactsPer100Laps.Should().BeNull();
        profile.Others.ContactsPer100Laps.Should().BeNull();
    }

    /// <summary>
    /// Verifies that there is no profile without a selected driver or without any of their sessions.
    /// </summary>
    [TestMethod]
    public void Create_returns_no_profile_without_a_driver_or_their_sessions()
    {
        var session = CreateSession("Race", [], [(Player, 10)]);

        DrivingProfilePresentation.Create([session], null).Should().BeNull();
        DrivingProfilePresentation.Create([session], Rival).Should().BeNull();
        DrivingProfilePresentation.Create([CreateSession("Practice1", [], [(Player, 10)])], Player).Should().BeNull();
    }

    /// <summary>
    /// Verifies the profile of the main driver on the real fixtures.
    /// </summary>
    [TestMethod]
    public void Create_gives_the_main_driver_a_real_profile_on_the_fixtures()
    {
        var parser = new XmlResultParser();
        var sessions = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(file =>
            {
                using var stream = File.OpenRead(file);
                return parser.Parse(stream);
            })
            .ToArray();

        var profile = DrivingProfilePresentation.Create(sessions, "Driver Tom")!;

        profile.Sessions.Should().Be(170);
        profile.Race.Sessions.Should().Be(87);
        profile.Player.Conduct.VehicleContacts.Should().BePositive();
        profile.Others.Laps.Should().BeGreaterThan(profile.Player.Laps);
        profile.ByCircuit.Sum(row => row.Sessions).Should().Be(170);
        profile.ByCategory.Sum(row => row.Sessions).Should().Be(170);
        profile.Opponents.Should().HaveCountLessThanOrEqualTo(5).And.NotBeEmpty();
        profile.RecentRaces.Should().HaveCount(10).And.OnlyContain(race => race.EventId != null);
    }

    private static Session CreateSession(
        string type,
        IEnumerable<SessionEvent> events,
        (string Name, int Laps)[] drivers,
        string venue = "Spa",
        string course = "Spa",
        string carClass = "GT3",
        long startedAt = 1760000000) => new(
        Guid.NewGuid(),
        DateTimeOffset.FromUnixTimeSeconds(startedAt),
        venue,
        course,
        "Event",
        type,
        drivers.Select((driver, index) => new Driver(driver.Name, "1", carClass, "Team", index + 1, [], lapsCompleted: driver.Laps)),
        events: events);
}
