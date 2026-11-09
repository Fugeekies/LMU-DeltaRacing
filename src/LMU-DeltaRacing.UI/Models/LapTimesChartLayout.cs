using System.Globalization;
using LMU_DeltaRacing.Presentation.Localization;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The geometry of the lap times chart, drawn in SVG: the time of each lap of the followed driver,
/// and of the selected driver when they are not the same.
/// </summary>
/// <param name="Width">The width of the SVG view box.</param>
/// <param name="Height">The height of the SVG view box.</param>
/// <param name="PlotLeft">The left edge of the plot.</param>
/// <param name="PlotRight">The right edge of the plot.</param>
/// <param name="PlotTop">The top edge of the plot.</param>
/// <param name="PlotBottom">The bottom edge of the plot.</param>
/// <param name="Series">The series, the followed driver last so that it is drawn on top.</param>
/// <param name="PaceY">The vertical position of the pace of the followed driver, when known.</param>
/// <param name="PitLaps">The horizontal positions of the pit laps of the followed driver.</param>
/// <param name="TimeTicks">The labels of the time axis.</param>
/// <param name="LapTicks">The labels of the lap axis.</param>
public sealed record LapTimesChartLayout(
    double Width,
    double Height,
    double PlotLeft,
    double PlotRight,
    double PlotTop,
    double PlotBottom,
    IReadOnlyList<LapTimesSeries> Series,
    double? PaceY,
    IReadOnlyList<double> PitLaps,
    IReadOnlyList<RaceStoryTick> TimeTicks,
    IReadOnlyList<RaceStoryTick> LapTicks)
{
    /// <summary>
    /// The slowest time of the scale, as a ratio of the pace of the followed driver: slower laps
    /// (start, stops, incidents) are drawn at the top edge, so that the pace laps fill the chart.
    /// </summary>
    public const double ScaleLimit = 1.12;

    private const double DefaultWidth = 1000;
    private const double PlotHeight = 260;
    private const double MarginLeft = 64;
    private const double MarginRight = 16;
    private const double MarginTop = 14;
    private const double MarginBottom = 34;
    private const int TimeTickCount = 5;
    private const int MaximumLapTicks = 10;
    private const double MinimumSectorRange = 0.3;

    /// <summary>
    /// Lays out the lap times, or returns <see langword="null"/> without any timed lap.
    /// </summary>
    /// <param name="focus">The analysis of the followed driver.</param>
    /// <param name="player">The analysis of the selected driver, when they are not the followed one.</param>
    /// <param name="sector">The sector to draw, from 1 to 3, or <see langword="null"/> for the lap times.</param>
    public static LapTimesChartLayout? Create(LapAnalysis focus, LapAnalysis? player, int? sector = null)
    {
        ArgumentNullException.ThrowIfNull(focus);

        LapAnalysis[] analyses = player is null ? [focus] : [player, focus];
        TimeSpan? Measure(LapAnalysisLap lap) => sector is { } number ? lap.SectorTime(number) : lap.Time;
        var times = analyses.SelectMany(analysis => analysis.Laps).Select(Measure).OfType<TimeSpan>().ToArray();
        if (times.Length == 0)
        {
            return null;
        }

        var fastest = times.Min().TotalSeconds;
        var pace = sector is { } paceSector
            ? focus.Sectors.FirstOrDefault(item => item.Number == paceSector)?.Pace?.TotalSeconds
            : focus.Regularity?.Pace.TotalSeconds;
        var minimumRange = sector is null ? 1 : MinimumSectorRange;
        var slowest = Math.Min(times.Max().TotalSeconds, (pace ?? times.Max().TotalSeconds) * ScaleLimit);
        if (slowest - fastest < minimumRange)
        {
            slowest = fastest + minimumRange;
        }

        var lastLap = Math.Max(analyses.Max(analysis => analysis.Laps.Max(lap => lap.Number)), 2);
        var plotRight = DefaultWidth - MarginRight;
        var plotBottom = MarginTop + PlotHeight;

        double X(int lap) => MarginLeft + ((plotRight - MarginLeft) * (lap - 1) / (lastLap - 1));
        // As on the progression chart, lower is faster.
        double Y(double seconds) => MarginTop + (PlotHeight * (slowest - Math.Min(seconds, slowest)) / (slowest - fastest));

        var series = analyses
            .Select(analysis =>
            {
                var isFocus = ReferenceEquals(analysis, focus);
                var segments = new List<string>();
                var segment = new List<string>();
                var points = new List<LapTimesPoint>();
                foreach (var lap in analysis.Laps)
                {
                    if (Measure(lap) is not { } time)
                    {
                        Flush();
                        continue;
                    }

                    var x = X(lap.Number);
                    var y = Y(time.TotalSeconds);
                    segment.Add(string.Create(CultureInfo.InvariantCulture, $"{x:0.#},{y:0.#}"));
                    points.Add(new LapTimesPoint(
                        x,
                        y,
                        (lap.IsPit ? "lap-point-pit" : lap.IsRepresentative ? "lap-point" : "lap-point-slow") + (time.TotalSeconds > slowest ? " lap-point-off" : string.Empty),
                        Describe(analysis.DriverName, lap, time, sector)));
                }

                Flush();
                return new LapTimesSeries(analysis.DriverName, isFocus ? "race-line-focus" : "race-line-player", segments, points);

                void Flush()
                {
                    if (segment.Count > 0)
                    {
                        segments.Add(string.Join(' ', segment));
                        segment.Clear();
                    }
                }
            })
            .ToArray();

        return new LapTimesChartLayout(
            DefaultWidth,
            MarginTop + PlotHeight + MarginBottom,
            MarginLeft,
            plotRight,
            MarginTop,
            plotBottom,
            series,
            pace is { } value ? Y(value) : null,
            focus.Laps.Where(lap => lap.IsPit).Select(lap => X(lap.Number)).ToArray(),
            Enumerable.Range(0, TimeTickCount)
                .Select(index => fastest + ((slowest - fastest) * index / (TimeTickCount - 1)))
                .Select(seconds => new RaceStoryTick(Y(seconds), FormatTime(TimeSpan.FromSeconds(seconds), sector)))
                .ToArray(),
            CreateLapTicks(lastLap).Select(lap => new RaceStoryTick(X(lap), lap.ToString(CultureInfo.InvariantCulture))).ToArray());
    }

    /// <summary>
    /// Formats a time of the chart: a lap as <c>m:ss.fff</c>, a sector in seconds (<c>38.120s</c>).
    /// </summary>
    /// <param name="time">The time.</param>
    /// <param name="sector">The sector drawn, or <see langword="null"/> for lap times.</param>
    public static string FormatTime(TimeSpan time, int? sector) =>
        sector is null
            ? DisplayFormat.Lap(time)
            : string.Create(CultureInfo.InvariantCulture, $"{time.TotalSeconds:0.000}s");

    private static string Describe(string driverName, LapAnalysisLap lap, TimeSpan time, int? sector)
    {
        var description = $"{driverName} · {Texts.Format("RaceDetails.LapNumber", lap.Number)} · {FormatTime(time, sector)}";
        return lap.IsPit
            ? $"{description} · {Texts.Get("LapAnalysis.PitLane")}"
            : lap.IsRepresentative ? description : $"{description} · {Texts.Get("LapAnalysis.SlowLap")}";
    }

    private static IEnumerable<int> CreateLapTicks(int lastLap)
    {
        var step = Math.Max(1, (int)Math.Ceiling((lastLap - 1) / (double)MaximumLapTicks));
        for (var lap = 1; lap <= lastLap; lap += step)
        {
            yield return lap;
        }
    }
}

/// <summary>
/// The laps of one driver on the lap times chart.
/// </summary>
/// <param name="Name">The driver name.</param>
/// <param name="CssClass">The class giving its style: followed driver or selected driver.</param>
/// <param name="Segments">The runs of consecutive timed laps, as SVG point lists.</param>
/// <param name="Points">One point per timed lap.</param>
public sealed record LapTimesSeries(string Name, string CssClass, IReadOnlyList<string> Segments, IReadOnlyList<LapTimesPoint> Points);

/// <summary>
/// One timed lap on the lap times chart.
/// </summary>
/// <param name="X">The horizontal position.</param>
/// <param name="Y">The vertical position, at the top edge for a lap slower than the scale.</param>
/// <param name="CssClass">The class giving its style: representative, slow or pit lap.</param>
/// <param name="Title">The text shown on hover.</param>
public sealed record LapTimesPoint(double X, double Y, string CssClass, string Title);
