using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Reads the position of every car of a race lap by lap, for the race chart of "race details".
/// </summary>
/// <remarks>
/// LMU gives the overall position of a car at the end of each of its laps (<c>p</c>). Positions are
/// compared lap number by lap number: a lapped car ends its lap later than the leader, but its
/// position at the end of that lap is still its place in the race.
/// </remarks>
public static class RacePositionsPresentation
{
    /// <summary>
    /// Creates the positions of the cars of a race, around a focused driver, or returns
    /// <see langword="null"/> for another kind of session or a driver without any lap position.
    /// </summary>
    /// <param name="session">The race.</param>
    /// <param name="focusName">The driver whose line is highlighted and whose class is kept in the class view.</param>
    /// <param name="playerName">The selected driver of the dashboard.</param>
    /// <param name="scope">Overall positions, or positions within the class of the focused driver.</param>
    public static RacePositions? Create(Session session, string? focusName, string? playerName, PositionScope scope)
    {
        ArgumentNullException.ThrowIfNull(session);

        var focus = DriverProfile.FindDriver(session, focusName);
        if (session.Kind != SessionKind.Race || focus is null || !focus.Laps.Any(lap => lap.Position.HasValue))
        {
            return null;
        }

        // Two merged names can appear in one session: the first result is kept, as in the other views.
        var drivers = session.Drivers
            .Where(driver => driver.Laps.Any(lap => lap.Position.HasValue))
            .Where(driver => scope == PositionScope.Overall
                || string.Equals(driver.CarClass.Trim(), focus.CarClass.Trim(), StringComparison.OrdinalIgnoreCase))
            .GroupBy(driver => driver.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        var positions = scope == PositionScope.Overall ? OverallPositions(drivers) : ClassPositions(drivers);

        var lines = drivers
            .Select(driver => new RacePositionLine(
                driver.Name.Trim(),
                driver.Category(),
                IsSameDriver(driver, focus.Name),
                IsSameDriver(driver, playerName),
                RaceResults.GetKind(driver.FinishStatus, driver.ClassPosition) is HomeResultKind.Retired or HomeResultKind.Disqualified,
                positions[driver]))
            .OrderBy(line => line.Points[^1].Position)
            .ToArray();

        return new RacePositions(
            focus.Name.Trim(),
            scope,
            lines.Max(line => line.Points[^1].Lap),
            lines.Length,
            lines);
    }

    private static Dictionary<Driver, IReadOnlyList<RacePositionPoint>> OverallPositions(IEnumerable<Driver> drivers) =>
        drivers.ToDictionary(
            driver => driver,
            driver => (IReadOnlyList<RacePositionPoint>)
            [
                .. driver.GridPosition is { } grid ? [new RacePositionPoint(0, grid)] : Array.Empty<RacePositionPoint>(),
                .. driver.Laps
                    .Where(lap => lap.Position.HasValue)
                    .OrderBy(lap => lap.Number)
                    .Select(lap => new RacePositionPoint(lap.Number, lap.Position!.Value))
            ]);

    /// <summary>
    /// Ranks the cars of one class among themselves on the grid and at the end of each lap, from
    /// their overall positions: the order within a class is the overall order.
    /// </summary>
    private static Dictionary<Driver, IReadOnlyList<RacePositionPoint>> ClassPositions(IReadOnlyCollection<Driver> drivers)
    {
        var points = drivers.ToDictionary(driver => driver, _ => new List<RacePositionPoint>());

        var grid = drivers.Where(driver => driver.GridPosition.HasValue).OrderBy(driver => driver.GridPosition).ToArray();
        for (var index = 0; index < grid.Length; index++)
        {
            points[grid[index]].Add(new RacePositionPoint(0, index + 1));
        }

        var laps = drivers
            .SelectMany(driver => driver.Laps
                .Where(lap => lap.Position.HasValue)
                .Select(lap => (Driver: driver, lap.Number, Position: lap.Position!.Value)))
            .GroupBy(lap => lap.Number)
            .OrderBy(group => group.Key);
        foreach (var lap in laps)
        {
            var rank = 0;
            foreach (var car in lap.OrderBy(car => car.Position))
            {
                points[car.Driver].Add(new RacePositionPoint(lap.Key, ++rank));
            }
        }

        return points.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<RacePositionPoint>)entry.Value);
    }

    private static bool IsSameDriver(Driver driver, string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && string.Equals(driver.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Which positions the race chart shows.
/// </summary>
public enum PositionScope
{
    /// <summary>Positions within the class of the focused driver, as in WEC timing.</summary>
    Class,

    /// <summary>Overall positions, every class together.</summary>
    Overall
}

/// <summary>
/// The positions of the cars of a race, lap by lap.
/// </summary>
/// <param name="FocusName">The highlighted driver.</param>
/// <param name="Scope">Overall or class positions.</param>
/// <param name="LastLap">The last lap completed by a car.</param>
/// <param name="FieldSize">The number of cars with a line.</param>
/// <param name="Lines">The line of each car, by final position.</param>
public sealed record RacePositions(
    string FocusName,
    PositionScope Scope,
    int LastLap,
    int FieldSize,
    IReadOnlyList<RacePositionLine> Lines)
{
    /// <summary>
    /// Keeps the focused driver and the cars that were at most <paramref name="places"/> places
    /// from them on the grid or at the end of a lap; every car without a number of places.
    /// </summary>
    public IReadOnlyList<RacePositionLine> Around(int? places)
    {
        if (places is not { } range || Lines.FirstOrDefault(line => line.IsFocus) is not { } focus)
        {
            return Lines;
        }

        var focusPositions = focus.Points.ToDictionary(point => point.Lap, point => point.Position);
        return Lines
            .Where(line => line.IsFocus || line.Points.Any(point =>
                focusPositions.TryGetValue(point.Lap, out var position) && Math.Abs(point.Position - position) <= range))
            .ToArray();
    }
}

/// <summary>
/// The line of one car on the race chart.
/// </summary>
/// <param name="Name">The driver name.</param>
/// <param name="Category">The WEC category of the car.</param>
/// <param name="IsFocus">Whether this is the highlighted driver.</param>
/// <param name="IsPlayer">Whether this is the selected driver of the dashboard.</param>
/// <param name="Retired">Whether the car retired or was disqualified: its line stops at its last lap.</param>
/// <param name="Points">The grid position (lap 0), then the position at the end of each lap.</param>
public sealed record RacePositionLine(
    string Name,
    string Category,
    bool IsFocus,
    bool IsPlayer,
    bool Retired,
    IReadOnlyList<RacePositionPoint> Points);

/// <summary>
/// A position of a car.
/// </summary>
/// <param name="Lap">The lap, 0 for the grid.</param>
/// <param name="Position">The position at the end of the lap.</param>
public sealed record RacePositionPoint(int Lap, int Position);
