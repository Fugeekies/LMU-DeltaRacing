using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

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

        var rivalries = DriversRankingPresentation.CreateRivalries(sessions, "Player", DriverRatings.Calculate(sessions));

        var rival = rivalries.Should().ContainSingle().Which;
        rival.Name.Should().Be("Rival");
        rival.SessionsFaced.Should().Be(3);
        rival.Wins.Should().Be(2);
        rival.Losses.Should().Be(1);
        rival.WinPercentage.Should().BeApproximately(66.6667, 0.001);
    }

    [TestMethod]
    public void CreateRivalries_gives_the_average_gap_and_the_last_race_faced()
    {
        var first = DateTimeOffset.Parse("2026-01-10T20:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var sessions = new[]
        {
            CreateDatedSession("Race", first, "Spa", [],
                new("Player", "GT3", Position: 1, ClassPosition: 1),
                new("Rival", "GT3", Position: 4, ClassPosition: 4)),
            CreateDatedSession("Race", first.AddDays(7), "Monza", [],
                new("Player", "GT3", Position: 3, ClassPosition: 3),
                new("Rival", "GT3", Position: 2, ClassPosition: 2)),
            // A later race without the rival does not move the last race faced.
            CreateDatedSession("Race", first.AddDays(14), "Imola", [],
                new("Player", "GT3", Position: 1, ClassPosition: 1),
                new("Other", "GT3", Position: 2, ClassPosition: 2))
        };

        var rival = DriversRankingPresentation.CreateRivalries(sessions, "Player", DriverRatings.Calculate(sessions))
            .Single(rivalry => rivalry.Name == "Rival");

        // ((4 - 1) + (2 - 3)) / 2: the player finishes one place ahead on average.
        rival.AverageClassGap.Should().Be(1);
        rival.LastFacedAt.Should().Be(first.AddDays(7));
        rival.LastFacedTrack.Should().Be("Monza");
    }

    [TestMethod]
    public void CreateRivalries_reads_the_current_rating_of_each_opponent()
    {
        var sessions = new[]
        {
            CreateSession("Race",
                new("Player", "GT3", Position: 1, ClassPosition: 1),
                new("Rival", "GT3", Position: 2, ClassPosition: 2))
        };
        var ratings = DriverRatings.Calculate(sessions);

        var rival = DriversRankingPresentation.CreateRivalries(sessions, "Player", ratings).Single();

        rival.Rating.Should().Be(ratings.FindDriver("Rival")!.Rating);
        rival.Rating.Should().BeLessThan(DriverRatings.InitialRating);
    }

    [TestMethod]
    public void CreateRivalries_counts_the_contacts_between_the_player_and_each_opponent()
    {
        var at = DateTimeOffset.Parse("2026-01-10T20:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var sessions = new[]
        {
            CreateDatedSession("Race", at, "Spa",
                [
                    // Both cars report the same contact: it counts once.
                    new SessionEvent(TimeSpan.FromSeconds(100), SessionEventKind.VehicleContact, "Player", "Rival"),
                    new SessionEvent(TimeSpan.FromSeconds(101), SessionEventKind.VehicleContact, "Rival", "Player"),
                    // Reported by the rival only: the player lived it too.
                    new SessionEvent(TimeSpan.FromSeconds(300), SessionEventKind.VehicleContact, "Rival", "Player"),
                    new SessionEvent(TimeSpan.FromSeconds(400), SessionEventKind.VehicleContact, "Player", "Other"),
                    new SessionEvent(TimeSpan.FromSeconds(500), SessionEventKind.ObstacleContact, "Player", "Post")
                ],
                new("Player", "GT3", Position: 1, ClassPosition: 1),
                new("Rival", "GT3", Position: 2, ClassPosition: 2),
                new("Other", "GT3", Position: 3, ClassPosition: 3)),
            // A race without a log adds no contact.
            CreateDatedSession("Race", at.AddDays(1), "Spa", [],
                new("Player", "GT3", Position: 1, ClassPosition: 1),
                new("Rival", "GT3", Position: 2, ClassPosition: 2))
        };

        var rivalries = DriversRankingPresentation.CreateRivalries(sessions, "Player", DriverRatings.Calculate(sessions));

        rivalries.Single(rivalry => rivalry.Name == "Rival").Contacts.Should().Be(2);
        rivalries.Single(rivalry => rivalry.Name == "Other").Contacts.Should().Be(1);
    }

    [TestMethod]
    public void CreateRivalries_has_no_contact_count_without_any_log()
    {
        var sessions = new[]
        {
            CreateSession("Race",
                new("Player", "GT3", Position: 1, ClassPosition: 1),
                new("Rival", "GT3", Position: 2, ClassPosition: 2))
        };

        var rival = DriversRankingPresentation.CreateRivalries(sessions, "Player", DriverRatings.Calculate(sessions)).Single();

        rival.Contacts.Should().BeNull();
    }

    private static Session CreateSession(string type, params Entry[] entries) =>
        CreateDatedSession(type, DateTimeOffset.UtcNow, "Track", [], entries);

    private static Session CreateDatedSession(
        string type,
        DateTimeOffset startedAt,
        string track,
        SessionEvent[] events,
        params Entry[] entries) =>
        new(
            Guid.NewGuid(),
            startedAt,
            track,
            track,
            "Event",
            type,
            entries.Select(entry => new Driver(
                entry.Name,
                string.Empty,
                entry.CarClass,
                string.Empty,
                entry.Position,
                [],
                classPosition: entry.ClassPosition)),
            events: events);

    private sealed record Entry(string Name, string CarClass, int? Position, int? ClassPosition);
}
