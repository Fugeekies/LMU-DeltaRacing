using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the lap-by-lap story of a race and what its incidents cost the driver.
/// </summary>
[TestClass]
public sealed class RaceStoryPresentationTests
{
    private const string Player = "Player";
    private const string Rival = "Rival";

    private static readonly Lazy<Session[]> Fixtures = new(LoadFixtures);

    /// <summary>
    /// Verifies that an incident lands on the lap it happened in, and before the start on lap 1.
    /// </summary>
    [TestMethod]
    public void Create_places_incidents_on_their_lap()
    {
        var session = CreateRace(
            CreateLaps(),
            Contact(50, Player, Rival),
            Contact(150, Player, Rival),
            Contact(350, Player, Rival));

        var story = RaceStoryPresentation.Create(session, Player)!;

        story.Laps.Select(lap => lap.Incidents.Count).Should().Equal(1, 1, 0, 1, 0);
        story.GridPosition.Should().Be(4);
        story.Laps.Select(lap => lap.Position).Should().Equal(3, 3, 3, 5, 5);
    }

    /// <summary>
    /// Verifies that a contact costs the time lost against the median lap of the race, and that a
    /// clean lap, a first lap or a pit lap costs nothing: they have other causes.
    /// </summary>
    [TestMethod]
    public void Create_measures_the_time_lost_on_laps_with_a_contact()
    {
        var session = CreateRace(
            CreateLaps(),
            Contact(150, Player, Rival),
            Contact(350, Player, Rival));

        var story = RaceStoryPresentation.Create(session, Player)!;

        story.ReferenceLap.Should().Be(TimeSpan.FromSeconds(100));
        story.Laps[1].Loss.Should().Be(TimeSpan.Zero);
        story.Laps[2].Loss.Should().BeNull();
        story.Laps[3].Loss.Should().Be(TimeSpan.FromSeconds(8));
        story.TimeLost.Should().Be(TimeSpan.FromSeconds(8));
    }

    /// <summary>
    /// Verifies that the positions lost on the laps with a contact are added up, without the gains.
    /// </summary>
    [TestMethod]
    public void Create_adds_up_the_positions_lost_on_laps_with_a_contact()
    {
        var session = CreateRace(CreateLaps(), Contact(350, Player, Rival));

        RaceStoryPresentation.Create(session, Player)!.PositionsLost.Should().Be(2);
    }

    /// <summary>
    /// Verifies that the sector of a contact comes from the times of its lap, and that a contact of
    /// the first lap has none: the start says nothing about a place on the track.
    /// </summary>
    [TestMethod]
    public void Create_finds_the_sector_of_a_contact()
    {
        var session = CreateRace(
            CreateLaps(),
            Contact(50, Player, Rival),
            Contact(105, Player, Rival),
            Contact(345, Player, Rival),
            Contact(395, Player, Rival));

        var story = RaceStoryPresentation.Create(session, Player)!;

        story.Contacts.Select(contact => contact.Sector).Should().Equal(null, 1, 2, 3);
        story.Contacts.Select(contact => contact.Lap).Should().Equal(1, 2, 4, 4);
    }

    /// <summary>
    /// Verifies that damage reported just after a contact is attached to it.
    /// </summary>
    [TestMethod]
    public void Create_attaches_damage_to_the_contact_before_it()
    {
        var session = CreateRace(
            CreateLaps(),
            Contact(150, Player, Rival),
            new SessionEvent(TimeSpan.FromSeconds(152), SessionEventKind.Damage, Player, "suspension"),
            Contact(350, Player, Rival),
            new SessionEvent(TimeSpan.FromSeconds(390), SessionEventKind.Damage, Player, "engine"));

        var story = RaceStoryPresentation.Create(session, Player)!;

        story.Contacts.Select(contact => contact.CausedDamage).Should().Equal(true, false);
    }

    /// <summary>
    /// Verifies that there is no story for a session other than a race or for an unknown driver.
    /// </summary>
    [TestMethod]
    public void Create_returns_no_story_without_a_race_of_the_driver()
    {
        RaceStoryPresentation.Create(CreateRace(CreateLaps()), "Unknown").Should().BeNull();
        RaceStoryPresentation.Create(CreateRace(CreateLaps(), type: "Qualify"), Player).Should().BeNull();
        RaceStoryPresentation.Create(CreateRace([]), Player).Should().BeNull();
    }

