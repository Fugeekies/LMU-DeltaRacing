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
    public void CreateRivalries_aggregates_only_classified_encounters_with_the_player()
    {
        var sessions = new[]
        {
            CreateSession(("Player", 1), ("Rival", 2)),
            CreateSession(("Player", 4), ("Rival", 2)),
            CreateSession(("Player", 1), ("Rival", 1)),
            CreateSession(("Player", 2), ("Unknown position", null)),
            CreateSession(("Different driver", 1), ("Unrelated session", 2))
        };

        var rivalries = DriversRankingPresentation.CreateRivalries(sessions, "Player");

        rivalries.Should().ContainSingle();
        rivalries[0].Name.Should().Be("Rival");
        rivalries[0].SessionsFaced.Should().Be(3);
        rivalries[0].Wins.Should().Be(1);
        rivalries[0].Losses.Should().Be(1);
        rivalries[0].WinPercentage.Should().BeApproximately(33.3333, 0.001);
    }

    private static Session CreateSession(
        (string Name, int? Position) first,
        (string Name, int? Position) second)
    {
        return new Session(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Track",
            "Track",
            "Event",
            "Race",
            new[] { first, second }.Select(entry => new Driver(
                entry.Name,
                string.Empty,
                "Hyper",
                string.Empty,
                entry.Position,
                [])));
    }
}
