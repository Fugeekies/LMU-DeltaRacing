using System.Globalization;
using LMU_DeltaRacing.Presentation.Localization;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// One line of a breakdown of the home page.
/// </summary>
/// <param name="Label">The category or the retirement cause.</param>
/// <param name="Share">The share of the total, from 0 to 1.</param>
/// <param name="Value">The value, already formatted.</param>
public sealed record HomeBarPart(string Label, double Share, string Value);

/// <summary>
/// A line of a breakdown, ready to be drawn.
/// </summary>
/// <param name="Label">The category or the retirement cause.</param>
/// <param name="Width">The width of its gauge, as a CSS percentage of the track.</param>
/// <param name="Tooltip">The text shown on hover: label, value and share.</param>
public sealed record HomeBarSegment(string Label, string Width, string Tooltip);

/// <summary>
/// Lays out the breakdowns of the home page: the gauge and hover text of each line.
/// </summary>
public static class HomeBarLayout
{
    /// <summary>
    /// Returns the lines of a breakdown, in the order of its parts.
    /// </summary>
    public static IReadOnlyList<HomeBarSegment> Create(IEnumerable<HomeBarPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var segments = new List<HomeBarSegment>();
        foreach (var part in parts)
        {
            segments.Add(new HomeBarSegment(
                part.Label,
                FormatPercent(part.Share),
                Texts.Format("Home.BarTooltip", part.Label, part.Value, DisplayFormat.Percent(part.Share))));
        }

        return segments;
    }

    private static string FormatPercent(double share) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(share * 100, 3)}%");
}
