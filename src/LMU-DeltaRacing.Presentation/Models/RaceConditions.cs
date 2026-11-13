namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// The track conditions noted by the player: the result files do not contain the weather.
/// </summary>
public enum RaceConditions
{
    /// <summary>
    /// A dry track.
    /// </summary>
    Dry,

    /// <summary>
    /// A damp track.
    /// </summary>
    Damp,

    /// <summary>
    /// A wet track, in the rain.
    /// </summary>
    Wet,

    /// <summary>
    /// Conditions that changed during the session.
    /// </summary>
    Mixed
}
