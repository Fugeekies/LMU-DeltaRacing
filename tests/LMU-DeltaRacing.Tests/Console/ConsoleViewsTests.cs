using FluentAssertions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.ConsoleApp.Views;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace LMU_DeltaRacing.Tests.Console;

/// <summary>
/// Verifies the text rendered by the console views, on the real result fixtures.
/// </summary>
[TestClass]
public sealed class ConsoleViewsTests
{
    private const string MainDriver = "Driver Tom";

    private static readonly Lazy<IReadOnlyList<Session>> FixtureSessions = new(LoadFixtureSessions);

    /// <summary>
    /// Verifies the status banner: driver, watcher state, driver sessions and failures.
    /// </summary>
    [TestMethod]
    public void StatusBanner_shows_driver_watcher_state_sessions_and_failures()
    {
        var sessions = FixtureSessions.Value;
        var driverSessions = DriverProfile.FilterSessions(sessions, MainDriver).Count;
        var failure = new SessionImportResult("broken.xml", SessionImportStatus.Failed, null, "XML illisible");
        var state = new DashboardState(false, 0, 0, true, null, sessions, [failure], "C:\\Results")
        {
            UserName = MainDriver
        };

        var output = Render(StatusBanner.Create(state));

        output.Should().Contain(MainDriver)
            .And.Contain("Surveillance active")
            .And.Contain($"{driverSessions} sessions")
            .And.Contain("1 échec");
    }

    /// <summary>
    /// Verifies the banner without a driver, with an inactive watcher, then with a loading error.
    /// </summary>
    [TestMethod]
    public void StatusBanner_shows_missing_driver_inactive_watcher_and_unavailable_results()
    {
        var inactive = new DashboardState(false, 0, 0, false, null, [], [], null);
        var unavailable = inactive with { ErrorMessage = "Dossier introuvable" };

        Render(StatusBanner.Create(inactive)).Should().Contain("Pilote non défini").And.Contain("Surveillance inactive");
        Render(StatusBanner.Create(unavailable)).Should().Contain("Résultats indisponibles");
    }

    /// <summary>
    /// Verifies the notice of the files imported by the watcher; ignored practice files are not listed.
    /// </summary>
    [TestMethod]
    public void ImportNotice_lists_new_results_and_failures_but_not_ignored_practice()
    {
        var race = FixtureSessions.Value.First(session => session.SessionType == "Race");
        var imports = new[]
        {
            new SessionImportResult("race.xml", SessionImportStatus.Imported, race, null),
            new SessionImportResult("practice.xml", SessionImportStatus.SkippedPractice, null, null),
            new SessionImportResult("broken.xml", SessionImportStatus.Failed, null, "XML illisible")
        };

        var output = Render(ImportNotice.Create(imports)!);

        output.Should().Contain("1 nouveau résultat")
            .And.Contain($"{race.TrackVenue} — Race")
            .And.Contain("broken.xml — XML illisible")
            .And.NotContain("practice.xml");
        ImportNotice.Create([imports[1]]).Should().BeNull();
    }

    /// <summary>
    /// Verifies the home view: every section, the totals and the breakdown by category.
    /// </summary>
    [TestMethod]
    public void HomeView_renders_every_section_with_totals_and_category_columns()
    {
        var dashboard = HomePresentation.Create(FixtureSessions.Value, MainDriver);

        var output = Render(HomeView.Create(dashboard, MainDriver));

        output.Should().Contain("Pilote").And.Contain(MainDriver)
            .And.Contain("Forme récente").And.Contain("Dernière course")
            .And.Contain("Palmarès").And.Contain("Roulage")
            .And.Contain("Qualification & départ").And.Contain("Abandons")
            .And.Contain("Causes d'abandon");
        output.Should().Contain(DisplayFormat.Count(dashboard.Races.Value))
            .And.Contain(DisplayFormat.Percent(dashboard.Wins.Rate!.Value))
            .And.Contain(DisplayFormat.Distance(dashboard.DistanceKm.Value));
        foreach (var category in dashboard.Races.Categories.Where(category => category.Value > 0))
        {
            output.Should().Contain(category.Category);
        }

        foreach (var entry in dashboard.RecentForm)
        {
            output.Should().Contain(entry.Label);
        }

        output.Should().Contain(dashboard.RetirementCauses[0].Label);
    }

    /// <summary>
    /// Verifies the home view of a driver without any race.
    /// </summary>
    [TestMethod]
    public void HomeView_shows_an_empty_state_without_races()
    {
        Render(HomeView.Create(HomePresentation.Create([], null), null))
            .Should().Contain("Aucune course disponible");
    }

