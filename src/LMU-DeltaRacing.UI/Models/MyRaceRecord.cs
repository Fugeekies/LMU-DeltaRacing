namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// A qualifying session and the race that follows it, as driven by the selected driver.
/// Either side can be missing when a session could not be paired.
/// </summary>
public sealed record MyRaceRecord(
    string TrackVenue,
    string TrackCourse,
    string TrackEvent,
    DateTimeOffset StartedAt,
    string Category,
    string Car,
    TimeSpan? QualifyingTime,
    TimeSpan? QualifyingDelta,
    int? GridPosition,
    TimeSpan? RaceTime,
    TimeSpan? RaceDelta,
    int? LapsCompleted,
    TimeSpan? FinishTime);
