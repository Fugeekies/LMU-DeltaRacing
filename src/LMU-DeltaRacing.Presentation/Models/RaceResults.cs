namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Reads how a race ended from the LMU finish status, for the home page and the "my races" page.
/// </summary>
public static class RaceResults
{
    /// <summary>
    /// Returns how a race ended: the DNF, DQ and "None" statuses win over the position.
    /// </summary>
    /// <param name="finishStatus">The LMU finish status (for example "Finished Normally" or "DNF").</param>
    /// <param name="classPosition">The class position used to tell a win or a podium from another result.</param>
    public static HomeResultKind GetKind(string finishStatus, int? classPosition)
    {
        ArgumentNullException.ThrowIfNull(finishStatus);

        return finishStatus.Trim() switch
        {
            var status when status.Equals("DNF", StringComparison.OrdinalIgnoreCase) => HomeResultKind.Retired,
            var status when status.Equals("DQ", StringComparison.OrdinalIgnoreCase) => HomeResultKind.Disqualified,
            var status when status.Equals("None", StringComparison.OrdinalIgnoreCase) => HomeResultKind.Unclassified,
            _ => classPosition switch
            {
                1 => HomeResultKind.Win,
                2 or 3 => HomeResultKind.Podium,
                _ => HomeResultKind.Classified
            }
        };
    }

    /// <summary>
    /// Returns the short label of a result: "P3", "DNF", "DQ", "NC", or "—" without position.
    /// </summary>
    /// <param name="kind">How the race ended.</param>
    /// <param name="classPosition">The class position shown for a classified result.</param>
    public static string GetLabel(HomeResultKind kind, int? classPosition) => kind switch
    {
        HomeResultKind.Retired => "DNF",
        HomeResultKind.Disqualified => "DQ",
        HomeResultKind.Unclassified => "NC",
        _ => classPosition is { } position ? $"P{position}" : DisplayFormat.Missing
    };
}
