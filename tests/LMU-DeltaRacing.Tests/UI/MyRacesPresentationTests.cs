using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the race and qualifying histories and their delta calculations.
/// </summary>
[TestClass]
public sealed class MyRacesPresentationTests
{
    [TestMethod]
    public void CreateSessions_returns_one_row_per_race_and_qualifying_with_deltas_per_session_type()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T11:00:00Z", [122.5, 122.25]),
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-22T11:30:00Z", [125, 124.5], gridPosition: 4),
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T12:00:00Z", [121.75]),
            CreateSession(
                "Race",
                "Spa",
                "Hyper",
                "Toyota",
                "2025-02-22T12:30:00Z",
                [125.5],
                gridPosition: 2,
                finishTime: TimeSpan.FromSeconds(1326.8575),
                lapsCompleted: 8)
        };

        var rows = MyRacesPresentation.CreateSessions(sessions, "Driver");

        var races = rows.Where(row => row.Kind == MyRaceSessionKind.Race).ToArray();
        races.Should().HaveCount(2);
        races[0].StartedAt.Should().Be(Date("2025-02-22T12:30:00Z"));
        races[0].Category.Should().Be("Hypercar");
        races[0].Car.Should().Be("Toyota");
        races[0].BestLap.Should().Be(TimeSpan.FromSeconds(125.5));
        races[0].Delta.Should().Be(TimeSpan.FromSeconds(1));
        races[0].GridPosition.Should().Be(2);
        races[0].LapsCompleted.Should().Be(8);
        races[0].FinishTime.Should().Be(TimeSpan.FromSeconds(1326.8575));
        races[1].BestLap.Should().Be(TimeSpan.FromSeconds(124.5));
        races[1].Delta.Should().BeNull();

        var qualifyings = rows.Where(row => row.Kind == MyRaceSessionKind.Qualifying).ToArray();
        qualifyings.Should().HaveCount(2);
        qualifyings[0].BestLap.Should().Be(TimeSpan.FromSeconds(121.75));
        qualifyings[0].Delta.Should().Be(TimeSpan.FromSeconds(-0.5));
        qualifyings[1].Delta.Should().BeNull();
    }

    [TestMethod]
    public void CreateSessions_uses_only_selected_driver_results_and_ignores_practice()
    {
        var sessions = new[]
        {
            CreateSession("Practice1", "Spa", "Hyper", "Toyota", "2025-02-22T10:00:00Z", [120]),
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T11:00:00Z", [122]),
            CreateSession("Qualify", "Spa", "Hyper", "Other", "2025-02-22T11:00:00Z", [110], driverName: "Opponent"),
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-22T12:00:00Z", [125]),
            CreateSession("Race", "Spa", "Hyper", "Other", "2025-02-22T12:00:00Z", [110], driverName: "Opponent")
        };

        var rows = MyRacesPresentation.CreateSessions(sessions, "Driver");

        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(row => row.Car == "Toyota");
        rows.Single(row => row.Kind == MyRaceSessionKind.Qualifying).BestLap.Should().Be(TimeSpan.FromSeconds(122));
        rows.Single(row => row.Kind == MyRaceSessionKind.Race).BestLap.Should().Be(TimeSpan.FromSeconds(125));
    }

    [TestMethod]
    public void CreateSessions_maps_every_lmu_class_and_keeps_unknown_classes_under_their_raw_name()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "Car", "2025-02-21T11:00:00Z", [120]),
            CreateSession("Qualify", "Spa", "LMP2", "Car", "2025-02-22T11:00:00Z", [121]),
            CreateSession("Qualify", "Spa", "LMP3", "Car", "2025-02-23T11:00:00Z", [122]),
            CreateSession("Qualify", "Spa", "GTE", "Car", "2025-02-24T11:00:00Z", [123]),
            CreateSession("Qualify", "Spa", "GT3", "Car", "2025-02-25T11:00:00Z", [124]),
            CreateSession("Qualify", "Spa", "Classic", "Car", "2025-02-26T11:00:00Z", [125])
        };

        var rows = MyRacesPresentation.CreateSessions(sessions, "Driver");

        rows.Select(row => row.Category)
            .Should()
            .BeEquivalentTo(["Classic", "LMGT3", "LMGTE", "LMP3", "LMP2", "Hypercar"], options => options.WithStrictOrdering());
    }

    [TestMethod]
    public void CreateSessions_compares_previous_results_on_same_track_and_category_even_when_vehicle_changes()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T11:00:00Z", [122]),
            CreateSession("Qualify", "Le Mans", "Hyper", "Toyota", "2025-02-23T11:00:00Z", [121]),
            CreateSession("Qualify", "Spa", "GT3", "BMW", "2025-02-24T12:00:00Z", [130]),
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-24T13:00:00Z", [125]),
            CreateSession("Qualify", "Spa", "Hyper", "Ferrari", "2025-02-25T13:00:00Z", [123])
        };

        var rows = MyRacesPresentation.CreateSessions(sessions, "Driver");

        rows.Should().HaveCount(5);
        rows.Should().ContainSingle(row =>
                row.TrackVenue == "Spa"
                && row.Kind == MyRaceSessionKind.Qualifying
                && row.Category == "Hypercar"
                && row.BestLap == TimeSpan.FromSeconds(123))
            .Which.Delta.Should().Be(TimeSpan.FromSeconds(1));
        rows.Should().Contain(row => row.TrackVenue == "Le Mans" && row.Delta == null);
        rows.Should().Contain(row => row.Category == "LMGT3" && row.Delta == null);
        rows.Should().Contain(row => row.Kind == MyRaceSessionKind.Race && row.Delta == null);
    }

    /// <summary>
    /// Guards against merging races: every race of the main fixture driver must have its own row.
    /// </summary>
    [TestMethod]
    public void CreateSessions_on_real_fixtures_keeps_every_race_and_every_class()
    {
        var parser = new LMU_DeltaRacing.Infrastructure.Parsing.XmlResultParser();
        var sessions = Directory
            .EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return parser.Parse(stream);
            })
            .ToArray();

        var rows = MyRacesPresentation.CreateSessions(sessions, "Driver Tom");

        rows.Count(row => row.Kind == MyRaceSessionKind.Race).Should().Be(87);
        rows.Select(row => row.Category).Should().Contain(["LMGTE", "LMP3", "LMGT3", "LMP2", "Hypercar"]);
    }

    private static DateTimeOffset Date(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    private static Session CreateSession(
        string type,
        string track,
        string carClass,
        string carType,
        string startedAt,
        double[] lapTimes,
        int? gridPosition = null,
        TimeSpan? finishTime = null,
        int? lapsCompleted = null,
        string driverName = "Driver")
    {
        return new Session(
            Guid.NewGuid(),
            Date(startedAt),
            track,
            track,
            $"Event {track}",
            type,
            [
                new Driver(
                    driverName,
                    "7",
                    carClass,
                    "Team",
                    1,
                    lapTimes.Select((time, index) => new Lap(index + 1, TimeSpan.FromSeconds(time), [])),
                    carType,
                    gridPosition,
                    finishTime,
                    lapsCompleted: lapsCompleted)
            ]);
    }
}