    /// <summary>
    /// Verifies the "my races" view: filters, one block per circuit and the abbreviated columns.
    /// </summary>
    [TestMethod]
    public void MyRacesView_renders_one_table_per_circuit_with_the_selected_filters()
    {
        var records = MyRacesPresentation.CreateRecords(FixtureSessions.Value, MainDriver);
        var venue = records[0].TrackVenue;
        var expected = MyRacesFilters.Apply(records, venue, null);

        var output = Render(MyRacesView.Create(records, venue, null));

        output.Should().Contain($"Circuit : {venue}").And.Contain("Catégorie : Toutes les catégories");
        output.Should().Contain("CLASS.").And.Contain("Δ QUALIF").And.Contain("Δ COURSE").And.Contain("TEMPS TOTAL");
        output.Should().Contain($"{expected.Count} résultat");
        foreach (var record in expected)
        {
            output.Should().Contain($"{DisplayFormat.DateTime(record.StartedAt)} ");
            output.Should().Contain(record.ResultLabel);
        }

        var delta = expected.Select(record => record.RaceDelta ?? record.QualifyingDelta).FirstOrDefault(value => value is not null);
        if (delta is not null)
        {
            output.Should().Contain(DisplayFormat.Delta(delta));
        }
    }

    /// <summary>
    /// Verifies that in a 120-column terminal only the car name wraps: dates, positions and times
    /// stay on one line.
    /// </summary>
    [TestMethod]
    public void MyRacesView_keeps_dates_positions_and_times_on_one_line_in_120_columns()
    {
        var records = MyRacesPresentation.CreateRecords(FixtureSessions.Value, MainDriver);
        var venue = records[0].TrackVenue;

        var console = new TestConsole().Width(120);
        console.Write(MyRacesView.Create(records, venue, null));

        foreach (var record in MyRacesFilters.Apply(records, venue, null))
        {
            console.Output.Should().Contain(DisplayFormat.DateTime(record.StartedAt))
                .And.Contain(record.ResultLabel)
                .And.Contain(DisplayFormat.TotalTime(record.FinishTime));
        }
    }

    /// <summary>
    /// Verifies that the class position of a podium is preceded by a trophy, as on the dashboard,
    /// and that no other row gets one.
    /// </summary>
    [TestMethod]
    public void MyRacesView_puts_a_trophy_before_podium_positions_only()
    {
        var records = MyRacesPresentation.CreateRecords(FixtureSessions.Value, MainDriver);

        records.Should().Contain(record => record.IsPodium, "the fixtures hold podiums of the main driver");
        foreach (var record in records)
        {
            var markup = ConsoleTheme.PositionMarkup(record);
            if (record.IsPodium)
            {
                markup.Should().Contain($"🏆 {record.ResultLabel}");
            }
            else
            {
                markup.Should().NotContain("🏆");
            }
        }

        var podium = records.First(record => record.IsPodium);
        Render(MyRacesView.Create(records, podium.TrackVenue, podium.Category)).Should().Contain($"🏆 {podium.ResultLabel}");
    }

    /// <summary>
    /// Verifies the empty states of the "my races" view.
    /// </summary>
    [TestMethod]
    public void MyRacesView_shows_empty_states()
    {
        var records = MyRacesPresentation.CreateRecords(FixtureSessions.Value, MainDriver);

        Render(MyRacesView.Create([], null, null)).Should().Contain("Aucune course disponible");
        Render(MyRacesView.Create(records, null, "Formula E"))
            .Should().Contain("Aucune course ne correspond à ces filtres");
    }

    /// <summary>
    /// Verifies the options view: current settings, import state and the ten most recent sessions.
    /// </summary>
    [TestMethod]
    public void OptionsView_shows_settings_import_state_and_recent_sessions()
    {
        var sessions = FixtureSessions.Value;
        var state = new DashboardState(false, 10, 10, true, null, sessions, [], @"C:\Results")
        {
            UserName = MainDriver,
            DriverFrequencies = DriverProfile.CalculateFrequencies(sessions),
            DriverMerges = [new DriverMerge("Driver Paul", "Driver Iris")]
        };
        var settings = new OptionsSettings(
            ConfiguredDirectory: string.Empty,
            DirectoryStatus: new ResultsDirectoryStatus(true, @"C:\Results", 342, null),
            WatchOnStartup: true,
            LogFilePath: "lmu-deltaracing.log",
            FileMinimumLogLevel: "Debug",
            MergesWithSharedSessions: 0);
        var recent = DashboardPresentation.OrderSessions(DriverProfile.FilterSessions(sessions, MainDriver))
            .Select(session => DisplayFormat.DateTime(session.StartedAt))
            .ToArray();
        // Sessions of one event share their start time: pick a date found only after the tenth row.
        var hiddenDate = recent.Skip(10).Except(recent.Take(10)).First();

        var output = Render(OptionsView.Create(state, settings));

        output.Should().Contain("Détection automatique")
            .And.Contain("Dossier valide · 342 fichier(s) XML")
            .And.Contain(MainDriver)
            .And.Contain("Oui")
            .And.Contain("lmu-deltaracing.log · Debug")
            .And.Contain($"Sessions récentes de {MainDriver}")
            .And.Contain(recent[0])
            .And.NotContain(hiddenDate);
    }

    private static string Render(IRenderable renderable)
    {
        // Wide enough to keep each cell on one line, so that values can be searched in the output.
        var console = new TestConsole().Width(220);
        console.Write(renderable);
        return console.Output;
    }

    private static IReadOnlyList<Session> LoadFixtureSessions()
    {
        var parser = new XmlResultParser();
        return Directory
            .EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "assets", "Results"), "*.xml")
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return parser.Parse(stream);
            })
            // The console never imports practice sessions.
            .Where(session => !session.SessionType.StartsWith("Practice", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
