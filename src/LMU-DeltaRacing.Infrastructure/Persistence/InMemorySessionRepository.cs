using System.Collections.Concurrent;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Infrastructure.Persistence;

/// <summary>
/// Stores sessions in memory for the current application process.
/// </summary>
public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<Guid, Session> sessions = new();

    /// <inheritdoc />
    public void Save(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        sessions[session.Id] = session;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Session> GetAll() => sessions.Values.ToArray();

    /// <inheritdoc />
    public void Clear() => sessions.Clear();

    /// <inheritdoc />
    public Session? GetById(Guid id) => sessions.GetValueOrDefault(id);
}
