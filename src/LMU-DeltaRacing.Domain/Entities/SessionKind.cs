namespace LMU_DeltaRacing.Domain.Entities;

/// <summary>
/// The kind of an LMU session, read from the phase name of the result file.
/// </summary>
/// <remarks>
/// The kinds of an event are declared in the order in which they are driven (practice, qualifying,
/// race): comparing two kinds tells whether a session can follow another one in the same event.
/// </remarks>
public enum SessionKind
{
    /// <summary>A phase the application does not use (for example a warm-up).</summary>
    Other,

    /// <summary>A practice session ("Practice1", "Practice2"...).</summary>
    Practice,

    /// <summary>A qualifying session ("Qualify").</summary>
    Qualifying,

    /// <summary>A race ("Race").</summary>
    Race
}
