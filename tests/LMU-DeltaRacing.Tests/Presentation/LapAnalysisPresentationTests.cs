using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the lap by lap analysis of a race: regularity, stints, fuel, energy and tyre wear.
/// </summary>
[TestClass]
public sealed class LapAnalysisPresentationTests
{
    private const string Player = "Player";

    /// <summary>
    /// Verifies the analysis of a real race with a stop: stints split at the lap entering the pits,
    /// pit stops read from LMU, slow laps left out of the regularity.
    /// </summary>
    [TestMethod]
    public void Create_analyses_a_real_race_with_a_pit_stop()
    {
        var analysis = LapAnalysisPresentation.Create(ParseFixture("2025_02_25_21_06_15-40R1.xml"), "Driver Ugo")!;

        analysis.PitStops.Should().Be(1);
        analysis.Stints.Select(stint => (stint.FirstLap, stint.LastLap)).Should().Equal((1, 2), (3, 11));
        analysis.Stints[1].Compounds.Should().Equal("Medium");

        // Laps 4 to 11 are timed and clean; 125.57 s and 108.19 s are slower than 107 % of their median.
        var regularity = analysis.Regularity!;
        regularity.Laps.Should().Be(6);
        regularity.ExcludedLaps.Should().Be(2);
        regularity.Pace.Should().Be(TimeSpan.FromSeconds((99.9380 + 100.1166) / 2));
        regularity.ClassRank.Should().BeInRange(1, regularity.ClassDrivers);
        analysis.Laps.Where(lap => lap.IsRepresentative).Select(lap => lap.Number).Should().Equal(5, 6, 7, 8, 9, 11);

        var consumption = analysis.Consumption!;
        consumption.FuelPerLap.Should().BeApproximately(0.0245, 0.00001);
        consumption.LapsPerTank.Should().BeApproximately(1 / 0.0245, 0.001);
        consumption.EnergyPerLap.Should().NotBeNull();
        consumption.TyreWearPerLap!.FrontLeft.Should().BeGreaterThan(0);
        analysis.TopSpeed!.Speed.Should().Be(263.06);
        analysis.TopSpeed.ClassBest.Should().BeGreaterThanOrEqualTo(263.06);
    }

    /// <summary>
    /// Verifies that a server hiding fuel data gives no consumption, while the regularity and the
    /// top speed remain.
    /// </summary>
    [TestMethod]
    public void Create_gives_no_consumption_when_the_server_hides_fuel_data()
    {
        var session = ParseFixture("2026_10_02_19_24_03-96R1.xml");
        var driver = session.Drivers.First(item => item.Laps.Count > 5);

        var analysis = LapAnalysisPresentation.Create(session, driver.Name)!;

        analysis.Consumption.Should().BeNull();
        analysis.TopSpeed.Should().NotBeNull();
        analysis.Stints.Should().OnlyContain(stint => stint.FuelPerLap == null && stint.TyreWear == null);
    }

    /// <summary>
    /// Verifies that a start from the pit lane does not end a stint, and that each later run of pit
    /// laps ends one on its first lap.
    /// </summary>
    [TestMethod]
    public void Create_splits_stints_at_the_lap_entering_the_pits()
    {
        var session = CreateRace(
            Lap(1, 110, pit: true),
            Lap(2, 101, pit: true),
            Lap(3, 100),
            Lap(4, 100),
            Lap(5, 104, pit: true),
            Lap(6, 130, pit: true),
            Lap(7, 100),
            Lap(8, 120, pit: true));

        var analysis = LapAnalysisPresentation.Create(session, Player)!;

        analysis.Laps.Select(lap => lap.Stint).Should().Equal(1, 1, 1, 1, 1, 2, 2, 2);
    }

