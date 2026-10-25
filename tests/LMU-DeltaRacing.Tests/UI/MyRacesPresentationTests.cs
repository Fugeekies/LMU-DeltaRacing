using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the race-history pairing and delta calculations.
/// </summary>
[TestClass]
public sealed class MyRacesPresentationTests
{
    [TestMethod]
    public void CreateRecords_pairs_each_race_with_the_preceding_qualifying_even_on_the_same_day()
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

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver");

        records.Should().HaveCount(2);
        records[0].StartedAt.Should().Be(DateTimeOffset.Parse("2025-02-22T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        records[0].Category.Should().Be("Hypercar");
        records[0].Car.Should().Be("Toyota");
        records[0].QualifyingTime.Should().Be(TimeSpan.FromSeconds(121.75));
        records[0].QualifyingDelta.Should().Be(TimeSpan.FromSeconds(-0.5));
        records[0].GridPosition.Should().Be(2);
        records[0].RaceTime.Should().Be(TimeSpan.FromSeconds(125.5));
        records[0].RaceDelta.Should().Be(TimeSpan.FromSeconds(1));
        records[0].LapsCompleted.Should().Be(8);
        records[0].FinishTime.Should().Be(TimeSpan.FromSeconds(1326.8575));
        records[1].QualifyingTime.Should().Be(TimeSpan.FromSeconds(122.25));
        records[1].RaceTime.Should().Be(TimeSpan.FromSeconds(124.5));
        records[1].GridPosition.Should().Be(4);
        records[1].QualifyingDelta.Should().BeNull();
        records[1].RaceDelta.Should().BeNull();
    }

    [TestMethod]
    public void CreateRecords_keeps_unpaired_qualifying_and_race_sessions_as_partial_records()
    {
        var sessions = new[]
        {
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-22T10:00:00Z", [126]),
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T11:00:00Z", [123]),
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T12:00:00Z", [122]),
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-22T12:30:00Z", [125])
        };

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver");

        records.Should().HaveCount(3);
        records[0].QualifyingTime.Should().Be(TimeSpan.FromSeconds(122));
        records[0].RaceTime.Should().Be(TimeSpan.FromSeconds(125));
        records[1].QualifyingTime.Should().Be(TimeSpan.FromSeconds(123));
        records[1].RaceTime.Should().BeNull();
        records[2].QualifyingTime.Should().BeNull();
        records[2].RaceTime.Should().Be(TimeSpan.FromSeconds(126));
    }

    [TestMethod]
    public void CreateRecords_does_not_pair_sessions_from_different_circuits()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T11:00:00Z", [122]),
            CreateSession("Race", "Monza", "Hyper", "Toyota", "2025-02-22T11:30:00Z", [105])
        };

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver");

        records.Should().HaveCount(2);
        records.Should().Contain(record => record.TrackVenue == "Spa" && record.RaceTime == null);
        records.Should().Contain(record => record.TrackVenue == "Monza" && record.QualifyingTime == null);
    }

    [TestMethod]
    public void CreateRecords_uses_the_race_category_and_car_when_they_differ_from_qualifying()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "LMP2", "Oreca 07", "2025-02-22T11:00:00Z", [130]),
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-22T11:30:00Z", [125])
        };

        var record = MyRacesPresentation.CreateRecords(sessions, "Driver").Should().ContainSingle().Which;

        record.Category.Should().Be("Hypercar");
        record.Car.Should().Be("Toyota");
    }

    [TestMethod]
    public void CreateRecords_uses_only_selected_driver_results()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T11:00:00Z", [122]),
            CreateSession("Qualify", "Spa", "Hyper", "Other", "2025-02-22T11:00:00Z", [110], driverName: "Opponent"),
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-22T12:00:00Z", [125]),
            CreateSession("Race", "Spa", "Hyper", "Other", "2025-02-22T12:00:00Z", [110], driverName: "Opponent")
        };

        var record = MyRacesPresentation.CreateRecords(sessions, "Driver").Should().ContainSingle().Which;

        record.Car.Should().Be("Toyota");
        record.QualifyingTime.Should().Be(TimeSpan.FromSeconds(122));
        record.RaceTime.Should().Be(TimeSpan.FromSeconds(125));
    }

    [TestMethod]
    public void CreateRecords_maps_every_lmu_class_and_keeps_unknown_classes_under_their_raw_name()
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

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver");

        records.Select(record => record.Category)
            .Should()
            .BeEquivalentTo(["Classic", "LMGT3", "LMGTE", "LMP3", "LMP2", "Hypercar"], options => options.WithStrictOrdering());
    }

    [TestMethod]
    public void CreateRecords_compares_previous_results_on_same_track_and_category_even_when_vehicle_changes()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T11:00:00Z", [122]),
            CreateSession("Qualify", "Le Mans", "Hyper", "Toyota", "2025-02-23T11:00:00Z", [121]),
            CreateSession("Qualify", "Spa", "GT3", "BMW", "2025-02-24T12:00:00Z", [130]),
            CreateSession("Qualify", "Spa", "Hyper", "Ferrari", "2025-02-25T13:00:00Z", [123]),
            CreateSession("Practice", "Spa", "Hyper", "Toyota", "2025-02-24T11:00:00Z", [120])
        };

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver");

        records.Should().HaveCount(4);
        records.Should().ContainSingle(record =>
                record.TrackVenue == "Spa"
                && record.Category == "Hypercar"
                && record.QualifyingTime == TimeSpan.FromSeconds(123))
            .Which.QualifyingDelta.Should().Be(TimeSpan.FromSeconds(1));
        records.Should().Contain(record =>
            record.TrackVenue == "Le Mans" && record.Category == "Hypercar" && record.QualifyingDelta == null);
        records.Should().Contain(record =>
            record.TrackVenue == "Spa" && record.Category == "LMGT3" && record.QualifyingDelta == null);
    }

    /// <summary>
    /// Guards against the former grouping by day, which merged the 87 races of the main fixture
    /// driver into 37 rows and hid every GTE and LMP3 session.
    /// </summary>
    [TestMethod]
    public void CreateRecords_on_real_fixtures_keeps_every_race_and_every_class()
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
        var raceCount = sessions.Count(session =>
            session.SessionType == "Race" && DriverProfile.FindDriver(session, "Driver 2038") is not null);

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver 2038");

        raceCount.Should().Be(87);
        records.Count.Should().BeGreaterThanOrEqualTo(raceCount);
        records.Select(record => record.Category).Should().Contain(["LMGTE", "LMP3", "LMGT3", "LMP2", "Hypercar"]);
    }

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
            DateTimeOffset.Parse(startedAt, System.Globalization.CultureInfo.InvariantCulture),
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
