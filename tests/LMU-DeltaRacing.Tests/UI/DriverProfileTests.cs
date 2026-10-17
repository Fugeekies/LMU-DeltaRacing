using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies driver frequency calculations and result filtering.
/// </summary>
[TestClass]
public sealed class DriverProfileTests
{
    [TestMethod]
    public void CalculateFrequencies_counts_a_driver_once_per_result_and_sorts_by_frequency()
    {
        var sessions = new[]
        {
            CreateSession("Driver A", "Driver B", "driver b"),
            CreateSession("Driver A", "Driver B", "Driver C"),
            CreateSession("Driver C"),
            CreateSession("Driver B")
        };

        var frequencies = DriverProfile.CalculateFrequencies(sessions);

        frequencies.Select(driver => driver.Name).Should().Equal("Driver B", "Driver A", "Driver C");
        frequencies[0].AppearanceCount.Should().Be(3);
        frequencies[0].TotalResults.Should().Be(4);
        frequencies[0].Percentage.Should().BeApproximately(3d / 4, 0.0001);
        frequencies[1].AppearanceCount.Should().Be(2);
        frequencies[2].AppearanceCount.Should().Be(2);
    }

    [TestMethod]
    public void FilterSessions_keeps_only_results_containing_the_selected_driver()
    {
        var matching = CreateSession("Driver A");
        var nonMatching = CreateSession("Driver B");

        DriverProfile.FilterSessions([matching, nonMatching], "driver a")
            .Should().ContainSingle().Which.Should().Be(matching);
        DriverProfile.FilterSessions([matching, nonMatching], null).Should().BeEmpty();
        DriverProfile.FindDriver(nonMatching, "Driver A").Should().BeNull();
    }

    private static Session CreateSession(params string[] names) =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Venue",
            "Course",
            "Event",
            "Race",
            names.Select(name => new Driver(name, "", "", "", null, [])));
}
