namespace LMU_DeltaRacing.Domain.Entities;

/// <summary>
/// Represents a driver and their results in a session.
/// </summary>
public sealed class Driver
{
    /// <summary>
    /// Creates a driver with their vehicle and lap results.
    /// </summary>
    /// <param name="name">The driver's display name.</param>
    /// <param name="carNumber">The vehicle number, when available.</param>
    /// <param name="carClass">The vehicle class, when available.</param>
    /// <param name="teamName">The team name, when available.</param>
    /// <param name="position">The finishing position, when available.</param>
    /// <param name="laps">The laps recorded for the driver.</param>
    /// <param name="isPlayer">Whether this result belongs to the player.</param>
    public Driver(
        string name,
        string carNumber,
        string carClass,
        string teamName,
        int? position,
        IEnumerable<Lap> laps,
        bool isPlayer = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(carNumber);
        ArgumentNullException.ThrowIfNull(carClass);
        ArgumentNullException.ThrowIfNull(teamName);
        ArgumentNullException.ThrowIfNull(laps);

        Name = name;
        CarNumber = carNumber;
        CarClass = carClass;
        TeamName = teamName;
        Position = position;
        Laps = Array.AsReadOnly(laps.ToArray());
        IsPlayer = isPlayer;
    }

    /// <summary>
    /// Gets the driver's display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the vehicle number.
    /// </summary>
    public string CarNumber { get; }

    /// <summary>
    /// Gets the vehicle class.
    /// </summary>
    public string CarClass { get; }

    /// <summary>
    /// Gets the team name.
    /// </summary>
    public string TeamName { get; }

    /// <summary>
    /// Gets the finishing position, when available.
    /// </summary>
    public int? Position { get; }

    /// <summary>
    /// Gets the laps recorded for the driver.
    /// </summary>
    public IReadOnlyList<Lap> Laps { get; }

    /// <summary>
    /// Gets whether this result belongs to the player.
    /// </summary>
    public bool IsPlayer { get; }
}
