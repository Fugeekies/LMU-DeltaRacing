using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies how domain sessions and driver results are presented by the Blazor dashboard.
/// </summary>
[TestClass]
public sealed class DashboardPresentationTests
{
    /// <summary>
    /// Verifies that sessions are ordered newest first and do not mutate the source collection.
    /// </summary>
    [TestMethod]
    public void OrderSessions_sorts_newest_first_without_changing_the_source()
    {
        var older = CreateSession(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero), []);
        var newer = CreateSession(new DateTimeOffset(2025, 2, 1, 12, 0, 0, TimeSpan.Zero), []);
        var sessions = new[] { older, newer };

        var ordered = DashboardPresentation.OrderSessions(sessions);

        ordered.Should().Equal(newer, older);
        sessions.Should().Equal(older, newer);
    }

    /// <summary>
    /// Verifies that driver results are sorted by finishing position, with unclassified drivers last.
    /// </summary>
    [TestMethod]
    public void GetClassification_sorts_by_position_and_places_missing_positions_last()
    {
        var session = CreateSession(
            DateTimeOffset.UnixEpoch,
            [
                CreateDriver("Unclassified", null),
                CreateDriver("Second", 2),
                CreateDriver("First B", 1),
                CreateDriver("First A", 1)
            ]);

        var classification = DashboardPresentation.GetClassification(session);

        classification.Select(driver => driver.Name)
            .Should().Equal("First A", "First B", "Second", "Unclassified");
    }

    /// <summary>
    /// Verifies that a session with no participants produces an empty classification.
    /// </summary>
    [TestMethod]
    public void GetClassification_returns_empty_results_for_a_session_without_drivers()
    {
        var session = CreateSession(DateTimeOffset.UnixEpoch, []);

        DashboardPresentation.GetClassification(session).Should().BeEmpty();
    }

    private static Session CreateSession(DateTimeOffset startedAt, IEnumerable<Driver> drivers) =>
        new(Guid.NewGuid(), startedAt, "Venue", "Course", "Event", "Race", drivers);

    private static Driver CreateDriver(string name, int? position) =>
        new(name, string.Empty, string.Empty, string.Empty, position, []);
}
