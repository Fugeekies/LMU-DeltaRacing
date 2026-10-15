namespace LMU_DeltaRacing.ConsoleApp.Configuration;

/// <summary>
/// Configurable paths and logging behavior for the local LMU results console.
/// </summary>
public sealed class LmuApplicationOptions
{
    /// <summary>
    /// Gets the configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "LMU";

    /// <summary>
    /// Gets or sets the result XML directory. An empty value uses the repository test assets.
    /// </summary>
    public string ResultsDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON Lines log file path, relative to the executable directory when not rooted.
    /// </summary>
    public string LogFilePath { get; set; } = "lmu-deltaracing.log";

    /// <summary>
    /// Gets or sets the minimum severity written to the log file.
    /// </summary>
    public string FileMinimumLogLevel { get; set; } = "Debug";
}
