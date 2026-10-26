using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the career statistics shown on the home dashboard.
/// </summary>
[TestClass]
public sealed class HomePresentationTests
{
    private static readonly DateTimeOffset Now = Date("2026-10-26T12:00:00Z");

    [TestMethod]
    public void Create_counts_results_on_the_official_class_position_and_excludes_unclassified_races()
    {
        var sessions = new[]
        {
            Race("2026-10-01T20:00:00Z", "GT3", classPosition: 1, classGrid: 3),
            Race("2026-10-02T20:00:00Z", "GT3", classPosition: 3, classGrid: 2, status: "DNF", dnfReason: "Suspension"),
            Race("2026-10-03T20:00:00Z", "LMP2", classPosition: 7, classGrid: 9, status: "DQ"),
            Race("2026-10-04T20:00:00Z", "LMP2", classPosition: 1, classGrid: 1, status: "None"),
            Race("2026-10-05T20:00:00Z", "GT3", classPosition: 12, classGrid: 10)
        };

        var dashboard = HomePresentation.Create(sessions, "Driver", null, HomePeriod.All, Now);

        dashboard.Races.Value.Should().Be(5);
        dashboard.Races.Categories.Should().Equal(new HomeCategoryValue("LMP2", 2), new HomeCategoryValue("LMGT3", 3));
        dashboard.Wins.Value.Should().Be(1);
        dashboard.Wins.Rate.Should().Be(0.2);
        dashboard.Podiums.Value.Should().Be(2, "a retirement keeps its official class position");
        dashboard.Top5.Value.Should().Be(2);
        dashboard.Top10.Value.Should().Be(3);
        dashboard.AveragePosition.Value.Should().Be(23 / 4.0);
        dashboard.Retirements.Value.Should().Be(1);
        dashboard.Disqualifications.Value.Should().Be(1);
        dashboard.Unclassified.Value.Should().Be(1);
        dashboard.Unclassified.Rate.Should().Be(0.2);
        dashboard.AverageGridPosition.Value.Should().Be(24 / 4.0);
        dashboard.AveragePositionsGained.Value.Should().Be(1 / 4.0);
        dashboard.TotalPositionsGained.Should().Be(1);
    }

    [TestMethod]
    public void Create_counts_poles_and_qualifying_positions_from_qualifying_sessions_only()
    {
        var sessions = new[]
        {
            Qualifying("2026-10-01T19:30:00Z", "GT3", classPosition: 1),
            Qualifying("2026-10-02T19:30:00Z", "GT3", classPosition: 4),
            Race("2026-10-02T20:00:00Z", "GT3", classPosition: 1, classGrid: 4),
            Session("Practice1", "2026-10-02T19:00:00Z", Driver("GT3", classPosition: 1))
        };

        var dashboard = HomePresentation.Create(sessions, "Driver", null, HomePeriod.All, Now);

        dashboard.Poles.Value.Should().Be(1);
        dashboard.Poles.Rate.Should().Be(0.5);
        dashboard.AverageQualifyingPosition.Value.Should().Be(2.5);
        dashboard.Races.Value.Should().Be(1);
    }

    [TestMethod]
    public void Create_groups_retirement_causes_with_french_labels()
    {
        var sessions = new[]
        {
            Race("2026-10-01T20:00:00Z", "GT3", 10, 1, "DNF", "Suspension"),
            Race("2026-10-02T20:00:00Z", "GT3", 10, 1, "DNF", "Suspension"),
            Race("2026-10-03T20:00:00Z", "GT3", 10, 1, "DNF", "Engine"),
            Race("2026-10-04T20:00:00Z", "GT3", 10, 1, "DNF", "DNF"),
            Race("2026-10-05T20:00:00Z", "GT3", 10, 1, "DNF", ""),
            Race("2026-10-06T20:00:00Z", "GT3", 10, 1, "DNF", "Gearbox")
        };

        var dashboard = HomePresentation.Create(sessions, "Driver", null, HomePeriod.All, Now);

        dashboard.RetirementCauses.Select(cause => (cause.Label, cause.Count)).Should().Equal(
            ("Non précisée", 2),
            ("Suspension", 2),
            ("Gearbox", 1),
            ("Moteur", 1));
        dashboard.RetirementCauses[0].Share.Should().BeApproximately(2 / 6.0, 1e-9);
    }

