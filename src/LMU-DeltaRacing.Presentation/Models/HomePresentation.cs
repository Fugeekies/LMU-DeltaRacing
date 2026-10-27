using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Builds the career statistics of the selected driver for the home dashboard.
/// </summary>
/// <remarks>
/// Results are counted on the official class position written by LMU, as in the head-to-head
/// ranking: a retirement classified third still counts as a podium. A race without finish
/// status ("None") is counted as a race but never as a result, a retirement or a position.
/// Practice sessions are ignored.
/// </remarks>
public static class HomePresentation
{
    private const int RecentFormSize = 10;

    /// <summary>
    /// Creates the dashboard of the selected driver.
    /// </summary>
    /// <param name="sessions">The imported sessions, with driver merges applied.</param>
    /// <param name="userName">The selected driver.</param>
    public static HomeDashboard Create(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var allSessions = sessions.ToArray();
        var results = GetResults(allSessions, userName).ToArray();
        var races = results.Where(result => !result.IsQualifying).ToArray();
        var qualifyings = results.Where(result => result.IsQualifying).ToArray();
        var classified = races.Where(result => result.Kind != HomeResultKind.Unclassified).ToArray();
        var retirements = races.Where(result => result.Kind == HomeResultKind.Retired).ToArray();
        var positionsGained = classified
            .Where(result => result.Driver.ClassGridPosition.HasValue && result.Rank.HasValue)
            .Select(result => result.Driver.ClassGridPosition!.Value - result.Rank!.Value)
            .ToArray();
        var recentRaces = races.OrderByDescending(result => result.Session.StartedAt).Take(RecentFormSize).ToArray();

        return new HomeDashboard(
            Count(races, _ => true, null),
            Count(races, result => IsClassifiedIn(result, 1), races.Length),
            Count(races, result => IsClassifiedIn(result, 3), races.Length),
            Count(races, result => IsClassifiedIn(result, 5), races.Length),
            Count(races, result => IsClassifiedIn(result, 10), races.Length),
            Average(classified, result => result.Rank),
            Count(qualifyings, result => result.Rank == 1, qualifyings.Length),
            Average(qualifyings, result => result.Rank),
            Average(classified, result => result.Driver.ClassGridPosition - result.Rank),
            positionsGained.Length == 0 ? null : positionsGained.Sum(),
            Count(races, result => result.Kind == HomeResultKind.Retired, races.Length),
            Count(races, result => result.Kind == HomeResultKind.Disqualified, races.Length),
            Count(races, result => result.Kind == HomeResultKind.Unclassified, races.Length),
            CreateShares(retirements.Select(result => GetRetirementCause(result.Driver.DnfReason))),
            Sum(results, result => result.LapsCompleted),
            Sum(results, result => result.Session.TrackLength is { } meters ? result.LapsCompleted * meters / 1000 : 0),
            Sum(results, result => result.Driver.Laps.Where(lap => lap.Time.HasValue).Sum(lap => lap.Time!.Value.TotalSeconds)),
            recentRaces.Select(result => CreateFormEntry(result, null)).ToArray(),
            recentRaces.Length == 0 ? null : CreateFormEntry(recentRaces[0], FindEventId(allSessions, userName, recentRaces[0].Session)),
            FindFavorite(races, result => result.Session.TrackVenue, result => result.Session.TrackCourse),
            FindFavorite(races.Where(result => result.Car != "—"), result => result.Car, _ => null))
        {
            IsEmpty = results.Length == 0,
            FirstRaceAt = races.Length == 0 ? null : races.Min(result => result.Session.StartedAt),
            LastRaceAt = races.Length == 0 ? null : races.Max(result => result.Session.StartedAt)
        };
    }

