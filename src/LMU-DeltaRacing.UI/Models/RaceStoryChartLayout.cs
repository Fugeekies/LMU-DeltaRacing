using System.Globalization;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Localization;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The geometry of the race chart, drawn in SVG: the position of each car lap by lap, the line of
/// the focused driver on top with their incidents and pit stops.
/// </summary>
/// <param name="Width">The width of the SVG view box.</param>
/// <param name="Height">The height of the SVG view box.</param>
/// <param name="PlotLeft">The left edge of the plot.</param>
/// <param name="PlotRight">The right edge of the plot.</param>
/// <param name="PlotTop">The top edge of the plot.</param>
/// <param name="PlotBottom">The bottom edge of the plot.</param>
/// <param name="Lines">The line of each car, the focused driver last so that it is drawn on top.</param>
/// <param name="Markers">The incidents of the focused driver.</param>
/// <param name="PitStops">The horizontal positions of the pit stops of the focused driver.</param>
/// <param name="PositionTicks">The labels of the position axis.</param>
/// <param name="LapTicks">The labels of the lap axis.</param>
public sealed record RaceStoryChartLayout(
    double Width,
    double Height,
    double PlotLeft,
    double PlotRight,
    double PlotTop,
    double PlotBottom,
    IReadOnlyList<RaceChartLine> Lines,
    IReadOnlyList<RaceStoryMarker> Markers,
    IReadOnlyList<double> PitStops,
    IReadOnlyList<RaceStoryTick> PositionTicks,
    IReadOnlyList<RaceStoryTick> LapTicks)
{
    /// <summary>
    /// Up to this number of cars, every line is named on the right; beyond, only the focused and the
    /// selected drivers are, so that the names stay readable.
    /// </summary>
    public const int MaximumLabels = 12;

    private const double DefaultWidth = 1000;
    private const double MarginLeft = 44;
    private const double MarginRight = 170;
    private const double MarginTop = 16;
    private const double MarginBottom = 34;
    private const double MinimumPlotHeight = 230;
    private const double MaximumPlotHeight = 620;
    private const double RowHeight = 16;
    private const double LabelSpacing = 13;
    private const int MaximumTicks = 8;

    /// <summary>
    /// Lays out the race: the grid on the left, then the position at the end of each lap, the first
    /// place at the top. The scale follows the cars shown, so that a close fight fills the chart.
    /// </summary>
    /// <param name="story">The story of the focused driver: incidents and pit stops.</param>
    /// <param name="shown">The lines to draw, the focused driver included.</param>
    public static RaceStoryChartLayout Create(RaceStory story, IReadOnlyList<RacePositionLine> shown)
    {
        ArgumentNullException.ThrowIfNull(story);
        ArgumentNullException.ThrowIfNull(shown);

        var points = shown.SelectMany(line => line.Points).ToArray();
        var best = points.Min(point => point.Position);
        var worst = Math.Max(points.Max(point => point.Position), best + 1);
        var lastLap = Math.Max(points.Max(point => point.Lap), 1);
        var plotHeight = Math.Clamp((worst - best + 1) * RowHeight, MinimumPlotHeight, MaximumPlotHeight);
        var height = MarginTop + plotHeight + MarginBottom;
        var plotRight = DefaultWidth - MarginRight;
        var plotBottom = MarginTop + plotHeight;

        double X(double lap) => MarginLeft + ((plotRight - MarginLeft) * lap / lastLap);
        double Y(double position) => MarginTop + ((plotBottom - MarginTop) * (position - best) / (worst - best));
        string Point(RacePositionPoint point) => string.Create(CultureInfo.InvariantCulture, $"{X(point.Lap):0.#},{Y(point.Position):0.#}");

        var labelled = shown.Count <= MaximumLabels
            ? shown.ToHashSet()
            : shown.Where(line => line.IsFocus || line.IsPlayer).ToHashSet();
        var labelPositions = SpreadLabels(labelled.ToDictionary(line => line, line => Y(line.Points[^1].Position)), MarginTop, plotBottom);
        var lines = shown
            .OrderBy(line => line.IsFocus ? 2 : line.IsPlayer ? 1 : 0)
            .Select(line => new RaceChartLine(
                line.Name,
                string.Join(' ', line.Points.Select(Point)),
                line.IsFocus ? "race-line-focus" : line.IsPlayer ? "race-line-player" : "race-line-other",
                Describe(line),
                labelPositions.TryGetValue(line, out var labelY) ? labelY : null,
                X(line.Points[^1].Lap),
                Y(line.Points[^1].Position),
                line.Retired))
            .ToArray();

        var focus = shown.FirstOrDefault(line => line.IsFocus);
        var markers = new List<RaceStoryMarker>();
        foreach (var lap in story.Laps.Where(lap => lap.Incidents.Count > 0))
        {
            if (focus?.Points.LastOrDefault(point => point.Lap <= lap.Number) is { } position)
            {
                markers.Add(new RaceStoryMarker(X(lap.Number), Y(position.Position), GetMarkerClass(lap.Incidents), Describe(lap)));
            }
        }

        return new RaceStoryChartLayout(
            DefaultWidth,
            height,
            MarginLeft,
            plotRight,
            MarginTop,
            plotBottom,
            lines,
            markers,
            story.Laps.Where(lap => lap.IsPit).Select(lap => X(lap.Number)).ToArray(),
            CreateTicks(best, worst).Select(position => new RaceStoryTick(Y(position), $"P{position}")).ToArray(),
            CreateTicks(1, lastLap).Select(lap => new RaceStoryTick(X(lap), lap.ToString(CultureInfo.InvariantCulture))).ToArray());
    }

    /// <summary>
    /// Places the names next to the end of their line, pushed apart so that they never overlap.
    /// </summary>
    private static Dictionary<RacePositionLine, double> SpreadLabels(
        IReadOnlyDictionary<RacePositionLine, double> ends,
        double top,
        double bottom)
    {
        var placed = new Dictionary<RacePositionLine, double>();
        var previous = double.NegativeInfinity;
        foreach (var (line, y) in ends.OrderBy(end => end.Value))
        {
            previous = Math.Max(y + 4, previous + LabelSpacing);
            placed[line] = previous;
        }

        // Labels pushed below the plot are moved back up together, keeping their spacing.
        var overflow = previous - (bottom + 4);
        if (overflow > 0)
        {
            foreach (var line in placed.Keys.ToArray())
            {
                placed[line] = Math.Max(top, placed[line] - overflow);
            }
        }

        return placed;
    }

    private static string Describe(RacePositionLine line)
    {
        var end = line.Points[^1];
        return line.Retired
            ? Texts.Format("RaceStory.RetiredLine", end.Position, line.Name, end.Lap)
            : $"P{end.Position} {line.Name}";
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
            lines.Add(Texts.Format("RaceStory.TimeLost", loss.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)));
        }

        return $"{Texts.Format("RaceDetails.LapNumber", lap.Number)}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
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
/// The line of one car on the chart.
/// </summary>
/// <param name="Name">The driver name.</param>
/// <param name="Points">The points of the line, as an SVG list.</param>
/// <param name="CssClass">The class giving its style: focused driver, selected driver or other car.</param>
/// <param name="Title">The text shown on hover and next to the line: final position and name.</param>
/// <param name="LabelY">The vertical position of the name on the right, or <see langword="null"/> when the line is not named.</param>
/// <param name="EndX">The horizontal position of the last point.</param>
/// <param name="EndY">The vertical position of the last point.</param>
/// <param name="Retired">Whether the car retired: its last point is a cross.</param>
public sealed record RaceChartLine(
    string Name,
    string Points,
    string CssClass,
    string Title,
    double? LabelY,
    double EndX,
    double EndY,
    bool Retired);

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
