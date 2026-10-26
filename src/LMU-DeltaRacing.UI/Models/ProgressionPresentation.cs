using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Builds the lap-time progression of the selected driver on one circuit and in one category.
/// </summary>
/// <remarks>
/// Each qualifying session or race gives one measure, the driver's best timed lap. Measures are
/// grouped into local periods (day, ISO week, month or year); each period keeps its fastest,
/// average and slowest measure. Practice sessions are not charted.
/// </remarks>
public static class ProgressionPresentation
{
    /// <summary>
    /// The color of the qualifying series when all cars are drawn together.
    /// </summary>
    public const string QualifyingColor = "#38bdf8";

    /// <summary>
    /// The color of the race series when all cars are drawn together (LMU red).
    /// </summary>
    public const string RaceColor = "#e11d48";

    /// <summary>
    /// The series colors of the per-car mode, given in alphabetical order of car and reused in a loop.
    /// The WEC purple, green and red are left out: they already mean best, gain and loss.
    /// </summary>
    public static IReadOnlyList<string> CarColors { get; } =
        ["#38bdf8", "#f97316", "#a3e635", "#f472b6", "#facc15", "#2dd4bf", "#c084fc", "#94a3b8"];

    private const string QualifyingLabel = "Qualification";
    private const string RaceLabel = "Course";

