using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies how sessions are grouped into race events for the race-details page.
/// </summary>
[TestClass]
public sealed class RaceEventPresentationTests
{
    [TestMethod]
    public void CreateEvents_groups_practice_qualifying_and_race_and_starts_a_new_event_after_a_race()
    {
        var sessions = new[]
        {
            CreateSession("Practice1", "Spa", "2025-02-22T19:00:00Z"),
            CreateSession("Qualify", "Spa", "2025-02-22T19:00:00Z"),
            CreateSession("Race", "Spa", "2025-02-22T19:00:00Z"),
            CreateSession("Practice1", "Spa", "2025-02-22T19:40:00Z"),
            CreateSession("Qualify", "Spa", "2025-02-22T19:40:00Z"),
            CreateSession("Race", "Spa", "2025-02-22T19:40:00Z")
        };

        var events = RaceEventPresentation.CreateEvents(sessions, "Driver");

        events.Should().HaveCount(2);
        events[0].StartedAt.Should().Be(Date("2025-02-22T19:40:00Z"));
        events.Should().OnlyContain(raceEvent =>
            raceEvent.Practice != null && raceEvent.Qualifying != null && raceEvent.Race != null);
        events[0].Race!.StartedAt.Should().Be(Date("2025-02-22T19:40:00Z"));
    }

    [TestMethod]
    public void CreateEvents_merges_consecutive_practices_and_splits_sessions_more_than_two_hours_apart()
    {
        var sessions = new[]
        {
            CreateSession("Practice1", "Spa", "2025-02-20T10:00:00Z"),
            CreateSession("Practice1", "Spa", "2025-02-22T18:40:00Z"),
            CreateSession("Practice1", "Spa", "2025-02-22T19:00:00Z"),
            CreateSession("Qualify", "Spa", "2025-02-22T19:10:00Z"),
            CreateSession("Race", "Spa", "2025-02-22T19:30:00Z")
        };

        var events = RaceEventPresentation.CreateEvents(sessions, "Driver");

        events.Should().HaveCount(2);
        events[0].StartedAt.Should().Be(Date("2025-02-22T18:40:00Z"));
        events[0].PracticeSessionCount.Should().Be(2);
        events[0].Qualifying.Should().NotBeNull();
        events[0].Race.Should().NotBeNull();
        events[1].PracticeSessionCount.Should().Be(1);
        events[1].Qualifying.Should().BeNull();
        events[1].Race.Should().BeNull();
    }

    [TestMethod]
    public void CreateEvents_never_groups_two_qualifyings_two_races_or_two_circuits()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "2025-02-22T19:00:00Z"),
            CreateSession("Qualify", "Spa", "2025-02-22T19:10:00Z"),
            CreateSession("Race", "Spa", "2025-02-22T19:20:00Z"),
            CreateSession("Race", "Spa", "2025-02-22T19:50:00Z"),
            CreateSession("Race", "Monza", "2025-02-22T19:55:00Z")
        };

        var events = RaceEventPresentation.CreateEvents(sessions, "Driver");

        events.Should().HaveCount(4);
        events.Count(raceEvent => raceEvent.TrackVenue == "Monza").Should().Be(1);
    }

    [TestMethod]
    public void CreateEvents_keeps_only_events_where_the_selected_driver_took_part()
    {
        var sessions = new[]
        {
            CreateSession("Race", "Spa", "2025-02-22T19:00:00Z"),
            CreateSession("Race", "Monza", "2025-02-23T19:00:00Z", driverName: "Opponent")
        };

        var raceEvent = RaceEventPresentation.CreateEvents(sessions, "Driver").Should().ContainSingle().Which;

        raceEvent.TrackVenue.Should().Be("Spa");
    }

    [TestMethod]
    public void CreateEvents_merges_practice_results_and_ranks_drivers_by_best_lap()
    {
        var first = new Session(
            Guid.NewGuid(),
            Date("2025-02-22T19:00:00Z"),
            "Spa",
            "Spa",
            "Event",
            "Practice1",
            [
                CreateDriver("Driver", "GT3", 1, lapsCompleted: 2, lapTimes: [101, 100]),
                CreateDriver("Rival", "GT3", 2, lapsCompleted: 1, lapTimes: [102])
            ]);
        var second = new Session(
            Guid.NewGuid(),
            Date("2025-02-22T19:20:00Z"),
            "Spa",
            "Spa",
            "Event",
            "Practice1",
            [
                CreateDriver("Rival", "GT3", 1, lapsCompleted: 3, lapTimes: [99.5, 103, 104]),
                CreateDriver("Late", "Hyper", 2, lapsCompleted: 1, lapTimes: [90])
            ]);

        var practice = RaceEventPresentation.CreateEvents([first, second], "Driver")
            .Should().ContainSingle().Which.Practice!;

        practice.Drivers.Select(driver => driver.Name).Should().BeEquivalentTo(["Late", "Rival", "Driver"]);
        var rival = practice.Drivers.Single(driver => driver.Name == "Rival");
        rival.Laps.Select(lap => lap.Time).Should().Equal(
            TimeSpan.FromSeconds(102),
            TimeSpan.FromSeconds(99.5),
            TimeSpan.FromSeconds(103),
            TimeSpan.FromSeconds(104));
        rival.Laps.Select(lap => lap.Number).Should().Equal(1, 2, 3, 4);
        rival.LapsCompleted.Should().Be(4);
        rival.Position.Should().Be(2);
        rival.ClassPosition.Should().Be(1);
        practice.Drivers.Single(driver => driver.Name == "Driver").ClassPosition.Should().Be(2);
        practice.Drivers.Single(driver => driver.Name == "Late").Position.Should().Be(1);
    }

    [TestMethod]
    public void CreateEvents_on_real_fixtures_gives_one_event_per_race_of_the_main_driver()
    {
        var parser = new LMU_DeltaRacing.Infrastructure.Parsing.XmlResultParser();
        var sessions = Directory
            .EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return parser.Parse(stream);
            })
            .ToArray();

        var events = RaceEventPresentation.CreateEvents(sessions, "Driver Tom");

        events.Count(raceEvent => raceEvent.Race is not null && DriverProfile.FindDriver(raceEvent.Race, "Driver Tom") is not null)
            .Should().Be(87);
    }

    private static DateTimeOffset Date(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    private static Session CreateSession(string type, string track, string startedAt, string driverName = "Driver") =>
        new(
            Guid.NewGuid(),
            Date(startedAt),
            track,
            track,
            "Event",
            type,
            [CreateDriver(driverName, "GT3", 1, lapsCompleted: 1, lapTimes: [100])]);

    private static Driver CreateDriver(
        string name,
        string carClass,
        int position,
        int lapsCompleted,
        double[] lapTimes) =>
        new(
            name,
            "7",
            carClass,
            "Team",
            position,
            lapTimes.Select((time, index) => new Lap(index + 1, TimeSpan.FromSeconds(time), [])),
            "Car",
            classPosition: position,
            lapsCompleted: lapsCompleted);
}
