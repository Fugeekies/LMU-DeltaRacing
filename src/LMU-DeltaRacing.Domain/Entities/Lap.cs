namespace LMU_DeltaRacing.Domain.Entities;

/// <summary>
/// Represents a driver's completed or incomplete lap.
/// </summary>
public sealed class Lap
{
    /// <summary>
    /// Creates a lap and its sector times.
    /// </summary>
    /// <param name="number">The one-based lap number.</param>
    /// <param name="time">The lap time, or <see langword="null"/> when the lap is incomplete.</param>
    /// <param name="sectors">The sector times recorded for this lap.</param>
    /// <param name="position">The overall position at the end of the lap, when known.</param>
    /// <param name="startedAt">The time elapsed in the session when the lap started, when known.</param>
    /// <param name="isPitLap">Whether the driver went through the pit lane during this lap.</param>
    /// <param name="telemetry">What LMU measured on the car during the lap, when known.</param>
    public Lap(
        int number,
        TimeSpan? time,
        IEnumerable<Sector> sectors,
        int? position = null,
        TimeSpan? startedAt = null,
        bool isPitLap = false,
        LapTelemetry? telemetry = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        ArgumentNullException.ThrowIfNull(sectors);

        Number = number;
        Time = time;
        Sectors = Array.AsReadOnly(sectors.ToArray());
        Position = position;
        StartedAt = startedAt;
        IsPitLap = isPitLap;
        Telemetry = telemetry ?? LapTelemetry.Empty;
    }

    /// <summary>
    /// Gets the one-based lap number.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// Gets the lap time, or <see langword="null"/> when the lap is incomplete.
    /// </summary>
    public TimeSpan? Time { get; }

    /// <summary>
    /// Gets the sector times recorded for this lap.
    /// </summary>
    public IReadOnlyList<Sector> Sectors { get; }

    /// <summary>
    /// Gets the overall position at the end of the lap, when known.
    /// </summary>
    public int? Position { get; }

    /// <summary>
    /// Gets the time elapsed in the session when the lap started, when known.
    /// </summary>
    public TimeSpan? StartedAt { get; }

    /// <summary>
    /// Gets whether the driver went through the pit lane during this lap (<c>pit="1"</c>). LMU marks
    /// the lap entering and often the lap leaving the pits, drive-through penalties and starts from
    /// the pit lane: the number of stops is <see cref="Driver.PitStops"/>.
    /// </summary>
    public bool IsPitLap { get; }

    /// <summary>
    /// Gets what LMU measured on the car during the lap: top speed, fuel, energy, tyres.
    /// </summary>
    public LapTelemetry Telemetry { get; }
}
