using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Localization;

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
    /// Sessions of the same second may come from a file without phase date: the race, run last,
    /// comes first and the identifier settles the rest, so that the order does not depend on the
    /// order in which the files were loaded.
    /// </remarks>
    /// <param name="sessions">The sessions to display.</param>
    /// <returns>The sessions ordered by start time.</returns>
    public static IReadOnlyList<Session> OrderSessions(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        return sessions
            .OrderByDescending(session => session.StartedAt)
            .ThenByDescending(session => session.Kind)
            .ThenBy(session => session.Id)
            .ToArray();
    }

    /// <summary>
    /// Formats the number of files ignored because their session was already imported.
    /// </summary>
    /// <param name="count">The number of ignored files.</param>
    public static string DuplicateLabel(int count) =>
        Texts.Plural("Overview.Duplicates", count);
}
