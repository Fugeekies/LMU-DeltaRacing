using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies detailed session classification and sector performance calculations.
/// </summary>
[TestClass]
public sealed class RaceDetailsPresentationTests
{
    [TestMethod]
    public void CreateResults_uses_fastest_lap_sectors_and_classifies_session_and_personal_bests()
    {
        var player = CreateDriver(
            "Player",
            1,
            true,
            TimeSpan.FromSeconds(100),
            [
                CreateLap(90, 30, 30, 30),
                CreateLap(91, 29, 31, 31)
            ]);
        var rival = CreateDriver(
            "Rival",
            2,
            false,
            TimeSpan.FromSeconds(102),
            [
                CreateLap(92, 31, 31, 29),
                CreateLap(93, 32, 30.5, 30.5)
            ]);
        var session = new Session(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Spa",
            "Grand Prix",
            "Race",
            "Race",
            [rival, player]);

        var results = RaceDetailsPresentation.CreateResults(session);

        results.Select(result => result.Name).Should().Equal("Player", "Rival");
        results[0].FastestLap.Should().Be(TimeSpan.FromSeconds(90));
        results[0].Sectors[0].Time.Should().Be(TimeSpan.FromSeconds(30));
        results[0].Sectors[0].Performance.Should().Be(SectorPerformance.Standard);
        results[0].Sectors[1].Performance.Should().Be(SectorPerformance.SessionBest);
        results[0].Sectors[2].Performance.Should().Be(SectorPerformance.PersonalBest);
        results[0].GapToLeader.Should().Be(TimeSpan.Zero);
        results[1].GapToLeader.Should().Be(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void CreateResults_uses_best_lap_gap_when_leader_has_no_finish_time()
    {
        var session = new Session(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Spa",
            "Grand Prix",
            "Qualifying",
            "Qualify",
            [
                CreateDriver("Player", 1, true, null, [CreateLap(90, 30, 30, 30)]),
                CreateDriver("Rival", 2, false, null, [CreateLap(91.25, 30.5, 30.25, 30.5)])
            ]);

        var results = RaceDetailsPresentation.CreateResults(session);

        results[1].GapToLeader.Should().Be(TimeSpan.FromSeconds(1.25));
    }

    private static Driver CreateDriver(
        string name,
        int position,
        bool isPlayer,
        TimeSpan? finishTime,
        IEnumerable<Lap> laps) =>
        new(
            name,
            position.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "Hyper",
            "Team",
            position,
            laps,
            isPlayer,
            $"{name} Car",
            finishTime: finishTime);

    private static Lap CreateLap(double time, double sector1, double sector2, double sector3) =>
        new(
            1,
            TimeSpan.FromSeconds(time),
            [
                new Sector(1, TimeSpan.FromSeconds(sector1)),
                new Sector(2, TimeSpan.FromSeconds(sector2)),
                new Sector(3, TimeSpan.FromSeconds(sector3))
            ]);
}
