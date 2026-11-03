using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Calculates a multiplayer Elo rating for every driver met in a race, from the class positions.
/// </summary>
/// <remarks>
/// Each race is read as a set of duels between the drivers of a class: finishing ahead of a driver
/// is a win against them. A driver gains more by beating higher-rated drivers, and loses more to
/// lower-rated ones. Only the local result files are used: the rating measures a driver against the
/// grids found in those files, not against the whole LMU community.
/// </remarks>
public static class DriverRatings
{
    /// <summary>
    /// The rating of a driver before their first race.
    /// </summary>
    public const double InitialRating = 1500;

    /// <summary>
    /// The number of races during which a rating is provisional and moves faster.
    /// </summary>
    public const int CalibrationRaces = 5;

    // FIDE practice: a new rating moves fast to find its level, then twice slower once settled.
    // The change of one race is divided by the number of opponents, so one race moves a rating by
    // at most this factor, whatever the size of the grid.
    private const double CalibrationFactor = 64;
    private const double SettledFactor = 32;

    // With this scale, a 400-point gap gives the stronger driver a 10-to-1 chance in a duel.
    private const double Scale = 400;

    /// <summary>
    /// Rates every race in chronological order and returns the ratings after the last one.
    /// </summary>
    /// <param name="sessions">The imported sessions; only races are rated.</param>
    public static DriverRatingHistory Calculate(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var ratings = new Dictionary<string, RatingTotals>(StringComparer.OrdinalIgnoreCase);
        var races = new List<RatedRace>();
        foreach (var session in sessions
            .Where(session => session.Kind == SessionKind.Race)
            .OrderBy(session => session.StartedAt)
            .ThenBy(session => session.Id))
        {
            var classes = RateRace(session, ratings);
            if (classes.Count == 0)
            {
                continue;
            }

            // Every change is calculated from the ratings before the race, then applied together.
            foreach (var change in classes.SelectMany(rated => rated.Drivers))
            {
                if (!ratings.TryGetValue(change.Name, out var totals))
                {
                    totals = new RatingTotals(change.Name);
                    ratings.Add(change.Name, totals);
                }

                totals.Apply(change.After, session.StartedAt);
            }

            races.Add(new RatedRace(session, classes));
        }

        return new DriverRatingHistory(
            races,
            ratings.Values
                .Select(totals => new DriverRating(totals.Name, totals.Rating, totals.Races, totals.Peak, totals.PeakAt, totals.LastRaceAt))
                .OrderByDescending(driver => driver.Rating)
                .ThenBy(driver => driver.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static IReadOnlyList<RatedClass> RateRace(Session session, Dictionary<string, RatingTotals> ratings)
    {
        // Two merged names can appear in one session: the driver keeps their best position.
        var drivers = session.Drivers
            .Where(driver => driver.ClassRank().HasValue)
            .OrderBy(driver => driver.ClassRank())
            .GroupBy(driver => driver.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());

        var classes = new List<RatedClass>();
        foreach (var carClass in drivers.GroupBy(driver => driver.CarClass.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var grid = carClass.OrderBy(driver => driver.ClassRank()).ToArray();

            // When the player quits early, LMU writes the file with every other driver still running
            // ("None"): without any finisher, the order of the class is not a result.
            if (grid.Length < 2 || !grid.Any(IsFinisher))
            {
                continue;
            }

            var before = grid
                .Select(driver => ratings.TryGetValue(driver.Name.Trim(), out var totals) ? totals : null)
                .ToArray();
            var strength = before.Average(totals => totals?.Rating ?? InitialRating);
            var changes = new RatingChange[grid.Length];
            for (var index = 0; index < grid.Length; index++)
            {
                var rating = before[index]?.Rating ?? InitialRating;
                var score = 0.0;
                var expectedRank = 1.0;
                for (var other = 0; other < grid.Length; other++)
                {
                    if (other == index)
                    {
                        continue;
                    }

                    var expected = Expected(rating, before[other]?.Rating ?? InitialRating);
                    var rank = grid[index].ClassRank()!.Value.CompareTo(grid[other].ClassRank()!.Value);
                    score += (rank < 0 ? 1 : rank > 0 ? 0 : 0.5) - expected;
                    expectedRank += 1 - expected;
                }

                var factor = (before[index]?.Races ?? 0) < CalibrationRaces ? CalibrationFactor : SettledFactor;
                changes[index] = new RatingChange(
                    grid[index].Name.Trim(),
                    grid[index],
                    grid[index].ClassRank()!.Value,
                    grid.Length,
                    rating,
                    rating + (factor * score / (grid.Length - 1)),
                    expectedRank,
                    strength);
            }

            classes.Add(new RatedClass(carClass.Key, strength, changes));
        }

        return classes;
    }

    private static bool IsFinisher(Driver driver) =>
        RaceResults.GetKind(driver.FinishStatus, driver.ClassPosition)
            is not (HomeResultKind.Retired or HomeResultKind.Disqualified or HomeResultKind.Unclassified);

    /// <summary>
    /// Returns the chance of a driver to finish ahead of an opponent, from their ratings.
    /// </summary>
    private static double Expected(double rating, double opponent) =>
        1 / (1 + Math.Pow(10, (opponent - rating) / Scale));

    private sealed class RatingTotals(string name)
    {
        public string Name { get; } = name;
        public double Rating { get; private set; } = InitialRating;
        public int Races { get; private set; }
        public double Peak { get; private set; } = double.MinValue;
        public DateTimeOffset PeakAt { get; private set; }
        public DateTimeOffset LastRaceAt { get; private set; }

        public void Apply(double rating, DateTimeOffset startedAt)
        {
            Rating = rating;
            Races++;
            LastRaceAt = startedAt;
            if (rating > Peak)
            {
                Peak = rating;
                PeakAt = startedAt;
            }
        }
    }
}

/// <summary>
/// The rated races and the ratings of every driver after the last one.
/// </summary>
public sealed class DriverRatingHistory
{
    private readonly Dictionary<string, DriverRating> drivers;
    private readonly Dictionary<Guid, RatedRace> races;

    /// <summary>
    /// Creates the history of the ratings.
    /// </summary>
    /// <param name="races">The rated races, in chronological order.</param>
    /// <param name="drivers">The ratings of the drivers, best first.</param>
    public DriverRatingHistory(IReadOnlyList<RatedRace> races, IReadOnlyList<DriverRating> drivers)
    {
        ArgumentNullException.ThrowIfNull(races);
        ArgumentNullException.ThrowIfNull(drivers);

        Races = races;
        Drivers = drivers;
        this.drivers = drivers.ToDictionary(driver => driver.Name, StringComparer.OrdinalIgnoreCase);
        this.races = races.ToDictionary(race => race.Session.Id);
    }

    /// <summary>
    /// Gets the rated races, in chronological order.
    /// </summary>
    public IReadOnlyList<RatedRace> Races { get; }

    /// <summary>
    /// Gets the ratings of the drivers after the last race, best first, provisional ones included.
    /// </summary>
    public IReadOnlyList<DriverRating> Drivers { get; }

    /// <summary>
    /// Finds the rating of a driver, or returns <see langword="null"/> for a driver never rated.
    /// </summary>
    public DriverRating? FindDriver(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : drivers.GetValueOrDefault(name.Trim());

    /// <summary>
    /// Finds a rated race, or returns <see langword="null"/> for a session that is not a rated race.
    /// </summary>
    public RatedRace? FindRace(Guid sessionId) => races.GetValueOrDefault(sessionId);
}

/// <summary>
/// A rated race: the rating changes of each class that reached the flag.
/// </summary>
/// <param name="Session">The race session.</param>
/// <param name="Classes">The rated classes.</param>
public sealed record RatedRace(Session Session, IReadOnlyList<RatedClass> Classes)
{
    /// <summary>
    /// Finds the change of a driver in this race, or returns <see langword="null"/> when they were not rated.
    /// </summary>
    public RatingChange? Find(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : Classes
                .SelectMany(rated => rated.Drivers)
                .FirstOrDefault(change => string.Equals(change.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// A rated class of a race.
/// </summary>
/// <param name="CarClass">The vehicle class, as written by LMU.</param>
/// <param name="FieldStrength">The average rating of the class before the race.</param>
/// <param name="Drivers">The changes of its drivers, in class order.</param>
public sealed record RatedClass(string CarClass, double FieldStrength, IReadOnlyList<RatingChange> Drivers);

/// <summary>
/// How one race changed the rating of a driver.
/// </summary>
/// <param name="Name">The driver name.</param>
/// <param name="Result">The result of the driver in the race.</param>
/// <param name="ClassRank">The official class position.</param>
/// <param name="FieldSize">The number of rated drivers in the class, the driver included.</param>
/// <param name="Before">The rating before the race.</param>
/// <param name="After">The rating after the race.</param>
/// <param name="ExpectedRank">The position the ratings predicted: 1 plus the chances of each opponent to finish ahead.</param>
/// <param name="FieldStrength">The average rating of the class before the race.</param>
public sealed record RatingChange(
    string Name,
    Driver Result,
    int ClassRank,
    int FieldSize,
    double Before,
    double After,
    double ExpectedRank,
    double FieldStrength)
{
    /// <summary>
    /// Gets the points won (positive) or lost (negative) in the race.
    /// </summary>
    public double Delta => After - Before;
}

/// <summary>
/// The rating of a driver after their last race.
/// </summary>
/// <param name="Name">The driver name, as first met.</param>
/// <param name="Rating">The current rating.</param>
/// <param name="Races">The number of rated races.</param>
/// <param name="Peak">The highest rating reached after a race.</param>
/// <param name="PeakAt">The date of the race that gave the highest rating.</param>
/// <param name="LastRaceAt">The date of the last rated race.</param>
public sealed record DriverRating(
    string Name,
    double Rating,
    int Races,
    double Peak,
    DateTimeOffset PeakAt,
    DateTimeOffset LastRaceAt)
{
    /// <summary>
    /// Gets whether the rating is still being calibrated: such a driver is not ranked.
    /// </summary>
    public bool IsProvisional => Races < DriverRatings.CalibrationRaces;
}
