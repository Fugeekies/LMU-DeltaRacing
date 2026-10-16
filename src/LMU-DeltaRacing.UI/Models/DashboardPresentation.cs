using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Defines stable presentation ordering for dashboard results.
/// </summary>
public static class DashboardPresentation
{
    /// <summary>
    /// Orders sessions from most recent to oldest without changing the source collection.
    /// </summary>
    /// <param name="sessions">The sessions to display.</param>
    /// <returns>The sessions ordered by start time.</returns>
    public static IReadOnlyList<Session> OrderSessions(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        return sessions.OrderByDescending(session => session.StartedAt).ToArray();
    }

    /// <summary>
    /// Orders drivers by finishing position, then by display name; missing positions appear last.
    /// </summary>
    /// <param name="session">The session to classify.</param>
    /// <returns>The classified drivers.</returns>
    public static IReadOnlyList<Driver> GetClassification(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Drivers
            .OrderBy(driver => driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
