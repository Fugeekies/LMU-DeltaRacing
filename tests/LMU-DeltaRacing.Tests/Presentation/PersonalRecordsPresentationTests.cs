using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the personal records of a circuit and the comparison of the sessions driven there.
/// </summary>
[TestClass]
public sealed class PersonalRecordsPresentationTests
{
    private const string Player = "Player";
    private static readonly string Category = CarCategory.FromCarClass("GT3");

    /// <summary>
    /// Verifies the record of each car: best lap and top speed, with the session that set them.
    /// </summary>
    [TestMethod]
    public void Create_keeps_the_best_lap_and_top_speed_of_each_car()
    {
        var records = PersonalRecordsPresentation.Create(CreateHistory(), Player, "Track", "Track", Category);

        records.Cars.Select(car => car.Car).Should().Equal("Car B", "Car A");
        var carA = records.Cars.Single(car => car.Car == "Car A");
        carA.Sessions.Should().Be(3);
        carA.BestLap!.Time.Should().Be(TimeSpan.FromSeconds(99));
        carA.BestLap.Kind.Should().Be(SessionKind.Race);
        carA.BestLap.StartedAt.Should().Be(Day(2));
        carA.TopSpeed!.Speed.Should().Be(255);
        carA.TopSpeed.StartedAt.Should().Be(Day(3));
    }

    /// <summary>
    /// Verifies the comparison: most recent first, gap to the record of today, and a mark on the
    /// sessions that beat the record of the earlier sessions with the same car.
    /// </summary>
    [TestMethod]
    public void Create_marks_the_sessions_that_break_a_record()
    {
        var sessions = PersonalRecordsPresentation.Create(CreateHistory(), Player, "Track", "Track", Category).Sessions;

        sessions.Select(row => row.StartedAt).Should().Equal(Day(4), Day(3), Day(2), Day(1));
        sessions.Select(row => row.BrokeLapRecord).Should().Equal(false, false, true, false);
        sessions.Select(row => row.BrokeSpeedRecord).Should().Equal(false, true, false, false);
        sessions.Select(row => row.GapToRecord).Should().Equal(
            TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.Zero, TimeSpan.FromSeconds(1));

        // Pace, regularity and pit stops are race measures.
        sessions[^1].Kind.Should().Be(SessionKind.Qualifying);
        sessions[^1].Pace.Should().BeNull();
        sessions[^1].PitStops.Should().BeNull();
        sessions[1].Pace.Should().Be(TimeSpan.FromSeconds(103));
        sessions[1].PitStops.Should().Be(1);
    }

    /// <summary>
    /// Verifies that other circuits, other categories and practice sessions are left out.
    /// </summary>
    [TestMethod]
    public void Create_keeps_the_circuit_and_category_only()
    {
        Session[] history =
        [
            .. CreateHistory(),
            CreateSession(Day(5), "Race", "Car A", 90, "Other track"),
            CreateSession(Day(6), "Race", "Car A", 90, carClass: "LMP2"),
            CreateSession(Day(7), "Practice1", "Car A", 90)
        ];

        var records = PersonalRecordsPresentation.Create(history, Player, "Track", "Track", Category);

        records.Sessions.Should().HaveCount(4);
        records.Cars.Single(car => car.Car == "Car A").BestLap!.Time.Should().Be(TimeSpan.FromSeconds(99));
    }

    private static Session[] CreateHistory() =>
    [
        CreateSession(Day(1), "Qualify", "Car A", 100, topSpeed: 250),
        CreateSession(Day(2), "Race", "Car A", 99, topSpeed: 248),
        CreateSession(Day(3), "Race", "Car A", 101, topSpeed: 255, pitStops: 1),
        CreateSession(Day(4), "Race", "Car B", 98, topSpeed: 240)
    ];

    private static DateTimeOffset Day(int day) => new(2026, 10, day, 20, 0, 0, TimeSpan.Zero);

    private static Session CreateSession(
        DateTimeOffset startedAt,
        string type,
        string car,
        double bestLap,
        string track = "Track",
        string carClass = "GT3",
        double topSpeed = 250,
        int? pitStops = null)
    {
        var telemetry = new LapTelemetry(TopSpeed: topSpeed);
        Lap[] laps =
        [
            new(1, TimeSpan.FromSeconds(bestLap + 10), [], telemetry: telemetry),
            new(2, TimeSpan.FromSeconds(bestLap), [], telemetry: telemetry),
            new(3, TimeSpan.FromSeconds(bestLap + 2), [], telemetry: telemetry),
            new(4, TimeSpan.FromSeconds(bestLap + 4), [], telemetry: telemetry)
        ];
        return new Session(
            Guid.NewGuid(),
            startedAt,
            track,
            track,
            "Event",
            type,
            [new Driver(Player, "1", carClass, "Team", 1, laps, carType: car, classPosition: 1, pitStops: pitStops)]);
    }
}
