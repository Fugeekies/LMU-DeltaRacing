namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The series of the progression chart for one circuit and one category.
/// </summary>
/// <param name="Series">The series, in legend order.</param>
/// <param name="Settings">The settings the chart was built with.</param>
public sealed record ProgressionChart(IReadOnlyList<ProgressionSeries> Series, ProgressionSettings Settings);

/// <summary>
/// One line of the progression chart: a session type, or a car.
/// </summary>
/// <param name="Key">The identifier used to hide or show the series.</param>
/// <param name="Label">The legend label.</param>
/// <param name="Color">The line color.</param>
/// <param name="Points">One point per period with at least one measure, in chronological order.</param>
/// <param name="Record">The fastest best lap of the series.</param>
/// <param name="SessionCount">The number of sessions measured.</param>
public sealed record ProgressionSeries(
    string Key,
    string Label,
    string Color,
    IReadOnlyList<ProgressionPoint> Points,
    ProgressionRecord Record,
    int SessionCount)
{
    /// <summary>
    /// Gets the first period of the series.
    /// </summary>
    public ProgressionPoint First => Points[0];

    /// <summary>
    /// Gets the last period of the series.
    /// </summary>
    public ProgressionPoint Last => Points[^1];

    /// <summary>
    /// Gets the plotted value of the last period minus the first one, negative when the driver got faster.
    /// Unknown with a single period.
    /// </summary>
    public TimeSpan? Gain => Points.Count < 2 ? null : Last.Value - First.Value;
}

/// <summary>
/// The best laps of one period.
/// </summary>
/// <param name="PeriodStart">The local start date of the period.</param>
/// <param name="Min">The fastest best lap.</param>
/// <param name="Average">The average of the best laps.</param>
/// <param name="Max">The slowest best lap.</param>
/// <param name="Value">The plotted value, chosen by the aggregate setting.</param>
/// <param name="SessionCount">The number of sessions in the period.</param>
public sealed record ProgressionPoint(
    DateTime PeriodStart,
    TimeSpan Min,
    TimeSpan Average,
    TimeSpan Max,
    TimeSpan Value,
    int SessionCount);

/// <summary>
/// The fastest best lap of a series.
/// </summary>
/// <param name="Time">The lap time.</param>
/// <param name="At">The start time of the session.</param>
public sealed record ProgressionRecord(TimeSpan Time, DateTimeOffset At);

/// <summary>
/// A circuit driven by the selected driver.
/// </summary>
/// <param name="TrackVenue">The track venue.</param>
/// <param name="TrackCourse">The track layout.</param>
/// <param name="Categories">The categories driven on the circuit, in display order.</param>
/// <param name="LatestCategory">The category of the most recent session on the circuit.</param>
public sealed record ProgressionCircuit(
    string TrackVenue,
    string TrackCourse,
    IReadOnlyList<string> Categories,
    string LatestCategory)
{
    /// <summary>
    /// Gets the value of the circuit drop-down list.
    /// </summary>
    public string Key => $"{TrackVenue}|{TrackCourse}";

    /// <summary>
    /// Gets the displayed name: the venue, followed by the layout when it differs.
    /// </summary>
    public string Label =>
        string.Equals(TrackVenue, TrackCourse, StringComparison.OrdinalIgnoreCase)
            ? TrackVenue
            : $"{TrackVenue} · {TrackCourse}";
}

/// <summary>
/// A circuit and a category to chart.
/// </summary>
/// <param name="TrackVenue">The track venue.</param>
/// <param name="TrackCourse">The track layout.</param>
/// <param name="Category">The category.</param>
public sealed record ProgressionSelection(string TrackVenue, string TrackCourse, string Category);
