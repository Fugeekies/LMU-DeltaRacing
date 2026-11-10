using System.Xml;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LMU_DeltaRacing.Application.Services;

/// <summary>
/// Imports individual LMU result files into the session repository.
/// </summary>
public sealed class SessionImportService : ISessionImportService
{
    private const int MaximumFileOpenAttempts = 6;
    private readonly IResultXmlParser parser;
    private readonly ISessionRepository repository;
    private readonly ILogger<SessionImportService> logger;
    private readonly Func<bool> importPracticeSessions;
    private readonly Func<int, TimeSpan> retryDelay;

    /// <summary>
    /// Creates an importer using the specified parser and session repository.
    /// </summary>
    /// <param name="parser">The result XML parser.</param>
    /// <param name="repository">The destination session repository.</param>
    /// <param name="logger">The logger used to report import progress and failures.</param>
    /// <param name="importPracticeSessions">
    /// Whether practice sessions are stored. The dashboard keeps them, when the
    /// <c>LMU:LoadPracticeSessions</c> option is on, for the practice tab of the race-details page.
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
    /// <param name="retryDelay">
    /// Returns the wait before the given retry of a locked file; by default 200 ms per attempt, capped at 1 s.
    /// </param>
    public SessionImportService(
        IResultXmlParser parser,
        ISessionRepository repository,
        ILogger<SessionImportService> logger,
        Func<bool> importPracticeSessions,
        Func<int, TimeSpan>? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(importPracticeSessions);

        this.parser = parser;
        this.repository = repository;
        this.logger = logger;
        this.importPracticeSessions = importPracticeSessions;
        this.retryDelay = retryDelay ?? (attempt => TimeSpan.FromMilliseconds(Math.Min(attempt * 200, 1000)));
    }

    /// <inheritdoc />
    public SessionImportResult ImportFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var sourceFile = Path.GetFileName(filePath);
        logger.LogInformation("Starting import for {SourceFile}", sourceFile);

        try
        {
            logger.LogDebug("Opening result file {SourceFile}", sourceFile);
            using var stream = OpenResultFile(filePath, sourceFile);
            logger.LogDebug("Parsing result file {SourceFile}", sourceFile);
            var session = parser.Parse(stream);

            if (session.Kind == SessionKind.Practice && !importPracticeSessions())
            {
                logger.LogInformation(
                    "Skipped practice session in {SourceFile}; practice data is not loaded",
                    sourceFile);

                return new SessionImportResult(
                    sourceFile,
                    SessionImportStatus.SkippedPractice,
                    null,
                    null);
            }

            logger.LogDebug("Parsed result file {SourceFile}; saving session {SessionId}", sourceFile, session.Id);
            if (!repository.TryAdd(session))
            {
                logger.LogInformation(
                    "Skipped {SourceFile}: session {SessionId} is already imported",
                    sourceFile,
                    session.Id);

                return new SessionImportResult(
                    sourceFile,
                    SessionImportStatus.SkippedDuplicate,
                    null,
                    null);
            }

            logger.LogInformation(
                "Imported {SourceFile}: {TrackVenue}, session {SessionType}, {DriverCount} drivers",
                sourceFile,
                session.TrackVenue,
                session.SessionType,
                session.Drivers.Count);

            return new SessionImportResult(sourceFile, SessionImportStatus.Imported, session, null);
        }
        // InvalidDataException is not an IOException: it is listed for well-formed XML that is not an
        // LMU result, so one foreign file never stops the startup load or the watcher.
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or XmlException or InvalidDataException
                or ArgumentException)
        {
            logger.LogError(exception, "Import failed for {SourceFile}", sourceFile);

            return new SessionImportResult(sourceFile, SessionImportStatus.Failed, null, exception.Message);
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

                var delay = retryDelay(attempt);
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
