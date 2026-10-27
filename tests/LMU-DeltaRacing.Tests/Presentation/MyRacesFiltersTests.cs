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

    private static MyRaceRecord Record(string venue, string category) =>
        new(venue, venue, $"Event {venue}", DateTimeOffset.UnixEpoch, category, "Car",
            null, null, null, null, null, null, null);
}
