using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Persistence;

namespace LMU_DeltaRacing.Tests.Infrastructure;

/// <summary>
/// Verifies storage and retrieval behavior of the in-memory session repository.
/// </summary>
[TestClass]
public sealed class InMemorySessionRepositoryTests
{
    /// <summary>
    /// Verifies that a saved session is available by identifier and in the full list.
    /// </summary>
    [TestMethod]
    public void Save_makes_a_session_available_by_id_and_in_the_session_list()
    {
        var repository = new InMemorySessionRepository();
        var session = new Session(
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch,
            "Circuit",
            "Layout",
            "Event",
            "Race",
            []);

        repository.Save(session);

        repository.GetById(session.Id).Should().BeSameAs(session);
        repository.GetAll().Should().ContainSingle().Which.Should().BeSameAs(session);
    }

    /// <summary>
    /// Verifies that clearing the repository removes all sessions and identifiers.
    /// </summary>
    [TestMethod]
    public void Clear_removes_all_sessions()
    {
        var repository = new InMemorySessionRepository();
        var session = new Session(
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch,
            "Circuit",
            "Layout",
            "Event",
            "Race",
            []);
        repository.Save(session);

        repository.Clear();

        repository.GetAll().Should().BeEmpty();
        repository.GetById(session.Id).Should().BeNull();
    }
}
