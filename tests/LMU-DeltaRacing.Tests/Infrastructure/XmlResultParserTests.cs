using System.Xml;
using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;

namespace LMU_DeltaRacing.Tests.Infrastructure;

/// <summary>
/// Verifies parsing of LMU result XML documents.
/// </summary>
[TestClass]
public sealed class XmlResultParserTests
{
    private const string FixturePath = "assets/Results/2025_09_20_19_03_47-43P1.xml";

    /// <summary>
    /// Verifies that an anonymized package fixture populates session, driver, lap, and sector data.
    /// </summary>
    [TestMethod]
    public void Parse_reads_session_metadata_drivers_and_laps_from_a_real_result_file()
    {
        var parser = new XmlResultParser();
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, FixturePath));

        var session = parser.Parse(stream);

        session.SessionType.Should().Be("Practice1");
        session.TrackVenue.Should().Be("Sebring International Raceway");
        session.TrackCourse.Should().Be("Sebring School Circuit");
        session.Drivers.Should().HaveCount(21);

        var driver = session.Drivers.Should()
            .ContainSingle(item => item.CarNumber == "16" && item.Position == 1)
            .Which;
        driver.Name.Should().MatchRegex(@"^Driver \p{Lu}\p{L}+( \d+)?$");
        driver.CarNumber.Should().Be("16");
        driver.CarType.Should().Be("Corvette C8.R GTE");
        driver.Position.Should().Be(1);
        driver.Laps.Should().HaveCount(2);
        driver.Laps[0].Time.Should().BeNull();
        driver.Laps[1].Time.Should().Be(TimeSpan.FromSeconds(65.1692));
        driver.Laps[1].Sectors.Should().BeEquivalentTo(
        [
            new Sector(1, TimeSpan.FromSeconds(22.8031)),
            new Sector(2, TimeSpan.FromSeconds(12.8257)),
            new Sector(3, TimeSpan.FromSeconds(29.5404))
        ]);
    }

    /// <summary>
    /// Verifies the class grid position, the retirement cause and the track length used by the home dashboard.
    /// </summary>
    [TestMethod]
    public void Parse_reads_class_grid_position_dnf_reason_and_track_length_from_a_real_race()
    {
        var parser = new XmlResultParser();
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets/Results/2025_02_24_19_37_55-00R1.xml"));

        var session = parser.Parse(stream);

        session.TrackLength.Should().Be(4635.3);
        var retired = session.Drivers.Should().ContainSingle(driver => driver.Name == "Driver Chloé").Which;
        retired.ClassGridPosition.Should().Be(2);
        retired.FinishStatus.Should().Be("DNF");
        retired.DnfReason.Should().Be("Suspension");
        var winner = session.Drivers.Should().ContainSingle(driver => driver.Name == "Driver Inès").Which;
        winner.ClassGridPosition.Should().Be(1);
        winner.DnfReason.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the game mode of the result file is read: "Race Weekend" marks an offline
    /// race against the AI, which the import can leave out.
    /// </summary>
    [TestMethod]
    [DataRow("2026_09_16_15_17_11-40R1.xml", "Race Weekend", true)]
    [DataRow("2025_02_24_19_37_55-00R1.xml", "Multiplayer", false)]
    public void Parse_reads_the_setting_of_a_real_result_file(string fileName, string setting, bool isRaceWeekend)
    {
        var parser = new XmlResultParser();
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "Results", fileName));

        var session = parser.Parse(stream);

        session.Setting.Should().Be(setting);
        session.IsRaceWeekend.Should().Be(isRaceWeekend);
    }

    [TestMethod]
    public void Parse_reads_grid_position_from_driver_result()
    {
        const string xml = """
            <rFactorXML>
              <RaceResults>
                <DateTime>1790967271</DateTime>
                <TrackVenue>Track</TrackVenue>
                <TrackCourse>Course</TrackCourse>
                <Race>
                  <MostLapsCompleted>8</MostLapsCompleted>
                  <Driver>
                    <Name>Player</Name>
                    <Position>1</Position>
                    <GridPos>2</GridPos>
                    <FinishTime>1326.8575</FinishTime>
                  </Driver>
                </Race>
              </RaceResults>
            </rFactorXML>
            """;
        var parser = new XmlResultParser();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

        var session = parser.Parse(stream);

        var driver = session.Drivers.Should().ContainSingle().Which;
        driver.GridPosition.Should().Be(2);
        driver.FinishTime.Should().Be(TimeSpan.FromSeconds(1326.8575));
        session.MostLapsCompleted.Should().Be(8);
    }

    /// <summary>
    /// Verifies that the class classification, completed laps and finish status are read, because
    /// multiclass comparisons and the gap to the class leader depend on them.
    /// </summary>
    [TestMethod]
    public void Parse_reads_class_position_completed_laps_and_finish_status()
    {
        const string xml = """
            <rFactorXML>
              <RaceResults>
                <DateTime>1790967271</DateTime>
                <TrackVenue>Track</TrackVenue>
                <TrackCourse>Course</TrackCourse>
                <Race>
                  <Driver>
                    <Name>Player</Name>
                    <CarClass>GT3</CarClass>
                    <Position>7</Position>
                    <ClassPosition>2</ClassPosition>
                    <Laps>11</Laps>
                    <FinishStatus>Finished Normally</FinishStatus>
                  </Driver>
                  <Driver>
                    <Name>Retired</Name>
                    <Position>8</Position>
                    <FinishStatus>DNF</FinishStatus>
                  </Driver>
                </Race>
              </RaceResults>
            </rFactorXML>
            """;
        var parser = new XmlResultParser();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

        var session = parser.Parse(stream);

        var player = session.Drivers.Should().ContainSingle(driver => driver.Name == "Player").Which;
        player.ClassPosition.Should().Be(2);
        player.LapsCompleted.Should().Be(11);
        player.FinishStatus.Should().Be("Finished Normally");
        var retired = session.Drivers.Should().ContainSingle(driver => driver.Name == "Retired").Which;
        retired.ClassPosition.Should().BeNull();
        retired.LapsCompleted.Should().BeNull();
        retired.FinishStatus.Should().Be("DNF");
    }

    /// <summary>
    /// Verifies that malformed XML is reported to the caller.
    /// </summary>
    [TestMethod]
    public void Parse_rejects_malformed_xml()
    {
        var parser = new XmlResultParser();
        using var stream = new MemoryStream("<rFactorXML>"u8.ToArray());

        var act = () => parser.Parse(stream);

        act.Should().Throw<XmlException>();
    }

    /// <summary>
    /// Locks the parsing of every real fixture: a SHA-256 of all the values read (session, drivers,
    /// laps and sectors) must match the one computed when this test was written. Any change of
    /// what the parser reads, even on one lap of one file, changes the fingerprint.
    /// </summary>
    [TestMethod]
    public void Parse_reads_every_fixture_as_the_reference_snapshot()
    {
        var parser = new XmlResultParser();
        var files = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Order(StringComparer.Ordinal)
            .ToArray();
        var snapshot = new System.Text.StringBuilder();

        foreach (var file in files)
        {
            using var stream = File.OpenRead(file);
            var session = parser.Parse(stream);
            snapshot.AppendLine(string.Join(
                '|',
                Path.GetFileName(file),
                session.StartedAt.ToUnixTimeSeconds(),
                session.TrackVenue,
                session.TrackCourse,
                session.TrackEvent,
                session.SessionType,
                session.MostLapsCompleted,
                session.TrackLength?.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                session.Setting));
            foreach (var driver in session.Drivers)
            {
                snapshot.AppendLine(string.Join(
                    '|',
                    driver.Name,
                    driver.CarNumber,
                    driver.CarClass,
                    driver.TeamName,
                    driver.CarType,
                    driver.Position,
                    driver.ClassPosition,
                    driver.GridPosition,
                    driver.ClassGridPosition,
                    driver.LapsCompleted,
                    driver.FinishTime?.Ticks,
                    driver.FinishStatus,
                    driver.DnfReason,
                    driver.PitStops));
                foreach (var lap in driver.Laps)
                {
                    snapshot.AppendLine($"{lap.Number}:{lap.Time?.Ticks}:{string.Join(',', lap.Sectors.Select(sector => $"{sector.Number}={sector.Time.Ticks}"))}:{lap.Position}:{lap.StartedAt?.Ticks}:{lap.IsPitLap}:{FormatTelemetry(lap.Telemetry)}");
                }
            }

            foreach (var sessionEvent in session.Events)
            {
                snapshot.AppendLine($"{sessionEvent.Elapsed.Ticks}|{sessionEvent.Kind}|{sessionEvent.DriverName}|{sessionEvent.Detail}|{sessionEvent.Lap}");
            }
        }

        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(snapshot.ToString())));

        files.Should().HaveCount(342);
        hash.Should().Be(ReferenceSnapshotHash, "the fingerprint of the parsed fixtures is now {0}", hash);
    }

    // Computed with the XDocument parser of version 0.1 on the fixtures of assets/Results, then on the
    // date of each phase (with RaceResults/DateTime instead, the fingerprint of version 0.1 is unchanged),
    // then with the session events added, then with the lap positions, lap starts, pit stops and damage
    // reports, then with the lap telemetry and the pit stop count, then with the session setting:
    // without each of these additions, the previous fingerprint is unchanged.
    private const string ReferenceSnapshotHash = "CB819D4FE53EFC124499459A49490B36F42679D2F62149384A5FAF08B020DBF8";

    private static string FormatTelemetry(LapTelemetry telemetry)
    {
        static string Number(double? value) => value?.ToString("R", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        var wear = telemetry.TyreWear is { } tyres
            ? string.Join('/', Number(tyres.FrontLeft), Number(tyres.FrontRight), Number(tyres.RearLeft), Number(tyres.RearRight))
            : string.Empty;
        return string.Join(
            ';',
            Number(telemetry.TopSpeed),
            Number(telemetry.Fuel),
            Number(telemetry.FuelUsed),
            Number(telemetry.VirtualEnergy),
            Number(telemetry.VirtualEnergyUsed),
            wear,
            telemetry.FrontCompound,
            telemetry.RearCompound);
    }

    /// <summary>
    /// Verifies that a session starts at the date of its phase: the two races of the restart of
    /// 16/09/2026 share <c>RaceResults/DateTime</c> but not their own date.
    /// </summary>
    [TestMethod]
    public void Parse_starts_a_session_at_the_date_of_its_phase()
    {
        ParseFixture("2026_09_16_15_09_20-06R1.xml").StartedAt.ToUnixTimeSeconds().Should().Be(1789563779);
        ParseFixture("2026_09_16_15_17_11-40R1.xml").StartedAt.ToUnixTimeSeconds().Should().Be(1789564165);
    }

    /// <summary>
    /// Verifies that the first date of the phase wins, and that dates of the event log are not read.
    /// </summary>
    [TestMethod]
    public void Parse_reads_the_first_date_of_the_phase()
    {
        var session = ParseResults("""
            <DateTime>1760000000</DateTime>
            <TrackVenue>Spa</TrackVenue>
            <TrackCourse>Spa GP</TrackCourse>
            <Race>
              <Stream><DateTime>5</DateTime></Stream>
              <DateTime>1760000600</DateTime>
              <DateTime>7</DateTime>
              <Driver><Name>Driver A</Name><Position>1</Position></Driver>
            </Race>
            """);

        session.StartedAt.ToUnixTimeSeconds().Should().Be(1760000600);
    }

    /// <summary>
    /// Verifies that the identifier of a session does not change from one read to the next and
    /// differs between the sessions of the fixtures, including sessions sharing their event date.
    /// </summary>
    [TestMethod]
    public void Parse_gives_every_fixture_a_stable_and_unique_identifier()
    {
        var files = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml").ToArray();

        var first = files.Select(file => ParseFixture(Path.GetFileName(file)).Id).ToArray();
        var second = files.Select(file => ParseFixture(Path.GetFileName(file)).Id).ToArray();

        second.Should().Equal(first);
        first.Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Verifies that the identifier ignores what can differ between two copies of a session (here
    /// the drivers), but separates two phases of the same circuit and two dates.
    /// </summary>
    [TestMethod]
    public void Parse_derives_the_identifier_from_the_circuit_the_phase_and_its_date()
    {
        static Session Results(string phase, long date, string driver) => ParseResults($"""
            <DateTime>1760000000</DateTime>
            <TrackVenue>Spa</TrackVenue>
            <TrackCourse>Spa GP</TrackCourse>
            <{phase}>
              <DateTime>{date}</DateTime>
              <Driver><Name>{driver}</Name><Position>1</Position></Driver>
            </{phase}>
            """);

        var session = Results("Race", 1760000600, "Driver A");

        Results("Race", 1760000600, "Driver B").Id.Should().Be(session.Id);
        Results("Qualify", 1760000600, "Driver A").Id.Should().NotBe(session.Id);
        Results("Race", 1760000601, "Driver A").Id.Should().NotBe(session.Id);
    }

    /// <summary>
    /// Verifies the events kept from the event log, and that the others are left out: scores,
    /// sector records, track-limit events without consequence and served penalties.
    /// </summary>
    [TestMethod]
    public void Parse_reads_contacts_track_limits_and_penalties_of_the_event_log()
    {
        var session = ParseResults("""
            <DateTime>1760000000</DateTime>
            <TrackVenue>Spa</TrackVenue>
            <TrackCourse>Spa GP</TrackCourse>
            <Race>
              <Stream>
                <Score et="1.0">Driver A(0) lap=0 point=1 t=44.227 et=188.460</Score>
                <Sector et="2.0">Driver A(0) reports new suspension damage</Sector>
                <Incident et="10.5">Driver A(0) reported contact (85.95) with another vehicle Driver (B)(1)</Incident>
                <Incident et="11.5">Driver A(0) reported contact (10.00) with Post</Incident>
                <Incident et="12.5">Driver A(0) reported contact (10.00) with Wing</Incident>
                <Incident et="13.5">not a contact</Incident>
                <TrackLimits Driver="Driver A" ID="0" Lap="2" WarningPoints="0" CurrentPoints="0" Resolution="5" et="20.0">No Further Action</TrackLimits>
                <TrackLimits Driver="Driver A" ID="0" Lap="2" WarningPoints="0.25" CurrentPoints="0.25" Resolution="2" et="21.0">Warning</TrackLimits>
                <TrackLimits Driver="Driver A" ID="0" Lap="3" et="22.0">Invalid Lap Cut Track</TrackLimits>
                <TrackLimits Driver="Driver A" ID="0" Lap="3" et="22.5">Drive Through Penalty</TrackLimits>
                <Penalty et="30.0">Driver A received Drive Thru penalty, 0s, 0laps for &quot;Speeding In Pitlane&quot;. Result: penalties=1, 1st=Drive Thru,0s</Penalty>
                <Penalty Driver="Driver A" ID="0" Penalty="Stop/Go" Time="10" Laps="0" Reason="Track Limits" et="31.0">Driver A received Stop/Go penalty, 10s, 0laps for Track Limits. Result: penalties=1, 1st=Stop/Go,10s</Penalty>
                <Penalty Driver="Driver A" ID="0" Penalty="Disqualify" et="32.0">Disqualify</Penalty>
                <Penalty et="33.0">Driver A served 1st Drive Thru penalty, result: penalties=0, 1st=Drive Thru,0s</Penalty>
              </Stream>
              <Driver><Name>Driver A</Name><Position>1</Position></Driver>
            </Race>
            """);

        session.Events.Should().Equal(
            new SessionEvent(TimeSpan.FromSeconds(2), SessionEventKind.Damage, "Driver A", "suspension"),
            new SessionEvent(TimeSpan.FromSeconds(10.5), SessionEventKind.VehicleContact, "Driver A", "Driver (B)"),
            new SessionEvent(TimeSpan.FromSeconds(11.5), SessionEventKind.ObstacleContact, "Driver A", "Post"),
            new SessionEvent(TimeSpan.FromSeconds(12.5), SessionEventKind.OtherContact, "Driver A", "Wing"),
            new SessionEvent(TimeSpan.FromSeconds(21), SessionEventKind.TrackLimitWarning, "Driver A", Lap: 3),
            new SessionEvent(TimeSpan.FromSeconds(22), SessionEventKind.InvalidLap, "Driver A", Lap: 4),
            new SessionEvent(TimeSpan.FromSeconds(30), SessionEventKind.Penalty, "Driver A", "Speeding In Pitlane"),
            new SessionEvent(TimeSpan.FromSeconds(31), SessionEventKind.Penalty, "Driver A", "Track Limits"),
            new SessionEvent(TimeSpan.FromSeconds(32), SessionEventKind.Penalty, "Driver A", "Disqualify"));
    }

    /// <summary>
    /// Verifies the lap values used by the race story, and the damage reports of the event log.
    /// </summary>
    [TestMethod]
    public void Parse_reads_the_position_start_and_pit_stop_of_laps_and_damage_reports()
    {
        var session = ParseResults("""
            <DateTime>1760000000</DateTime>
            <TrackVenue>Spa</TrackVenue>
            <TrackCourse>Spa GP</TrackCourse>
            <Race>
              <Stream>
                <Sector et="2.0">Driver A(0) reports new suspension damage</Sector>
                <Sector et="3.0">Driver A(0) reports new engine damage</Sector>
                <Sector et="4.0">Driver A(0) set new best for sector 2</Sector>
              </Stream>
              <Driver>
                <Name>Driver A</Name>
                <Position>1</Position>
                <Lap num="1" p="3" et="144.2225" s1="44.2">150.7752</Lap>
                <Lap num="2" p="0" et="--.----" pit="1">--.----</Lap>
              </Driver>
            </Race>
            """);

        var laps = session.Drivers[0].Laps;
        laps[0].Position.Should().Be(3);
        laps[0].StartedAt.Should().Be(TimeSpan.FromSeconds(144.2225));
        laps[0].IsPitLap.Should().BeFalse();
        laps[1].Position.Should().BeNull();
        laps[1].StartedAt.Should().BeNull();
        laps[1].IsPitLap.Should().BeTrue();
        session.Events.Should().Equal(
            new SessionEvent(TimeSpan.FromSeconds(2), SessionEventKind.Damage, "Driver A", "suspension"),
            new SessionEvent(TimeSpan.FromSeconds(3), SessionEventKind.Damage, "Driver A", "engine"));
    }

    /// <summary>
    /// Verifies the lap telemetry (top speed, fuel, virtual energy, tyre wear and compounds) and the
    /// pit stop count of a real race whose server shares fuel data.
    /// </summary>
    [TestMethod]
    public void Parse_reads_lap_telemetry_and_pit_stops_from_a_real_race()
    {
        var driver = ParseFixture("2025_02_25_21_06_15-40R1.xml").Drivers.Single(item => item.Name == "Driver Ugo");

        driver.PitStops.Should().Be(1);
        var first = driver.Laps[0].Telemetry;
        first.TopSpeed.Should().Be(241.89);
        first.Fuel.Should().Be(0.345);
        first.FuelUsed.Should().Be(0.155);
        first.VirtualEnergy.Should().Be(0.463);
        first.VirtualEnergyUsed.Should().Be(0.039);
        first.TyreWear.Should().Be(new TyreWear(0.992, 0.992, 0.992, 0.992));
        first.FrontCompound.Should().Be("Medium");
        first.RearCompound.Should().Be("Medium");

        // The lap of the stop is refuelled: the fuel used is negative.
        driver.Laps[2].Telemetry.FuelUsed.Should().Be(-0.027);
        driver.Laps[9].Telemetry.TyreWear.Should().Be(new TyreWear(0.902, 0.922, 0.875, 0.902));
    }

    /// <summary>
    /// Verifies that a server hiding fuel data leaves fuel, energy and tyre wear unknown, and that
    /// a top speed of zero and the <c>N/A</c> compound of a car that never ran are unknown too.
    /// </summary>
    [TestMethod]
    public void Parse_leaves_missing_lap_telemetry_unknown()
    {
        var hidden = ParseFixture("2026_10_02_19_24_03-96R1.xml").Drivers.SelectMany(item => item.Laps).First().Telemetry;
        hidden.TopSpeed.Should().Be(250.44);
        hidden.Fuel.Should().BeNull();
        hidden.FuelUsed.Should().BeNull();
        hidden.VirtualEnergy.Should().BeNull();
        hidden.TyreWear.Should().BeNull();
        hidden.FrontCompound.Should().Be("Wet");

        var idle = ParseFixture("2025_11_11_16_53_41-98R1.xml").Drivers.Single(item => item.Name == "Driver Albane").Laps[0].Telemetry;
        idle.TopSpeed.Should().BeNull();
        idle.FrontCompound.Should().BeEmpty();
        idle.RearCompound.Should().BeEmpty();
    }

    private static Session ParseFixture(string fileName)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "Results", fileName));
        return new XmlResultParser().Parse(stream);
    }

    private static Session ParseResults(string results)
    {
        var xml = $"<rFactorXML><RaceResults>{results}</RaceResults></rFactorXML>";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return new XmlResultParser().Parse(stream);
    }

    /// <summary>
    /// Verifies the reading rules on a small document: the first of repeated elements wins, the
    /// event name falls back to the layout, an invalid lap time or sector is missing, a negative
    /// track length is unknown and the event log is not mistaken for a driver.
    /// </summary>
    [TestMethod]
    public void Parse_applies_the_reading_rules_of_lmu_results()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <!DOCTYPE rF [ <!ENTITY rFEnt "rFactor Entity"> ]>
            <rFactorXML version="1.0">
              <Other><Race><Driver><Name>Ghost</Name><Position>1</Position></Driver></Race></Other>
              <RaceResults>
                <DateTime>1760000000</DateTime>
                <DateTime>1</DateTime>
                <TrackVenue> Spa </TrackVenue>
                <TrackCourse>Spa GP</TrackCourse>
                <TrackLength>-5</TrackLength>
                <Setting>Multiplayer</Setting>
                <Race>
                  <MostLapsCompleted>2</MostLapsCompleted>
                  <Stream><Incident et="1">Driver A reported contact</Incident><Lap num="0">1.0</Lap></Stream>
                  <Driver>
                    <Name>Driver A</Name>
                    <Name>Driver B</Name>
                    <Position>1</Position>
                    <CarClass>GT3</CarClass>
                    <Lap num="1" s1="30.5" s2="--.----">--.----</Lap>
                    <Lap num="2" s1="30.1" s2="40.2" s3="20.3">90.6</Lap>
                    <Laps>2</Laps>
                  </Driver>
                  <Driver><Position>2</Position></Driver>
                </Race>
                <Qualify><Driver><Name>Late</Name><Position>1</Position></Driver></Qualify>
              </RaceResults>
            </rFactorXML>
            """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

        var session = new XmlResultParser().Parse(stream);

        session.StartedAt.ToUnixTimeSeconds().Should().Be(1760000000);
        session.TrackVenue.Should().Be("Spa");
        session.TrackEvent.Should().Be("Spa GP");
        session.TrackLength.Should().BeNull();
        session.SessionType.Should().Be("Race");
        session.MostLapsCompleted.Should().Be(2);
        var driver = session.Drivers.Should().ContainSingle().Which;
        driver.Name.Should().Be("Driver A");
        driver.LapsCompleted.Should().Be(2);
        driver.CarNumber.Should().BeEmpty();
        driver.Laps[0].Time.Should().BeNull();
        driver.Laps[0].Sectors.Should().Equal(new Sector(1, TimeSpan.FromSeconds(30.5)));
        driver.Laps[1].Time.Should().Be(TimeSpan.FromSeconds(90.6));
        driver.Laps[1].Sectors.Select(sector => sector.Number).Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Verifies that documents that cannot be results are rejected with the reason.
    /// </summary>
    [TestMethod]
    [DataRow("", typeof(XmlException))]
    [DataRow("<rFactorXML><RaceResults /></rFactorXML><extra />", typeof(XmlException))]
    [DataRow("<rFactorXML><RaceResults><DateTime>1</DateTime><TrackVenue>V</TrackVenue><TrackCourse>C</TrackCourse><Race><Driver><Name>A</Name><Position>1</Position></Driver></Race></RaceResults><broken></rFactorXML>", typeof(XmlException))]
    [DataRow("<rFactorXML />", typeof(InvalidDataException))]
    [DataRow("<rFactorXML><RaceResults><DateTime>1</DateTime><Race /></RaceResults></rFactorXML>", typeof(InvalidDataException))]
    [DataRow("<rFactorXML><RaceResults><DateTime>soon</DateTime><Race><Driver><Name>A</Name><Position>1</Position></Driver></Race></RaceResults></rFactorXML>", typeof(InvalidDataException))]
    [DataRow("<rFactorXML><RaceResults><DateTime>1</DateTime><TrackVenue>V</TrackVenue><TrackCourse>C</TrackCourse><Race><Driver><Name> </Name><Position>1</Position></Driver></Race></RaceResults></rFactorXML>", typeof(InvalidDataException))]
    [DataRow("<rFactorXML><RaceResults><DateTime>1</DateTime><TrackCourse>C</TrackCourse><Race><Driver><Name>A</Name><Position>1</Position></Driver></Race></RaceResults></rFactorXML>", typeof(InvalidDataException))]
    [DataRow("<rFactorXML><RaceResults><DateTime>1</DateTime><TrackVenue>V</TrackVenue><TrackCourse>C</TrackCourse><TrackEvent></TrackEvent><Race><Driver><Name>A</Name><Position>1</Position></Driver></Race></RaceResults></rFactorXML>", typeof(ArgumentException))]
    public void Parse_rejects_documents_that_are_not_results(string xml, Type exceptionType)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

        var act = () => new XmlResultParser().Parse(stream);

        act.Should().Throw<Exception>().Which.Should().BeOfType(exceptionType);
    }

    /// <summary>
    /// Verifies that a stream that cannot be read is rejected before parsing.
    /// </summary>
    [TestMethod]
    public void Parse_rejects_an_unreadable_stream()
    {
        var stream = new MemoryStream();
        stream.Dispose();

        var act = () => new XmlResultParser().Parse(stream);

        act.Should().Throw<ArgumentException>();
    }
}
