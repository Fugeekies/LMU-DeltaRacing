using System.Diagnostics;
using System.Xml;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Application.Telemetry;

namespace LMU_DeltaRacing.Application.Services;

/// <summary>
/// Imports individual LMU result files and records import telemetry.
/// </summary>
public sealed class SessionImportService : ISessionImportService
{
    private readonly IResultXmlParser parser;
    private readonly ISessionRepository repository;

    /// <summary>
    /// Creates an importer using the specified parser and session repository.
    /// </summary>
    /// <param name="parser">The result XML parser.</param>
    /// <param name="repository">The destination session repository.</param>
    public SessionImportService(IResultXmlParser parser, ISessionRepository repository)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(repository);

        this.parser = parser;
        this.repository = repository;
    }

    /// <inheritdoc />
    public SessionImportResult ImportFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var sourceFile = Path.GetFileName(filePath);
        var stopwatch = Stopwatch.StartNew();
        using var activity = ApplicationTelemetry.ActivitySource.StartActivity("session.import");
        activity?.SetTag("file.name", sourceFile);

        try
        {
            using var stream = File.OpenRead(filePath);
            var session = parser.Parse(stream);
            repository.Save(session);

            activity?.SetTag("import.status", "imported");
            ApplicationTelemetry.Imports.Add(
                1,
                new KeyValuePair<string, object?>("import.status", "imported"));

            return new SessionImportResult(sourceFile, SessionImportStatus.Imported, session, null);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Result-file import failed.");
            activity?.SetTag("import.status", "failed");
            activity?.SetTag("error.type", exception.GetType().Name);
            ApplicationTelemetry.Imports.Add(
                1,
                new KeyValuePair<string, object?>("import.status", "failed"));

            return new SessionImportResult(sourceFile, SessionImportStatus.Failed, null, exception.Message);
        }
        finally
        {
            stopwatch.Stop();
            ApplicationTelemetry.ImportDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("file.extension", Path.GetExtension(sourceFile)));
        }
    }
}
