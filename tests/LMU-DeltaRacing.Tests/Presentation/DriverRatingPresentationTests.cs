using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the rating page of the selected driver: tiers, goals, highlights and leaderboard.
/// </summary>
[TestClass]
public sealed class DriverRatingPresentationTests
{
    private static readonly string[] Grid = ["D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D10"];

    /// <summary>
    /// Verifies that there is no rating without a selected driver or without any of their rated races.
    /// </summary>
    [TestMethod]
    public void Create_returns_no_rating_without_a_driver_or_their_rated_races()
    {
        var sessions = new[] { Race(0, "D1", "D2") };

        DriverRatingPresentation.Create(sessions, null).Should().BeNull();
        DriverRatingPresentation.Create(sessions, "D3").Should().BeNull();
    }

    /// <summary>
    /// Verifies the tiers, given by the share of ranked drivers ahead: platinum for the top 10 %,
    /// gold up to 30 %, silver up to 60 %, bronze below.
    /// </summary>
    [TestMethod]
    public void Ranked_drivers_get_a_tier_from_their_share_of_the_ranking()
    {
        var view = DriverRatingPresentation.Create(SameOrderSeason(), "D5")!;

        view.Leaderboard.Select(row => row.Tier).Should().Equal(
            RatingTier.Platinum,
            RatingTier.Gold, RatingTier.Gold,
            RatingTier.Silver, RatingTier.Silver, RatingTier.Silver,
            RatingTier.Bronze, RatingTier.Bronze, RatingTier.Bronze, RatingTier.Bronze);
        view.Rank.Should().Be(5);
        view.RankedDrivers.Should().Be(10);
        view.Tier.Should().Be(RatingTier.Silver);
        view.Tiers.Select(threshold => (threshold.Tier, threshold.MinimumRating)).Should().Equal(
            (RatingTier.Platinum, view.Leaderboard[0].Rating),
            (RatingTier.Gold, view.Leaderboard[2].Rating),
            (RatingTier.Silver, view.Leaderboard[5].Rating));
    }

