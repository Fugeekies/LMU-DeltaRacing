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
                session.TrackLength?.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
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
                    driver.DnfReason));
                foreach (var lap in driver.Laps)
                {
                    snapshot.AppendLine($"{lap.Number}:{lap.Time?.Ticks}:{string.Join(',', lap.Sectors.Select(sector => $"{sector.Number}={sector.Time.Ticks}"))}");
                }
            }
        }

        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(snapshot.ToString())));

        files.Should().HaveCount(342);
        hash.Should().Be(ReferenceSnapshotHash, "the fingerprint of the parsed fixtures is now {0}", hash);
    }

    // Computed with the XDocument parser of version 0.1 on the fixtures of assets/Results.
    private const string ReferenceSnapshotHash = "A5DFEC510FDE9B435BA5ECD6BC013F3EEF8F4102B55E16AFCDEA6033C136F471";

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
