using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Application.Models;

/// <summary>
/// Contains the outcome of importing one results file.
/// </summary>
/// <param name="SourceFile">The name of the imported file.</param>
/// <param name="Status">The import outcome.</param>
/// <param name="Session">The imported session, when successful.</param>
/// <param name="ErrorMessage">The failure reason, when the import failed.</param>
public sealed record SessionImportResult(
    string SourceFile,
    SessionImportStatus Status,
    Session? Session,
    string? ErrorMessage);
