namespace LMU_DeltaRacing.Domain.Entities;

/// <summary>
/// Represents one LMU results session.
/// </summary>
public sealed class Session
{
    /// <summary>
    /// Creates a session and its driver results.
    /// </summary>
    /// <param name="id">The unique session identifier.</param>
    /// <param name="startedAt">The session start time.</param>
    /// <param name="trackVenue">The track venue.</param>
    /// <param name="trackCourse">The track layout.</param>
    /// <param name="trackEvent">The event name.</param>
    /// <param name="sessionType">The session type from the result file.</param>
    /// <param name="drivers">The drivers and their results.</param>
    /// <param name="mostLapsCompleted">The highest number of laps completed in the session.</param>
    public Session(
        Guid id,
        DateTimeOffset startedAt,
        string trackVenue,
        string trackCourse,
        string trackEvent,
        string sessionType,
        IEnumerable<Driver> drivers,
        int? mostLapsCompleted = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackVenue);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackCourse);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionType);
        ArgumentNullException.ThrowIfNull(drivers);

        Id = id;
        StartedAt = startedAt;
        TrackVenue = trackVenue;
        TrackCourse = trackCourse;
        TrackEvent = trackEvent;
        SessionType = sessionType;
        Drivers = Array.AsReadOnly(drivers.ToArray());
        MostLapsCompleted = mostLapsCompleted;
    }

    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the session start time.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Gets the track venue.
    /// </summary>
    public string TrackVenue { get; }

    /// <summary>
    /// Gets the track layout.
    /// </summary>
    public string TrackCourse { get; }

    /// <summary>
    /// Gets the event name.
    /// </summary>
    public string TrackEvent { get; }

    /// <summary>
    /// Gets the session type from the result file.
    /// </summary>
    public string SessionType { get; }

    /// <summary>
    /// Gets the drivers and their results.
    /// </summary>
    public IReadOnlyList<Driver> Drivers { get; }

    /// <summary>
    /// Gets the highest number of laps completed in this session, when available.
    /// </summary>
    public int? MostLapsCompleted { get; }
}
