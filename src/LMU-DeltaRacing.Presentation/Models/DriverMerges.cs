using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// A driver name declared as another name of the same person (for example after a name change).
/// </summary>
/// <param name="From">The name that is replaced in the views.</param>
/// <param name="To">The name kept in the views.</param>
public sealed record DriverMerge(string From, string To);

/// <summary>
/// Merges driver names in memory, so a driver who changed name keeps a single history.
/// The result files are never modified: removing a merge restores the original names.
/// </summary>
public static class DriverMerges
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Returns the sessions with merged names replaced by the kept name. Sessions without merged
    /// drivers are returned unchanged; laps and results are shared, not copied.
    /// </summary>
    public static IReadOnlyList<Session> Apply(IEnumerable<Session> sessions, IReadOnlyCollection<DriverMerge> merges)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(merges);
        if (merges.Count == 0)
        {
            return sessions.ToArray();
        }

        var keptNames = ToDictionary(merges);
        return sessions
            .Select(session => session.Drivers.Any(driver => keptNames.ContainsKey(driver.Name.Trim()))
                ? new Session(
                    session.Id,
                    session.StartedAt,
                    session.TrackVenue,
                    session.TrackCourse,
                    session.TrackEvent,
                    session.SessionType,
                    session.Drivers.Select(driver => keptNames.TryGetValue(driver.Name.Trim(), out var keptName)
                        ? Rename(driver, keptName)
                        : driver),
                    session.MostLapsCompleted,
                    session.TrackLength)
                : session)
            .ToArray();
    }

    /// <summary>
    /// Returns the name kept for a driver, or the name itself when it is not merged.
    /// </summary>
    public static string? Resolve(string? name, IReadOnlyCollection<DriverMerge> merges)
    {
        ArgumentNullException.ThrowIfNull(merges);
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return ToDictionary(merges).TryGetValue(name.Trim(), out var keptName) ? keptName : name;
    }

    /// <summary>
    /// Adds a merge and keeps the list consistent: every name points directly to a kept name,
    /// and merging a kept name into one of its merged names reverses the previous merges.
    /// </summary>
    public static IReadOnlyList<DriverMerge> Add(IReadOnlyCollection<DriverMerge> merges, string from, string to)
    {
        ArgumentNullException.ThrowIfNull(merges);
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);

        from = from.Trim();
        to = to.Trim();

        // Merging a kept name into one of its merged names reverses the merge: "to" becomes the
        // kept name. Otherwise the target is followed to its kept name, to avoid chains.
        var resolvedTo = Resolve(to, merges)!;
        var target = NameComparer.Equals(resolvedTo, from) ? to : resolvedTo;
        if (NameComparer.Equals(from, target))
        {
            throw new ArgumentException("A driver cannot be merged into itself.", nameof(to));
        }

        return merges
            .Where(merge => !NameComparer.Equals(merge.From, from) && !NameComparer.Equals(merge.From, target))
            .Select(merge => NameComparer.Equals(merge.To, from) ? merge with { To = target } : merge)
            .Append(new DriverMerge(from, target))
            .ToArray();
    }

    /// <summary>
    /// Cancels the merge of a name.
    /// </summary>
    public static IReadOnlyList<DriverMerge> Remove(IReadOnlyCollection<DriverMerge> merges, string from)
    {
        ArgumentNullException.ThrowIfNull(merges);
        ArgumentException.ThrowIfNullOrWhiteSpace(from);

        return merges.Where(merge => !NameComparer.Equals(merge.From, from.Trim())).ToArray();
    }

    /// <summary>
    /// Counts the sessions in which both names appear. One person cannot drive twice in the same
    /// session, so a non-zero count means the two names probably belong to different people.
    /// </summary>
    public static int CountSharedSessions(IEnumerable<Session> sessions, string firstName, string secondName) =>
        CountSharedSessions(sessions, [firstName], [secondName]);

    /// <summary>
    /// Counts the sessions in which a name of the first group and a name of the second group appear.
    /// </summary>
    public static int CountSharedSessions(
        IEnumerable<Session> sessions,
        IReadOnlyCollection<string> firstNames,
        IReadOnlyCollection<string> secondNames)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(firstNames);
        ArgumentNullException.ThrowIfNull(secondNames);

        var first = new HashSet<string>(firstNames.Select(name => name.Trim()), NameComparer);
        var second = new HashSet<string>(secondNames.Select(name => name.Trim()), NameComparer);
        return sessions.Count(session =>
            session.Drivers.Any(driver => first.Contains(driver.Name.Trim()))
            && session.Drivers.Any(driver => second.Contains(driver.Name.Trim())));
    }

    /// <summary>
    /// Counts the original sessions shared by two drivers, taking the saved merges into account:
    /// a name already merged is compared alone with the other names of its kept name, and a kept
    /// name brings all the names merged into it.
    /// </summary>
    public static int CountSharedSessions(
        IEnumerable<Session> sessions,
        string firstName,
        string secondName,
        IReadOnlyCollection<DriverMerge> merges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(secondName);
        ArgumentNullException.ThrowIfNull(merges);

        IReadOnlyList<string> NamesOf(string name) =>
            merges.Any(merge => NameComparer.Equals(merge.From, name.Trim())) ? [name.Trim()] : GetNames(name, merges);

        var firstNames = NamesOf(firstName);
        var secondNames = NamesOf(secondName)
            .Where(name => !firstNames.Contains(name, NameComparer))
            .ToArray();
        return CountSharedSessions(sessions, firstNames, secondNames);
    }

    /// <summary>
    /// Returns every original name of a driver: the kept name and the names merged into it.
    /// </summary>
    public static IReadOnlyList<string> GetNames(string name, IReadOnlyCollection<DriverMerge> merges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(merges);

        var keptName = Resolve(name.Trim(), merges)!;
        return merges
            .Where(merge => NameComparer.Equals(merge.To, keptName))
            .Select(merge => merge.From)
            .Prepend(keptName)
            .Distinct(NameComparer)
            .ToArray();
    }

    private static Dictionary<string, string> ToDictionary(IEnumerable<DriverMerge> merges)
    {
        var keptNames = new Dictionary<string, string>(NameComparer);
        foreach (var merge in merges)
        {
            keptNames[merge.From.Trim()] = merge.To.Trim();
        }

        return keptNames;
    }

    private static Driver Rename(Driver driver, string name) =>
        new(
            name,
            driver.CarNumber,
            driver.CarClass,
            driver.TeamName,
            driver.Position,
            driver.Laps,
            driver.CarType,
            driver.GridPosition,
            driver.FinishTime,
            driver.ClassPosition,
            driver.LapsCompleted,
            driver.FinishStatus,
            driver.ClassGridPosition,
            driver.DnfReason);
}
