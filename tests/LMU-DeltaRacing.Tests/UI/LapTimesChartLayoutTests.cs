using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the geometry of the lap times chart: lower is faster, slow laps at the top edge.
/// </summary>
[TestClass]
public sealed class LapTimesChartLayoutTests
{
    /// <summary>
    /// Verifies that the fastest lap is at the bottom, that a lap slower than the scale stays at the
    /// top edge, and that an untimed lap breaks the line.
    /// </summary>
    [TestMethod]
    public void Lower_is_faster_and_slow_laps_stay_at_the_top_edge()
    {
        var analysis = Analyse("Focus", 140, 100, 101, 99, null, 100, 102);

        var chart = LapTimesChartLayout.Create(analysis, null)!;

        var points = chart.Series.Single().Points;
        points.Should().HaveCount(6);
        points.MinBy(point => point.Y)!.Y.Should().Be(chart.PlotTop);
        points[3].Y.Should().Be(chart.PlotBottom);
        points[0].CssClass.Should().Be("lap-point-slow");
        chart.Series.Single().Segments.Should().HaveCount(2);
        chart.PaceY.Should().BeInRange(chart.PlotTop, chart.PlotBottom);
    }

    /// <summary>
    /// Verifies that the selected driver is drawn below the followed one, and that the pit laps of
    /// the followed driver are marked.
    /// </summary>
    [TestMethod]
    public void The_followed_driver_is_drawn_last_with_their_pit_laps()
    {
        var focus = Analyse("Focus", 110, 100, 101, 100, 101, pit: 3);
        var player = Analyse("Player", 112, 101, 102, 101, 102);

        var chart = LapTimesChartLayout.Create(focus, player)!;

        chart.Series.Select(series => series.CssClass).Should().Equal("race-line-player", "race-line-focus");
        chart.PitLaps.Should().ContainSingle();
        chart.Series[^1].Points[2].CssClass.Should().Be("lap-point-pit");
        chart.TimeTicks.Should().HaveCount(5);
    }

    /// <summary>
    /// Verifies that a driver without any timed lap gives no chart.
    /// </summary>
    [TestMethod]
    public void No_chart_without_any_timed_lap()
    {
        LapTimesChartLayout.Create(Analyse("Focus", null, null), null).Should().BeNull();
    }

    private static LapAnalysis Analyse(string name, params double?[] times) => Analyse(name, times, 0);

    private static LapAnalysis Analyse(string name, double? first, double? second, double? third, double? fourth, double? fifth, int pit) =>
        Analyse(name, [first, second, third, fourth, fifth], pit);

    private static LapAnalysis Analyse(string name, double?[] times, int pit)
    {
        var laps = times
            .Select((time, index) => new Lap(
                index + 1,
                time is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
                [],
                position: 1,
                isPitLap: index + 1 == pit))
            .ToArray();
        var session = new Session(
            Guid.NewGuid(),
            DateTimeOffset.FromUnixTimeSeconds(1760000000),
            "Track",
            "Track",
            "Event",
            "Race",
            [new Driver(name, "1", "GT3", "Team", 1, laps)]);
        return LapAnalysisPresentation.Create(session, name)!;
    }
}
