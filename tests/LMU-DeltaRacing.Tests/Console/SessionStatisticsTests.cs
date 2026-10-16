using FluentAssertions;
using LMU_DeltaRacing.ConsoleApp;
using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Tests.Console;

/// <summary>
/// Verifies circuit and vehicle statistics calculated from imported sessions.
/// </summary>
[TestClass]
public sealed class SessionStatisticsTests
{
    /// <summary>
    /// Verifies that circuit statistics compare the fastest valid lap with the previous dated session
    /// on the same venue and layout, regardless of input order.
    /// </summary>
    [TestMethod]
    public void ByCircuit_orders_sessions_and_calculates_deltas_per_venue_and_layout()
    {
        var sessions = new[]
        {
            CreateSession(
                new DateTimeOffset(2025, 2, 1, 12, 0, 0, TimeSpan.Zero),
                "Spa",
                "GP",
                "Race",
                DriverWithLaps("Driver A", "Hypercar", "Team A", 100, 99)),
            CreateSession(
                new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
                "Spa",
                "GP",
                "Qualifying",
                DriverWithLaps("Driver A", "Hypercar", "Team A", 102, 101)),
            CreateSession(
                new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero),
                "Spa",
                "Short",
                "Race",
                DriverWithLaps("Driver A", "Hypercar", "Team A", 90))
        };

        var rows = SessionStatistics.ByCircuit(sessions);

        rows.Should().HaveCount(3);
        rows.Where(row => row.TrackCourse == "GP")
            .Should().SatisfyRespectively(
                first =>
                {
                    first.StartedAt.Should().Be(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
                    first.BestLap.Should().Be(TimeSpan.FromSeconds(101));
                    first.Delta.Should().BeNull();
                },
                second =>
                {
                    second.StartedAt.Should().Be(new DateTimeOffset(2025, 2, 1, 12, 0, 0, TimeSpan.Zero));
                    second.BestLap.Should().Be(TimeSpan.FromSeconds(99));
                    second.Delta.Should().Be(TimeSpan.FromSeconds(-2));
                });
    }

    /// <summary>
    /// Verifies that vehicle statistics are grouped by class and team and compare each group over time.
    /// </summary>
    [TestMethod]
    public void ByCircuitAndCar_groups_by_class_and_team_and_calculates_each_delta_independently()
    {
        var sessions = new[]
        {
            CreateSession(
                new DateTimeOffset(2025, 2, 1, 12, 0, 0, TimeSpan.Zero),
                "Le Mans",
                "Circuit",
                "Race",
                DriverWithLaps("Driver A", "Hypercar", "Team A", 201),
                DriverWithLaps("Driver B", "LMP2", "Team B", 220)),
            CreateSession(
                new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
                "Le Mans",
                "Circuit",
                "Practice",
                DriverWithLaps("Driver A", "Hypercar", "Team A", 205),
                DriverWithLaps("Driver B", "LMP2", "Team B", 222))
        };

        var rows = SessionStatistics.ByCircuitAndCar(sessions);

        rows.Should().HaveCount(4);
        rows.Where(row => row.CarClass == "Hypercar" && row.TeamName == "Team A")
            .Should().SatisfyRespectively(
                first =>
                {
                    first.BestLap.Should().Be(TimeSpan.FromSeconds(205));
                    first.Delta.Should().BeNull();
                },
                second =>
                {
                    second.BestLap.Should().Be(TimeSpan.FromSeconds(201));
                    second.Delta.Should().Be(TimeSpan.FromSeconds(-4));
                });
        rows.Where(row => row.CarClass == "LMP2" && row.TeamName == "Team B")
            .Should().SatisfyRespectively(
                first => first.Delta.Should().BeNull(),
                second => second.Delta.Should().Be(TimeSpan.FromSeconds(-2)));
    }

    /// <summary>
    /// Verifies that sessions without valid lap times remain visible and do not prevent
    /// the next measured session from comparing against the previous available measurement.
    /// </summary>
    [TestMethod]
    public void ByCircuit_keeps_sessions_without_valid_laps_without_resetting_the_comparison()
    {
        var sessions = new[]
        {
            CreateSession(
                new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.Zero),
                "Monza",
                "GP",
                "Race",
                DriverWithLaps("Driver A", "GT3", "Team A", 98)),
            CreateSession(
                new DateTimeOffset(2025, 2, 1, 12, 0, 0, TimeSpan.Zero),
                "Monza",
                "GP",
                "Race",
                DriverWithLaps("Driver A", "GT3", "Team A", (double?)null)),
            CreateSession(
                new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
                "Monza",
                "GP",
                "Race",
                DriverWithLaps("Driver A", "GT3", "Team A", 100))
        };

        var rows = SessionStatistics.ByCircuit(sessions);

        rows.Should().SatisfyRespectively(
            first => first.Delta.Should().BeNull(),
            second =>
            {
                second.BestLap.Should().BeNull();
                second.Delta.Should().BeNull();
            },
            third => third.Delta.Should().Be(TimeSpan.FromSeconds(-2)));
    }

    private static Session CreateSession(
        DateTimeOffset startedAt,
        string venue,
        string course,
        string sessionType,
        params Driver[] drivers) =>
        new(Guid.NewGuid(), startedAt, venue, course, course, sessionType, drivers);

    private static Driver DriverWithLaps(string name, string carClass, string teamName, params double?[] times) =>
        new(
            name,
            string.Empty,
            carClass,
            teamName,
            null,
            times.Select((time, index) => new Lap(
                index + 1,
                time is null ? null : TimeSpan.FromSeconds(time.Value),
                [])));
}