    /// <summary>
    /// Verifies the two goals of the driver: the next driver to overtake and the next tier.
    /// </summary>
    [TestMethod]
    public void The_goals_are_the_next_driver_and_the_next_tier()
    {
        var view = DriverRatingPresentation.Create(SameOrderSeason(), "D5")!;

        view.NextRival!.Label.Should().Be("D4");
        view.NextRival.Points.Should().Be((int)Math.Ceiling(view.Leaderboard[3].Rating - view.Rating));
        view.NextTier!.Label.Should().Be("Or");
        view.NextTier.Points.Should().Be((int)Math.Ceiling(view.Leaderboard[2].Rating - view.Rating));

        var leader = DriverRatingPresentation.Create(SameOrderSeason(), "D1")!;
        leader.NextRival.Should().BeNull();
        leader.NextTier.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a driver in calibration has a rating but no rank, and is left out of the leaderboard.
    /// </summary>
    [TestMethod]
    public void A_provisional_driver_is_not_ranked()
    {
        var sessions = SameOrderSeason().Append(Race(10, "D1", "Newcomer")).ToArray();

        var view = DriverRatingPresentation.Create(sessions, "Newcomer")!;

        view.IsProvisional.Should().BeTrue();
        view.RacesToCalibrate.Should().Be(DriverRatings.CalibrationRaces - 1);
        view.Rank.Should().BeNull();
        view.Tier.Should().BeNull();
        view.NextRival.Should().BeNull();
        view.Timeline.Should().ContainSingle();
        view.Leaderboard.Should().NotContain(row => row.Name == "Newcomer");
    }

    /// <summary>
    /// Verifies the highlights: the biggest gains and the biggest losses, three of each at most.
    /// </summary>
    [TestMethod]
    public void The_highlights_are_the_biggest_gains_and_losses()
    {
        var sessions = new[]
        {
            Race(0, "D1", "D2", "D3"),
            Race(1, "D2", "D1", "D3"),
            Race(2, "D3", "D2", "D1"),
            Race(3, "D1", "D3", "D2"),
            Race(4, "D3", "D1", "D2"),
            Race(5, "D2", "D3", "D1")
        };

        var view = DriverRatingPresentation.Create(sessions, "D1")!;

        // Two wins and two third places: whatever the second places give, both lists have 2 to 3 races.
        view.Exploits.Should().HaveCountGreaterThanOrEqualTo(2).And.HaveCountLessThanOrEqualTo(3).And.OnlyContain(race => race.Delta > 0);
        view.Exploits.Select(race => race.Delta).Should().BeInDescendingOrder();
        view.Exploits[0].Delta.Should().Be(view.Timeline.Max(race => race.Delta));
        view.Setbacks.Should().HaveCountGreaterThanOrEqualTo(2).And.HaveCountLessThanOrEqualTo(3).And.OnlyContain(race => race.Delta < 0);
        view.Setbacks.Select(race => race.Delta).Should().BeInAscendingOrder();
        view.Setbacks[0].Delta.Should().Be(view.Timeline.Min(race => race.Delta));
    }

    /// <summary>
    /// Verifies the summary of the recent races and of the races above expectation.
    /// </summary>
    [TestMethod]
    public void The_summary_adds_up_the_recent_races()
    {
        var sessions = Enumerable.Range(0, 12)
            .Select(day => day % 3 == 0 ? Race(day, "D2", "D1", "D3") : Race(day, "D1", "D2", "D3"))
            .ToArray();

        var view = DriverRatingPresentation.Create(sessions, "D1")!;

        view.RecentRaces.Should().Be(10);
        view.RecentChange.Should().BeApproximately(view.Timeline.TakeLast(10).Sum(race => race.Delta), 0.0001);
        view.RacesAboveExpectation.Should().Be(view.Timeline.Count(race => race.ClassRank < race.ExpectedRank));
        view.Timeline.Select(race => race.StartedAt).Should().BeInAscendingOrder();
        view.Peak.Should().Be(view.Timeline.Max(race => race.After));
    }

    /// <summary>
    /// Verifies the shortlist of the leaderboard: the top 10 and the neighbours of the driver.
    /// </summary>
    [TestMethod]
    public void The_shortlist_keeps_the_top_ten_and_the_neighbours_of_the_driver()
    {
        var rows = Enumerable.Range(1, 30)
            .Select(rank => new RatingLeaderboardRow(rank, $"D{rank}", 2000 - rank, RatingTier.Bronze, 5, DateTimeOffset.UnixEpoch, rank == 20))
            .ToArray();

        DriverRatingPresentation.Shortlist(rows).Select(row => row.Rank)
            .Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 17, 18, 19, 20, 21, 22, 23);
        DriverRatingPresentation.Shortlist(rows.Select(row => row with { IsPlayer = row.Rank == 2 }).ToArray()).Select(row => row.Rank)
            .Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }

    /// <summary>
    /// Verifies the rating page of the main driver on the real fixtures.
    /// </summary>
    [TestMethod]
    public void The_main_driver_of_the_fixtures_gets_a_complete_rating_page()
    {
        var parser = new XmlResultParser();
        var sessions = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(file =>
            {
                using var stream = File.OpenRead(file);
                return parser.Parse(stream);
            })
            .ToArray();

        var view = DriverRatingPresentation.Create(sessions, "Driver Tom")!;

        view.Timeline.Should().HaveCount(72).And.OnlyContain(race => race.EventId != null);
        view.Timeline.Should().Contain(race => race.Result == "P1");
        view.IsProvisional.Should().BeFalse();
        view.Tier.Should().Be(RatingTier.Platinum);
        view.Leaderboard.Should().ContainSingle(row => row.IsPlayer).Which.Rank.Should().Be(view.Rank);
        view.Exploits.Should().HaveCount(3);
        view.Setbacks.Should().HaveCount(3);
    }

    /// <summary>
    /// Five races of the same ten drivers, always in the same order: the ratings follow that order.
    /// </summary>
    private static Session[] SameOrderSeason() =>
        Enumerable.Range(0, DriverRatings.CalibrationRaces).Select(day => Race(day, Grid)).ToArray();

    private static Session Race(int day, params string[] drivers) => new(
        Guid.NewGuid(),
        DateTimeOffset.FromUnixTimeSeconds(1760000000).AddDays(day),
        "Spa",
        "Spa",
        "Event",
        "Race",
        drivers.Select((name, index) => new Driver(
            name,
            "1",
            "GT3",
            "Team",
            index + 1,
            [],
            classPosition: index + 1,
            finishStatus: "Finished Normally")));
}
