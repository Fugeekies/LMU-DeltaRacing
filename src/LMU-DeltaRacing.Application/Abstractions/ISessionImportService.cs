using LMU_DeltaRacing.Application.Models;

namespace LMU_DeltaRacing.Application.Abstractions;

/// <summary>
/// Imports LMU result files into the session repository.
/// </summary>
public interface ISessionImportService
{
    /// <summary>
    /// Imports one result XML file and reports its outcome.
    /// </summary>
    /// <param name="filePath">The path to the result XML file.</param>
    /// <returns>The outcome of the import.</returns>
    SessionImportResult ImportFile(string filePath);
}
