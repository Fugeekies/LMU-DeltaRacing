using FluentAssertions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Infrastructure.Persistence;
using LMU_DeltaRacing.Infrastructure.Parsing;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMU_DeltaRacing.Tests.Application;

/// <summary>
/// Verifies importing result files through the application service.
/// </summary>
[TestClass]
public sealed class SessionImportServiceTests
{
    private const string FixturePath = "assets/Results/2025_02_24_20_26_56-18R1.xml";

    /// <summary>
    /// Verifies that a parsed session is stored and returned as a successful import.
    /// </summary>
    [TestMethod]
    public void ImportFile_stores_the_parsed_session_and_returns_imported_status()
    {
        var repository = new InMemorySessionRepository();
        var service = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);
        var path = Path.Combine(AppContext.BaseDirectory, FixturePath);

        var result = service.ImportFile(path);

        result.Status.Should().Be(SessionImportStatus.Imported);
        result.Session.Should().NotBeNull();
        repository.GetById(result.Session!.Id).Should().BeSameAs(result.Session);
        result.SourceFile.Should().Be(Path.GetFileName(path));
        result.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Verifies that malformed XML produces a failed result without storing a session.
    /// </summary>
    [TestMethod]
    public void ImportFile_returns_failed_status_for_invalid_xml()
    {
        var repository = new InMemorySessionRepository();
        var service = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");

        try
        {
            File.WriteAllText(path, "<not-valid-xml>");

            var result = service.ImportFile(path);

            result.Status.Should().Be(SessionImportStatus.Failed);
            result.Session.Should().BeNull();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            repository.GetAll().Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that well-formed XML which is not an LMU result (the parser reports it with
    /// <see cref="InvalidDataException"/>) is a failed import, not an exception that would stop
    /// the startup load or the watcher.
    /// </summary>
    [TestMethod]
    [DataRow("<rFactorXML><Other /></rFactorXML>")]
    [DataRow("<rFactorXML><RaceResults><TrackVenue>Spa</TrackVenue><TrackCourse>Spa</TrackCourse><Race><Driver><Name>A</Name><Position>1</Position></Driver></Race></RaceResults></rFactorXML>")]
    [DataRow("<rFactorXML><RaceResults><DateTime>1760000000</DateTime><TrackVenue>Spa</TrackVenue><TrackCourse>Spa</TrackCourse><Race><Driver><Name>A</Name><Position>1</Position><Lap num=\"0\">90.1</Lap></Driver></Race></RaceResults></rFactorXML>")]
    public void ImportFile_returns_failed_status_for_xml_that_is_not_an_lmu_result(string content)
    {
        var repository = new InMemorySessionRepository();
        var service = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");

        try
        {
            File.WriteAllText(path, content);

            var result = service.ImportFile(path);

            result.Status.Should().Be(SessionImportStatus.Failed);
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            repository.GetAll().Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that a missing file is reported as a failed import once the retries are exhausted.
    /// </summary>
    [TestMethod]
    public void ImportFile_returns_failed_status_for_a_missing_file()
    {
        var service = new SessionImportService(
            new XmlResultParser(),
            new InMemorySessionRepository(),
            NullLogger<SessionImportService>.Instance,
            () => false,
            retryDelay: _ => TimeSpan.Zero);

        var result = service.ImportFile(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml"));

        result.Status.Should().Be(SessionImportStatus.Failed);
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifies that practice data is not stored or returned to consuming menus.
    /// </summary>
    [TestMethod]
    public void ImportFile_skips_practice_sessions_without_storing_them()
    {
        var repository = new InMemorySessionRepository();
        var service = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "Results",
            "2025_09_20_19_03_47-43P1.xml");

        var result = service.ImportFile(path);

        result.Status.Should().Be(SessionImportStatus.SkippedPractice);
        result.Session.Should().BeNull();
        repository.GetAll().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the dashboard can keep practice sessions, which the race-details page can display.
    /// </summary>
    [TestMethod]
    public void ImportFile_stores_practice_sessions_when_practice_import_is_enabled()
    {
        var repository = new InMemorySessionRepository();
        var service = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance,
            importPracticeSessions: true);
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "Results",
            "2025_09_20_19_03_47-43P1.xml");

        var result = service.ImportFile(path);

        result.Status.Should().Be(SessionImportStatus.Imported);
        result.Session!.SessionType.Should().Be("Practice1");
        repository.GetAll().Should().ContainSingle();
    }

    /// <summary>
    /// Verifies that the practice preference is read at each import, so the dashboard option
    /// applies to the next reload without restarting the application.
    /// </summary>
    [TestMethod]
    public void ImportFile_reads_the_practice_preference_at_each_import()
    {
        var repository = new InMemorySessionRepository();
        var importPractice = false;
        var service = new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance,
            () => importPractice);
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "Results",
            "2025_09_20_19_03_47-43P1.xml");

        service.ImportFile(path).Status.Should().Be(SessionImportStatus.SkippedPractice);
        importPractice = true;
        service.ImportFile(path).Status.Should().Be(SessionImportStatus.Imported);
    }

    /// <summary>
    /// Verifies that a blank file path is rejected as invalid input.
    /// </summary>
    [TestMethod]
    public void ImportFile_rejects_a_blank_path()
    {
        var service = new SessionImportService(
            new XmlResultParser(),
            new InMemorySessionRepository(),
            NullLogger<SessionImportService>.Instance);

        var act = () => service.ImportFile(" ");

        act.Should().Throw<ArgumentException>();
    }
}
