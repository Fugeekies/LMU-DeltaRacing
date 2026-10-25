namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// One qualifying or race session driven by the selected driver.
/// </summary>
/// <param name="TrackVenue">The track venue.</param>
/// <param name="TrackCourse">The track layout.</param>
/// <param name="TrackEvent">The event name.</param>
/// <param name="StartedAt">The session start time.</param>
/// <param name="Kind">Whether the session is a race or a qualifying session.</param>
/// <param name="Category">The WEC category of the driver's vehicle.</param>
/// <param name="Car">The driver's vehicle.</param>
/// <param name="BestLap">The driver's best timed lap.</param>
/// <param name="Delta">The gap to the previous session of the same type, circuit and category.</param>
/// <param name="GridPosition">The starting position, for races.</param>
/// <param name="LapsCompleted">The laps completed by the driver.</param>
/// <param name="FinishTime">The total race time, for races.</param>
public sealed record MyRaceSession(
    string TrackVenue,
    string TrackCourse,
    string TrackEvent,
    DateTimeOffset StartedAt,
    MyRaceSessionKind Kind,
    string Category,
    string Car,
    TimeSpan? BestLap,
    TimeSpan? Delta,
    int? GridPosition,
    int? LapsCompleted,
    TimeSpan? FinishTime);

/// <summary>
/// The session types shown in the race history.
/// </summary>
public enum MyRaceSessionKind
{
    /// <summary>A race, shown first because it is the result that counts.</summary>
    Race,

    /// <summary>A qualifying session.</summary>
    Qualifying
}
