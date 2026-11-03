using FluentAssertions;
using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the geometry of the rating chart.
/// </summary>
[TestClass]
public sealed class RatingChartLayoutTests
{
    [TestMethod]
    public void The_line_starts_from_the_rating_before_the_first_race()
    {
        var chart = RatingChartLayout.Create(View([(1500, 1532), (1532, 1520), (1520, 1520.2)]));

        chart.Line.Split(' ').Should().HaveCount(4);
        chart.Markers.Select(marker => marker.CssClass).Should().Equal("rating-gain", "rating-loss", "rating-even");
        chart.Markers.Select(marker => marker.X).Should().BeInAscendingOrder();
        chart.Markers[0].Y.Should().BeLessThan(chart.Markers[1].Y, "a higher rating is drawn higher");
    }

    [TestMethod]
    public void The_tiers_are_drawn_inside_the_plot()
    {
        var chart = RatingChartLayout.Create(View(
            [(1500, 1510)],
            [new RatingTierThreshold(RatingTier.Platinum, 1600), new RatingTierThreshold(RatingTier.Gold, 1450)]));

        chart.TierLines.Select(line => line.Label).Should().Equal("Platine 1 600", "Or 1 450");
        chart.TierLines.Select(line => line.CssClass).Should().Equal("tier-platinum", "tier-gold");
        chart.TierLines.Should().OnlyContain(line => line.Y > chart.PlotTop && line.Y < chart.PlotBottom);
    }

    [TestMethod]
    public void The_rating_axis_uses_round_steps()
    {
        var chart = RatingChartLayout.Create(View([(1500, 1532), (1532, 1561)]));

        chart.RatingTicks.Should().HaveCountGreaterThan(1).And.HaveCountLessThanOrEqualTo(7);
        chart.RatingTicks.Select(tick => tick.Label).Should().Contain("1 500");
    }

    [TestMethod]
    public void The_race_axis_shows_at_most_six_dates()
    {
        var races = Enumerable.Range(0, 40).Select(index => (1500.0 + index, 1501.0 + index)).ToArray();

        var chart = RatingChartLayout.Create(View(races));

        chart.DateTicks.Should().HaveCountLessThanOrEqualTo(6);
        chart.DateTicks[0].Label.Should().MatchRegex(@"^\d\d/\d\d/\d\d$");
    }

    [TestMethod]
    public void A_marker_tells_its_race()
    {
        var chart = RatingChartLayout.Create(View([(1500, 1525)]));

        chart.Markers.Single().Title.Should().Contain("Spa").And.Contain("P3 sur 21 (attendu : 10.9)").And.Contain("+25 → 1 525");
    }

    private static DriverRatingView View((double Before, double After)[] races, RatingTierThreshold[]? tiers = null)
    {
        var timeline = races
            .Select((race, index) => new RatingRaceEntry(
                null,
                DateTimeOffset.FromUnixTimeSeconds(1760000000).AddDays(index),
                "Spa",
                "LMGT3",
                "Porsche",
                HomeResultKind.Podium,
                "P3",
                3,
                21,
                10.9,
                1500,
                race.Before,
                race.After))
            .ToArray();
        return new DriverRatingView(
            "Player", timeline[^1].After, timeline.Length, 1, 10, RatingTier.Gold, timeline.Max(race => race.After), timeline[0].StartedAt,
            0, timeline.Length, 1500, 0, null, null, tiers ?? [], timeline, [], [], []);
    }
}
