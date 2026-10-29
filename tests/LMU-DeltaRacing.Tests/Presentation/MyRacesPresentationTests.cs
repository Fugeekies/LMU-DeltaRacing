using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

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
            session.SessionType == "Race" && DriverProfile.FindDriver(session, "Driver Tom") is not null);

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver Tom");

        raceCount.Should().Be(87);
        records.Count.Should().BeGreaterThanOrEqualTo(raceCount);
        records.Select(record => record.Category).Should().Contain(["LMGTE", "LMP3", "LMGT3", "LMP2", "Hypercar"]);
    }

    [TestMethod]
    public void CreateRecords_reports_the_class_position_of_the_race_and_not_of_the_qualifying()
    {
        var sessions = new[]
        {
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T11:00:00Z", [122], classPosition: 5),
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-22T11:30:00Z", [125], classPosition: 2, finishStatus: "Finished Normally"),
            CreateSession("Qualify", "Spa", "Hyper", "Toyota", "2025-02-22T12:00:00Z", [121], classPosition: 1)
        };

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver");

        records.Should().HaveCount(2);
        records[0].RaceResult.Should().BeNull("a qualifying session without race has no race result");
        records[0].ClassPosition.Should().BeNull();
        records[0].ResultLabel.Should().Be("—");
        records[0].PositionTier.Should().Be(MyRacePositionTier.None);
        records[1].ClassPosition.Should().Be(2);
        records[1].RaceResult.Should().Be(HomeResultKind.Podium);
        records[1].ResultLabel.Should().Be("2e");
        records[1].PositionTier.Should().Be(MyRacePositionTier.Silver);
    }

    [TestMethod]
    public void CreateRecords_highlights_the_podium_the_top_five_and_the_top_ten_only()
    {
        int[] positions = [1, 2, 3, 4, 5, 6, 10, 11];
        var sessions = positions
            .Select(position => CreateSession(
                "Race",
                $"Track {position:D2}",
                "Hyper",
                "Toyota",
                "2025-02-22T11:30:00Z",
                [125],
                classPosition: position,
                finishStatus: "Finished Normally"))
            .ToArray();

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver").OrderBy(record => record.ClassPosition).ToArray();
        var tiers = records.Select(record => record.PositionTier);

        tiers.Should().Equal(
            MyRacePositionTier.Gold,
            MyRacePositionTier.Silver,
            MyRacePositionTier.Bronze,
            MyRacePositionTier.TopFive,
            MyRacePositionTier.TopFive,
            MyRacePositionTier.TopTen,
            MyRacePositionTier.TopTen,
            MyRacePositionTier.None);
        records.Select(record => record.IsPodium).Should().Equal(true, true, true, false, false, false, false, false);
    }

    [TestMethod]
    public void CreateRecords_labels_retirements_disqualifications_and_unclassified_races_without_highlight()
    {
        var sessions = new[]
        {
            CreateSession("Race", "Spa", "Hyper", "Toyota", "2025-02-22T11:30:00Z", [125], classPosition: 1, finishStatus: "DNF"),
            CreateSession("Race", "Monza", "Hyper", "Toyota", "2025-02-22T11:30:00Z", [125], classPosition: 2, finishStatus: "DQ"),
            CreateSession("Race", "Imola", "Hyper", "Toyota", "2025-02-22T11:30:00Z", [125], classPosition: 3, finishStatus: "None")
        };

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver").ToDictionary(record => record.TrackVenue);

        records["Spa"].ResultLabel.Should().Be("DNF");
        records["Monza"].ResultLabel.Should().Be("DQ");
        records["Imola"].ResultLabel.Should().Be("NC");
        records.Values.Select(record => record.PositionTier).Should().AllBeEquivalentTo(MyRacePositionTier.None);
        records.Values.Should().OnlyContain(record => !record.IsPodium, "a retired, disqualified or unclassified race earns no trophy");
    }

    [TestMethod]
    public void CreateRecords_on_real_fixtures_shows_the_class_position_of_each_race()
    {
        var parser = new LMU_DeltaRacing.Infrastructure.Parsing.XmlResultParser();
        var races = Directory
            .EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return parser.Parse(stream);
            })
            .Where(session => session.SessionType == "Race")
            .Select(session => (Session: session, Driver: DriverProfile.FindDriver(session, "Driver Tom")))
            .Where(race => race.Driver is not null)
            .ToArray();

        var records = MyRacesPresentation.CreateRecords(races.Select(race => race.Session), "Driver Tom");

        records.Should().HaveCount(races.Length);
        records.Select(record => record.ClassPosition)
            .Should()
            .BeEquivalentTo(races.Select(race => race.Driver!.ClassPosition));
        records.Select(record => record.PositionTier).Should().Contain(
            [MyRacePositionTier.Gold, MyRacePositionTier.Bronze, MyRacePositionTier.TopFive, MyRacePositionTier.TopTen]);

        // The fixtures hold three class wins, a class P1 with a DNF and another one not classified:
        // only the three wins are highlighted in gold.
        records.Where(record => record.ClassPosition == 1).Select(record => record.ResultLabel)
            .Should().BeEquivalentTo(["1er", "1er", "1er", "DNF", "NC"]);
        records.Count(record => record.PositionTier == MyRacePositionTier.Gold).Should().Be(3);
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
        string driverName = "Driver",
        int? classPosition = null,
        string finishStatus = "")
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
                    classPosition,
                    lapsCompleted,
                    finishStatus)
            ]);
    }
}