    /// <summary>
    /// Verifies what the profile says about the cost of incidents, with sentences that hold figures.
    /// </summary>
    [TestMethod]
    public void Profile_adds_up_the_cost_of_incidents()
    {
        var sessions = new[]
        {
            CreateRace(CreateLaps(), "Spa", Contact(350, Player, Rival), Contact(50, Player, Rival)),
            CreateRace(CreateLaps(), "Monza", Contact(50, Player, Rival), Contact(70, Player, "Other"), Contact(90, Player, "Third"))
        };

        var cost = DrivingProfilePresentation.Create(sessions, Player)!.Cost;

        cost.TimeLost.Should().Be(TimeSpan.FromSeconds(8));
        cost.Races.Should().Be(2);
        cost.Contacts.Should().Be(5);
        cost.FirstLapContacts.Should().Be(4);
        cost.CostliestCircuit.Should().Be("Spa");
        cost.Takeaways.Should().Contain("80 % de vos contacts arrivent au départ ou au premier tour.");
    }

    /// <summary>
    /// Verifies on the real fixtures that a lap with a contact is slower than a clean lap, which is
    /// what makes the time lost meaningful, and that each race can tell its story.
    /// </summary>
    [TestMethod]
    public void Stories_of_the_fixtures_show_that_contacts_cost_time()
    {
        var stories = Fixtures.Value
            .Where(session => session.Kind == SessionKind.Race)
            .Select(session => RaceStoryPresentation.Create(session, "Driver Tom"))
            .OfType<RaceStory>()
            .ToArray();
        var laps = stories.SelectMany(story => story.Laps.Select(lap => (Story: story, Lap: lap)))
            .Where(item => item.Lap.Time.HasValue && item.Story.ReferenceLap.HasValue && !item.Lap.IsPit && item.Lap.Number > 1)
            .ToArray();
        static double Median(IEnumerable<double> values)
        {
            var sorted = values.Order().ToArray();
            return sorted[sorted.Length / 2];
        }

        var withContact = Median(laps.Where(item => item.Lap.Loss.HasValue)
            .Select(item => (item.Lap.Time!.Value - item.Story.ReferenceLap!.Value).TotalSeconds));
        var clean = Median(laps.Where(item => item.Lap.Loss is null)
            .Select(item => (item.Lap.Time!.Value - item.Story.ReferenceLap!.Value).TotalSeconds));

        stories.Should().HaveCountGreaterThan(70);
        withContact.Should().BeGreaterThan(clean + 1);
    }

    /// <summary>
    /// Verifies on the real fixtures that the position of a lap is the one at its end: the last lap
    /// of the driver who finished gives their final position.
    /// </summary>
    [TestMethod]
    public void The_last_lap_of_a_finisher_gives_the_final_position_on_the_fixtures()
    {
        var finishers = Fixtures.Value
            .Where(session => session.Kind == SessionKind.Race)
            .SelectMany(session => session.Drivers)
            .Where(driver => driver.FinishStatus == "Finished Normally" && driver.Laps.Count > 0)
            .ToArray();

        var matching = finishers.Count(driver => driver.Laps[^1].Position == driver.Position);

        ((double)matching / finishers.Length).Should().BeGreaterThan(0.8);
    }

    private static SessionEvent Contact(double seconds, string driver, string opponent) =>
        new(TimeSpan.FromSeconds(seconds), SessionEventKind.VehicleContact, driver, opponent);

    // Laps of 100 s from t = 0: lap 3 stops in the pits, lap 4 loses 8 s and two positions.
    private static Lap[] CreateLaps() =>
    [
        CreateLap(1, 0, 100, 3),
        CreateLap(2, 100, 100, 3),
        new Lap(3, TimeSpan.FromSeconds(120), [], position: 3, startedAt: TimeSpan.FromSeconds(200), isPitLap: true),
        CreateLap(4, 320, 108, 5),
        CreateLap(5, 428, 100, 5)
    ];

    private static Lap CreateLap(int number, double start, double time, int position) =>
        new(
            number,
            TimeSpan.FromSeconds(time),
            [new Sector(1, TimeSpan.FromSeconds(time * 0.2)), new Sector(2, TimeSpan.FromSeconds(time * 0.4)), new Sector(3, TimeSpan.FromSeconds(time * 0.4))],
            position,
            TimeSpan.FromSeconds(start));

    private static Session CreateRace(Lap[] laps, params SessionEvent[] events) => CreateRace(laps, "Spa", events);

    private static Session CreateRace(Lap[] laps, string venue, params SessionEvent[] events) => CreateRace(laps, venue, "Race", events);

    private static Session CreateRace(Lap[] laps, string venue = "Spa", string type = "Race", params SessionEvent[] events) => new(
        Guid.NewGuid(),
        DateTimeOffset.FromUnixTimeSeconds(1760000000),
        venue,
        venue,
        "Event",
        type,
        [new Driver(Player, "1", "GT3", "Team", 5, laps, gridPosition: 4), new Driver(Rival, "2", "GT3", "Team", 1, [])],
        events: events);

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
}
