using FluentAssertions;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the circuit and category filters of the "my races" view.
/// </summary>
[TestClass]
public sealed class MyRacesFiltersTests
{
    /// <summary>
    /// Verifies that circuits are offered once, sorted by name.
    /// </summary>
    [TestMethod]
    public void GetTracks_returns_distinct_venues_in_name_order()
    {
        var records = new[] { Record("Spa", "LMGT3"), Record("Monza", "LMP2"), Record("spa", "LMGT3") };

        MyRacesFilters.GetTracks(records).Should().Equal("Monza", "Spa");
    }

    /// <summary>
    /// Verifies that the known categories are always offered and unknown classes are appended.
    /// </summary>
    [TestMethod]
    public void GetCategories_offers_known_categories_then_unknown_classes()
    {
        var records = new[] { Record("Spa", "LMGT3"), Record("Spa", "GT4"), Record("Spa", "Cup") };

        MyRacesFilters.GetCategories(records)
            .Should().Equal("Hypercar", "LMP2", "LMP3", "LMGTE", "LMGT3", "Cup", "GT4");
    }

    /// <summary>
    /// Verifies that an empty filter keeps every record and a set filter keeps matching records.
    /// </summary>
    [TestMethod]
    public void Apply_keeps_records_matching_the_selected_track_and_category()
    {
        var records = new[] { Record("Spa", "LMGT3"), Record("Monza", "LMGT3"), Record("Spa", "LMP2") };

        MyRacesFilters.Apply(records, null, null).Should().HaveCount(3);
        MyRacesFilters.Apply(records, "spa", null).Should().HaveCount(2);
        MyRacesFilters.Apply(records, "Spa", "LMP2").Should().ContainSingle()
            .Which.Category.Should().Be("LMP2");
    }

    /// <summary>
    /// Verifies that the rows of a circuit are grouped by category, in the order of the filter,
    /// then sorted from the most recent to the oldest.
    /// </summary>
    [TestMethod]
    public void OrderRows_groups_categories_in_filter_order_then_sorts_by_date_descending()
    {
        var records = new[]
        {
            Record("Spa", "LMGT3", day: 1),
            Record("Spa", "GT4", day: 2),
            Record("Spa", "Hypercar", day: 3),
            Record("Spa", "LMGT3", day: 4),
            Record("Spa", "LMP2", day: 5),
            Record("Spa", "Hypercar", day: 6),
            Record("Spa", "Cup", day: 7)
        };

        MyRacesFilters.OrderRows(records)
            .Select(record => (record.Category, record.StartedAt.Day))
            .Should().Equal(
                ("Hypercar", 6),
                ("Hypercar", 3),
                ("LMP2", 5),
                ("LMGT3", 4),
                ("LMGT3", 1),
                ("Cup", 7),
                ("GT4", 2));
    }

    private static MyRaceRecord Record(string venue, string category, int day = 1) =>
        new(venue, venue, $"Event {venue}", new DateTimeOffset(2026, 1, day, 20, 0, 0, TimeSpan.Zero), category, "Car",
            null, null, null, null, null, null, null);
}
