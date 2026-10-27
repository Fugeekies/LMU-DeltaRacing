namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Describes how often a driver appears in the imported result files.
/// </summary>
public sealed record DriverFrequency(string Name, int AppearanceCount, int TotalResults)
{
    /// <summary>
    /// Gets the share of imported result files containing this driver.
    /// </summary>
    public double Percentage => TotalResults == 0 ? 0 : (double)AppearanceCount / TotalResults;
}
