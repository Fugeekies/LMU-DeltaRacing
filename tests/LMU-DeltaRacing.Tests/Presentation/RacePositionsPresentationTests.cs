using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the positions of every car lap by lap, drawn by the race chart of "race details".
/// </summary>
[TestClass]
public sealed class RacePositionsPresentationTests
{
    /// <summary>
    /// Verifies that each car gets a line from its grid position, then its position after each lap.
    /// </summary>
    [TestMethod]
    public void Every_car_gets_its_overall_position_lap_by_lap_from_the_grid()
    {
        var session = Race(
            ("Alice", "GT3", 1, [1, 2, 2]),
            ("Bob", "GT3", 2, [2, 1, 1]),
            ("Hugo", "Hyper", 3, [3, 3, 3]));

        var positions = RacePositionsPresentation.Create(session, "Alice", "Alice", PositionScope.Overall)!;

        positions.Lines.Should().HaveCount(3);
        positions.LastLap.Should().Be(3);
        positions.FieldSize.Should().Be(3);
        var alice = positions.Lines.Single(line => line.Name == "Alice");
        alice.IsFocus.Should().BeTrue();
        alice.IsPlayer.Should().BeTrue();
        alice.Points.Should().Equal(new RacePositionPoint(0, 1), new RacePositionPoint(1, 1), new RacePositionPoint(2, 2), new RacePositionPoint(3, 2));
        alice.Category.Should().Be("LMGT3");
    }

