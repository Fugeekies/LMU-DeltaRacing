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
            CreateSession("Qualify", "Spa", "Hyper", "7", "Toyota", "2025-02-22T11:00:00Z", 122.5, 122.25),
            CreateSession("Race", "Spa", "Hyper", "7", "Toyota", "2025-02-22T12:00:00Z", 125, 124.5),
            CreateSession("Qualify", "Spa", "Hyper", "7", "Toyota", "2025-02-23T11:00:00Z", 121.75),
            CreateSession("Race", "Spa", "Hyper", "7", "Toyota", "2025-02-23T12:00:00Z", 125.5)
        };

        var records = MyRacesPresentation.CreateRecords(sessions);

        records.Should().HaveCount(2);
        records[0].Category.Should().Be("Hypercar");
        records[0].Car.Should().Be("#7 · Toyota");
        records[0].QualifyingTime.Should().Be(TimeSpan.FromSeconds(121.75));
        records[0].QualifyingDelta.Should().Be(TimeSpan.FromSeconds(-0.5));
        records[0].RaceTime.Should().Be(TimeSpan.FromSeconds(125.5));
        records[0].RaceDelta.Should().Be(TimeSpan.FromSeconds(1));
        records[1].QualifyingDelta.Should().BeNull();
        records[1].RaceDelta.Should().BeNull();
    }

    [TestMethod]
    public void CreateRecords_keeps_deltas_scoped_to_circuit_category_and_vehicle()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "7", "Toyota", "2025-02-22T11:00:00Z", 122),
            CreateSession("Qualify", "Le Mans", "Hyper", "7", "Toyota", "2025-02-23T11:00:00Z", 121),
            CreateSession("Qualify", "Spa", "GT3", "7", "Toyota", "2025-02-23T12:00:00Z", 130),
            CreateSession("Qualify", "Spa", "Hyper", "8", "Toyota", "2025-02-23T13:00:00Z", 123),
            CreateSession("Practice", "Spa", "Hyper", "7", "Toyota", "2025-02-24T11:00:00Z", 120),
            CreateSession("Qualify", "Spa", "Unknown", "7", "Toyota", "2025-02-24T12:00:00Z", 120)
        };

        var records = MyRacesPresentation.CreateRecords(sessions);

        records.Should().HaveCount(4);
        records.Should().OnlyContain(record => record.QualifyingDelta == null);
        records.Should().Contain(record => record.Category == "LMGT3");
    }

    private static Session CreateSession(
        string type,
        string track,
        string carClass,
        string carNumber,
        string team,
        string startedAt,
        params double[] lapTimes) =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.Parse(startedAt, System.Globalization.CultureInfo.InvariantCulture),
            track,
            track,
            $"Event {startedAt[..10]}",
            type,
            [
                new Driver(
                    "Driver",
                    carNumber,
                    carClass,
                    team,
                    1,
                    lapTimes.Select((time, index) =>
                        new Lap(index + 1, TimeSpan.FromSeconds(time), [])))
            ]);
}
