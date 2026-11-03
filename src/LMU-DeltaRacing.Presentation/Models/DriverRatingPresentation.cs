using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Builds the rating page of the selected driver: their rating, tier and goals, the story of their
/// rating race by race, and the leaderboard of the drivers met.
/// </summary>
public static class DriverRatingPresentation
{
    /// <summary>
    /// The number of races of the recent trend.
    /// </summary>
    public const int RecentRaceCount = 10;

    private const int HighlightCount = 3;
    private const int ShortlistTop = 10;
    private const int ShortlistNeighbours = 3;

    // Shares of the ranked drivers, best first. The tiers borrow the names of the FIA driver
    // categorisation, known to endurance racers; the shares are this application's choice.
    private static readonly (RatingTier Tier, int Percent)[] TierShares =
    [
        (RatingTier.Platinum, 10),
        (RatingTier.Gold, 30),
        (RatingTier.Silver, 60)
    ];

    /// <summary>
    /// Creates the rating page of a driver, or returns <see langword="null"/> without a selected
    /// driver or without any rated race of theirs.
    /// </summary>
    public static DriverRatingView? Create(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var allSessions = sessions as IReadOnlyCollection<Session> ?? sessions.ToArray();
        var history = DriverRatings.Calculate(allSessions);
        if (history.FindDriver(userName) is not { } rating)
        {
            return null;
        }

        var eventIds = RaceEventPresentation.CreateEvents(allSessions, userName)
            .Where(raceEvent => raceEvent.Race is not null)
            .ToDictionary(raceEvent => raceEvent.Race!.Id, raceEvent => raceEvent.Id);
        var timeline = history.Races
            .Select(race => (race.Session, Change: race.Find(rating.Name)))
            .Where(race => race.Change is not null)
            .Select(race => CreateEntry(race.Session, race.Change!, eventIds.GetValueOrDefault(race.Session.Id)))
            .ToArray();
        var leaderboard = CreateLeaderboard(history, rating.Name);
        var tiers = CreateThresholds(leaderboard);
        var player = leaderboard.FirstOrDefault(row => row.IsPlayer);
        var recent = timeline.TakeLast(RecentRaceCount).ToArray();

        return new DriverRatingView(
            rating.Name,
            rating.Rating,
            rating.Races,
            player?.Rank,
            leaderboard.Count,
            player?.Tier,
            rating.Peak,
            rating.PeakAt,
            recent.Sum(race => race.Delta),
            recent.Length,
            timeline.Average(race => race.FieldStrength),
            timeline.Count(race => race.ClassRank < race.ExpectedRank),
            player is { Rank: > 1 } ? Goal(leaderboard[player.Rank - 2].Name, leaderboard[player.Rank - 2].Rating, rating.Rating) : null,
            player is null ? null : CreateTierGoal(player.Tier, tiers, rating.Rating),
            tiers,
            timeline,
            timeline.Where(race => race.Delta > 0).OrderByDescending(race => race.Delta).Take(HighlightCount).ToArray(),
            timeline.Where(race => race.Delta < 0).OrderBy(race => race.Delta).Take(HighlightCount).ToArray(),
            leaderboard);
    }

    /// <summary>
    /// Returns the tier of a ranked driver from their share of the ranking.
    /// </summary>
    /// <param name="rank">The rank, from 1.</param>
    /// <param name="rankedDrivers">The number of ranked drivers.</param>
    public static RatingTier GetTier(int rank, int rankedDrivers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rank, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rankedDrivers, rank);

        // Integer comparison: rank / drivers <= percent / 100, without rounding errors.
        foreach (var (tier, percent) in TierShares)
        {
            if (rank * 100 <= rankedDrivers * percent)
            {
                return tier;
            }
        }

