using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies head-to-head aggregation from imported sessions.
/// </summary>
[TestClass]
public sealed class DriversRankingPresentationTests
{
    [TestMethod]
    public void CreateRivalries_counts_only_classified_races_against_drivers_of_the_same_class()
    {
        var sessions = new[]
        {
            // Win in class even though a faster-class car finished ahead overall.
            CreateSession(
                "Race",
                new("Player", "GT3", Position: 5, ClassPosition: 1),
                new("Rival", "GT3", Position: 6, ClassPosition: 2),
                new("Prototype", "Hyper", Position: 1, ClassPosition: 1)),
            CreateSession(
                "Race",
                new("Player", "GT3", Position: 4, ClassPosition: 4),
                new("Rival", "GT3", Position: 2, ClassPosition: 2)),
            // Without a class position, the overall position orders drivers of the same class.
            CreateSession(
                "Race",
                new("Player", "GT3", Position: 1, ClassPosition: null),
                new("Rival", "GT3", Position: 2, ClassPosition: null)),
            CreateSession(
                "Qualify",
                new("Player", "GT3", Position: 3, ClassPosition: 3),
                new("Rival", "GT3", Position: 1, ClassPosition: 1)),
            CreateSession(
                "Race",
                new("Player", "GT3", Position: 2, ClassPosition: 2),
                new("Rival", "GT3", Position: null, ClassPosition: null)),
            CreateSession(
                "Race",
                new("Different driver", "GT3", Position: 1, ClassPosition: 1),
                new("Rival", "GT3", Position: 2, ClassPosition: 2))
        };

        var rivalries = DriversRankingPresentation.CreateRivalries(sessions, "Player");

        var rival = rivalries.Should().ContainSingle().Which;
        rival.Name.Should().Be("Rival");
        rival.SessionsFaced.Should().Be(3);
        rival.Wins.Should().Be(2);
        rival.Losses.Should().Be(1);
        rival.WinPercentage.Should().BeApproximately(66.6667, 0.001);
    }

    private static Session CreateSession(string type, params Entry[] entries) =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Track",
            "Track",
            "Event",
            type,
            entries.Select(entry => new Driver(
                entry.Name,
                string.Empty,
                entry.CarClass,
                string.Empty,
                entry.Position,
                [],
                classPosition: entry.ClassPosition)));

    private sealed record Entry(string Name, string CarClass, int? Position, int? ClassPosition);
}
