using System.Globalization;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The SVG geometry of the progression chart: scales, axis ticks and the paths of each visible series.
/// </summary>
/// <param name="Width">The width of the SVG view box.</param>
/// <param name="Height">The height of the SVG view box.</param>
/// <param name="PlotLeft">The left edge of the plot area.</param>
/// <param name="PlotRight">The right edge of the plot area.</param>
/// <param name="PlotTop">The top edge of the plot area.</param>
/// <param name="PlotBottom">The bottom edge of the plot area.</param>
/// <param name="Series">The visible series.</param>
/// <param name="XTicks">The date axis ticks.</param>
/// <param name="YTicks">The lap-time axis ticks.</param>
/// <param name="EmptyPeriods">The runs of periods without any visible measure, marked on the date axis.</param>
public sealed record ProgressionChartLayout(
    double Width,
    double Height,
    double PlotLeft,
    double PlotRight,
    double PlotTop,
    double PlotBottom,
    IReadOnlyList<ProgressionSeriesLayout> Series,
    IReadOnlyList<ProgressionAxisTick> XTicks,
    IReadOnlyList<ProgressionTimeTick> YTicks,
    IReadOnlyList<ProgressionEmptyPeriod> EmptyPeriods)
{
    private const double DefaultWidth = 1000;
    private const double DefaultHeight = 360;
    private const double MarginLeft = 72;
    private const double MarginRight = 44;
    private const double MarginTop = 16;
    private const double MarginBottom = 34;
    private const int MaximumXTicks = 10;
    private const int TargetYTicks = 5;

    // Lap-time steps of the time axis, in seconds: a readable step is picked for any lap-time range.
    private static readonly double[] TimeSteps = [0.05, 0.1, 0.2, 0.25, 0.5, 1, 2, 5, 10, 15, 30, 60];

    /// <summary>
    /// Lays out the series that are not hidden.
    /// </summary>
    /// <param name="chart">The chart series.</param>
    /// <param name="hiddenSeries">The keys of the series hidden from the legend.</param>
    public static ProgressionChartLayout Create(ProgressionChart chart, IReadOnlyCollection<string> hiddenSeries)
    {
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentNullException.ThrowIfNull(hiddenSeries);

        var plotLeft = MarginLeft;
        var plotRight = DefaultWidth - MarginRight;
        var plotTop = MarginTop;
        var plotBottom = DefaultHeight - MarginBottom;
        var visible = chart.Series.Where(series => !hiddenSeries.Contains(series.Key)).ToArray();
        if (visible.Length == 0)
        {
            return new ProgressionChartLayout(DefaultWidth, DefaultHeight, plotLeft, plotRight, plotTop, plotBottom, [], [], [], []);
        }

        // With several cars, overlapping min-max bands would hide the lines: bands need a single car.
        var showsBand = chart.Settings.Mode == ProgressionMode.MixedCars || visible.Length == 1;
        var granularity = chart.Settings.Granularity;
        var firstPeriod = visible.Min(series => series.First.PeriodStart);
        var lastPeriod = visible.Max(series => series.Last.PeriodStart);
        double X(DateTime period) =>
            firstPeriod == lastPeriod
                ? (plotLeft + plotRight) / 2
                : plotLeft + (period - firstPeriod).Ticks / (double)(lastPeriod - firstPeriod).Ticks * (plotRight - plotLeft);

        var times = visible
            .SelectMany(series => series.Points
                .SelectMany(point => showsBand ? new[] { point.Min, point.Max, point.Value } : [point.Value])
                .Append(series.Record.Time))
            .Select(time => time.TotalSeconds)
            .ToArray();
        var (lower, upper, step) = GetTimeScale(times.Min(), times.Max());
        double Y(TimeSpan time) =>
            plotBottom - (time.TotalSeconds - lower) / (upper - lower) * (plotBottom - plotTop);

        var seriesLayouts = visible
            .Select(series => CreateSeriesLayout(series, showsBand, X, Y))
            .ToArray();

        var yTicks = new List<ProgressionTimeTick>();
        for (var index = 0; lower + index * step <= upper + step / 1000; index++)
        {
            var time = TimeSpan.FromSeconds(Math.Round(lower + index * step, 3));
            yTicks.Add(new ProgressionTimeTick(Y(time), time, FormatTime(time)));
        }

        var periods = new List<DateTime>();
        for (var period = firstPeriod; period <= lastPeriod; period = ProgressionPeriods.Next(period, granularity))
        {
            periods.Add(period);
        }

        var tickStep = (int)Math.Ceiling(periods.Count / (double)MaximumXTicks);
        var xTicks = periods
            .Where((_, index) => index % tickStep == 0)
            .Select(period => new ProgressionAxisTick(X(period), ProgressionPeriods.AxisLabel(period)))
            .ToArray();

        var measured = visible.SelectMany(series => series.Points).Select(point => point.PeriodStart).ToHashSet();
        var emptyPeriods = new List<ProgressionEmptyPeriod>();
        DateTime? runStart = null;
        DateTime? runEnd = null;
        foreach (var period in periods.Append(DateTime.MaxValue))
        {
            if (period != DateTime.MaxValue && !measured.Contains(period))
            {
                runStart ??= period;
                runEnd = period;
            }
            else if (runStart is { } start && runEnd is { } end)
            {
                emptyPeriods.Add(new ProgressionEmptyPeriod(start, end, X(start), X(end)));
                runStart = null;
                runEnd = null;
            }
        }

        return new ProgressionChartLayout(
            DefaultWidth,
            DefaultHeight,
            plotLeft,
            plotRight,
            plotTop,
            plotBottom,
            seriesLayouts,
            xTicks,
            yTicks,
            emptyPeriods);
    }

    /// <summary>
    /// Formats a lap time as <c>m:ss.fff</c>.
    /// </summary>
    public static string FormatTime(TimeSpan time) => DisplayFormat.Lap(time);

    private static ProgressionSeriesLayout CreateSeriesLayout(
        ProgressionSeries series,
        bool showsBand,
        Func<DateTime, double> x,
        Func<TimeSpan, double> y)
    {
        var points = series.Points
            .Select(point => new ProgressionPointLayout(point, x(point.PeriodStart), y(point.Value)))
            .ToArray();

        // Periods without session do not break the line or the band: they are marked on the date axis instead.
        var linePath = points.Length < 2
            ? string.Empty
            : $"M{string.Join(" L", points.Select(point => $"{Format(point.X)} {Format(point.Y)}"))}";
        IReadOnlyList<string> bands = showsBand ? [CreateBandPath(points, y)] : [];

        return new ProgressionSeriesLayout(series, points, linePath, bands, y(series.Record.Time), showsBand);
    }

    private static string CreateBandPath(IReadOnlyList<ProgressionPointLayout> run, Func<TimeSpan, double> y)
    {
        if (run.Count == 1)
        {
            var point = run[0];
            return $"M{Format(point.X)} {Format(y(point.Point.Max))} V{Format(y(point.Point.Min))}";
        }

        var upperEdge = run.Select(point => $"{Format(point.X)} {Format(y(point.Point.Max))}");
        var lowerEdge = run.Reverse().Select(point => $"{Format(point.X)} {Format(y(point.Point.Min))}");
        return $"M{string.Join(" L", upperEdge.Concat(lowerEdge))} Z";
    }

    private static (double Lower, double Upper, double Step) GetTimeScale(double min, double max)
    {
        var range = Math.Max(max - min, 0.5);
        var step = TimeSteps.FirstOrDefault(candidate => candidate >= range / TargetYTicks, TimeSteps[^1]);
        var lower = Math.Floor((min - range * 0.05) / step) * step;
        var upper = Math.Ceiling((max + range * 0.05) / step) * step;
        return (lower, upper, step);
    }

    private static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

/// <summary>
/// The SVG geometry of one visible series.
/// </summary>
/// <param name="Series">The series.</param>
/// <param name="Points">The position of each period.</param>
/// <param name="LinePath">The solid line through every period, empty with a single period.</param>
/// <param name="BandPaths">The min-max band of the whole series, when drawn; a single period is a vertical line.</param>
/// <param name="RecordY">The height of the record line.</param>
/// <param name="ShowsBand">Whether the min-max band is drawn.</param>
public sealed record ProgressionSeriesLayout(
    ProgressionSeries Series,
    IReadOnlyList<ProgressionPointLayout> Points,
    string LinePath,
    IReadOnlyList<string> BandPaths,
    double RecordY,
    bool ShowsBand);

/// <summary>
/// Consecutive periods without any visible measure, between the first and the last measured period.
/// </summary>
/// <param name="Start">The first empty period.</param>
/// <param name="End">The last empty period, equal to <paramref name="Start"/> for a single period.</param>
/// <param name="X1">The horizontal position of the first empty period.</param>
/// <param name="X2">The horizontal position of the last empty period.</param>
public sealed record ProgressionEmptyPeriod(DateTime Start, DateTime End, double X1, double X2);

/// <summary>
/// The position of one period on the chart.
/// </summary>
/// <param name="Point">The period values.</param>
/// <param name="X">The horizontal position.</param>
/// <param name="Y">The vertical position of the plotted value.</param>
public sealed record ProgressionPointLayout(ProgressionPoint Point, double X, double Y);

/// <summary>
/// A tick of the date axis.
/// </summary>
/// <param name="X">The horizontal position.</param>
/// <param name="Label">The short period label.</param>
public sealed record ProgressionAxisTick(double X, string Label);

/// <summary>
/// A tick of the lap-time axis.
/// </summary>
/// <param name="Y">The vertical position.</param>
/// <param name="Time">The lap time.</param>
/// <param name="Label">The lap time as <c>m:ss.fff</c>.</param>
public sealed record ProgressionTimeTick(double Y, TimeSpan Time, string Label);