    private static IEnumerable<SessionResult> GetResults(IEnumerable<Session> sessions, string? userName)
    {
        foreach (var session in sessions)
        {
            var isQualifying = session.SessionType.Contains("Qual", StringComparison.OrdinalIgnoreCase);
            if (!isQualifying && !session.SessionType.Contains("Race", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (DriverProfile.FindDriver(session, userName) is { } driver)
            {
                yield return new SessionResult(session, driver, isQualifying);
            }
        }
    }

    // A retirement or a disqualification keeps a class position in LMU (a lone car in its class
    // that retires after 0 laps is classified P1), but it is not a result: only finishes count.
    private static bool IsClassifiedIn(SessionResult result, int lastPosition) =>
        result.Kind is HomeResultKind.Win or HomeResultKind.Podium or HomeResultKind.Classified
        && result.Rank <= lastPosition;

    private static HomeStat Count(IReadOnlyList<SessionResult> source, Func<SessionResult, bool> predicate, int? reference) =>
        Aggregate(
            source,
            results => results.Count(predicate),
            reference is > 0 ? count => count / reference.Value : null);

    private static HomeStat Sum(IReadOnlyList<SessionResult> source, Func<SessionResult, double> selector) =>
        Aggregate(source, results => results.Sum(selector), null);

    private static HomeStat Average(IReadOnlyList<SessionResult> source, Func<SessionResult, int?> selector) =>
        Aggregate(
            source,
            results => results.Select(selector).OfType<int>().ToArray() is { Length: > 0 } values ? values.Average() : null,
            null);

    private static HomeStat Aggregate(
        IReadOnlyList<SessionResult> source,
        Func<IEnumerable<SessionResult>, double?> aggregate,
        Func<double, double>? rate)
    {
        var value = source.Count == 0 ? null : aggregate(source);
        return new HomeStat(
            value,
            value is { } total && rate is not null ? rate(total) : null,
            source
                .GroupBy(result => result.Category, StringComparer.Ordinal)
                .OrderBy(group => GetCategoryOrder(group.Key))
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new HomeCategoryValue(group.Key, aggregate(group)))
                .ToArray());
    }

    private static IReadOnlyList<HomeShare> CreateShares(IEnumerable<string> labels)
    {
        var values = labels.ToArray();
        return values
            .GroupBy(label => label, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new HomeShare(group.Key, group.Count(), (double)group.Count() / values.Length))
            .ToArray();
    }

    private static HomeFavorite? FindFavorite(
        IEnumerable<SessionResult> races,
        Func<SessionResult, string> name,
        Func<SessionResult, string?> detail)
    {
        // On a tie, the most recently driven wins: it reflects the driver's current habits.
        var favorite = races
            .GroupBy(result => (Name: name(result).Trim(), Detail: detail(result)?.Trim()), VenueAndCourseComparer.Instance)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Max(result => result.Session.StartedAt))
            .FirstOrDefault();
        if (favorite is null)
        {
            return null;
        }

        var (favoriteName, favoriteDetail) = favorite.Key;
        return new HomeFavorite(
            favoriteName,
            string.Equals(favoriteName, favoriteDetail, StringComparison.OrdinalIgnoreCase) ? null : favoriteDetail,
            favorite.Count(),
            favorite.Count(result => IsClassifiedIn(result, 1)),
            favorite.Count(result => IsClassifiedIn(result, 3)));
    }

    private static HomeFormEntry CreateFormEntry(SessionResult result, string? eventId) =>
        new(
            eventId,
            result.Session.StartedAt,
            result.Session.TrackVenue,
            result.Session.TrackCourse,
            result.Category,
            result.Car,
            result.Rank,
            result.Kind,
            result.Driver.Laps.Where(lap => lap.Time.HasValue).Min(lap => lap.Time));

    private static string? FindEventId(IEnumerable<Session> sessions, string? userName, Session race) =>
        RaceEventPresentation.CreateEvents(sessions, userName)
            .FirstOrDefault(raceEvent => raceEvent.Race?.Id == race.Id)
            ?.Id;

    private static string GetRetirementCause(string dnfReason) =>
        dnfReason.Trim() switch
        {
            "" => "Non précisée",
            var reason when reason.Equals("DNF", StringComparison.OrdinalIgnoreCase) => "Non précisée",
            var reason when reason.Equals("Accident", StringComparison.OrdinalIgnoreCase) => "Accident",
            var reason when reason.Equals("Engine", StringComparison.OrdinalIgnoreCase) => "Moteur",
            var reason when reason.Equals("Suspension", StringComparison.OrdinalIgnoreCase) => "Suspension",
            var reason when reason.Equals("Fuel", StringComparison.OrdinalIgnoreCase) => "Carburant",
            var reason => reason
        };

    private static int GetCategoryOrder(string category)
    {
        var index = CarCategory.Known.ToList().IndexOf(category);
        return index < 0 ? CarCategory.Known.Count : index;
    }

    private sealed record SessionResult(Session Session, Driver Driver, bool IsQualifying)
    {
        public string Category { get; } = CarCategory.FromCarClass(Driver.CarClass);

        public string Car { get; } = !string.IsNullOrWhiteSpace(Driver.CarType)
            ? Driver.CarType.Trim()
            : string.IsNullOrWhiteSpace(Driver.CarNumber) ? "—" : $"#{Driver.CarNumber}";

        /// <summary>
        /// The class position; the overall position keeps the same order within a class, so it is a valid fallback.
        /// </summary>
        public int? Rank { get; } = Driver.ClassPosition ?? Driver.Position;

        public double LapsCompleted { get; } = Driver.LapsCompleted ?? Driver.Laps.Count;

        public HomeResultKind Kind => RaceResults.GetKind(Driver.FinishStatus, Rank);
    }

    private sealed class VenueAndCourseComparer : IEqualityComparer<(string Name, string? Detail)>
    {
        public static VenueAndCourseComparer Instance { get; } = new();

        public bool Equals((string Name, string? Detail) x, (string Name, string? Detail) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name)
            && StringComparer.OrdinalIgnoreCase.Equals(x.Detail ?? string.Empty, y.Detail ?? string.Empty);

        public int GetHashCode((string Name, string? Detail) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Detail ?? string.Empty));
    }
}
