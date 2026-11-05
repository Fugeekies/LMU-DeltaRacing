namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// A driver's head-to-head record against the player.
/// </summary>
/// <param name="Name">The opponent name.</param>
/// <param name="SessionsFaced">The races both drivers finished classified in the same class.</param>
/// <param name="Wins">The faced races where the player finished ahead in class.</param>
/// <param name="Losses">The faced races where the player finished behind in class.</param>
/// <param name="AverageClassGap">
/// The average of the opponent's class rank minus the player's: positive when the player finishes ahead.
/// </param>
/// <param name="LastFacedAt">The start of the last faced race.</param>
/// <param name="LastFacedTrack">The track venue of the last faced race.</param>
/// <param name="Rating">The current driver rating of the opponent, when rated.</param>
/// <param name="Contacts">
/// The contacts between both cars in the faced races, or <see langword="null"/> when none of
/// them has an event log.
/// </param>
public sealed record DriverRivalry(
    string Name,
    int SessionsFaced,
    int Wins,
    int Losses,
    double AverageClassGap,
    DateTimeOffset LastFacedAt,
    string LastFacedTrack,
    double? Rating,
    int? Contacts)
{
    /// <summary>
    /// Gets the percentage of faced sessions won by the player.
    /// </summary>
    public double WinPercentage => SessionsFaced == 0 ? 0 : Wins * 100d / SessionsFaced;
}
