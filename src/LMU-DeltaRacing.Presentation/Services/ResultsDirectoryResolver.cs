namespace LMU_DeltaRacing.Presentation.Services;

/// <summary>
/// Resolves the LMU results directory or the bundled sample-data fallback.
/// </summary>
public static class ResultsDirectoryResolver
{
    /// <summary>
    /// Finds the configured results folder, an LMU default folder, or packaged/development samples.
    /// </summary>
    /// <param name="configuredDirectory">Configured results directory, if any.</param>
    /// <param name="applicationDirectory">Directory containing the running application.</param>
    /// <param name="documentsDirectory">User documents directory, or an empty string to skip user paths.</param>
    /// <returns>The first usable results directory.</returns>
    /// <exception cref="ArgumentException">The application directory is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">No configured, LMU, or sample results directory exists.</exception>
    public static string Resolve(
        string? configuredDirectory,
        string applicationDirectory,
        string? documentsDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);
        var applicationPath = Path.GetFullPath(applicationDirectory);

        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            var configuredPath = Path.GetFullPath(configuredDirectory, applicationPath);
            if (!Directory.Exists(configuredPath))
            {
                throw new DirectoryNotFoundException(
                    $"The configured results directory does not exist: '{configuredPath}'.");
            }

            return configuredPath;
        }

        documentsDirectory ??= Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documentsDirectory))
        {
            var documentsPath = Path.GetFullPath(documentsDirectory);
            var userResultDirectories = new[]
            {
                Path.Combine(documentsPath, "Le Mans Ultimate", "UserData", "Log", "Results"),
                Path.Combine(documentsPath, "My Games", "Le Mans Ultimate", "UserData", "Log", "Results")
            };

            foreach (var directory in userResultDirectories)
            {
                if (ContainsXmlResults(directory))
                {
                    return directory;
                }
            }
        }

        var localResultDirectory = Path.Combine(applicationPath, "UserData", "Log", "Results");
        if (ContainsXmlResults(localResultDirectory))
        {
            return localResultDirectory;
        }

        var packagedSamples = Path.Combine(applicationPath, "TestAssets", "Results");
        if (ContainsXmlResults(packagedSamples))
        {
            return packagedSamples;
        }

        var developmentSamples = Path.Combine(applicationPath, "assets", "Results");
        if (ContainsXmlResults(developmentSamples))
        {
            return developmentSamples;
        }

        DirectoryInfo? directoryInfo = new(applicationPath);
        while (directoryInfo is not null)
        {
            developmentSamples = Path.Combine(directoryInfo.FullName, "assets", "Results");
            if (ContainsXmlResults(developmentSamples))
            {
                return developmentSamples;
            }

            directoryInfo = directoryInfo.Parent;
        }

        throw new DirectoryNotFoundException(
            "No results directory is available. Configure LMU:ResultsDirectory or install the bundled sample results.");
    }

    private static bool ContainsXmlResults(string directory) =>
        Directory.Exists(directory)
        && Directory.EnumerateFiles(directory, "*.xml", SearchOption.TopDirectoryOnly).Any();
}
