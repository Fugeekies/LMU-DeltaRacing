using FluentAssertions;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Persistence;
using LMU_DeltaRacing.Infrastructure.Parsing;

namespace LMU_DeltaRacing.Tests.Application;

public sealed class SessionImportServiceTests
{
    private const string FixturePath = "TestAssets/Results/2025_09_20_19_03_47-43P1.xml";

    [Fact]
    public void ImportFile_stores_the_parsed_session_and_returns_imported_status()
    {
        var repository = new InMemorySessionRepository();
        var service = new SessionImportService(new XmlResultParser(), repository);
        var path = Path.Combine(AppContext.BaseDirectory, FixturePath);

        var result = service.ImportFile(path);

        result.Status.Should().Be(SessionImportStatus.Imported);
        result.Session.Should().NotBeNull();
        repository.GetById(result.Session!.Id).Should().BeSameAs(result.Session);
        result.SourceFile.Should().Be(Path.GetFileName(path));
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ImportFile_returns_failed_status_for_invalid_xml()
    {
        var repository = new InMemorySessionRepository();
        var service = new SessionImportService(new XmlResultParser(), repository);
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

    [Fact]
    public void ImportFile_rejects_a_blank_path()
    {
        var service = new SessionImportService(
            new XmlResultParser(),
            new InMemorySessionRepository());

        var act = () => service.ImportFile(" ");

        act.Should().Throw<ArgumentException>();
    }
}