    [TestMethod]
    public void Create_sums_laps_distance_and_timed_laps_of_races_and_qualifying_sessions()
    {
        var sessions = new[]
        {
            Session("Qualify", "2026-10-01T19:30:00Z", Driver("GT3", lapsCompleted: 3, lapTimes: [100, null, 101]), trackLength: 5000),
            Session("Race", "2026-10-01T20:00:00Z", Driver("GT3", classPosition: 1, lapsCompleted: 10, lapTimes: [102, 103]), trackLength: 5000),
            Session("Race", "2026-10-02T20:00:00Z", Driver("LMP2", classPosition: 1, lapsCompleted: 2, lapTimes: [90]), track: "Monza"),
            Session("Practice1", "2026-10-02T19:00:00Z", Driver("LMP2", lapsCompleted: 40, lapTimes: [90]), trackLength: 5000)
        };

        var dashboard = HomePresentation.Create(sessions, "Driver", null, HomePeriod.All, Now);

        dashboard.Laps.Value.Should().Be(15);
        dashboard.Laps.Categories.Should().Equal(new HomeCategoryValue("LMP2", 2), new HomeCategoryValue("LMGT3", 13));
        dashboard.DistanceKm.Value.Should().Be(65, "a session without track length adds no distance");
        dashboard.TimeOnTrack.Value.Should().Be(496, "laps without a time are not counted");
        dashboard.Circuits.Should().Be(2);
    }

    [TestMethod]
    public void Create_lists_the_last_ten_races_most_recent_first()
    {
        var sessions = Enumerable.Range(1, 12)
            .Select(day => Race($"2026-10-{day:00}T20:00:00Z", "GT3", classPosition: day, classGrid: 1))
            .Append(Race("2026-10-20T20:00:00Z", "GT3", 4, 1, "DNF"))
            .Append(Race("2026-10-21T20:00:00Z", "GT3", 2, 1, "None"))
            .ToArray();

        var dashboard = HomePresentation.Create(sessions, "Driver", null, HomePeriod.All, Now);

        dashboard.RecentForm.Should().HaveCount(10);
        dashboard.RecentForm.Select(entry => entry.Label).Should().Equal("NC", "DNF", "P12", "P11", "P10", "P9", "P8", "P7", "P6", "P5");
        dashboard.RecentForm.Select(entry => entry.Kind).Should().StartWith([HomeResultKind.Unclassified, HomeResultKind.Retired]);
        dashboard.LastRace!.StartedAt.Should().Be(Date("2026-10-21T20:00:00Z"));
        dashboard.LastRace.EventId.Should().Be(sessions[^1].Id.ToString());
    }

    [TestMethod]
    public void Create_marks_wins_and_podiums_in_the_recent_form()
    {
        var sessions = new[]
        {
            Race("2026-10-01T20:00:00Z", "GT3", 1, 1),
            Race("2026-10-02T20:00:00Z", "GT3", 3, 1),
            Race("2026-10-03T20:00:00Z", "GT3", 4, 1),
            Race("2026-10-04T20:00:00Z", "GT3", 2, 1, "DQ")
        };

        var dashboard = HomePresentation.Create(sessions, "Driver", null, HomePeriod.All, Now);

        dashboard.RecentForm.Select(entry => entry.Kind).Should().Equal(
            HomeResultKind.Disqualified,
            HomeResultKind.Classified,
            HomeResultKind.Podium,
            HomeResultKind.Win);
        dashboard.RecentForm[0].Label.Should().Be("DQ");
    }

    [TestMethod]
    public void Create_finds_favorites_and_breaks_ties_with_the_most_recent()
    {
        var sessions = new[]
        {
            Race("2026-10-01T20:00:00Z", "GT3", 1, 1, track: "Spa", car: "BMW M4 LMGT3"),
            Race("2026-10-02T20:00:00Z", "GT3", 2, 1, track: "Spa", car: "BMW M4 LMGT3"),
            Race("2026-10-03T20:00:00Z", "LMP2", 5, 1, track: "Monza", car: "Oreca 07"),
            Race("2026-10-04T20:00:00Z", "LMP2", 1, 1, track: "Monza", car: "Oreca 07")
        };

        var dashboard = HomePresentation.Create(sessions, "Driver", null, HomePeriod.All, Now);

        dashboard.FavoriteCircuit!.Name.Should().Be("Monza");
        dashboard.FavoriteCircuit.Races.Should().Be(2);
        dashboard.FavoriteCircuit.Wins.Should().Be(1);
        dashboard.FavoriteCircuit.Podiums.Should().Be(1);
        dashboard.FavoriteCar!.Name.Should().Be("Oreca 07");
        dashboard.CategoryShares.Select(share => (share.Label, share.Count)).Should().Equal(("LMP2", 2), ("LMGT3", 2));
    }

    [TestMethod]
    public void Create_applies_the_category_and_period_filters()
    {
        var sessions = new[]
        {
            Race("2025-06-01T20:00:00Z", "GT3", 1, 1),
            Race("2026-01-10T20:00:00Z", "GT3", 2, 1),
            Race("2026-10-20T20:00:00Z", "GT3", 3, 1),
            Race("2026-10-21T20:00:00Z", "LMP2", 4, 1)
        };

        HomePresentation.Create(sessions, "Driver", "LMGT3", HomePeriod.All, Now).Races.Value.Should().Be(3);
        HomePresentation.Create(sessions, "Driver", null, HomePeriod.Last30Days, Now).Races.Value.Should().Be(2);
        HomePresentation.Create(sessions, "Driver", null, HomePeriod.Last12Months, Now).Races.Value.Should().Be(3);
        HomePresentation.Create(sessions, "Driver", "LMGT3", HomePeriod.ForYear(2025), Now).Races.Value.Should().Be(1);
        HomePresentation.Create(sessions, "Driver", "LMP3", HomePeriod.All, Now).IsEmpty.Should().BeTrue();
    }

