using System.Globalization;
using LMU_DeltaRacing.Presentation.Localization;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The geometry of the rating chart of a driver, drawn in SVG: one point per rated race, and the
/// rating to reach to enter each tier.
/// </summary>
/// <param name="Width">The width of the SVG view box.</param>
/// <param name="Height">The height of the SVG view box.</param>
/// <param name="PlotLeft">The left edge of the plot.</param>
/// <param name="PlotRight">The right edge of the plot.</param>
/// <param name="PlotTop">The top edge of the plot.</param>
/// <param name="PlotBottom">The bottom edge of the plot.</param>
/// <param name="Line">The points of the rating line, as an SVG list, from the rating before the first race.</param>
/// <param name="Markers">The rated races.</param>
/// <param name="TierLines">The tier thresholds.</param>
/// <param name="RatingTicks">The labels of the rating axis.</param>
/// <param name="DateTicks">The labels of the race axis.</param>
public sealed record RatingChartLayout(
    double Width,
    double Height,
    double PlotLeft,
    double PlotRight,
    double PlotTop,
    double PlotBottom,
    string Line,
    IReadOnlyList<RatingChartMarker> Markers,
    IReadOnlyList<RatingChartTierLine> TierLines,
    IReadOnlyList<RaceStoryTick> RatingTicks,
    IReadOnlyList<RaceStoryTick> DateTicks)
{
    private const double DefaultWidth = 1000;
    private const double DefaultHeight = 300;
    private const double MarginLeft = 52;
    private const double MarginRight = 92;
    private const double MarginTop = 16;
    private const double MarginBottom = 34;
    private const int MaximumDateTicks = 6;
    private const int MaximumRatingTicks = 6;
    private const double MinimumPadding = 10;
    private static readonly int[] RatingSteps = [5, 10, 20, 25, 50, 100, 200, 250, 500];

    /// <summary>
    /// Lays out the rating of a driver: races are evenly spaced, in chronological order, so that a
    /// busy evening reads as clearly as a quiet month.
    /// </summary>
    public static RatingChartLayout Create(DriverRatingView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        var plotRight = DefaultWidth - MarginRight;
        var plotBottom = DefaultHeight - MarginBottom;
        var races = view.Timeline;
        double[] ratings = [races.Count == 0 ? view.Rating : races[0].Before, .. races.Select(race => race.After)];

        // The tiers widen the scale, so that the chart shows how far the next one is.
        var values = ratings.Concat(view.Tiers.Select(tier => tier.MinimumRating)).ToArray();
        var padding = Math.Max(MinimumPadding, (values.Max() - values.Min()) * 0.1);
        var low = values.Min() - padding;
        var high = values.Max() + padding;
        var steps = Math.Max(1, races.Count);

        double X(int index) => MarginLeft + ((plotRight - MarginLeft) * index / steps);
        double Y(double rating) => MarginTop + ((plotBottom - MarginTop) * (high - rating) / (high - low));
        string Point(double x, double y) => string.Create(CultureInfo.InvariantCulture, $"{x:0.#},{y:0.#}");

        return new RatingChartLayout(
            DefaultWidth,
            DefaultHeight,
            MarginLeft,
            plotRight,
            MarginTop,
            plotBottom,
            string.Join(' ', ratings.Select((rating, index) => Point(X(index), Y(rating)))),
            races.Select((race, index) => new RatingChartMarker(X(index + 1), Y(race.After), GetMarkerClass(race.Delta), Describe(race))).ToArray(),
            view.Tiers
                .Select(tier => new RatingChartTierLine(
                    Y(tier.MinimumRating),
                    $"{DriverRatingPresentation.Label(tier.Tier)} {DisplayFormat.Rating(tier.MinimumRating)}",
                    TierStyle.CssClass(tier.Tier)))
                .ToArray(),
            CreateRatingTicks(low, high).Select(rating => new RaceStoryTick(Y(rating), DisplayFormat.Rating(rating))).ToArray(),
            CreateDateTicks(races.Count)
                .Select(index => new RaceStoryTick(X(index + 1), races[index].StartedAt.ToLocalTime().ToString("dd/MM/yy", CultureInfo.InvariantCulture)))
                .ToArray());
    }

    private static string GetMarkerClass(double delta) =>
        Math.Round(delta, MidpointRounding.AwayFromZero) switch
        {
            > 0 => "rating-gain",
            < 0 => "rating-loss",
            _ => "rating-even"
        };

    private static string Describe(RatingRaceEntry race) =>
        string.Join(
            Environment.NewLine,
            $"{DisplayFormat.DateTime(race.StartedAt)} · {race.TrackVenue}",
            Texts.Format("RatingChart.RaceResult", race.Result, race.FieldSize, DisplayFormat.Average(race.ExpectedRank)),
            $"{DisplayFormat.RatingChange(race.Delta)} → {DisplayFormat.Rating(race.After)}");

    // A round step, so that the labels read 1 500, 1 520, 1 540 rather than 1 503, 1 521...
    private static IEnumerable<double> CreateRatingTicks(double low, double high)
    {
        var step = RatingSteps.FirstOrDefault(step => (high - low) / step <= MaximumRatingTicks, RatingSteps[^1]);
        for (var rating = Math.Ceiling(low / step) * step; rating <= high; rating += step)
        {
            yield return rating;
        }
    }

    private static IEnumerable<int> CreateDateTicks(int races)
    {
        if (races == 0)
        {
            yield break;
        }

        var step = Math.Max(1, (int)Math.Ceiling(races / (double)MaximumDateTicks));
        for (var index = 0; index < races; index += step)
        {
            yield return index;
        }
    }
}

/// <summary>
/// A rated race on the chart.
/// </summary>
/// <param name="X">The horizontal position.</param>
/// <param name="Y">The vertical position, the rating after the race.</param>
/// <param name="CssClass">The class giving its colour: gain, loss or even.</param>
/// <param name="Title">The text shown on hover.</param>
public sealed record RatingChartMarker(double X, double Y, string CssClass, string Title);

/// <summary>
/// The line of a tier threshold.
/// </summary>
/// <param name="Y">The vertical position.</param>
/// <param name="Label">The tier and its rating.</param>
/// <param name="CssClass">The class giving the colour of the tier.</param>
public sealed record RatingChartTierLine(double Y, string Label, string CssClass);
