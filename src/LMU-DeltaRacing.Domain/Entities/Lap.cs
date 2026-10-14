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
    public Lap(int number, TimeSpan? time, IEnumerable<Sector> sectors)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        ArgumentNullException.ThrowIfNull(sectors);

        Number = number;
        Time = time;
        Sectors = Array.AsReadOnly(sectors.ToArray());
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
}
