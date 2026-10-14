namespace LMU_DeltaRacing.Application.Models;

/// <summary>
/// Describes the outcome of importing a results file.
/// </summary>
public enum SessionImportStatus
{
    /// <summary>
    /// The file was parsed and its session was stored.
    /// </summary>
    Imported,

    /// <summary>
    /// The file could not be read or parsed.
    /// </summary>
    Failed
}
