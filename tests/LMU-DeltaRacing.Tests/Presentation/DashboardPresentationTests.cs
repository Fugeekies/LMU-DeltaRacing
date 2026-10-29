using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

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

    /// <summary>
    /// Verifies that the sessions of one event, which share the event time in LMU files, are
    /// ordered the same way at every run: the race, run last, comes first.
    /// </summary>
    [TestMethod]
    public void OrderSessions_puts_the_race_before_the_qualifying_and_practice_of_its_event()
    {
        var startedAt = new DateTimeOffset(2025, 1, 1, 20, 0, 0, TimeSpan.Zero);
        var practice = CreateSession(startedAt, "Practice1");
        var qualifying = CreateSession(startedAt, "Qualify");
        var race = CreateSession(startedAt, "Race");

        DashboardPresentation.OrderSessions([qualifying, practice, race]).Should().Equal(race, qualifying, practice);
        DashboardPresentation.OrderSessions([race, practice, qualifying]).Should().Equal(race, qualifying, practice);
    }

    private static Session CreateSession(DateTimeOffset startedAt, string sessionType = "Race") =>
        new(Guid.NewGuid(), startedAt, "Venue", "Course", "Event", sessionType, []);
}
