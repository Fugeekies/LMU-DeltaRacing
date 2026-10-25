using System.Diagnostics;
using System.Xml;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Application.Telemetry;
using Microsoft.Extensions.Logging;

namespace LMU_DeltaRacing.Application.Services;

/// <summary>
/// Imports individual LMU result files and records import telemetry.
/// </summary>
public sealed class SessionImportService : ISessionImportService
{
    private const int MaximumFileOpenAttempts = 6;
    private readonly IResultXmlParser parser;
    private readonly ISessionRepository repository;
    private readonly ILogger<SessionImportService> logger;
    private readonly Func<bool> importPracticeSessions;

    /// <summary>
    /// Creates an importer using the specified parser and session repository.
    /// </summary>
    /// <param name="parser">The result XML parser.</param>
    /// <param name="repository">The destination session repository.</param>
    /// <param name="logger">The logger used to report import progress and failures.</param>
    /// <param name="importPracticeSessions">
    /// Whether practice sessions are stored. The console statistics ignore them, while the
    /// dashboard keeps them for the practice tab of the race-details page.
    /// </param>
    public SessionImportService(
        IResultXmlParser parser,
        ISessionRepository repository,
        ILogger<SessionImportService> logger,
        bool importPracticeSessions = false)
        : this(parser, repository, logger, () => importPracticeSessions)
    {
    }

    /// <summary>
    /// Creates an importer whose practice preference is read at each import, so a setting
    /// changed while the application runs applies to the next reload.
    /// </summary>
    /// <param name="parser">The result XML parser.</param>
    /// <param name="repository">The destination session repository.</param>
    /// <param name="logger">The logger used to report import progress and failures.</param>
    /// <param name="importPracticeSessions">Returns whether practice sessions are stored.</param>
    public SessionImportService(
        IResultXmlParser parser,
        ISessionRepository repository,
        ILogger<SessionImportService> logger,
        Func<bool> importPracticeSessions)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(importPracticeSessions);

        this.parser = parser;
        this.repository = repository;
        this.logger = logger;
        this.importPracticeSessions = importPracticeSessions;
    }

    /// <inheritdoc />
    public SessionImportResult ImportFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var sourceFile = Path.GetFileName(filePath);
        var stopwatch = Stopwatch.StartNew();
        using var activity = ApplicationTelemetry.ActivitySource.StartActivity("session.import");
        activity?.SetTag("file.name", sourceFile);
        logger.LogInformation("Starting import for {SourceFile}", sourceFile);

        try
        {
            logger.LogDebug("Opening result file {SourceFile}", sourceFile);
            using var stream = OpenResultFile(filePath, sourceFile);
            logger.LogDebug("Parsing result file {SourceFile}", sourceFile);
            var session = parser.Parse(stream);

            if (!importPracticeSessions()
                && session.SessionType.StartsWith("Practice", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation(
                    "Skipped practice session in {SourceFile}; practice data is not loaded",
                    sourceFile);
                activity?.SetTag("import.status", "skipped_practice");
                ApplicationTelemetry.Imports.Add(
                    1,
                    new KeyValuePair<string, object?>("import.status", "skipped_practice"));

                return new SessionImportResult(
                    sourceFile,
                    SessionImportStatus.SkippedPractice,
                    null,
                    null);
            }

            logger.LogDebug("Parsed result file {SourceFile}; saving session {SessionId}", sourceFile, session.Id);
            repository.Save(session);
            logger.LogInformation(
                "Imported {SourceFile}: {TrackVenue}, session {SessionType}, {DriverCount} drivers",
                sourceFile,
                session.TrackVenue,
                session.SessionType,
                session.Drivers.Count);

            activity?.SetTag("import.status", "imported");
            ApplicationTelemetry.Imports.Add(
                1,
                new KeyValuePair<string, object?>("import.status", "imported"));

            return new SessionImportResult(sourceFile, SessionImportStatus.Imported, session, null);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
        {
            logger.LogError(exception, "Import failed for {SourceFile}", sourceFile);
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

    private FileStream OpenResultFile(string filePath, string sourceFile)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                if (attempt >= MaximumFileOpenAttempts)
                {
                    throw;
                }

                var delay = TimeSpan.FromMilliseconds(Math.Min(attempt * 200, 1000));
                logger.LogWarning(
                    exception,
                    "Result file {SourceFile} is locked or not yet available; retry {Attempt}/{MaxAttempts} in {DelayMilliseconds} ms",
                    sourceFile,
                    attempt,
                    MaximumFileOpenAttempts,
                    delay.TotalMilliseconds);
                Thread.Sleep(delay);
            }
        }
    }
}
