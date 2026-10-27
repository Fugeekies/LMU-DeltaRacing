namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Provides the circuit and category filters of the "my races" view.
/// </summary>
public static class MyRacesFilters
{
    /// <summary>
    /// Returns the circuit venues found in the records, once each, sorted by name.
    /// </summary>
    public static IReadOnlyList<string> GetTracks(IEnumerable<MyRaceRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        return records
            .Select(record => record.TrackVenue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns the known WEC categories, always offered, followed by the unknown classes found in
    /// the records.
    /// </summary>
    public static IReadOnlyList<string> GetCategories(IEnumerable<MyRaceRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        return
        [
            .. CarCategory.Known,
            .. records
                .Select(record => record.Category)
                .Where(category => !CarCategory.Known.Contains(category))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.OrdinalIgnoreCase)
        ];
    }

    /// <summary>
    /// Keeps the records of the selected venue and category; an empty selection keeps them all.
    /// </summary>
    public static IReadOnlyList<MyRaceRecord> Apply(
        IEnumerable<MyRaceRecord> records,
        string? trackVenue,
        string? category)
    {
        ArgumentNullException.ThrowIfNull(records);
        return records
            .Where(record => string.IsNullOrEmpty(trackVenue)
                || string.Equals(record.TrackVenue, trackVenue, StringComparison.OrdinalIgnoreCase))
            .Where(record => string.IsNullOrEmpty(category)
                || string.Equals(record.Category, category, StringComparison.Ordinal))
            .ToArray();
    }
}
