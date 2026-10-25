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
    /// <param name="carType">The vehicle model, when available.</param>
    /// <param name="gridPosition">The starting grid position, when available.</param>
    /// <param name="finishTime">The total race time, when available.</param>
    /// <param name="classPosition">The finishing position within the vehicle class, when available.</param>
    /// <param name="lapsCompleted">The number of laps completed by the driver, when available.</param>
    /// <param name="finishStatus">The finish status reported by LMU (for example "DNF"), when available.</param>
    public Driver(
        string name,
        string carNumber,
        string carClass,
        string teamName,
        int? position,
        IEnumerable<Lap> laps,
        string carType = "",
        int? gridPosition = null,
        TimeSpan? finishTime = null,
        int? classPosition = null,
        int? lapsCompleted = null,
        string finishStatus = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(carNumber);
        ArgumentNullException.ThrowIfNull(carClass);
        ArgumentNullException.ThrowIfNull(teamName);
        ArgumentNullException.ThrowIfNull(laps);
        ArgumentNullException.ThrowIfNull(carType);
        ArgumentNullException.ThrowIfNull(finishStatus);

        Name = name;
        CarNumber = carNumber;
        CarClass = carClass;
        TeamName = teamName;
        Position = position;
        Laps = Array.AsReadOnly(laps.ToArray());
        CarType = carType;
        GridPosition = gridPosition;
        FinishTime = finishTime;
        ClassPosition = classPosition;
        LapsCompleted = lapsCompleted;
        FinishStatus = finishStatus;
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
    /// Gets the vehicle model, when available.
    /// </summary>
    public string CarType { get; }

    /// <summary>
    /// Gets the starting grid position, when available.
    /// </summary>
    public int? GridPosition { get; }

    /// <summary>
    /// Gets the total race time, when available.
    /// </summary>
    public TimeSpan? FinishTime { get; }

    /// <summary>
    /// Gets the finishing position, when available.
    /// </summary>
    public int? Position { get; }

    /// <summary>
    /// Gets the finishing position within the vehicle class, when available.
    /// Multiclass sessions are compared within each class, as in WEC timing.
    /// </summary>
    public int? ClassPosition { get; }

    /// <summary>
    /// Gets the number of laps completed by the driver, when available.
    /// </summary>
    public int? LapsCompleted { get; }

    /// <summary>
    /// Gets the finish status reported by LMU ("Finished Normally", "DNF", "DQ", "None"...), or an empty string.
    /// </summary>
    public string FinishStatus { get; }

    /// <summary>
    /// Gets the laps recorded for the driver.
    /// </summary>
    public IReadOnlyList<Lap> Laps { get; }
}
