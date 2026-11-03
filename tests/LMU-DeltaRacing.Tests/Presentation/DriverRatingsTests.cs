using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the multiplayer Elo rating calculated for every driver from the race results.
/// </summary>
[TestClass]
public sealed class DriverRatingsTests
{
    private const string Finished = "Finished Normally";

    /// <summary>
    /// Verifies that between two new drivers, the winner takes what the loser gives.
    /// </summary>
    [TestMethod]
    public void The_winner_of_an_even_race_takes_the_points_of_the_loser()
    {
        var history = DriverRatings.Calculate([Race(0, ("Alice", 1), ("Bob", 2))]);

        var race = history.Races.Should().ContainSingle().Subject;
        race.Find("Alice")!.After.Should().Be(1532);
        race.Find("Bob")!.After.Should().Be(1468);
        history.FindDriver("alice ")!.Rating.Should().Be(1532);
    }

    /// <summary>
    /// Verifies the expected rank and the field strength, calculated with the ratings before the race.
    /// </summary>
    [TestMethod]
    public void The_expected_rank_and_the_field_strength_use_the_ratings_before_the_race()
    {
        var history = DriverRatings.Calculate(
        [
            Race(0, ("Alice", 1), ("Bob", 2), ("Chloé", 3)),
            Race(1, ("Alice", 1), ("Bob", 2), ("Chloé", 3))
        ]);

        var first = history.Races[0].Find("Alice")!;
        first.ExpectedRank.Should().Be(2);
        first.FieldStrength.Should().Be(1500);
        first.FieldSize.Should().Be(3);

        var second = history.Races[1];
        second.Find("Alice")!.ExpectedRank.Should().BeLessThan(2);
        second.Find("Chloé")!.ExpectedRank.Should().BeGreaterThan(2);
        second.Find("Alice")!.FieldStrength.Should().BeApproximately(1500, 0.001);
        second.Find("Alice")!.Before.Should().Be(first.After);
    }

    /// <summary>
    /// Verifies that drivers are only compared within their class, as in the head-to-head view.
    /// </summary>
    [TestMethod]
    public void Drivers_are_only_compared_within_their_class()
    {
        var history = DriverRatings.Calculate(
        [
            Race(0, ("Alice", 1, "GT3"), ("Bob", 2, "GT3"), ("Hugo", 1, "Hyper"))
        ]);

        var race = history.Races.Single();
        race.Find("Alice")!.FieldSize.Should().Be(2);
        race.Find("Hugo").Should().BeNull("a car alone in its class has nobody to be compared with");
        history.FindDriver("Hugo").Should().BeNull();
    }

    /// <summary>
    /// Verifies that a class is rated only when the race reached the flag: when the player quits
    /// at the start, LMU writes the file with every other driver still running ("None").
    /// </summary>
    [TestMethod]
    public void A_class_without_any_finisher_is_not_rated()
    {
        var history = DriverRatings.Calculate(
        [
            Race(0, ("Alice", 1, "GT3", "None"), ("Bob", 2, "GT3", "None"), ("Tom", 3, "GT3", "DNF"),
                ("Hugo", 1, "Hyper", Finished), ("Inès", 2, "Hyper", "DNF"))
        ]);

        var race = history.Races.Single();
        race.Classes.Should().ContainSingle().Which.CarClass.Should().Be("Hyper");
        history.FindDriver("Tom").Should().BeNull();
    }

    /// <summary>
    /// Verifies that drivers still running when the file was written, retirements and
    /// disqualifications keep their official class position once the race reached the flag.
    /// </summary>
    [TestMethod]
    public void Unclassified_retired_and_disqualified_drivers_keep_their_official_position()
    {
        var history = DriverRatings.Calculate(
        [
            Race(0, ("Alice", 1, "GT3", Finished), ("Bob", 2, "GT3", "None"), ("Chloé", 3, "GT3", "DNF"), ("Dan", 4, "GT3", "DQ"))
        ]);

        var race = history.Races.Single();
        race.Find("Bob")!.ClassRank.Should().Be(2);
        race.Find("Dan")!.Delta.Should().BeNegative();
        race.Classes.Single().Drivers.Select(change => change.Name).Should().Equal("Alice", "Bob", "Chloé", "Dan");
    }

    /// <summary>
    /// Verifies that the rating moves faster during the calibration races, then slows down.
    /// </summary>
    [TestMethod]
    public void The_rating_moves_twice_slower_after_the_calibration_races()
    {
        var races = Enumerable.Range(0, DriverRatings.CalibrationRaces + 1)
            .Select(index => Race(index, ("Alice", 1), ("Bob", 2)))
            .ToArray();

        var history = DriverRatings.Calculate(races);

        var calibration = history.Races[DriverRatings.CalibrationRaces - 1].Find("Alice")!;
        var settled = history.Races[DriverRatings.CalibrationRaces].Find("Alice")!;
        calibration.Delta.Should().BeApproximately(64 * (1 - Expected(calibration.Before, 3000 - calibration.Before)), 0.001);
        settled.Delta.Should().BeApproximately(32 * (1 - Expected(settled.Before, 3000 - settled.Before)), 0.001);
        history.FindDriver("Alice")!.IsProvisional.Should().BeFalse();
        history.FindDriver("Alice")!.Races.Should().Be(DriverRatings.CalibrationRaces + 1);
    }

