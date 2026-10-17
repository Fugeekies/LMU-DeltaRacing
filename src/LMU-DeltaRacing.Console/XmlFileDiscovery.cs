namespace LMU_DeltaRacing.ConsoleApp;

/// <summary>
/// Locates result XML files for the console application.
/// </summary>
public static class XmlFileDiscovery
{
    /// <summary>
    /// Finds top-level XML files in a results directory in deterministic order.
    /// </summary>
    /// <param name="directoryPath">The directory containing result files.</param>
    /// <returns>The XML file paths, sorted by file name.</returns>
    /// <exception cref="DirectoryNotFoundException">The results directory does not exist.</exception>
    public static IReadOnlyList<string> GetXmlFiles(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"The results directory '{directoryPath}' does not exist.");
        }

        return Directory.EnumerateFiles(directoryPath, "*.xml", SearchOption.TopDirectoryOnly)
            .OrderBy(filePath => Path.GetFileName(filePath), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Searches the specified directory and its parents for repository XML result assets.
    /// </summary>
    /// <param name="startingDirectory">The directory where the search begins.</param>
    /// <returns>The repository's results directory.</returns>
    /// <exception cref="DirectoryNotFoundException">The results assets directory could not be found.</exception>
    public static string FindAssetsResultsDirectory(string startingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startingDirectory);

        var directory = new DirectoryInfo(Path.GetFullPath(startingDirectory));
        while (directory is not null)
        {
            var resultsDirectory = Path.Combine(directory.FullName, "assets", "Results");

            if (Directory.Exists(resultsDirectory))
            {
                return resultsDirectory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find the repository results assets directory starting from '{startingDirectory}'.");
    }
}
