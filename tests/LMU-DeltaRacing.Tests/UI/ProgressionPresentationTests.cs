using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.UI.Models;
using Microsoft.Extensions.Configuration;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the lap-time progression series, periods and chart layout.
/// </summary>
[TestClass]
public sealed class ProgressionPresentationTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [TestMethod]
    public void Periods_start_on_the_iso_week_month_and_year()
    {
        var wednesday = new DateTime(2026, 10, 7, 21, 0, 0);
        var sunday = new DateTime(2026, 10, 11, 21, 0, 0);

        ProgressionPeriods.Start(wednesday, ProgressionGranularity.Day).Should().Be(new DateTime(2026, 10, 7));
        ProgressionPeriods.Start(wednesday, ProgressionGranularity.Week).Should().Be(new DateTime(2026, 10, 5));
        ProgressionPeriods.Start(sunday, ProgressionGranularity.Week).Should().Be(new DateTime(2026, 10, 5));
        ProgressionPeriods.Start(wednesday, ProgressionGranularity.Month).Should().Be(new DateTime(2026, 10, 1));
        ProgressionPeriods.Start(wednesday, ProgressionGranularity.Year).Should().Be(new DateTime(2026, 1, 1));
        ProgressionPeriods.Next(new DateTime(2026, 12, 1), ProgressionGranularity.Month).Should().Be(new DateTime(2027, 1, 1));
        ProgressionPeriods.Next(new DateTime(2026, 10, 5), ProgressionGranularity.Week).Should().Be(new DateTime(2026, 10, 12));
    }

    [TestMethod]
    public void Periods_have_french_labels()
    {
        ProgressionPeriods.Label(new DateTime(2025, 11, 1), ProgressionGranularity.Month).Should().Be("novembre 2025");
        ProgressionPeriods.Label(new DateTime(2026, 10, 5), ProgressionGranularity.Week).Should().Be("semaine du 05/10/2026");
        ProgressionPeriods.Label(new DateTime(2026, 10, 7), ProgressionGranularity.Day).Should().Be("07/10/2026");
        ProgressionPeriods.Label(new DateTime(2026, 1, 1), ProgressionGranularity.Year).Should().Be("2026");
        ProgressionPeriods.AxisLabel(new DateTime(2026, 2, 1), ProgressionGranularity.Month).Should().Be("févr. 26");
    }

    [TestMethod]
    public void Create_groups_best_laps_by_month_with_one_series_per_session_type()
    {
        var sessions = new[]
        {
            Session("Qualify", "2026-01-10T20:00:00Z", 100.5),
            Session("Race", "2026-01-10T20:30:00Z", 102, 101),
            Session("Race", "2026-01-20T20:30:00Z", 103),
            Session("Race", "2026-02-05T20:30:00Z", 100)
        };

        var chart = Create(sessions, ProgressionSettings.Default);

        chart.Series.Select(series => series.Label).Should().Equal("Course", "Qualification");
        var race = chart.Series[0];
        race.Color.Should().Be("#e11d48");
        race.SessionCount.Should().Be(3);
        race.Points.Should().HaveCount(2);
        race.Points[0].PeriodStart.Should().Be(new DateTime(2026, 1, 1));
        race.Points[0].Min.Should().Be(Seconds(101));
        race.Points[0].Max.Should().Be(Seconds(103));
        race.Points[0].Average.Should().Be(Seconds(102));
        race.Points[0].Value.Should().Be(Seconds(102), "the average is plotted by default");
        race.Points[0].SessionCount.Should().Be(2);
        chart.Series[1].Color.Should().Be("#38bdf8");
        chart.Series[1].Points.Should().ContainSingle().Which.Value.Should().Be(Seconds(100.5));
    }

    [TestMethod]
    public void Create_plots_the_selected_aggregate()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 101),
            Session("Race", "2026-01-20T20:30:00Z", 103)
        };

        Create(sessions, ProgressionSettings.Default with { Aggregate = ProgressionAggregate.Min })
            .Series[0].Points[0].Value.Should().Be(Seconds(101));
        Create(sessions, ProgressionSettings.Default with { Aggregate = ProgressionAggregate.Max })
            .Series[0].Points[0].Value.Should().Be(Seconds(103));
    }

    [TestMethod]
    public void Create_uses_the_local_date_of_the_session()
    {
        var paris = TimeZoneInfo.CreateCustomTimeZone("UTC+2", TimeSpan.FromHours(2), "UTC+2", "UTC+2");
        var sessions = new[] { Session("Race", "2026-10-31T23:30:00Z", 100) };

        var chart = ProgressionPresentation.Create(sessions, "Driver", "Spa", "Spa", "LMGT3", ProgressionSettings.Default, paris);

        chart.Series[0].Points[0].PeriodStart.Should().Be(new DateTime(2026, 11, 1));
    }

    [TestMethod]
    public void Create_keeps_only_the_selected_driver_circuit_category_and_timed_sessions()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 100),
            Session("Race", "2026-01-11T20:30:00Z", 90, track: "Monza"),
            Session("Race", "2026-01-12T20:30:00Z", 90, carClass: "LMP2"),
            Session("Race", "2026-01-13T20:30:00Z", 90, driverName: "Someone else"),
            Session("Practice1", "2026-01-14T20:30:00Z", 90),
            SessionWithoutTimedLap("Race", "2026-01-15T20:30:00Z")
        };

        var chart = Create(sessions, ProgressionSettings.Default);

        chart.Series.Should().ContainSingle().Which.SessionCount.Should().Be(1);
        chart.Series[0].Points[0].Value.Should().Be(Seconds(100));
    }

    [TestMethod]
    public void Create_draws_one_series_per_car_for_the_selected_session_type()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 100, car: "Ferrari 296 LMGT3 Evo"),
            Session("Race", "2026-01-11T20:30:00Z", 101, car: "Ferrari 296 LMGT3"),
            Session("Race", "2026-01-12T20:30:00Z", 102, car: "BMW M4 LMGT3"),
            Session("Qualify", "2026-01-12T20:00:00Z", 99, car: "Aston Martin Vantage AMR LMGT3")
        };

        var settings = ProgressionSettings.Default with { Mode = ProgressionMode.PerCar };
        var chart = Create(sessions, settings);

        chart.Series.Select(series => series.Label).Should().Equal("BMW M4 LMGT3", "Ferrari 296 LMGT3", "Ferrari 296 LMGT3 Evo");
        chart.Series.Select(series => series.Color).Should().Equal("#38bdf8", "#f97316", "#a3e635");
        Create(sessions, settings with { Session = ProgressionSession.Qualifying })
            .Series.Should().ContainSingle().Which.Label.Should().Be("Aston Martin Vantage AMR LMGT3");
    }

    [TestMethod]
    public void Segments_mark_periods_without_session_as_gaps()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 103),
            Session("Race", "2026-02-10T20:30:00Z", 102),
            Session("Race", "2026-04-10T20:30:00Z", 101)
        };

        var series = Create(sessions, ProgressionSettings.Default).Series[0];

        series.Segments.Select(segment => segment.IsGap).Should().Equal(false, true);
        series.Segments[1].From.PeriodStart.Should().Be(new DateTime(2026, 2, 1));
        series.Segments[1].To.PeriodStart.Should().Be(new DateTime(2026, 4, 1));
    }

    [TestMethod]
    public void Series_expose_the_record_and_the_gain_since_the_first_period()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 103, 104),
            Session("Race", "2026-02-10T20:30:00Z", 99.5),
            Session("Race", "2026-03-10T20:30:00Z", 101)
        };

        var series = Create(sessions, ProgressionSettings.Default).Series[0];

        series.Record.Time.Should().Be(Seconds(99.5));
        series.Record.At.Should().Be(Date("2026-02-10T20:30:00Z"));
        series.First.Value.Should().Be(Seconds(103));
        series.Last.Value.Should().Be(Seconds(101));
        series.Gain.Should().Be(Seconds(-2));
    }

    [TestMethod]
    public void Gain_is_unknown_with_a_single_period()
    {
        var series = Create([Session("Race", "2026-01-10T20:30:00Z", 103)], ProgressionSettings.Default).Series[0];

        series.Gain.Should().BeNull();
        series.Segments.Should().BeEmpty();
    }

    [TestMethod]
    public void GetCircuits_lists_driven_circuits_with_their_categories_and_latest_category()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 100, carClass: "LMP2"),
            Session("Race", "2026-02-10T20:30:00Z", 100, carClass: "GT3"),
            Session("Qualify", "2026-03-10T20:30:00Z", 100, carClass: "GTE", track: "Monza"),
            Session("Race", "2026-04-10T20:30:00Z", 100, driverName: "Someone else", track: "Sebring")
        };

        var circuits = ProgressionPresentation.GetCircuits(sessions, "Driver");

        circuits.Select(circuit => circuit.TrackVenue).Should().Equal("Monza", "Spa");
        circuits[1].Categories.Should().Equal("LMP2", "LMGT3");
        circuits[1].LatestCategory.Should().Be("LMGT3");
        circuits[1].Key.Should().Be("Spa|Spa");
    }

    [TestMethod]
    public void GetDefaultSelection_uses_the_most_recent_race()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 100, carClass: "LMP2"),
            Session("Qualify", "2026-03-10T20:30:00Z", 100, carClass: "GTE", track: "Monza")
        };

        var selection = ProgressionPresentation.GetDefaultSelection(sessions, "Driver");

        selection.Should().Be(new ProgressionSelection("Spa", "Spa", "LMP2"));
        ProgressionPresentation.GetDefaultSelection([sessions[1]], "Driver")
            .Should().Be(new ProgressionSelection("Monza", "Monza", "LMGTE"));
        ProgressionPresentation.GetDefaultSelection([], "Driver").Should().BeNull();
    }

    [TestMethod]
    public void Settings_are_read_from_configuration_with_defaults_for_invalid_values()
    {
        using var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            """
            { "LMU": { "Progression": { "Granularity": "week", "Aggregate": "Min", "Mode": "unknown", "Session": "Qualifying" } } }
            """));
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(json)
            .Build();

        ProgressionSettings.Load(configuration).Should().Be(new ProgressionSettings(
            ProgressionGranularity.Week,
            ProgressionAggregate.Min,
            ProgressionMode.MixedCars,
            ProgressionSession.Qualifying));
        ProgressionSettings.Default.Should().Be(new ProgressionSettings(
            ProgressionGranularity.Month,
            ProgressionAggregate.Average,
            ProgressionMode.MixedCars,
            ProgressionSession.Race));
    }

    [TestMethod]
    public void Layout_scales_visible_series_and_draws_gaps_as_dotted_steps()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 103),
            Session("Race", "2026-02-10T20:30:00Z", 102),
            Session("Race", "2026-04-10T20:30:00Z", 101),
            Session("Qualify", "2026-04-10T20:00:00Z", 80)
        };
        var chart = Create(sessions, ProgressionSettings.Default);

        var layout = ProgressionChartLayout.Create(chart, hiddenSeries: ["Qualification"]);

        layout.Series.Should().ContainSingle().Which.Series.Label.Should().Be("Course");
        var race = layout.Series[0];
        race.Points.First().X.Should().Be(layout.PlotLeft);
        race.Points.Last().X.Should().Be(layout.PlotRight);
        race.Points.Last().Y.Should().BeGreaterThan(race.Points.First().Y, "a faster lap is drawn lower");
        race.LinePath.Should().StartWith("M");
        race.GapPath.Should().Contain("H").And.Contain("V");
        layout.YTicks.Should().NotBeEmpty();
        layout.YTicks.Min(tick => tick.Time).Should().BeGreaterThan(Seconds(90), "the hidden qualifying series does not stretch the scale");
        layout.XTicks.Should().NotBeEmpty();
        race.ShowsBand.Should().BeTrue();
    }

    [TestMethod]
    public void Layout_hides_bands_when_several_cars_are_visible()
    {
        var sessions = new[]
        {
            Session("Race", "2026-01-10T20:30:00Z", 103, car: "A"),
            Session("Race", "2026-02-10T20:30:00Z", 102, car: "B")
        };
        var chart = Create(sessions, ProgressionSettings.Default with { Mode = ProgressionMode.PerCar });

        ProgressionChartLayout.Create(chart, []).Series.Should().OnlyContain(series => !series.ShowsBand);
        ProgressionChartLayout.Create(chart, ["A"]).Series.Should().ContainSingle().Which.ShowsBand.Should().BeTrue();
    }

    /// <summary>
    /// Cross-checks the record of the main fixture driver with the best laps of "Mes courses".
    /// </summary>
    [TestMethod]
    public void Create_on_real_fixtures_matches_the_best_laps_of_my_races()
    {
        var parser = new LMU_DeltaRacing.Infrastructure.Parsing.XmlResultParser();
        var sessions = Directory
            .EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return parser.Parse(stream);
            })
            .ToArray();
        var selection = ProgressionPresentation.GetDefaultSelection(sessions, "Driver Tom")!;

        var chart = ProgressionPresentation.Create(
            sessions,
            "Driver Tom",
            selection.TrackVenue,
            selection.TrackCourse,
            selection.Category,
            ProgressionSettings.Default,
            TimeZoneInfo.Local);

        var records = MyRacesPresentation.CreateRecords(sessions, "Driver Tom")
            .Where(record => record.TrackVenue == selection.TrackVenue
                && record.TrackCourse == selection.TrackCourse
                && record.Category == selection.Category)
            .ToArray();
        chart.Series.Single(series => series.Label == "Course").Record.Time
            .Should().Be(records.Where(record => record.RaceTime.HasValue).Min(record => record.RaceTime!.Value));
        chart.Series.Single(series => series.Label == "Qualification").Record.Time
            .Should().Be(records.Where(record => record.QualifyingTime.HasValue).Min(record => record.QualifyingTime!.Value));
    }

    private static ProgressionChart Create(IEnumerable<Session> sessions, ProgressionSettings settings) =>
        ProgressionPresentation.Create(sessions, "Driver", "Spa", "Spa", "LMGT3", settings, Utc);

    private static TimeSpan Seconds(double value) => TimeSpan.FromSeconds(value);

    private static DateTimeOffset Date(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    private static Session Session(
        string type,
        string startedAt,
        double bestLap,
        double? otherLap = null,
        string track = "Spa",
        string carClass = "GT3",
        string car = "Car",
        string driverName = "Driver") =>
        new(
            Guid.NewGuid(),
            Date(startedAt),
            track,
            track,
            $"Event {track}",
            type,
            [
                new Driver(
                    driverName,
                    "7",
                    carClass,
                    "Team",
                    1,
                    new[] { (double?)bestLap, otherLap, null }
                        .Select((time, index) => new Lap(index + 1, time is { } seconds ? TimeSpan.FromSeconds(seconds) : null, [])),
                    car)
            ]);

    private static Session SessionWithoutTimedLap(string type, string startedAt) =>
        new(
            Guid.NewGuid(),
            Date(startedAt),
            "Spa",
            "Spa",
            "Event Spa",
            type,
            [new Driver("Driver", "7", "GT3", "Team", 1, [new Lap(1, null, [])], "Car")]);
}
