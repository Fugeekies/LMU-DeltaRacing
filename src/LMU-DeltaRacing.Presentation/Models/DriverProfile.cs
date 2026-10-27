using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Finds and filters imported results for a selected driver.
/// </summary>
public static class DriverProfile
{
    /// <summary>
    /// Calculates each driver's share of imported result files.
    /// A driver is counted at most once in any single result file.
    /// </summary>
    public static IReadOnlyList<DriverFrequency> CalculateFrequencies(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var results = sessions.ToArray();
        if (results.Length == 0)
        {
            return [];
        }

        var counts = new Dictionary<string, (string Name, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var session in results)
        {
            foreach (var name in session.Drivers
                .Select(driver => driver.Name.Trim())
                .Where(name => name.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (counts.TryGetValue(name, out var current))
                {
                    counts[name] = (current.Name, current.Count + 1);
                }
                else
                {
                    counts.Add(name, (name, 1));
                }
            }
        }

        return counts
            .Select(entry => new DriverFrequency(entry.Value.Name, entry.Value.Count, results.Length))
            .OrderByDescending(driver => driver.AppearanceCount)
            .ThenBy(driver => driver.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(driver => driver.Name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Finds the selected driver's result in a session, if present.
    /// </summary>
    public static Driver? FindDriver(Session session, string? userName)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return session.Drivers.FirstOrDefault(driver =>
            string.Equals(driver.Name.Trim(), userName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Keeps only sessions containing the selected driver.
    /// </summary>
    public static IReadOnlyList<Session> FilterSessions(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        if (string.IsNullOrWhiteSpace(userName))
        {
            return [];
        }

        return sessions
            .Where(session => FindDriver(session, userName) is not null)
            .ToArray();
    }
}
