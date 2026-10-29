namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Maps LMU vehicle classes to the WEC categories displayed in the dashboard.
/// </summary>
public static class CarCategory
{
    /// <summary>
    /// Gets the known categories in display order, from the fastest class to the slowest.
    /// </summary>
    public static IReadOnlyList<string> Known { get; } = ["Hypercar", "LMP2", "LMP3", "LMGTE", "LMGT3"];

    /// <summary>
    /// Returns the display rank of a category: its index in <see cref="Known"/>, unknown classes last.
    /// </summary>
    public static int Order(string category)
    {
        for (var index = 0; index < Known.Count; index++)
        {
            if (string.Equals(Known[index], category, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return Known.Count;
    }

    /// <summary>
    /// Returns the category of an LMU vehicle class.
    /// Unknown classes keep their raw name so that their results stay visible instead of being dropped.
    /// </summary>
    /// <param name="carClass">The <c>CarClass</c> value from the result file.</param>
    /// <returns>The category name, the trimmed raw class, or "—" when the class is empty.</returns>
    public static string FromCarClass(string? carClass)
    {
        var value = carClass?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return "—";
        }

        // LMU writes short class names ("Hyper", "LMP2", "GTE", "GT3"); full names are accepted too.
        if (value.Contains("Hyper", StringComparison.OrdinalIgnoreCase))
        {
            return "Hypercar";
        }

        if (value.Contains("LMP2", StringComparison.OrdinalIgnoreCase)
            || value.Equals("P2", StringComparison.OrdinalIgnoreCase))
        {
            return "LMP2";
        }

        if (value.Contains("LMP3", StringComparison.OrdinalIgnoreCase)
            || value.Equals("P3", StringComparison.OrdinalIgnoreCase))
        {
            return "LMP3";
        }

        if (value.Contains("GTE", StringComparison.OrdinalIgnoreCase))
        {
            return "LMGTE";
        }

        if (value.Contains("GT3", StringComparison.OrdinalIgnoreCase))
        {
            return "LMGT3";
        }

        return value;
    }
}
