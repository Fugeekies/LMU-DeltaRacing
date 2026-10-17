namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Describes the validation result for a configured results directory.
/// </summary>
/// <param name="IsValid">Whether the directory exists and can be read.</param>
/// <param name="DirectoryPath">The resolved absolute path, when it could be resolved.</param>
/// <param name="XmlFileCount">The number of top-level XML files in the directory.</param>
/// <param name="ErrorMessage">A user-facing reason when the directory is invalid.</param>
public sealed record ResultsDirectoryStatus(
    bool IsValid,
    string? DirectoryPath,
    int XmlFileCount,
    string? ErrorMessage);
