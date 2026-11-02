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
    /// Verifies that a stored session is available by identifier and in the full list.
    /// </summary>
    [TestMethod]
    public void TryAdd_makes_a_session_available_by_id_and_in_the_session_list()
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

        repository.TryAdd(session).Should().BeTrue();

        repository.GetById(session.Id).Should().BeSameAs(session);
        repository.GetAll().Should().ContainSingle().Which.Should().BeSameAs(session);
    }

    /// <summary>
    /// Verifies that a second session with the same identifier does not replace the first one.
    /// </summary>
    [TestMethod]
    public void TryAdd_keeps_the_first_session_of_an_identifier()
    {
        var repository = new InMemorySessionRepository();
        var first = new Session(Guid.NewGuid(), DateTimeOffset.UnixEpoch, "Circuit", "Layout", "Event", "Race", []);
        var duplicate = new Session(first.Id, DateTimeOffset.UnixEpoch, "Circuit", "Layout", "Event", "Race", []);
        repository.TryAdd(first);

        repository.TryAdd(duplicate).Should().BeFalse();

        repository.GetAll().Should().ContainSingle().Which.Should().BeSameAs(first);
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
        repository.TryAdd(session);

        repository.Clear();

        repository.GetAll().Should().BeEmpty();
        repository.GetById(session.Id).Should().BeNull();
    }
}
