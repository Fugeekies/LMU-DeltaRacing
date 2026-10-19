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
        driver.Name.Should().MatchRegex("^Driver \\d{4,}$");
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
}
