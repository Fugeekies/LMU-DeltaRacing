using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies detailed session classification, sector and gap calculations.
/// </summary>
[TestClass]
public sealed class RaceDetailsPresentationTests
{
    [TestMethod]
    public void CreateResults_shows_personal_best_sectors_from_timed_laps_and_marks_class_bests()
    {
        var player = CreateDriver(
            "Player",
            "Hyper",
            position: 1,
            laps:
            [
                CreateLap(90, 30, 30, 30),
                CreateLap(91, 29, 31, 31),
                // A lap without time (cut or incomplete) must not provide best sectors.
                CreateLap(null, 28, 28, 28)
            ]);
        var rival = CreateDriver(
            "Rival",
            "Hyper",
            position: 2,
            laps:
            [
                CreateLap(92, 31, 31, 29),
                CreateLap(93, 32, 30.5, 30.5)
            ]);

        var results = RaceDetailsPresentation.CreateResults(CreateSession("Qualify", rival, player), "Player");

        results.Select(result => result.Name).Should().Equal("Player", "Rival");
        results[0].IsPlayer.Should().BeTrue();
        results[0].Category.Should().Be("Hypercar");
        results[0].Sectors.Select(sector => sector.Time).Should().Equal(Seconds(29), Seconds(30), Seconds(30));
        results[0].Sectors.Select(sector => sector.Performance).Should().Equal(
            SectorPerformance.ClassBest,
            SectorPerformance.ClassBest,
            SectorPerformance.Standard);
        results[0].Sectors.Select(sector => sector.Delta).Should().Equal(TimeSpan.Zero, TimeSpan.Zero, Seconds(1));
        results[0].IdealLap.Should().Be(Seconds(89));
        results[0].DeltaToBestIdealLap.Should().Be(TimeSpan.Zero);
        results[0].FastestLap.Should().Be(Seconds(90));
        results[0].DeltaToBestLap.Should().Be(TimeSpan.Zero);

        results[1].Sectors.Select(sector => sector.Time).Should().Equal(Seconds(31), Seconds(30.5), Seconds(29));
        results[1].Sectors.Select(sector => sector.Performance).Should().Equal(
            SectorPerformance.Standard,
            SectorPerformance.Standard,
            SectorPerformance.ClassBest);
        results[1].IdealLap.Should().Be(Seconds(90.5));
        results[1].DeltaToBestIdealLap.Should().Be(Seconds(1.5));
        results[1].DeltaToBestLap.Should().Be(Seconds(2));
    }

    [TestMethod]
    public void CreateResults_compares_sectors_and_laps_within_each_class()
    {
        var prototype = CreateDriver("Prototype", "Hyper", position: 1, classPosition: 1, laps: [CreateLap(75, 25, 25, 25)]);
        var player = CreateDriver("Player", "GT3", position: 2, classPosition: 1, laps: [CreateLap(90, 30, 30, 30)]);
        var rival = CreateDriver("Rival", "GT3", position: 3, classPosition: 2, laps: [CreateLap(93, 31, 31, 31)]);

        var results = RaceDetailsPresentation.CreateResults(CreateSession("Qualify", prototype, player, rival), "Player");

        var playerResult = results.Single(result => result.Name == "Player");
        playerResult.Category.Should().Be("LMGT3");
        playerResult.ClassPosition.Should().Be(1);
        playerResult.Sectors.Should().OnlyContain(sector =>
            sector.Performance == SectorPerformance.ClassBest && sector.Delta == TimeSpan.Zero);
        playerResult.DeltaToBestLap.Should().Be(TimeSpan.Zero);
        playerResult.GapToLeader.Kind.Should().Be(LeaderGapKind.Leader);

        var rivalResult = results.Single(result => result.Name == "Rival");
        rivalResult.Sectors[0].Delta.Should().Be(Seconds(1));
        rivalResult.DeltaToBestLap.Should().Be(Seconds(3));
        rivalResult.GapToLeader.Should().Be(new LeaderGap(LeaderGapKind.Time, Time: Seconds(3)));
    }

    [TestMethod]
    public void CreateResults_calculates_race_gaps_to_the_class_leader()
    {
        var session = CreateSession(
            "Race",
            CreateDriver("Prototype", "Hyper", position: 1, classPosition: 1, finishTime: Seconds(1400), lapsCompleted: 22),
            CreateDriver("Leader", "GT3", position: 2, classPosition: 1, finishTime: Seconds(1500), lapsCompleted: 20),
            CreateDriver("Second", "GT3", position: 3, classPosition: 2, finishTime: Seconds(1512.5), lapsCompleted: 20),
            CreateDriver("Lapped", "GT3", position: 4, classPosition: 3, finishTime: Seconds(1530), lapsCompleted: 19),
            CreateDriver("Retired", "GT3", position: 5, classPosition: 4, lapsCompleted: 8, finishStatus: "DNF"));

        var results = RaceDetailsPresentation.CreateResults(session, "Second");
        var gaps = results.ToDictionary(result => result.Name, result => result.GapToLeader);

        results.Single(result => result.Name == "Second").TotalTime.Should().Be(Seconds(1512.5));
        results.Single(result => result.Name == "Retired").TotalTime.Should().BeNull();

        gaps["Prototype"].Kind.Should().Be(LeaderGapKind.Leader);
        gaps["Leader"].Kind.Should().Be(LeaderGapKind.Leader);
        gaps["Second"].Should().Be(new LeaderGap(LeaderGapKind.Time, Time: Seconds(12.5)));
        gaps["Lapped"].Should().Be(new LeaderGap(LeaderGapKind.Laps, Laps: 1));
        gaps["Retired"].Should().Be(new LeaderGap(LeaderGapKind.Status, Status: "DNF"));
    }

    private static TimeSpan Seconds(double value) => TimeSpan.FromSeconds(value);

    private static Session CreateSession(string type, params Driver[] drivers) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Spa", "Grand Prix", "Event", type, drivers);

    private static Driver CreateDriver(
        string name,
        string carClass,
        int position,
        int? classPosition = null,
        TimeSpan? finishTime = null,
        int? lapsCompleted = null,
        string finishStatus = "",
        IEnumerable<Lap>? laps = null) =>
        new(
            name,
            position.ToString(System.Globalization.CultureInfo.InvariantCulture),
            carClass,
            "Team",
            position,
            laps ?? [],
            $"{name} Car",
            finishTime: finishTime,
            classPosition: classPosition,
            lapsCompleted: lapsCompleted,
            finishStatus: finishStatus);

    private static Lap CreateLap(double? time, double sector1, double sector2, double sector3) =>
        new(
            1,
            time is { } value ? TimeSpan.FromSeconds(value) : null,
            [
                new Sector(1, TimeSpan.FromSeconds(sector1)),
                new Sector(2, TimeSpan.FromSeconds(sector2)),
                new Sector(3, TimeSpan.FromSeconds(sector3))
            ]);
}
