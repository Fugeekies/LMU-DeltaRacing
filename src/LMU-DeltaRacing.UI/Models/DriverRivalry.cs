namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// A driver's head-to-head record against the player.
/// </summary>
public sealed record DriverRivalry(
    string Name,
    int SessionsFaced,
    int Wins,
    int Losses)
{
    /// <summary>
    /// Gets the percentage of faced sessions won by the player.
    /// </summary>
    public double WinPercentage => SessionsFaced == 0 ? 0 : Wins * 100d / SessionsFaced;
}
