using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Analyses the race of one driver lap by lap: regularity, stints, fuel, energy and tyre wear.
/// </summary>
/// <remarks>
/// A representative lap is a timed lap that is neither the first lap nor a pit lap, as for the
/// reference lap of <see cref="RaceStoryPresentation"/>, and that is no slower than 107 % of the
/// driver's median of these laps: a spin, a yellow flag or a contact says nothing about the pace.
/// Fuel, energy and tyre wear are only known when the server shares them, often not at all.
/// </remarks>
public static class LapAnalysisPresentation
{
    /// <summary>
    /// The slowest representative lap, as a ratio of the driver's median lap.
    /// </summary>
    public const double RepresentativeLapLimit = 1.07;

    /// <summary>
    /// The fewest representative laps for a regularity: a deviation of two laps says nothing.
    /// </summary>
    public const int MinimumRegularityLaps = 3;

    /// <summary>
    /// Creates the analysis of a driver in a race, or returns <see langword="null"/> for another
    /// kind of session, an unknown driver or a driver without any lap.
    /// </summary>
    /// <param name="session">The race.</param>
    /// <param name="driverName">The driver to analyse.</param>
    public static LapAnalysis? Create(Session session, string? driverName)
    {
        ArgumentNullException.ThrowIfNull(session);

        var driver = DriverProfile.FindDriver(session, driverName);
        if (session.Kind != SessionKind.Race || driver is null || driver.Laps.Count == 0)
        {
            return null;
        }

        var laps = driver.Laps.OrderBy(lap => lap.Number).ToArray();
        var representative = FindRepresentativeLaps(laps);
        var stintNumbers = NumberStints(laps);
        var analysisLaps = laps
            .Select(lap => new LapAnalysisLap(
                lap.Number,
                lap.Time,
                lap.Position,
                lap.IsPitLap,
                representative.Contains(lap.Number),
                stintNumbers[lap.Number],
                lap.Telemetry,
                lap.Sectors))
            .ToArray();

        var classDrivers = session.Drivers
            .Where(other => string.Equals(other.CarClass.Trim(), driver.CarClass.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var sectors = CreateSectors(laps, representative, classDrivers);
        var weakest = sectors.Where(sector => sector.GapToClassPace > TimeSpan.Zero).MaxBy(sector => sector.GapToClassPace);

        return new LapAnalysis(
            driver.Name,
            driver.PitStops,
            CreateRegularity(driver, laps, representative, classDrivers),
            CreateTopSpeed(laps, classDrivers),
            CreateStints(analysisLaps),
            CreateConsumption(analysisLaps),
            analysisLaps,
            sectors,
            sectors.Count == 3 && sectors.All(sector => sector.Best.HasValue) ? sectors.Aggregate(TimeSpan.Zero, (total, sector) => total + sector.Best!.Value) : null,
            driver.BestLap,
            weakest?.Number);
    }

    /// <summary>
    /// Returns the races of the selected driver that have laps to analyse, most recent first.
    /// </summary>
    /// <param name="sessions">The imported sessions, with driver merges applied.</param>
    /// <param name="userName">The selected driver.</param>
    public static IReadOnlyList<Session> GetRaces(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        return sessions
            .Where(session => session.Kind == SessionKind.Race
                && DriverProfile.FindDriver(session, userName) is { Laps.Count: > 0 })
            .OrderByDescending(session => session.StartedAt)
            .ToArray();
    }

    /// <summary>
    /// Returns the drivers of a race that have laps to analyse, in finishing order.
    /// </summary>
    /// <param name="session">The race.</param>
    public static IReadOnlyList<Driver> GetDrivers(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return session.Drivers
            .Where(driver => driver.Laps.Count > 0)
            .OrderBy(driver => driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns the standard deviation of the representative laps of a driver, or <see langword="null"/>
    /// with fewer than <see cref="MinimumRegularityLaps"/> of them.
    /// </summary>
    public static TimeSpan? Deviation(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        var laps = driver.Laps.OrderBy(lap => lap.Number).ToArray();
        var representative = FindRepresentativeLaps(laps);
        return Deviation(laps.Where(lap => representative.Contains(lap.Number)).Select(lap => lap.Time!.Value).ToArray());
    }

    /// <summary>
    /// Returns the median of the representative laps of a driver, their race pace, or
    /// <see langword="null"/> without any of them.
    /// </summary>
    public static TimeSpan? Pace(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        var laps = driver.Laps.OrderBy(lap => lap.Number).ToArray();
        var representative = FindRepresentativeLaps(laps);
        return Median(laps.Where(lap => representative.Contains(lap.Number)).Select(lap => lap.Time!.Value));
    }

    /// <summary>
    /// Returns the highest top speed of a driver in a session, or <see langword="null"/> when unknown.
    /// </summary>
    public static double? TopSpeed(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        return driver.Laps.Max(lap => lap.Telemetry.TopSpeed);
    }

    private static HashSet<int> FindRepresentativeLaps(IReadOnlyList<Lap> laps)
    {
        var candidates = laps
            .Where(lap => lap.Time.HasValue && lap.Number > 1 && !lap.IsPitLap)
            .ToArray();
        if (Median(candidates.Select(lap => lap.Time!.Value)) is not { } median)
        {
            return [];
        }

        var limit = median * RepresentativeLapLimit;
        return candidates.Where(lap => lap.Time <= limit).Select(lap => lap.Number).ToHashSet();
    }

    /// <summary>
    /// Numbers the stints of each lap. A stint ends on the first lap of each run of pit laps (the lap
    /// entering the pits); the pit lane start of the first lap does not end one.
    /// </summary>
    private static Dictionary<int, int> NumberStints(IReadOnlyList<Lap> laps)
    {
        var stints = new Dictionary<int, int>(laps.Count);
        var stint = 1;
        var previousWasPit = false;
        var startedFromPits = laps.Count > 0 && laps[0].IsPitLap;
        foreach (var lap in laps)
        {
            stints[lap.Number] = stint;
            if (lap.IsPitLap && !previousWasPit && !startedFromPits)
            {
                stint++;
            }

            startedFromPits &= lap.IsPitLap;
            previousWasPit = lap.IsPitLap;
        }

        return stints;
    }

    /// <summary>
    /// Analyses the three sectors. Bests come from every timed lap, as in the race details; paces and
    /// deviations from the representative laps only. The class pace of a sector is the best median
    /// of that sector among the drivers of the class: the gap to it shows where the time is lost.
    /// </summary>
    private static IReadOnlyList<LapSectorAnalysis> CreateSectors(
        IReadOnlyList<Lap> laps,
        HashSet<int> representative,
        IReadOnlyList<Driver> classDrivers)
    {
        var classLaps = classDrivers
            .Select(other =>
            {
                var ordered = other.Laps.OrderBy(lap => lap.Number).ToArray();
                var kept = FindRepresentativeLaps(ordered);
                return (Timed: ordered.Where(lap => lap.Time.HasValue).ToArray(), Representative: ordered.Where(lap => kept.Contains(lap.Number)).ToArray());
            })
            .ToArray();

        return Enumerable.Range(1, 3)
            .Select(number =>
            {
                var paceTimes = SectorTimes(laps.Where(lap => representative.Contains(lap.Number)), number);
                var pace = Median(paceTimes);
                var classPace = Minimum(classLaps.Select(other => Median(SectorTimes(other.Representative, number))).OfType<TimeSpan>());
                var classBest = Minimum(classLaps.SelectMany(other => SectorTimes(other.Timed, number)));
                return new LapSectorAnalysis(
                    number,
                    Minimum(SectorTimes(laps.Where(lap => lap.Time.HasValue), number)),
                    classBest,
                    pace,
                    Deviation(paceTimes),
                    classPace,
                    pace - classPace);
            })
            .ToArray();
    }

    private static TimeSpan? Minimum(IEnumerable<TimeSpan> times)
    {
        var values = times.ToArray();
        return values.Length == 0 ? null : values.Min();
    }

    private static TimeSpan[] SectorTimes(IEnumerable<Lap> laps, int number) =>
        laps.SelectMany(lap => lap.Sectors.Where(sector => sector.Number == number).Select(sector => sector.Time)).ToArray();

    private static LapRegularity? CreateRegularity(
        Driver driver,
        IReadOnlyList<Lap> laps,
        HashSet<int> representative,
        IReadOnlyList<Driver> classDrivers)
    {
        var times = laps.Where(lap => representative.Contains(lap.Number)).Select(lap => lap.Time!.Value).ToArray();
        if (Deviation(times) is not { } deviation || driver.BestLap is not { } best)
        {
            return null;
        }

        var average = TimeSpan.FromTicks((long)times.Average(time => time.Ticks));
        var classDeviations = classDrivers
            .Select(Deviation)
            .OfType<TimeSpan>()
            .ToArray();
        var timedLaps = laps.Count(lap => lap.Time.HasValue && lap.Number > 1 && !lap.IsPitLap);

        return new LapRegularity(
            times.Length,
            timedLaps - times.Length,
            deviation,
            Median(times)!.Value,
            average - best,
            classDeviations.Count(other => other < deviation) + 1,
            classDeviations.Length);
    }

    private static LapTopSpeed? CreateTopSpeed(IReadOnlyList<Lap> laps, IReadOnlyList<Driver> classDrivers)
    {
        var fastest = laps
            .Where(lap => lap.Telemetry.TopSpeed.HasValue)
            .OrderByDescending(lap => lap.Telemetry.TopSpeed)
            .ThenBy(lap => lap.Number)
            .FirstOrDefault();
        if (fastest is null)
        {
            return null;
        }

        var classBest = classDrivers.Select(TopSpeed).Max();
        return new LapTopSpeed(fastest.Telemetry.TopSpeed!.Value, fastest.Number, classBest ?? fastest.Telemetry.TopSpeed!.Value);
    }

    private static IReadOnlyList<LapStint> CreateStints(IReadOnlyList<LapAnalysisLap> laps) =>
        laps
            .GroupBy(lap => lap.Stint)
            .Select(group =>
            {
                var stintLaps = group.ToArray();
                var times = stintLaps.Where(lap => lap.IsRepresentative).Select(lap => lap.Time!.Value).ToArray();
                var compounds = stintLaps
                    .Select(lap => FormatCompounds(lap.Telemetry))
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return new LapStint(
                    group.Key,
                    stintLaps[0].Number,
                    stintLaps[^1].Number,
                    compounds,
                    Median(times),
                    stintLaps.Where(lap => lap.Time.HasValue).Min(lap => lap.Time),
                    AverageUse(stintLaps, lap => lap.Telemetry.FuelUsed),
                    AverageUse(stintLaps, lap => lap.Telemetry.VirtualEnergyUsed),
                    MeasureWear(stintLaps));
            })
            .ToArray();

    /// <summary>
    /// Returns the compounds of a lap: one name when the front and rear match, "front / rear" otherwise.
    /// </summary>
    public static string FormatCompounds(LapTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        var front = telemetry.FrontCompound;
        var rear = telemetry.RearCompound;
        if (front.Length == 0 || rear.Length == 0 || string.Equals(front, rear, StringComparison.OrdinalIgnoreCase))
        {
            return front.Length > 0 ? front : rear;
        }

        return $"{front} / {rear}";
    }

    private static LapConsumption? CreateConsumption(IReadOnlyList<LapAnalysisLap> laps)
    {
        var fuel = AverageUse(laps, lap => lap.Telemetry.FuelUsed);
        var energy = AverageUse(laps, lap => lap.Telemetry.VirtualEnergyUsed);
        var wear = AverageWearPerLap(laps);
        if (fuel is null && energy is null && wear is null)
        {
            return null;
        }

        return new LapConsumption(
            fuel,
            fuel is > 0 ? 1 / fuel.Value : null,
            energy,
            energy is > 0 ? 1 / energy.Value : null,
            wear);
    }

    /// <summary>
    /// Averages a use over the representative laps: the first lap includes the start, a pit lap the
    /// refuelling, which makes its use negative.
    /// </summary>
    private static double? AverageUse(IEnumerable<LapAnalysisLap> laps, Func<LapAnalysisLap, double?> use)
    {
        var values = laps
            .Where(lap => lap.IsRepresentative)
            .Select(use)
            .OfType<double>()
            .Where(value => value > 0)
            .ToArray();
        return values.Length == 0 ? null : values.Average();
    }

    /// <summary>
    /// Measures the tread worn during a stint: from the first lap of the stint, which may include a
    /// tyre change, to its last lap.
    /// </summary>
    private static TyreWear? MeasureWear(IReadOnlyList<LapAnalysisLap> laps)
    {
        var known = laps.Where(lap => lap.Telemetry.TyreWear is not null).ToArray();
        if (known.Length < 2)
        {
            return null;
        }

        var first = known[0].Telemetry.TyreWear!;
        var last = known[^1].Telemetry.TyreWear!;
        return new TyreWear(
            first.FrontLeft - last.FrontLeft,
            first.FrontRight - last.FrontRight,
            first.RearLeft - last.RearLeft,
            first.RearRight - last.RearRight);
    }

    /// <summary>
    /// Averages the tread worn per lap between two consecutive representative laps; a tyre change
    /// only happens on a pit lap, which is never representative.
    /// </summary>
    private static TyreWear? AverageWearPerLap(IReadOnlyList<LapAnalysisLap> laps)
    {
        var wears = new List<TyreWear>();
        for (var index = 1; index < laps.Count; index++)
        {
            var lap = laps[index];
            if (lap.IsRepresentative
                && laps[index - 1].Number == lap.Number - 1
                && !laps[index - 1].IsPit
                && laps[index - 1].Telemetry.TyreWear is { } before
                && lap.Telemetry.TyreWear is { } after)
            {
                wears.Add(new TyreWear(
                    before.FrontLeft - after.FrontLeft,
                    before.FrontRight - after.FrontRight,
                    before.RearLeft - after.RearLeft,
                    before.RearRight - after.RearRight));
            }
        }

        return wears.Count == 0
            ? null
            : new TyreWear(
                wears.Average(wear => wear.FrontLeft),
                wears.Average(wear => wear.FrontRight),
                wears.Average(wear => wear.RearLeft),
                wears.Average(wear => wear.RearRight));
    }

    private static TimeSpan? Deviation(IReadOnlyList<TimeSpan> times)
    {
        if (times.Count < MinimumRegularityLaps)
        {
            return null;
        }

        var mean = times.Average(time => time.TotalSeconds);
        var variance = times.Sum(time => Math.Pow(time.TotalSeconds - mean, 2)) / times.Count;
        return TimeSpan.FromSeconds(Math.Sqrt(variance));
    }

    private static TimeSpan? Median(IEnumerable<TimeSpan> times)
    {
        var sorted = times.Order().ToArray();
        if (sorted.Length == 0)
        {
            return null;
        }

        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1
            ? sorted[middle]
            : TimeSpan.FromTicks((sorted[middle - 1].Ticks + sorted[middle].Ticks) / 2);
    }
}

/// <summary>
/// The lap by lap analysis of one driver in a race.
/// </summary>
/// <param name="DriverName">The driver name.</param>
/// <param name="PitStops">The number of pit stops counted by LMU, when known.</param>
/// <param name="Regularity">The regularity of the driver, when enough laps are representative.</param>
/// <param name="TopSpeed">The top speed of the driver, when known.</param>
/// <param name="Stints">The stints, in order.</param>
/// <param name="Consumption">The fuel, energy and tyre use per lap, when the server shares them.</param>
/// <param name="Laps">The laps, in order.</param>
/// <param name="Sectors">The analysis of the three sectors, in order.</param>
/// <param name="IdealLap">The sum of the best sectors, when the three are known.</param>
/// <param name="BestLap">The best timed lap of the driver.</param>
/// <param name="WeakestSector">The sector with the largest gap to the class pace, when one is behind it.</param>
public sealed record LapAnalysis(
    string DriverName,
    int? PitStops,
    LapRegularity? Regularity,
    LapTopSpeed? TopSpeed,
    IReadOnlyList<LapStint> Stints,
    LapConsumption? Consumption,
    IReadOnlyList<LapAnalysisLap> Laps,
    IReadOnlyList<LapSectorAnalysis> Sectors,
    TimeSpan? IdealLap,
    TimeSpan? BestLap,
    int? WeakestSector);

/// <summary>
/// One sector of a driver in a race, compared with their class.
/// </summary>
/// <param name="Number">The sector number, from 1 to 3.</param>
/// <param name="Best">The best time of the driver in this sector, on timed laps.</param>
/// <param name="ClassBest">The best time of the class in this sector, on timed laps.</param>
/// <param name="Pace">The median of the sector on the representative laps of the driver.</param>
/// <param name="Deviation">The standard deviation of the sector on the representative laps, from three laps.</param>
/// <param name="ClassPace">The best sector median among the drivers of the class.</param>
/// <param name="GapToClassPace">The pace of the driver minus the class pace: the time lost per lap.</param>
public sealed record LapSectorAnalysis(
    int Number,
    TimeSpan? Best,
    TimeSpan? ClassBest,
    TimeSpan? Pace,
    TimeSpan? Deviation,
    TimeSpan? ClassPace,
    TimeSpan? GapToClassPace)
{
    /// <summary>
    /// Gets whether the best time of the driver is the best of the class in this sector.
    /// </summary>
    public bool IsClassBest => Best.HasValue && Best == ClassBest;
}

/// <summary>
/// One lap of a lap by lap analysis.
/// </summary>
/// <param name="Number">The lap number.</param>
/// <param name="Time">The lap time, when timed.</param>
/// <param name="Position">The overall position at the end of the lap.</param>
/// <param name="IsPit">Whether the driver went through the pit lane.</param>
/// <param name="IsRepresentative">Whether the lap counts for the pace and the regularity.</param>
/// <param name="Stint">The number of the stint of the lap, from 1.</param>
/// <param name="Telemetry">What LMU measured during the lap.</param>
/// <param name="Sectors">The sector times of the lap; a sector may be missing.</param>
public sealed record LapAnalysisLap(
    int Number,
    TimeSpan? Time,
    int? Position,
    bool IsPit,
    bool IsRepresentative,
    int Stint,
    LapTelemetry Telemetry,
    IReadOnlyList<Sector> Sectors)
{
    /// <summary>
    /// Returns the time of a sector of the lap, when known.
    /// </summary>
    /// <param name="number">The sector number, from 1 to 3.</param>
    public TimeSpan? SectorTime(int number) => Sectors.FirstOrDefault(sector => sector.Number == number)?.Time;
}

/// <summary>
/// How regular the laps of a driver are, compared with the drivers of their class.
/// </summary>
/// <param name="Laps">The number of representative laps.</param>
/// <param name="ExcludedLaps">The timed laps left out for being slower than 107 % of the median.</param>
/// <param name="Deviation">The standard deviation of the representative laps.</param>
/// <param name="Pace">The median of the representative laps.</param>
/// <param name="AverageGapToBest">The average representative lap minus the best lap of the driver.</param>
/// <param name="ClassRank">The rank of the deviation in the class, 1 being the most regular.</param>
/// <param name="ClassDrivers">The drivers of the class with a regularity.</param>
public sealed record LapRegularity(
    int Laps,
    int ExcludedLaps,
    TimeSpan Deviation,
    TimeSpan Pace,
    TimeSpan AverageGapToBest,
    int ClassRank,
    int ClassDrivers);

/// <summary>
/// The top speed of a driver in a race.
/// </summary>
/// <param name="Speed">The highest top speed in km/h.</param>
/// <param name="Lap">The lap where it was reached.</param>
/// <param name="ClassBest">The highest top speed of the class in km/h.</param>
public sealed record LapTopSpeed(double Speed, int Lap, double ClassBest);

/// <summary>
/// The laps between two passages through the pit lane.
/// </summary>
/// <param name="Number">The stint number, from 1.</param>
/// <param name="FirstLap">The first lap of the stint.</param>
/// <param name="LastLap">The last lap of the stint.</param>
/// <param name="Compounds">The tyre compounds used during the stint.</param>
/// <param name="Pace">The median of the representative laps of the stint.</param>
/// <param name="BestLap">The best lap of the stint.</param>
/// <param name="FuelPerLap">The fuel used per lap, as a fraction of a full tank, when known.</param>
/// <param name="EnergyPerLap">The virtual energy used per lap, as a fraction, when known.</param>
/// <param name="TyreWear">The tread worn on each tyre during the stint, when known.</param>
public sealed record LapStint(
    int Number,
    int FirstLap,
    int LastLap,
    IReadOnlyList<string> Compounds,
    TimeSpan? Pace,
    TimeSpan? BestLap,
    double? FuelPerLap,
    double? EnergyPerLap,
    TyreWear? TyreWear)
{
    /// <summary>
    /// Gets the number of laps of the stint.
    /// </summary>
    public int LapCount => LastLap - FirstLap + 1;
}

/// <summary>
/// The fuel, energy and tyre use per lap of a driver in a race.
/// </summary>
/// <param name="FuelPerLap">The fuel used per lap, as a fraction of a full tank, when known.</param>
/// <param name="LapsPerTank">The laps a full tank lasts at this use, when known.</param>
/// <param name="EnergyPerLap">The virtual energy used per lap, as a fraction, when known.</param>
/// <param name="LapsPerEnergy">The laps a full virtual energy lasts at this use, when known.</param>
/// <param name="TyreWearPerLap">The tread worn per lap on each tyre, when known.</param>
public sealed record LapConsumption(
    double? FuelPerLap,
    double? LapsPerTank,
    double? EnergyPerLap,
    double? LapsPerEnergy,
    TyreWear? TyreWearPerLap);
