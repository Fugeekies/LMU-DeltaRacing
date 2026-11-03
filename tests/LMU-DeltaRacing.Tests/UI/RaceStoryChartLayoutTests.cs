using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the geometry of the race chart: one line per car, the focused driver on top.
/// </summary>
[TestClass]
public sealed class RaceStoryChartLayoutTests
{
    [TestMethod]
    public void The_focused_driver_is_drawn_last_so_that_it_stays_on_top()
    {
        var chart = RaceStoryChartLayout.Create(Story(), [Line("Focus", [1, 2, 2], focus: true), Line("Player", [2, 1, 1], player: true), Line("Other", [3, 3, 3])]);

        chart.Lines.Select(line => line.CssClass).Should().Equal("race-line-other", "race-line-player", "race-line-focus");
        chart.Lines[^1].Points.Split(' ').Should().HaveCount(3);
    }

    [TestMethod]
    public void Every_line_is_named_in_a_small_field_and_only_the_drivers_in_a_large_one()
    {
        var small = RaceStoryChartLayout.Create(Story(), [Line("Focus", [1, 1], focus: true), Line("Other", [2, 2])]);
        var large = RaceStoryChartLayout.Create(
            Story(),
            [Line("Focus", [1, 1], focus: true), .. Enumerable.Range(2, RaceStoryChartLayout.MaximumLabels).Select(position => Line($"D{position}", [position, position]))]);

        small.Lines.Should().OnlyContain(line => line.LabelY != null);
        large.Lines.Where(line => line.LabelY != null).Select(line => line.Name).Should().Equal("Focus");
    }

    [TestMethod]
    public void Names_never_overlap()
    {
        var chart = RaceStoryChartLayout.Create(Story(), [.. Enumerable.Range(1, 10).Select(position => Line($"D{position}", [position, position], focus: position == 1))]);

        var labels = chart.Lines.Select(line => line.LabelY!.Value).Order().ToArray();
        labels.Zip(labels.Skip(1), (above, below) => below - above).Should().OnlyContain(gap => gap >= 12.9);
        labels[^1].Should().BeLessThanOrEqualTo(chart.PlotBottom + 4);
    }

    [TestMethod]
    public void A_retired_car_is_named_with_its_last_lap()
    {
        var chart = RaceStoryChartLayout.Create(Story(), [Line("Focus", [1, 1, 1], focus: true), Line("Out", [2, 2], retired: true)]);

        var retired = chart.Lines.Single(line => line.Name == "Out");
        retired.Retired.Should().BeTrue();
        retired.Title.Should().Be("P2 Out (abandon au tour 1)");
        retired.EndX.Should().BeLessThan(chart.PlotRight);
    }

    [TestMethod]
    public void The_scale_follows_the_cars_shown()
    {
        var chart = RaceStoryChartLayout.Create(Story(), [Line("Focus", [12, 11, 10], focus: true), Line("Other", [11, 12, 11])]);

        chart.PositionTicks[0].Label.Should().Be("P10");
        chart.PositionTicks[^1].Label.Should().Be("P12");
    }

    [TestMethod]
    public void Incidents_sit_on_the_line_of_the_focused_driver()
    {
        var story = Story(new RaceStoryLap(2, 5, TimeSpan.FromSeconds(120), null, true,
            [new DrivingIncident(TimeSpan.FromSeconds(200), SessionEventKind.VehicleContact, "Rival", 2)]));

        var chart = RaceStoryChartLayout.Create(story, [Line("Focus", [1, 2, 3], focus: true), Line("Other", [3, 1, 1])]);

        var marker = chart.Markers.Should().ContainSingle().Subject;
        marker.CssClass.Should().Be("story-contact");
        marker.Y.Should().Be(chart.Lines[^1].EndY, "the focused driver is 3rd after lap 2, like at the end of its line");
        chart.PitStops.Should().ContainSingle().Which.Should().Be(marker.X);
    }

    private static RacePositionLine Line(string name, int[] positions, bool focus = false, bool player = false, bool retired = false) =>
        new(name, "LMGT3", focus, player, retired, positions.Select((position, lap) => new RacePositionPoint(lap, position)).ToArray());

    private static RaceStory Story(params RaceStoryLap[] laps) =>
        new("Focus", 1, laps.Length == 0 ? [new RaceStoryLap(1, 1, null, null, false, [])] : laps, null, TimeSpan.Zero, 0, []);
}