    [TestMethod]
    public void Filters_offer_the_categories_and_years_driven_by_the_selected_driver()
    {
        var sessions = new[]
        {
            Race("2025-06-01T20:00:00Z", "GT3", 1, 1),
            Qualifying("2026-01-10T20:00:00Z", "Hyper", 1),
            Race("2026-10-21T20:00:00Z", "Custom", 4, 1),
            Session("Race", "2024-10-21T20:00:00Z", Driver("LMP3", classPosition: 1, name: "Someone else"))
        };

        HomePresentation.GetCategories(sessions, "Driver").Should().Equal("Hypercar", "LMGT3", "Custom");
        HomePresentation.GetYears(sessions, "Driver").Should().Equal(2026, 2025);
    }

    [TestMethod]
    public void HomePeriod_round_trips_through_its_key()
    {
        HomePeriod.Parse(HomePeriod.ForYear(2025).Key).Should().Be(HomePeriod.ForYear(2025));
        HomePeriod.Parse(HomePeriod.Last30Days.Key).Should().Be(HomePeriod.Last30Days);
        HomePeriod.Parse("unknown").Should().Be(HomePeriod.All);
    }

    [TestMethod]
    public void Create_returns_an_empty_dashboard_without_a_selected_driver()
    {
        var dashboard = HomePresentation.Create([Race("2026-10-01T20:00:00Z", "GT3", 1, 1)], null, null, HomePeriod.All, Now);

        dashboard.IsEmpty.Should().BeTrue();
        dashboard.AveragePosition.Value.Should().BeNull();
        dashboard.LastRace.Should().BeNull();
    }

    /// <summary>
    /// Cross-checks the main fixture driver against values counted directly in the XML files.
    /// </summary>
    [TestMethod]
    public void Create_on_real_fixtures_matches_the_results_of_the_main_driver()
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

        var dashboard = HomePresentation.Create(sessions, "Driver Tom", null, HomePeriod.All, Now);

        dashboard.Races.Value.Should().Be(87);
        dashboard.Races.Categories.Should().Equal(
            new HomeCategoryValue("Hypercar", 3),
            new HomeCategoryValue("LMP2", 6),
            new HomeCategoryValue("LMP3", 13),
            new HomeCategoryValue("LMGTE", 23),
            new HomeCategoryValue("LMGT3", 42));
        dashboard.Wins.Value.Should().Be(1);
        dashboard.Podiums.Value.Should().Be(6);
        dashboard.Top5.Value.Should().Be(14);
        dashboard.Top10.Value.Should().Be(46);
        dashboard.Retirements.Value.Should().Be(13);
        dashboard.Disqualifications.Value.Should().Be(0);
        dashboard.Unclassified.Value.Should().Be(3);
        dashboard.TotalPositionsGained.Should().Be(116);
        dashboard.RetirementCauses.Select(cause => (cause.Label, cause.Count)).Should().Equal(
            ("Non précisée", 7),
            ("Suspension", 5),
            ("Moteur", 1));
        dashboard.DistanceKm.Value.Should().BePositive();
        dashboard.LastRace!.EventId.Should().NotBeNull();
    }

    private static DateTimeOffset Date(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    private static Session Race(
        string startedAt,
        string carClass,
        int classPosition,
        int classGrid,
        string status = "Finished Normally",
        string dnfReason = "",
        string track = "Spa",
        string car = "Car") =>
        Session("Race", startedAt, Driver(carClass, classPosition, classGrid, status, dnfReason, car), track);

    private static Session Qualifying(string startedAt, string carClass, int classPosition) =>
        Session("Qualify", startedAt, Driver(carClass, classPosition, status: "None"));

    private static Session Session(
        string type,
        string startedAt,
        Driver driver,
        string track = "Spa",
        double? trackLength = null) =>
        new(Guid.NewGuid(), Date(startedAt), track, track, $"Event {track}", type, [driver], trackLength: trackLength);

    private static Driver Driver(
        string carClass,
        int? classPosition = null,
        int? classGrid = null,
        string status = "Finished Normally",
        string dnfReason = "",
        string car = "Car",
        int? lapsCompleted = null,
        double?[]? lapTimes = null,
        string name = "Driver") =>
        new(
            name,
            "7",
            carClass,
            "Team",
            classPosition,
            (lapTimes ?? []).Select((time, index) => new Lap(index + 1, time is { } seconds ? TimeSpan.FromSeconds(seconds) : null, [])),
            car,
            classPosition: classPosition,
            lapsCompleted: lapsCompleted,
            finishStatus: status,
            classGridPosition: classGrid,
            dnfReason: dnfReason);
}
