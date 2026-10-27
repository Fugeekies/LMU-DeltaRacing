using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies how driver names are merged in memory after a name change.
/// </summary>
[TestClass]
public sealed class DriverMergesTests
{
    [TestMethod]
    public void Apply_renames_merged_drivers_in_memory_and_keeps_other_sessions_unchanged()
    {
        var renamed = CreateSession("Old Name", "Rival");
        var untouched = CreateSession("Other", "Rival");
        var merges = new[] { new DriverMerge("old name", "New Name") };

        var sessions = DriverMerges.Apply([renamed, untouched], merges);

        sessions[0].Drivers.Select(driver => driver.Name).Should().Equal("New Name", "Rival");
        sessions[0].Id.Should().Be(renamed.Id);
        sessions[0].Drivers[0].Laps.Should().Equal(renamed.Drivers[0].Laps);
        sessions[0].Drivers[0].ClassGridPosition.Should().Be(1);
        sessions[0].Drivers[0].DnfReason.Should().Be("Engine");
        sessions[0].TrackLength.Should().Be(7004);
        sessions[1].Should().BeSameAs(untouched);
        renamed.Drivers[0].Name.Should().Be("Old Name");
    }

    [TestMethod]
    public void Add_resolves_chains_and_replaces_a_reversed_merge()
    {
        IReadOnlyList<DriverMerge> merges = [new DriverMerge("A", "B")];

        merges = DriverMerges.Add(merges, "B", "C");
        merges.Should().BeEquivalentTo([new DriverMerge("A", "C"), new DriverMerge("B", "C")]);

        merges = DriverMerges.Add(merges, "Z", "A");
        merges.Should().ContainEquivalentOf(new DriverMerge("Z", "C"));

        merges = DriverMerges.Add(merges, "C", "B");
        merges.Should().BeEquivalentTo(
            [new DriverMerge("A", "B"), new DriverMerge("Z", "B"), new DriverMerge("C", "B")]);
    }

    [TestMethod]
    public void Add_rejects_merging_a_driver_into_itself()
    {
        var act = () => DriverMerges.Add([], "Same", " same ");

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Remove_cancels_a_merge()
    {
        var merges = DriverMerges.Remove([new DriverMerge("A", "B"), new DriverMerge("C", "B")], "a");

        merges.Should().BeEquivalentTo([new DriverMerge("C", "B")]);
    }

    [TestMethod]
    public void Resolve_returns_the_kept_name_for_a_merged_driver()
    {
        var merges = new[] { new DriverMerge("Old Name", "New Name") };

        DriverMerges.Resolve("old name", merges).Should().Be("New Name");
        DriverMerges.Resolve("Someone", merges).Should().Be("Someone");
        DriverMerges.Resolve(null, merges).Should().BeNull();
    }

    /// <summary>
    /// Two names found in the same session belong to two different people: the merge is suspicious.
    /// </summary>
    [TestMethod]
    public void CountSharedSessions_counts_sessions_where_both_drivers_took_part()
    {
        var sessions = new[]
        {
            CreateSession("Old Name", "New Name"),
            CreateSession("Old Name", "Rival"),
            CreateSession("New Name", "Rival"),
            CreateSession("old name", "NEW NAME")
        };

        DriverMerges.CountSharedSessions(sessions, "Old Name", "New Name").Should().Be(2);
        DriverMerges.CountSharedSessions(sessions, "Old Name", "Nobody").Should().Be(0);
    }

    /// <summary>
    /// The warning must still appear for a merge already saved, and must include the names
    /// already merged into the kept name.
    /// </summary>
    [TestMethod]
    public void CountSharedSessions_with_merges_checks_saved_merges_and_merged_groups()
    {
        var sessions = new[]
        {
            CreateSession("Old Name", "New Name"),
            CreateSession("Older Name", "Third"),
            CreateSession("Old Name", "Rival")
        };
        var merges = new[] { new DriverMerge("Old Name", "New Name"), new DriverMerge("Older Name", "New Name") };

        DriverMerges.CountSharedSessions(sessions, "Old Name", "New Name", merges).Should().Be(1);
        DriverMerges.CountSharedSessions(sessions, "Older Name", "New Name", merges).Should().Be(0);
        DriverMerges.CountSharedSessions(sessions, "Third", "New Name", merges).Should().Be(1);
        DriverMerges.CountSharedSessions(sessions, "Rival", "New Name", merges).Should().Be(1);
    }

    private static Session CreateSession(params string[] driverNames) =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Spa",
            "Spa",
            "Event",
            "Race",
            driverNames.Select((name, index) => new Driver(
                name,
                "7",
                "GT3",
                "Team",
                index + 1,
                [new Lap(1, TimeSpan.FromSeconds(100), [])],
                "Car",
                classPosition: index + 1,
                lapsCompleted: 1,
                finishStatus: "DNF",
                classGridPosition: index + 1,
                dnfReason: "Engine")),
            trackLength: 7004);
}
