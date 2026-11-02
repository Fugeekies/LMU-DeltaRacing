using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies, on the real result fixtures, that the order of the sessions and the identifier of the
/// events depend neither on the load order nor on the application run.
/// </summary>
[TestClass]
public sealed class SessionChronologyTests
{
    private const string MainDriver = "Driver Tom";

    /// <summary>
    /// Verifies that the rows and deltas of "Mes courses", including those of the two races of the
    /// restart of 16/09/2026, are the same whatever the order in which the files were loaded.
    /// </summary>
    [TestMethod]
    public void CreateRecords_does_not_depend_on_the_load_order()
    {
        var sessions = LoadFixtures();

        var forward = MyRacesPresentation.CreateRecords(sessions, MainDriver);
        var backward = MyRacesPresentation.CreateRecords(sessions.Reverse().ToArray(), MainDriver);

        backward.Should().Equal(forward);
    }

    /// <summary>
    /// Verifies that two sessions starting at the same second are still ordered the same way.
    /// </summary>
    [TestMethod]
    public void CreateRecords_orders_sessions_of_the_same_second_by_identifier()
    {
        var first = CreateRace(Guid.Parse("00000000-0000-0000-0000-000000000001"), 90);
        var second = CreateRace(Guid.Parse("00000000-0000-0000-0000-000000000002"), 91);

        var forward = MyRacesPresentation.CreateRecords([first, second], MainDriver);
        var backward = MyRacesPresentation.CreateRecords([second, first], MainDriver);

        backward.Should().Equal(forward);
        forward.Should().ContainSingle(record => record.RaceDelta == TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that two events starting at the same second keep the same order.
    /// </summary>
    [TestMethod]
    public void CreateEvents_orders_events_of_the_same_second_by_identifier()
    {
        var first = CreateRace(Guid.Parse("00000000-0000-0000-0000-000000000001"), 90);
        var second = CreateRace(Guid.Parse("00000000-0000-0000-0000-000000000002"), 91);

        var forward = RaceEventPresentation.CreateEvents([first, second], MainDriver);
        var backward = RaceEventPresentation.CreateEvents([second, first], MainDriver);

        backward.Select(raceEvent => raceEvent.Id).Should().Equal(forward.Select(raceEvent => raceEvent.Id));
    }

    /// <summary>
    /// Verifies that a link to an event stays valid after a restart, the files being read again.
    /// </summary>
    [TestMethod]
    public void CreateEvents_keeps_the_identifiers_after_a_reload()
    {
        var first = RaceEventPresentation.CreateEvents(LoadFixtures(), MainDriver);
        var second = RaceEventPresentation.CreateEvents(LoadFixtures(), MainDriver);

        second.Select(raceEvent => raceEvent.Id).Should().Equal(first.Select(raceEvent => raceEvent.Id));
    }

    /// <summary>
    /// Verifies that a link to an event stays valid when the option loading practice sessions changes.
    /// </summary>
    [TestMethod]
    public void CreateEvents_keeps_the_identifiers_with_or_without_practice_sessions()
    {
        var sessions = LoadFixtures();

        var withPractice = RaceEventPresentation.CreateEvents(sessions, MainDriver)
            .Where(raceEvent => raceEvent.Qualifying is not null || raceEvent.Race is not null);
        var withoutPractice = RaceEventPresentation.CreateEvents(
            sessions.Where(session => session.Kind != SessionKind.Practice),
            MainDriver);

        withoutPractice.Select(raceEvent => raceEvent.Id).Should().BeEquivalentTo(withPractice.Select(raceEvent => raceEvent.Id));
    }

    /// <summary>
    /// Verifies that the most recent sessions stay ordered when they start at the same second.
    /// </summary>
    [TestMethod]
    public void OrderSessions_orders_sessions_of_the_same_second_by_identifier()
    {
        var first = CreateRace(Guid.Parse("00000000-0000-0000-0000-000000000001"), 90);
        var second = CreateRace(Guid.Parse("00000000-0000-0000-0000-000000000002"), 91);

        DashboardPresentation.OrderSessions([second, first]).Should().Equal(DashboardPresentation.OrderSessions([first, second]));
    }

    private static Session[] LoadFixtures()
    {
        var parser = new XmlResultParser();
        return Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Order(StringComparer.Ordinal)
            .Select(file =>
            {
                using var stream = File.OpenRead(file);
                return parser.Parse(stream);
            })
            .ToArray();
    }

    private static Session CreateRace(Guid id, int bestLapSeconds) => new(
        id,
        DateTimeOffset.FromUnixTimeSeconds(1760000000),
        "Spa",
        "Spa GP",
        "Spa GP",
        "Race",
        [new Driver(MainDriver, "1", "GT3", "Team", 1, [new Lap(1, TimeSpan.FromSeconds(bestLapSeconds), [])])]);
}