    /// <summary>
    /// Creates the chart series of a circuit and a category.
    /// </summary>
    /// <param name="sessions">The imported sessions, with driver merges applied.</param>
    /// <param name="userName">The selected driver.</param>
    /// <param name="trackVenue">The track venue.</param>
    /// <param name="trackCourse">The track layout.</param>
    /// <param name="category">The category, as returned by <see cref="CarCategory.FromCarClass"/>.</param>
    /// <param name="settings">The period length, plotted value and series split.</param>
    /// <param name="timeZone">The time zone of the periods, the local one in the application.</param>
    public static ProgressionChart Create(
        IEnumerable<Session> sessions,
        string? userName,
        string trackVenue,
        string trackCourse,
        string category,
        ProgressionSettings settings,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(trackVenue);
        ArgumentNullException.ThrowIfNull(trackCourse);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(timeZone);

        var measures = GetResults(sessions, userName)
            .Where(result => IsSameCircuit(result.Session, trackVenue, trackCourse)
                && string.Equals(result.Category, category, StringComparison.Ordinal))
            .Select(result => (Result: result, BestLap: result.Driver.Laps.Where(lap => lap.Time.HasValue).Min(lap => lap.Time)))
            .Where(measure => measure.BestLap.HasValue)
            .Select(measure => new Measure(
                measure.Result.Session.StartedAt,
                TimeZoneInfo.ConvertTime(measure.Result.Session.StartedAt, timeZone).DateTime,
                measure.Result.IsQualifying,
                measure.Result.Car,
                measure.BestLap!.Value))
            .ToArray();

        IReadOnlyList<ProgressionSeries> series = settings.Mode == ProgressionMode.MixedCars
            ?
            [
                .. CreateSeries(RaceLabel, RaceColor, measures.Where(measure => !measure.IsQualifying), settings),
                .. CreateSeries(QualifyingLabel, QualifyingColor, measures.Where(measure => measure.IsQualifying), settings)
            ]
            : measures
                .Where(measure => measure.IsQualifying == (settings.Session == ProgressionSession.Qualifying))
                .GroupBy(measure => measure.Car, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .SelectMany((group, index) => CreateSeries(group.Key, CarColors[index % CarColors.Count], group, settings))
                .ToArray();

        return new ProgressionChart(series, settings);
    }

    /// <summary>
    /// Returns the circuits of the selected driver's races and qualifying sessions, sorted by venue.
    /// </summary>
    public static IReadOnlyList<ProgressionCircuit> GetCircuits(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        return GetResults(sessions, userName)
            .GroupBy(
                result => (Venue: result.Session.TrackVenue, Course: result.Session.TrackCourse),
                CircuitComparer.Instance)
            .Select(circuit => new ProgressionCircuit(
                circuit.Key.Venue,
                circuit.Key.Course,
                circuit
                    .Select(result => result.Category)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(GetCategoryOrder)
                    .ThenBy(category => category, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                circuit.MaxBy(result => result.Session.StartedAt)!.Category))
            .OrderBy(circuit => circuit.TrackVenue, StringComparer.OrdinalIgnoreCase)
            .ThenBy(circuit => circuit.TrackCourse, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns the circuit and category of the driver's most recent race, or of the most recent
    /// qualifying session when there is no race.
    /// </summary>
    public static ProgressionSelection? GetDefaultSelection(IEnumerable<Session> sessions, string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var latest = GetResults(sessions, userName)
            .OrderBy(result => result.IsQualifying ? 1 : 0)
            .ThenByDescending(result => result.Session.StartedAt)
            .FirstOrDefault();

        return latest is null
            ? null
            : new ProgressionSelection(latest.Session.TrackVenue, latest.Session.TrackCourse, latest.Category);
    }

    private static IEnumerable<ProgressionSeries> CreateSeries(
        string label,
        string color,
        IEnumerable<Measure> measures,
        ProgressionSettings settings)
    {
        var values = measures.ToArray();
        if (values.Length == 0)
        {
            yield break;
        }

        var points = values
            .GroupBy(measure => ProgressionPeriods.Start(measure.LocalTime, settings.Granularity))
            .OrderBy(period => period.Key)
            .Select(period =>
            {
                var min = period.Min(measure => measure.BestLap);
                var max = period.Max(measure => measure.BestLap);
                var average = TimeSpan.FromTicks((long)period.Average(measure => measure.BestLap.Ticks));
                var value = settings.Aggregate switch
                {
                    ProgressionAggregate.Min => min,
                    ProgressionAggregate.Max => max,
                    _ => average
                };

                return new ProgressionPoint(period.Key, min, average, max, value, period.Count());
            })
            .ToArray();
        var record = values.OrderBy(measure => measure.BestLap).ThenBy(measure => measure.StartedAt).First();

        yield return new ProgressionSeries(
            label,
            label,
            color,
            points,
            new ProgressionRecord(record.BestLap, record.StartedAt),
            values.Length,
            settings.Granularity);
    }

    private static IEnumerable<SessionResult> GetResults(IEnumerable<Session> sessions, string? userName)
    {
        foreach (var session in sessions)
        {
            var isQualifying = session.SessionType.Contains("Qual", StringComparison.OrdinalIgnoreCase);
            if (!isQualifying && !session.SessionType.Contains("Race", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (DriverProfile.FindDriver(session, userName) is { } driver)
            {
                yield return new SessionResult(session, driver, isQualifying);
            }
        }
    }

    private static bool IsSameCircuit(Session session, string trackVenue, string trackCourse) =>
        string.Equals(session.TrackVenue.Trim(), trackVenue.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(session.TrackCourse.Trim(), trackCourse.Trim(), StringComparison.OrdinalIgnoreCase);

    private static int GetCategoryOrder(string category)
    {
        var index = CarCategory.Known.ToList().IndexOf(category);
        return index < 0 ? CarCategory.Known.Count : index;
    }

    private sealed record Measure(DateTimeOffset StartedAt, DateTime LocalTime, bool IsQualifying, string Car, TimeSpan BestLap);

    private sealed record SessionResult(Session Session, Driver Driver, bool IsQualifying)
    {
        public string Category { get; } = CarCategory.FromCarClass(Driver.CarClass);

        public string Car { get; } = !string.IsNullOrWhiteSpace(Driver.CarType)
            ? Driver.CarType.Trim()
            : string.IsNullOrWhiteSpace(Driver.CarNumber) ? "—" : $"#{Driver.CarNumber}";
    }

    private sealed class CircuitComparer : IEqualityComparer<(string Venue, string Course)>
    {
        public static CircuitComparer Instance { get; } = new();

        public bool Equals((string Venue, string Course) x, (string Venue, string Course) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Venue.Trim(), y.Venue.Trim())
            && StringComparer.OrdinalIgnoreCase.Equals(x.Course.Trim(), y.Course.Trim());

        public int GetHashCode((string Venue, string Course) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Venue.Trim()),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Course.Trim()));
    }
}
