namespace LMU_DeltaRacing.Domain.Entities;

/// <summary>
/// One event of a session log that says something about how a driver drove.
/// </summary>
/// <param name="Elapsed">The time elapsed since the start of the session.</param>
/// <param name="Kind">The kind of event.</param>
/// <param name="DriverName">The driver the event is about.</param>
/// <param name="Detail">
/// The other driver of a vehicle contact, the object of any other contact or the reason of a
/// penalty, as written by LMU.
/// </param>
/// <param name="Lap">The lap of the event (from 1), for track-limit events.</param>
public sealed record SessionEvent(
    TimeSpan Elapsed,
    SessionEventKind Kind,
    string DriverName,
    string? Detail = null,
    int? Lap = null);

/// <summary>
/// Describes what happened in a <see cref="SessionEvent"/>.
/// </summary>
public enum SessionEventKind
{
    /// <summary>A contact with another car.</summary>
    VehicleContact,

    /// <summary>A contact with an obstacle of the track: wall, post, sign or cone.</summary>
    ObstacleContact,

    /// <summary>A contact reported with something else, whose exact meaning is not established.</summary>
    OtherContact,

    /// <summary>A track-limit warning.</summary>
    TrackLimitWarning,

    /// <summary>A lap invalidated for cutting the track.</summary>
    InvalidLap,

    /// <summary>A penalty received, or a disqualification.</summary>
    Penalty
}
