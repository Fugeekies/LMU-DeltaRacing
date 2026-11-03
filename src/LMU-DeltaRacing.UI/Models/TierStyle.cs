using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Links a rating tier to its color, defined by the <c>tier-*</c> classes of the stylesheet.
/// </summary>
public static class TierStyle
{
    /// <summary>
    /// Returns the color class of a tier.
    /// </summary>
    public static string CssClass(RatingTier tier) => $"tier-{tier.ToString().ToLowerInvariant()}";
}