    /// <summary>
    /// Verifies the regularity: standard deviation of the representative laps and rank in the class,
    /// and none with fewer than three representative laps.
    /// </summary>
    [TestMethod]
    public void Create_measures_the_regularity_against_the_class()
    {
        var regular = new Driver("Regular", "2", "GT3", "Team", 2, [Lap(1, 120), Lap(2, 100), Lap(3, 100), Lap(4, 100)]);
        var otherClass = new Driver("Other", "3", "LMP2", "Team", 3, [Lap(1, 120), Lap(2, 90), Lap(3, 90), Lap(4, 90)]);
        var session = CreateRace([Lap(1, 130), Lap(2, 99), Lap(3, 101), Lap(4, 100), Lap(5, 102)], regular, otherClass);

        var regularity = LapAnalysisPresentation.Create(session, Player)!.Regularity!;

        regularity.Laps.Should().Be(4);
        regularity.Deviation.TotalSeconds.Should().BeApproximately(Math.Sqrt(1.25), 0.0001);
        regularity.Pace.Should().Be(TimeSpan.FromSeconds(100.5));
        regularity.AverageGapToBest.Should().Be(TimeSpan.FromSeconds(1.5));
        regularity.ClassRank.Should().Be(2);
        regularity.ClassDrivers.Should().Be(2);

        LapAnalysisPresentation.Create(CreateRace(Lap(1, 130), Lap(2, 99), Lap(3, 101)), Player)!.Regularity.Should().BeNull();
    }

    /// <summary>
    /// Verifies that only races are analysed.
    /// </summary>
    [TestMethod]
    public void Create_returns_nothing_outside_a_race()
    {
        LapAnalysisPresentation.Create(CreateRace("Qualify", [Lap(1, 100)]), Player).Should().BeNull();
        LapAnalysisPresentation.Create(CreateRace(Lap(1, 100)), "Unknown").Should().BeNull();
    }

    /// <summary>
    /// Verifies the races offered by the page: races of the selected driver with laps, most recent
    /// first; and their drivers with laps, in finishing order.
    /// </summary>
    [TestMethod]
    public void GetRaces_offers_the_races_of_the_selected_driver_most_recent_first()
    {
        var older = CreateRace(Lap(1, 100));
        var newer = new Session(
            Guid.NewGuid(),
            older.StartedAt.AddDays(1),
            "Track",
            "Track",
            "Event",
            "Race",
            [new Driver("Second", "2", "GT3", "Team", 2, [Lap(1, 101)]), new Driver(Player, "1", "GT3", "Team", 1, [Lap(1, 100)]), new Driver("Idle", "3", "GT3", "Team", 3, [])]);
        var qualifying = CreateRace("Qualify", [Lap(1, 100)]);
        var withoutLaps = CreateRace();

        LapAnalysisPresentation.GetRaces([older, qualifying, newer, withoutLaps], Player).Should().Equal(newer, older);
        LapAnalysisPresentation.GetDrivers(newer).Select(driver => driver.Name).Should().Equal(Player, "Second");
    }

    /// <summary>
    /// Verifies the compounds label: one name when front and rear match.
    /// </summary>
    [TestMethod]
    public void FormatCompounds_names_front_and_rear_when_they_differ()
    {
        LapAnalysisPresentation.FormatCompounds(new LapTelemetry(FrontCompound: "Soft", RearCompound: "Soft")).Should().Be("Soft");
        LapAnalysisPresentation.FormatCompounds(new LapTelemetry(FrontCompound: "Hard", RearCompound: "Soft")).Should().Be("Hard / Soft");
        LapAnalysisPresentation.FormatCompounds(LapTelemetry.Empty).Should().BeEmpty();
    }

    private static Lap Lap(int number, double seconds, bool pit = false) =>
        new(number, TimeSpan.FromSeconds(seconds), [], position: 1, isPitLap: pit);

    private static Session CreateRace(params Lap[] laps) => CreateRace("Race", laps);

    private static Session CreateRace(Lap[] laps, params Driver[] others) => CreateRace("Race", laps, others);

    private static Session CreateRace(string type, Lap[] laps, params Driver[] others) => new(
        Guid.NewGuid(),
        DateTimeOffset.FromUnixTimeSeconds(1760000000),
        "Track",
        "Track",
        "Event",
        type,
        [new Driver(Player, "1", "GT3", "Team", 1, laps), .. others]);

    private static Session ParseFixture(string fileName)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "Results", fileName));
        return new XmlResultParser().Parse(stream);
    }
}