        return RatingTier.Bronze;
    }

    /// <summary>
    /// Returns the French name of a tier.
    /// </summary>
    public static string Label(RatingTier tier) => tier switch
    {
        RatingTier.Platinum => "Platine",
        RatingTier.Gold => "Or",
        RatingTier.Silver => "Argent",
        _ => "Bronze"
    };

    /// <summary>
    /// Keeps the rows shown by default: the top 10, then the driver and their neighbours.
    /// </summary>
    public static IReadOnlyList<RatingLeaderboardRow> Shortlist(IReadOnlyList<RatingLeaderboardRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var player = rows.FirstOrDefault(row => row.IsPlayer)?.Rank;
        return rows
            .Where(row => row.Rank <= ShortlistTop
                || (player is { } rank && Math.Abs(row.Rank - rank) <= ShortlistNeighbours))
            .ToArray();
    }

    private static RatingRaceEntry CreateEntry(Session session, RatingChange change, string? eventId)
    {
        var kind = RaceResults.GetKind(change.Result.FinishStatus, change.ClassRank);
        return new RatingRaceEntry(
            eventId,
            session.StartedAt,
            session.TrackVenue,
            change.Result.Category(),
            change.Result.CarName(),
            kind,
            RaceResults.GetLabel(kind, change.ClassRank),
            change.ClassRank,
            change.FieldSize,
            change.ExpectedRank,
            change.FieldStrength,
            change.Before,
            change.After);
    }

    private static IReadOnlyList<RatingLeaderboardRow> CreateLeaderboard(DriverRatingHistory history, string playerName)
    {
        var ranked = history.Drivers.Where(driver => !driver.IsProvisional).ToArray();
        return ranked
            .Select((driver, index) => new RatingLeaderboardRow(
                index + 1,
                driver.Name,
                driver.Rating,
                GetTier(index + 1, ranked.Length),
                driver.Races,
                driver.LastRaceAt,
                string.Equals(driver.Name, playerName, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    /// <summary>
    /// The lowest rating of each tier: the rating to reach to enter it today.
    /// </summary>
    private static IReadOnlyList<RatingTierThreshold> CreateThresholds(IReadOnlyList<RatingLeaderboardRow> leaderboard) =>
        TierShares
            .Select(share => leaderboard.LastOrDefault(row => row.Tier == share.Tier))
            .OfType<RatingLeaderboardRow>()
            .Select(row => new RatingTierThreshold(row.Tier, row.Rating))
            .ToArray();

    private static RatingGoal? CreateTierGoal(RatingTier tier, IReadOnlyList<RatingTierThreshold> thresholds, double rating) =>
        thresholds
            .Where(threshold => threshold.Tier == tier + 1)
            .Select(threshold => Goal(Label(threshold.Tier), threshold.MinimumRating, rating))
            .FirstOrDefault();

    // A rating equal to the target is not enough to pass it: at least one point is needed.
    private static RatingGoal Goal(string label, double target, double rating) =>
        new(label, Math.Max(1, (int)Math.Ceiling(target - rating)));
}

/// <summary>
/// A tier of the ranking, from the share of ranked drivers ahead.
/// </summary>
public enum RatingTier
{
    /// <summary>Below the top 60 %.</summary>
    Bronze,

    /// <summary>The top 60 %, below gold.</summary>
    Silver,

    /// <summary>The top 30 %, below platinum.</summary>
    Gold,

    /// <summary>The top 10 %.</summary>
    Platinum
}

/// <summary>
/// The rating page of a driver.
/// </summary>
/// <param name="Name">The driver name.</param>
/// <param name="Rating">The current rating.</param>
/// <param name="Races">The number of rated races.</param>
/// <param name="Rank">The rank among the ranked drivers, or <see langword="null"/> while provisional.</param>
/// <param name="RankedDrivers">The number of ranked drivers, past their calibration races.</param>
/// <param name="Tier">The tier, or <see langword="null"/> while provisional.</param>
/// <param name="Peak">The highest rating reached.</param>
/// <param name="PeakAt">The date of the race that gave the highest rating.</param>
/// <param name="RecentChange">The points won or lost over the recent races.</param>
/// <param name="RecentRaces">The number of recent races, at most <see cref="DriverRatingPresentation.RecentRaceCount"/>.</param>
/// <param name="AverageFieldStrength">The average field strength of the rated races.</param>
/// <param name="RacesAboveExpectation">The number of races finished ahead of the expected position.</param>
/// <param name="NextRival">The next driver to overtake in the ranking, or <see langword="null"/> for the leader.</param>
/// <param name="NextTier">The next tier to reach, or <see langword="null"/> in platinum or while provisional.</param>
/// <param name="Tiers">The rating to reach to enter each tier, best tier first.</param>
/// <param name="Timeline">The rated races, in chronological order.</param>
/// <param name="Exploits">The races that gave the most points, best first.</param>
/// <param name="Setbacks">The races that cost the most points, worst first.</param>
/// <param name="Leaderboard">The ranked drivers, best first.</param>
public sealed record DriverRatingView(
    string Name,
    double Rating,
    int Races,
    int? Rank,
    int RankedDrivers,
    RatingTier? Tier,
    double Peak,
    DateTimeOffset PeakAt,
    double RecentChange,
    int RecentRaces,
    double AverageFieldStrength,
    int RacesAboveExpectation,
    RatingGoal? NextRival,
    RatingGoal? NextTier,
    IReadOnlyList<RatingTierThreshold> Tiers,
    IReadOnlyList<RatingRaceEntry> Timeline,
    IReadOnlyList<RatingRaceEntry> Exploits,
    IReadOnlyList<RatingRaceEntry> Setbacks,
    IReadOnlyList<RatingLeaderboardRow> Leaderboard)
{
    /// <summary>
    /// Gets whether the rating is still being calibrated.
    /// </summary>
    public bool IsProvisional => Races < DriverRatings.CalibrationRaces;

    /// <summary>
    /// Gets the number of races left before the driver is ranked.
    /// </summary>
    public int RacesToCalibrate => Math.Max(0, DriverRatings.CalibrationRaces - Races);
}

/// <summary>
/// A goal of the driver: a driver or a tier to reach, and the points still needed.
/// </summary>
/// <param name="Label">The driver name or the tier name.</param>
/// <param name="Points">The points needed, at least 1.</param>
public sealed record RatingGoal(string Label, int Points);

/// <summary>
/// The rating to reach to enter a tier today.
/// </summary>
/// <param name="Tier">The tier.</param>
/// <param name="MinimumRating">The rating of the last driver of the tier.</param>
public sealed record RatingTierThreshold(RatingTier Tier, double MinimumRating);

/// <summary>
/// A rated race of the driver.
/// </summary>
/// <param name="EventId">The event of the race in "race details", when known.</param>
/// <param name="StartedAt">The race start.</param>
/// <param name="TrackVenue">The track venue.</param>
/// <param name="Category">The WEC category of the car.</param>
/// <param name="Car">The car.</param>
/// <param name="Kind">How the race ended.</param>
/// <param name="Result">The short result: <c>P3</c>, <c>DNF</c>, <c>DQ</c> or <c>NC</c>.</param>
/// <param name="ClassRank">The official class position.</param>
/// <param name="FieldSize">The number of rated drivers in the class.</param>
/// <param name="ExpectedRank">The position predicted by the ratings.</param>
/// <param name="FieldStrength">The average rating of the class before the race.</param>
/// <param name="Before">The rating before the race.</param>
/// <param name="After">The rating after the race.</param>
public sealed record RatingRaceEntry(
    string? EventId,
    DateTimeOffset StartedAt,
    string TrackVenue,
    string Category,
    string Car,
    HomeResultKind Kind,
    string Result,
    int ClassRank,
    int FieldSize,
    double ExpectedRank,
    double FieldStrength,
    double Before,
    double After)
{
    /// <summary>
    /// Gets the points won (positive) or lost (negative) in the race.
    /// </summary>
    public double Delta => After - Before;
}

/// <summary>
/// A ranked driver.
/// </summary>
/// <param name="Rank">The rank, from 1.</param>
/// <param name="Name">The driver name.</param>
/// <param name="Rating">The current rating.</param>
/// <param name="Tier">The tier.</param>
/// <param name="Races">The number of rated races.</param>
/// <param name="LastRaceAt">The date of the last rated race.</param>
/// <param name="IsPlayer">Whether this is the selected driver.</param>
public sealed record RatingLeaderboardRow(
    int Rank,
    string Name,
    double Rating,
    RatingTier Tier,
    int Races,
    DateTimeOffset LastRaceAt,
    bool IsPlayer);
