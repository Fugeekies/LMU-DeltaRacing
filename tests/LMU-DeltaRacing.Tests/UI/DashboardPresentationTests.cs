using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies how domain sessions are presented by the Blazor dashboard.
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
        var older = CreateSession(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var newer = CreateSession(new DateTimeOffset(2025, 2, 1, 12, 0, 0, TimeSpan.Zero));
        var sessions = new[] { older, newer };

        var ordered = DashboardPresentation.OrderSessions(sessions);

        ordered.Should().Equal(newer, older);
        sessions.Should().Equal(older, newer);
    }

    private static Session CreateSession(DateTimeOffset startedAt) =>
        new(Guid.NewGuid(), startedAt, "Venue", "Course", "Event", "Race", []);
}