    /// <summary>
    /// Verifies that the class view keeps the class of the driver and ranks its cars among themselves.
    /// </summary>
    [TestMethod]
    public void The_class_view_ranks_the_cars_of_the_class_among_themselves()
    {
        var session = Race(
            ("Hugo", "Hyper", 1, [1, 1]),
            ("Alice", "GT3", 2, [2, 3]),
            ("Bob", "GT3", 3, [3, 2]));

        var positions = RacePositionsPresentation.Create(session, "Bob", "Alice", PositionScope.Class)!;

        positions.Lines.Select(line => line.Name).Should().BeEquivalentTo("Alice", "Bob");
        positions.FieldSize.Should().Be(2);
        positions.Lines.Single(line => line.Name == "Bob").Points.Select(point => point.Position).Should().Equal(2, 2, 1);
        positions.Lines.Single(line => line.Name == "Alice").IsPlayer.Should().BeTrue();
        positions.Lines.Single(line => line.Name == "Bob").IsFocus.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a retired car stops at its last lap and is marked as retired.
    /// </summary>
    [TestMethod]
    public void A_retired_car_stops_at_its_last_lap()
    {
        var session = Race(
            ("Alice", "GT3", 1, [1, 1, 1], "Finished Normally"),
            ("Bob", "GT3", 2, [2], "DNF"));

        var positions = RacePositionsPresentation.Create(session, "Alice", "Alice", PositionScope.Overall)!;

        var bob = positions.Lines.Single(line => line.Name == "Bob");
        bob.Retired.Should().BeTrue();
        bob.FinalRank.Should().Be(2);
        bob.Points.Should().HaveCount(2);
        positions.Lines.Single(line => line.Name == "Alice").Retired.Should().BeFalse();
    }

    /// <summary>
    /// Verifies the cars kept around the driver: the N cars just ahead and the N just behind them in
    /// the final classification, whatever happened during the race, so that ±N never shows more
    /// than 2N + 1 lines.
    /// </summary>
    [TestMethod]
    public void Around_keeps_the_cars_just_ahead_and_just_behind_at_the_finish()
    {
        var session = Race(
            ("A", "GT3", 1, [1, 1, 1]),
            ("B", "GT3", 2, [2, 2, 2]),
            ("C", "GT3", 3, [3, 4, 5]),
            ("D", "GT3", 4, [5, 5, 4]),
            ("E", "GT3", 5, [4, 3, 3]),
            ("F", "GT3", 6, [6, 6, 6]));

        var positions = RacePositionsPresentation.Create(session, "C", "C", PositionScope.Overall)!;

        // C finishes 5th: D (4th) and F (6th) are kept, E is not, although it ran next to C all race.
        positions.Around(1).Select(line => line.Name).Should().BeEquivalentTo("C", "D", "F");
        positions.Around(2).Should().HaveCount(4, "only one car finished behind C");
        positions.Around(null).Should().HaveCount(6);
        positions.Lines.Select(line => line.FinalRank).Should().Equal(1, 2, 3, 4, 5, 6);
        RacePositionsPresentation.Create(session, "A", "A", PositionScope.Overall)!
            .Around(1).Select(line => line.Name).Should().BeEquivalentTo("A", "B");
    }

    /// <summary>
    /// Verifies that there are no positions for a qualifying session or an unknown driver.
    /// </summary>
    [TestMethod]
    public void There_are_no_positions_outside_a_race_or_for_an_unknown_driver()
    {
        var race = Race(("Alice", "GT3", 1, [1]), ("Bob", "GT3", 2, [2]));

        RacePositionsPresentation.Create(race, "Nobody", "Alice", PositionScope.Overall).Should().BeNull();
        RacePositionsPresentation.Create(Race("Qualify", ("Alice", "GT3", 1, [1], "")), "Alice", "Alice", PositionScope.Overall).Should().BeNull();
    }

    /// <summary>
    /// Verifies the multiclass race of the fixtures (Hyper, LMP2, GTE): every car with lap positions
    /// gets a line, and the class view keeps the GTE cars only.
    /// </summary>
    [TestMethod]
    public void The_multiclass_race_of_the_fixtures_gets_a_line_per_car()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "Results", "2025_03_04_18_40_59-34R1.xml"));
        var session = new XmlResultParser().Parse(stream);
        var focus = session.Drivers.First(driver => driver.CarClass == "GTE" && driver.Laps.Any(lap => lap.Position.HasValue)).Name;

        var overall = RacePositionsPresentation.Create(session, focus, focus, PositionScope.Overall)!;
        var inClass = RacePositionsPresentation.Create(session, focus, focus, PositionScope.Class)!;

        overall.Lines.Should().HaveCount(session.Drivers.Count(driver => driver.Laps.Any(lap => lap.Position.HasValue)));
        inClass.Lines.Should().HaveCountLessThan(overall.Lines.Count);
        inClass.Lines.Should().OnlyContain(line => line.Category == "LMGTE");
        inClass.Lines.SelectMany(line => line.Points).Max(point => point.Position).Should().BeLessThanOrEqualTo(inClass.FieldSize);
        overall.Around(2).Should().Contain(line => line.IsFocus).And.HaveCountLessThanOrEqualTo(5);
        inClass.Around(1).Should().Contain(line => line.IsFocus).And.HaveCountLessThanOrEqualTo(3);
    }

    private static Session Race(params (string Name, string CarClass, int Grid, int[] Positions)[] drivers) =>
        Race("Race", drivers.Select(driver => (driver.Name, driver.CarClass, driver.Grid, driver.Positions, "Finished Normally")).ToArray());

    private static Session Race(params (string Name, string CarClass, int Grid, int[] Positions, string Status)[] drivers) =>
        Race("Race", drivers);

    private static Session Race(string type, params (string Name, string CarClass, int Grid, int[] Positions, string Status)[] drivers) => new(
        Guid.NewGuid(),
        DateTimeOffset.FromUnixTimeSeconds(1760000000),
        "Spa",
        "Spa",
        "Event",
        type,
        drivers.Select(driver => new Driver(
            driver.Name,
            "1",
            driver.CarClass,
            "Team",
            driver.Positions[^1],
            driver.Positions.Select((position, index) => new Lap(index + 1, TimeSpan.FromSeconds(120), [], position)),
            gridPosition: driver.Grid,
            finishStatus: driver.Status)));
}
