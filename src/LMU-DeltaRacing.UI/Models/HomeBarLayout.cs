using System.Globalization;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// One part of a stacked bar of the home page.
/// </summary>
/// <param name="Label">The category or the retirement cause.</param>
/// <param name="Share">The share of the total, from 0 to 1.</param>
/// <param name="Value">The value, already formatted.</param>
public sealed record HomeBarPart(string Label, double Share, string Value);

/// <summary>
/// A part of a stacked bar, ready to be drawn.
/// </summary>
/// <param name="Label">The category or the retirement cause.</param>
/// <param name="Width">The width of the segment, as a CSS percentage of the bar.</param>
/// <param name="Center">The center of the segment, as a CSS percentage of the bar.</param>
/// <param name="Tooltip">The text of the hover popup.</param>
public sealed record HomeBarSegment(string Label, string Width, string Center, string Tooltip);

/// <summary>
/// Lays out the stacked bars of the home page and their hover popups.
/// </summary>
public static class HomeBarLayout
{
    /// <summary>
    /// Returns the segments of a bar, in the order of its parts.
    /// </summary>
    public static IReadOnlyList<HomeBarSegment> Create(IEnumerable<HomeBarPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var start = 0d;
        var segments = new List<HomeBarSegment>();
        foreach (var part in parts)
        {
            segments.Add(new HomeBarSegment(
                part.Label,
                FormatPercent(part.Share),
                FormatPercent(start + (part.Share / 2)),
                $"{part.Label} : {part.Value} ({DisplayFormat.Percent(part.Share)})"));
            start += part.Share;
        }

        return segments;
    }

    private static string FormatPercent(double share) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(share * 100, 3)}%");
}
