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
            CreateSession(
                ("Player", 1, true),
                ("Rival", 2, false)),
            CreateSession(
                ("Player", 4, true),
                ("Rival", 2, false)),
            CreateSession(
                ("Player", 1, true),
                ("Rival", 1, false)),
            CreateSession(
                ("Player", 2, true),
                ("Unknown position", null, false)),
            CreateSession(
                ("Player", 1, true),
                ("Unrelated session", 2, false),
                playerIsMarked: false)
        };

        var rivalries = DriversRankingPresentation.CreateRivalries(sessions);

        rivalries.Should().ContainSingle();
        rivalries[0].Name.Should().Be("Rival");
        rivalries[0].SessionsFaced.Should().Be(3);
        rivalries[0].Wins.Should().Be(1);
        rivalries[0].Losses.Should().Be(1);
        rivalries[0].WinPercentage.Should().BeApproximately(33.3333, 0.001);
    }

    private static Session CreateSession(
        (string Name, int? Position, bool IsPlayer) first,
        (string Name, int? Position, bool IsPlayer) second,
        (string Name, int? Position, bool IsPlayer)? third = null,
        bool playerIsMarked = true)
    {
        var entries = third is { } thirdEntry
            ? new[] { first, second, thirdEntry }
            : [first, second];
        if (!playerIsMarked)
        {
            entries = entries.Select(entry => (entry.Name, entry.Position, IsPlayer: false)).ToArray();
        }

        return new Session(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Track",
            "Track",
            "Event",
            "Race",
            entries.Select(entry => new Driver(
                entry.Name,
                string.Empty,
                "Hyper",
                string.Empty,
                entry.Position,
                [],
                entry.IsPlayer)));
    }
}
