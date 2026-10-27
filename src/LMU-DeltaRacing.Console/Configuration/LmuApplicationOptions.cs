namespace LMU_DeltaRacing.ConsoleApp.Configuration;

/// <summary>
/// File logging settings of the console. The results directory, the driver and the driver merges
/// live in the same section but are read and written by the shared dashboard services.
/// </summary>
public sealed class LmuApplicationOptions
{
    /// <summary>
    /// Gets the configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "LMU";

    /// <summary>
    /// Gets or sets the JSON Lines log file path, relative to the executable directory when not rooted.
    /// </summary>
    public string LogFilePath { get; set; } = "lmu-deltaracing.log";

    /// <summary>
    /// Gets or sets the minimum severity written to the log file.
    /// </summary>
    public string FileMinimumLogLevel { get; set; } = "Debug";
}
