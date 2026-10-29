using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Defines stable presentation ordering for dashboard sessions.
/// </summary>
public static class DashboardPresentation
{
    /// <summary>
    /// Orders sessions from most recent to oldest without changing the source collection.
    /// </summary>
    /// <remarks>
    /// The sessions of one event share the event time in LMU files: the race, run last, comes
    /// first, so that the order does not depend on the order in which the files were loaded.
    /// </remarks>
    /// <param name="sessions">The sessions to display.</param>
    /// <returns>The sessions ordered by start time.</returns>
    public static IReadOnlyList<Session> OrderSessions(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        return sessions
            .OrderByDescending(session => session.StartedAt)
            .ThenByDescending(session => session.Kind)
            .ToArray();
    }
}
