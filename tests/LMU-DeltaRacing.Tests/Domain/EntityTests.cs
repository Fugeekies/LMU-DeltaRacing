using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Tests.Domain;

/// <summary>
/// Verifies the validation and the derived values of the domain entities.
/// </summary>
[TestClass]
public sealed class EntityTests
{
    /// <summary>
    /// Verifies that the session kind is read from the phase name written by LMU.
    /// </summary>
    [TestMethod]
    [DataRow("Practice1", SessionKind.Practice)]
    [DataRow("practice3", SessionKind.Practice)]
    [DataRow("Qualify", SessionKind.Qualifying)]
    [DataRow("Qualifying2", SessionKind.Qualifying)]
    [DataRow("Race", SessionKind.Race)]
    [DataRow("Race2", SessionKind.Race)]
    [DataRow("Warmup", SessionKind.Other)]
    public void Session_kind_is_read_from_the_session_type(string sessionType, SessionKind expected)
    {
        CreateSession(sessionType).Kind.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that the best lap is the fastest timed lap, and is unknown without timed lap.
    /// </summary>
    [TestMethod]
    public void Driver_best_lap_is_the_fastest_timed_lap()
    {
        CreateDriver(new Lap(1, null, []), new Lap(2, TimeSpan.FromSeconds(91), []), new Lap(3, TimeSpan.FromSeconds(90.5), []))
            .BestLap.Should().Be(TimeSpan.FromSeconds(90.5));
        CreateDriver(new Lap(1, null, [])).BestLap.Should().BeNull();
        CreateDriver().BestLap.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the collections given to the entities are copied, so the entities stay immutable.
    /// </summary>
    [TestMethod]
    public void Entities_copy_their_collections()
    {
        var laps = new List<Lap> { new(1, TimeSpan.FromSeconds(90), [new Sector(1, TimeSpan.FromSeconds(30))]) };
        var driver = CreateDriver([.. laps]);
        var drivers = new List<Driver> { driver };
        var session = new Session(Guid.NewGuid(), DateTimeOffset.UnixEpoch, "Spa", "Spa", "Spa", "Race", drivers);

        drivers.Clear();
        laps.Clear();

        session.Drivers.Should().ContainSingle();
        driver.Laps.Should().ContainSingle();
    }

    /// <summary>
    /// Verifies that invalid values are rejected by the constructors.
    /// </summary>
    [TestMethod]
    public void Constructors_reject_invalid_values()
    {
        FluentActions.Invoking(() => new Lap(0, null, [])).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new Lap(1, null, null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new Driver(" ", "", "", "", 1, [])).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new Driver("A", null!, "", "", 1, [])).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => CreateSession(" ")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new Session(Guid.NewGuid(), DateTimeOffset.UnixEpoch, "", "Spa", "Spa", "Race", []))
            .Should().Throw<ArgumentException>();
    }

    private static Session CreateSession(string sessionType) =>
        new(Guid.NewGuid(), DateTimeOffset.UnixEpoch, "Spa", "Spa", "Spa", sessionType, []);

    private static Driver CreateDriver(params Lap[] laps) => new("Driver A", "7", "GT3", "Team", 1, laps);
}