    /// <summary>
    /// Verifies that a driver is provisional during the calibration races.
    /// </summary>
    [TestMethod]
    public void A_driver_is_provisional_during_the_calibration_races()
    {
        var history = DriverRatings.Calculate([Race(0, ("Alice", 1), ("Bob", 2))]);

        history.FindDriver("Alice")!.IsProvisional.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that races are rated in chronological order, whatever the order of the files,
    /// and that qualifying and practice sessions are ignored.
    /// </summary>
    [TestMethod]
    public void Races_are_rated_in_chronological_order_and_other_sessions_are_ignored()
    {
        var sessions = new[]
        {
            Race(2, ("Bob", 1), ("Alice", 2)),
            Race(1, ("Alice", 1), ("Bob", 2)),
            Race(0, [("Bob", 1), ("Alice", 2)], "Qualify"),
            Race(0, [("Bob", 1), ("Alice", 2)], "Practice1")
        };

        var history = DriverRatings.Calculate(sessions);

        history.Races.Select(race => race.Session.StartedAt).Should().BeInAscendingOrder();
        history.Races.Should().HaveCount(2);
        history.Races[0].Find("Alice")!.Delta.Should().Be(32);
    }

    /// <summary>
    /// Verifies that a driver listed twice in a session (two merged names) is rated once, at their best position.
    /// </summary>
    [TestMethod]
    public void A_driver_listed_twice_in_a_race_is_rated_once()
    {
        var history = DriverRatings.Calculate([Race(0, ("Alice", 1), ("Bob", 2), ("ALICE", 3))]);

        var race = history.Races.Single();
        race.Classes.Single().Drivers.Should().HaveCount(2);
        race.Find("Alice")!.ClassRank.Should().Be(1);
    }

    /// <summary>
    /// Verifies the peak and the ranking of the drivers, best first.
    /// </summary>
    [TestMethod]
    public void Drivers_are_ranked_best_first_with_their_peak()
    {
        var history = DriverRatings.Calculate(
        [
            Race(0, ("Alice", 1), ("Bob", 2)),
            Race(1, ("Bob", 1), ("Alice", 2)),
            Race(2, ("Bob", 1), ("Alice", 2))
        ]);

        history.Drivers.Select(driver => driver.Name).Should().Equal("Bob", "Alice");
        var alice = history.FindDriver("Alice")!;
        alice.Peak.Should().Be(1532);
        alice.PeakAt.Should().Be(history.Races[0].Session.StartedAt);
        alice.LastRaceAt.Should().Be(history.Races[2].Session.StartedAt);
    }

    /// <summary>
    /// Verifies the rating of the main driver on the real fixtures.
    /// </summary>
    [TestMethod]
    public void The_main_driver_of_the_fixtures_is_rated_on_the_races_that_reached_the_flag()
    {
        var parser = new XmlResultParser();
        var sessions = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(file =>
            {
                using var stream = File.OpenRead(file);
                return parser.Parse(stream);
            })
            .ToArray();

        var history = DriverRatings.Calculate(sessions);

        // 107 races, of which 19 never reached the flag: the player quit and LMU wrote "None" for the others.
        history.Races.Should().HaveCount(88);
        var tom = history.FindDriver("Driver Tom")!;
        tom.Races.Should().Be(72);
        tom.IsProvisional.Should().BeFalse();
        tom.Rating.Should().BeInRange(1550, 1600);
        history.Drivers.Where(driver => !driver.IsProvisional).ToList().FindIndex(driver => driver.Name == "Driver Tom")
            .Should().BeLessThan(20, "the demonstration driver wins races and finishes in the top half");
    }

    private static double Expected(double rating, double opponent) =>
        1 / (1 + Math.Pow(10, (opponent - rating) / 400));

    private static Session Race(int day, params (string Name, int Rank)[] drivers) =>
        Race(day, drivers.Select(driver => (driver.Name, driver.Rank, "GT3", Finished)).ToArray());

    private static Session Race(int day, params (string Name, int Rank, string CarClass)[] drivers) =>
        Race(day, drivers.Select(driver => (driver.Name, driver.Rank, driver.CarClass, Finished)).ToArray());

    private static Session Race(int day, params (string Name, int Rank, string CarClass, string Status)[] drivers) =>
        Race(day, drivers, "Race");

    private static Session Race(int day, (string Name, int Rank)[] drivers, string type) =>
        Race(day, drivers.Select(driver => (driver.Name, driver.Rank, "GT3", Finished)).ToArray(), type);

    private static Session Race(int day, (string Name, int Rank, string CarClass, string Status)[] drivers, string type) => new(
        Guid.NewGuid(),
        DateTimeOffset.FromUnixTimeSeconds(1760000000).AddDays(day),
        "Spa",
        "Spa",
        "Event",
        type,
        drivers.Select((driver, index) => new Driver(
            driver.Name,
            "1",
            driver.CarClass,
            "Team",
            index + 1,
            [],
            classPosition: driver.Rank,
            finishStatus: driver.Status)));
}
