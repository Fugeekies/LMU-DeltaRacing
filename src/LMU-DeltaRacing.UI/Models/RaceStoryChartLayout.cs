using System.Globalization;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The geometry of the position chart of a race story, drawn in SVG.
/// </summary>
/// <param name="Width">The width of the SVG view box.</param>
/// <param name="Height">The height of the SVG view box.</param>
/// <param name="PlotLeft">The left edge of the plot.</param>
/// <param name="PlotRight">The right edge of the plot.</param>
/// <param name="PlotTop">The top edge of the plot.</param>
/// <param name="PlotBottom">The bottom edge of the plot.</param>
/// <param name="Line">The points of the position line, as an SVG list.</param>
/// <param name="Markers">The incidents of the race.</param>
/// <param name="PitStops">The horizontal positions of the pit stops.</param>
/// <param name="PositionTicks">The labels of the position axis.</param>
/// <param name="LapTicks">The labels of the lap axis.</param>
public sealed record RaceStoryChartLayout(
    double Width,
    double Height,
    double PlotLeft,
    double PlotRight,
    double PlotTop,
    double PlotBottom,
    string Line,
    IReadOnlyList<RaceStoryMarker> Markers,
    IReadOnlyList<double> PitStops,
    IReadOnlyList<RaceStoryTick> PositionTicks,
    IReadOnlyList<RaceStoryTick> LapTicks)
{
    private const double DefaultWidth = 1000;
    private const double DefaultHeight = 280;
    private const double MarginLeft = 44;
    private const double MarginRight = 16;
    private const double MarginTop = 16;
    private const double MarginBottom = 34;
    private const int MaximumTicks = 8;

    /// <summary>
    /// Lays out a race story: the grid position on the left, then the position at the end of each
    /// lap, the first place at the top.
    /// </summary>
    public static RaceStoryChartLayout Create(RaceStory story)
    {
        ArgumentNullException.ThrowIfNull(story);

        var plotRight = DefaultWidth - MarginRight;
        var plotBottom = DefaultHeight - MarginBottom;
        var laps = story.Laps.Where(lap => lap.Position.HasValue).ToArray();
        var positions = laps.Select(lap => lap.Position!.Value)
            .Concat(story.GridPosition is { } grid ? [grid] : Array.Empty<int>())
            .ToArray();
        var best = positions.Min();
        var worst = Math.Max(positions.Max(), best + 1);
        var lastLap = Math.Max(story.Laps[^1].Number, 1);

        double X(double lap) => MarginLeft + ((plotRight - MarginLeft) * lap / lastLap);
        double Y(double position) => MarginTop + ((plotBottom - MarginTop) * (position - best) / (worst - best));
        string Point(double x, double y) => string.Create(CultureInfo.InvariantCulture, $"{x:0.#},{y:0.#}");

        var points = new List<string>(laps.Length + 1);
        if (story.GridPosition is { } start)
        {
            points.Add(Point(X(0), Y(start)));
        }

        points.AddRange(laps.Select(lap => Point(X(lap.Number), Y(lap.Position!.Value))));

        var markers = new List<RaceStoryMarker>();
        var lastPosition = story.GridPosition ?? best;
        foreach (var lap in story.Laps)
        {
            lastPosition = lap.Position ?? lastPosition;
            if (lap.Incidents.Count > 0)
            {
                markers.Add(new RaceStoryMarker(
                    X(lap.Number),
                    Y(lastPosition),
                    GetMarkerClass(lap.Incidents),
                    Describe(lap)));
            }
        }

        return new RaceStoryChartLayout(
            DefaultWidth,
            DefaultHeight,
            MarginLeft,
            plotRight,
            MarginTop,
            plotBottom,
            string.Join(' ', points),
            markers,
            story.Laps.Where(lap => lap.IsPit).Select(lap => X(lap.Number)).ToArray(),
            CreateTicks(best, worst).Select(position => new RaceStoryTick(Y(position), $"P{position}")).ToArray(),
            CreateTicks(1, lastLap).Select(lap => new RaceStoryTick(X(lap), lap.ToString(CultureInfo.InvariantCulture))).ToArray());
    }

    /// <summary>
    /// Gets the class of a marker from the most serious incident of the lap.
    /// </summary>
    private static string GetMarkerClass(IReadOnlyList<DrivingIncident> incidents)
    {
        bool Has(SessionEventKind kind) => incidents.Any(incident => incident.Kind == kind);

        return Has(SessionEventKind.Damage) ? "story-damage"
            : Has(SessionEventKind.VehicleContact) ? "story-contact"
            : Has(SessionEventKind.ObstacleContact) ? "story-obstacle"
            : Has(SessionEventKind.Penalty) ? "story-penalty"
            : "story-limit";
    }

    private static string Describe(RaceStoryLap lap)
    {
        var lines = lap.Incidents.Select(DrivingConductPresentation.Describe).Distinct().ToList();
        if (lap.Loss is { } loss && loss > TimeSpan.Zero)
        {
            lines.Add($"Temps perdu : {loss.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)} s");
        }

        return $"Tour {lap.Number}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }

    private static IEnumerable<int> CreateTicks(int first, int last)
    {
        var step = Math.Max(1, (int)Math.Ceiling((last - first) / (double)MaximumTicks));
        for (var value = first; value <= last; value += step)
        {
            yield return value;
        }
    }
}

/// <summary>
/// An incident on the chart.
/// </summary>
/// <param name="X">The horizontal position.</param>
/// <param name="Y">The vertical position.</param>
/// <param name="CssClass">The class giving its colour.</param>
/// <param name="Title">The text shown on hover.</param>
public sealed record RaceStoryMarker(double X, double Y, string CssClass, string Title);

/// <summary>
/// A label of a chart axis.
/// </summary>
/// <param name="Position">The position along the axis.</param>
/// <param name="Label">The text.</param>
public sealed record RaceStoryTick(double Position, string Label);
