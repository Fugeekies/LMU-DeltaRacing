using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Persistence;

namespace LMU_DeltaRacing.Tests.Infrastructure;

public sealed class InMemorySessionRepositoryTests
{
    [Fact]
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
}
