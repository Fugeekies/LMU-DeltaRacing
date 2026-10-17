namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// A vehicle's qualifying and race performance for one event.
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
    TimeSpan? RaceDelta);
