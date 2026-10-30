using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Links a category to its official color, defined by the <c>category-*</c> classes of the stylesheet.
/// </summary>
public static class CategoryStyle
{
    /// <summary>
    /// Returns the color class of a category; unknown classes stay neutral.
    /// </summary>
    public static string CssClass(string category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return CarCategory.Known.Contains(category) ? $"category-{category.ToLowerInvariant()}" : "category-other";
    }
}
