namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Builds the links that open the progression page on a circuit and a category.
/// </summary>
public static class ProgressionLink
{
    /// <summary>
    /// Returns the address of the progression page, preselected on a circuit and a category.
    /// </summary>
    public static string For(string trackVenue, string trackCourse, string category)
    {
        ArgumentNullException.ThrowIfNull(trackVenue);
        ArgumentNullException.ThrowIfNull(trackCourse);
        ArgumentNullException.ThrowIfNull(category);

        return $"/progression?venue={Uri.EscapeDataString(trackVenue)}"
            + $"&course={Uri.EscapeDataString(trackCourse)}"
            + $"&category={Uri.EscapeDataString(category)}";
    }
}
