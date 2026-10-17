using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the race-history grouping and delta calculations.
/// </summary>
[TestClass]
public sealed class MyRacesPresentationTests
{
    [TestMethod]
    public void CreateRecords_pairs_qualifying_and_race_and_compares_each_to_previous_matching_event()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "7", "Toyota", "2025-02-22T11:00:00Z", [122.5, 122.25]),
            CreateSession("Race", "Spa", "Hyper", "7", "Toyota", "2025-02-22T12:00:00Z", [125, 124.5], gridPosition: 4),
            CreateSession("Qualify", "Spa", "Hyper", "7", "Toyota", "2025-02-23T11:00:00Z", [121.75]),
            CreateSession(
                "Race",
                "Spa",
                "Hyper",
                "7",
                "Toyota",
                "2025-02-23T12:00:00Z",
                [125.5],
                gridPosition: 2,
                finishTime: TimeSpan.FromSeconds(1326.8575),
                mostLapsCompleted: 8)
        };

        var records = MyRacesPresentation.CreateRecords(sessions);

        records.Should().HaveCount(2);
        records[0].Category.Should().Be("Hypercar");
        records[0].Car.Should().Be("Test Car");
        records[0].QualifyingTime.Should().Be(TimeSpan.FromSeconds(121.75));
        records[0].QualifyingDelta.Should().Be(TimeSpan.FromSeconds(-0.5));
        records[0].GridPosition.Should().Be(2);
        records[0].RaceTime.Should().Be(TimeSpan.FromSeconds(125.5));
        records[0].RaceDelta.Should().Be(TimeSpan.FromSeconds(1));
        records[0].MostLapsCompleted.Should().Be(8);
        records[0].FinishTime.Should().Be(TimeSpan.FromSeconds(1326.8575));
        records[1].QualifyingDelta.Should().BeNull();
        records[1].RaceDelta.Should().BeNull();
    }

    [TestMethod]
    public void CreateRecords_uses_only_player_results_and_returns_one_record_per_race()
    {
        var firstQualifying = CreateSession(
            "Qualify",
            "Spa",
            "Hyper",
            "7",
            "Toyota",
            "2025-02-22T11:00:00Z",
            [122]);
        var secondQualifying = CreateSession(
            "Qualify",
            "Spa",
            "Hyper",
            "8",
            "Other team",
            "2025-02-22T11:00:00Z",
            [110],
            isPlayer: false);
        var firstRace = CreateSession(
            "Race",
            "Spa",
            "Hyper",
            "7",
            "Toyota",
            "2025-02-22T12:00:00Z",
            [125]);
        var secondRace = CreateSession(
            "Race",
            "Spa",
            "Hyper",
            "8",
            "Other team",
            "2025-02-22T12:00:00Z",
            [110],
            isPlayer: false);

        var records = MyRacesPresentation.CreateRecords(
            [firstQualifying, secondQualifying, firstRace, secondRace]);

        records.Should().ContainSingle();
        records[0].Car.Should().Be("Test Car");
        records[0].QualifyingTime.Should().Be(TimeSpan.FromSeconds(122));
        records[0].RaceTime.Should().Be(TimeSpan.FromSeconds(125));
    }

    [TestMethod]
    public void CreateRecords_compares_previous_results_on_same_track_and_category_even_when_vehicle_changes()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "7", "Toyota", "2025-02-22T11:00:00Z", [122]),
            CreateSession("Qualify", "Le Mans", "Hyper", "7", "Toyota", "2025-02-23T11:00:00Z", [121]),
            CreateSession("Qualify", "Spa", "GT3", "7", "Toyota", "2025-02-24T12:00:00Z", [130]),
            CreateSession("Qualify", "Spa", "Hyper", "8", "Toyota", "2025-02-25T13:00:00Z", [123]),
            CreateSession("Practice", "Spa", "Hyper", "7", "Toyota", "2025-02-24T11:00:00Z", [120]),
            CreateSession("Qualify", "Spa", "Unknown", "7", "Toyota", "2025-02-24T12:00:00Z", [120])
        };

        var records = MyRacesPresentation.CreateRecords(sessions);

        records.Should().HaveCount(4);
        var spaHypercar = records.Should()
            .ContainSingle(record =>
                record.TrackVenue == "Spa"
                && record.Category == "Hypercar"
                && record.QualifyingTime == TimeSpan.FromSeconds(123))
            .Which;
        spaHypercar.QualifyingDelta.Should().Be(TimeSpan.FromSeconds(1));

        records.Should().Contain(record =>
            record.TrackVenue == "Le Mans" && record.Category == "Hypercar" && record.QualifyingDelta == null);
        records.Should().Contain(record =>
            record.TrackVenue == "Spa" && record.Category == "LMGT3" && record.QualifyingDelta == null);
    }

    private static Session CreateSession(
        string type,
        string track,
        string carClass,
        string carNumber,
        string team,
        string startedAt,
        double[] lapTimes,
        bool isPlayer = true,
        int? gridPosition = null,
        TimeSpan? finishTime = null,
        int? mostLapsCompleted = null)
    {
        return new Session(
            Guid.NewGuid(),
            DateTimeOffset.Parse(startedAt, System.Globalization.CultureInfo.InvariantCulture),
            track,
            track,
            $"Event {startedAt[..10]}",
            type,
            [CreateDriver("Driver", carNumber, carClass, team, lapTimes, isPlayer, gridPosition, finishTime)],
            mostLapsCompleted);
    }

    private static Driver CreateDriver(
        string name,
        string carNumber,
        string carClass,
        string team,
        double[] lapTimes,
        bool isPlayer,
        int? gridPosition = null,
        TimeSpan? finishTime = null) =>
        new(
            name,
            carNumber,
            carClass,
            team,
            1,
            lapTimes.Select((time, index) => new Lap(index + 1, TimeSpan.FromSeconds(time), [])),
            isPlayer,
            "Test Car",
            gridPosition,
            finishTime);
}
