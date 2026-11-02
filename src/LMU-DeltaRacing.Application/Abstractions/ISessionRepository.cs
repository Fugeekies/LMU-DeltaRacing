using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Application.Abstractions;

/// <summary>
/// Stores and retrieves parsed sessions.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Stores a session, unless a session with the same identifier is already stored.
    /// </summary>
    /// <param name="session">The session to store.</param>
    /// <returns><see langword="true"/> when the session was stored; <see langword="false"/> when it is a duplicate.</returns>
    bool TryAdd(Session session);

    /// <summary>
    /// Gets a snapshot of all stored sessions.
    /// </summary>
    /// <returns>All stored sessions.</returns>
    IReadOnlyCollection<Session> GetAll();

    /// <summary>
    /// Removes all sessions from the repository.
    /// </summary>
    void Clear();

    /// <summary>
    /// Finds a stored session by its identifier.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <returns>The session, or <see langword="null"/> when it is not stored.</returns>
    Session? GetById(Guid id);
}
