using System.Globalization;
using System.Text;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Benchmarks;

/// <summary>
/// Measures the calculations behind each dashboard page on the real history, repeated to simulate
/// longer careers. Each page recomputes them whenever the dashboard state changes.
/// </summary>
internal static class PresentationBenchmark
{
    private static readonly int[] Factors = [1, 10, 50];

    public static string Run()
    {
        var all = FixtureSessions.LoadAll();
        var withoutPractice = all
            .Where(session => session.Kind != SessionKind.Practice)
            .ToArray();
        var output = new StringBuilder();
        output.AppendLine("## Calculs des vues");
        output.AppendLine();
        output.AppendLine(
            $"L'historique réel ({withoutPractice.Length} qualifications et courses, pilote « {FixtureSessions.UserName} ») " +
            "est répété ×1, ×10 et ×50, chaque copie décalée dans le temps. Ces calculs sont refaits par la page " +
            "affichée à chaque changement d'état (import, fusion, changement de pilote).");
        output.AppendLine();

        var table = new BenchmarkTable("Sessions");
        foreach (var factor in Factors)
        {
            var sessions = FixtureSessions.Scale(withoutPractice, factor);
            var scale = $"{sessions.Count.ToString("N0", CultureInfo.InvariantCulture)} (×{factor})";
            MeasureViews(table, scale, sessions);
        }

        // A classification depends on one session only: the largest real race is measured once.
        var largestRace = withoutPractice
            .Where(session => session.Kind == SessionKind.Race)
            .MaxBy(session => session.Drivers.Sum(driver => driver.Laps.Count))!;
        table.Measure(
            $"1 ({largestRace.Drivers.Count} pilotes)",
            "Détail des courses : classement de la plus grosse course",
            () => RaceDetailsPresentation.CreateResults(largestRace, FixtureSessions.UserName).Count,
            20,
            51);

        table.AppendTo(output);
        return output.ToString();
    }

    private static void MeasureViews(BenchmarkTable table, string scale, IReadOnlyList<Session> sessions)
    {
        var user = FixtureSessions.UserName;
        var frequencies = DriverProfile.CalculateFrequencies(sessions);
        var opponents = frequencies.Where(driver => driver.Name != user).Take(4).ToArray();
        var rival = DriversRankingPresentation.CreateRivalries(sessions, user).MaxBy(rivalry => rivalry.SessionsFaced)!.Name;
        DriverMerge[] merges =
        [
            new(opponents[0].Name, opponents[1].Name),
            new(opponents[2].Name, opponents[3].Name)
        ];
        var selection = ProgressionPresentation.GetDefaultSelection(sessions, user)!;
        var chart = ProgressionPresentation.Create(
            sessions, user, selection.TrackVenue, selection.TrackCourse, selection.Category, ProgressionSettings.Default, TimeZoneInfo.Local);
        var records = MyRacesPresentation.CreateRecords(sessions, user);

        table.Measure(scale, "Fusions de pilotes appliquées (2 fusions)", () => DriverMerges.Apply(sessions, merges).Count);
        table.Measure(scale, "Fréquences des pilotes (Options)", () => DriverProfile.CalculateFrequencies(sessions).Count);
        table.Measure(scale, "Sessions en commun de deux pilotes (Options)", () => DriverMerges.CountSharedSessions(sessions, user, rival, merges));
        table.Measure(scale, "Accueil", () => HomePresentation.Create(sessions, user).RecentForm.Count);
        table.Measure(scale, "Mes courses : lignes", () => MyRacesPresentation.CreateRecords(sessions, user).Count);
        table.Measure(scale, "Mes courses : filtres et tri par circuit", () => records
            .GroupBy(record => (record.TrackVenue, record.TrackCourse))
            .Sum(circuit => MyRacesFilters.OrderRows(circuit).Count) + MyRacesFilters.GetTracks(records).Count);
        table.Measure(scale, "Classement des coureurs", () => DriversRankingPresentation.CreateRivalries(sessions, user).Count);
        table.Measure(scale, "Détail des courses : événements", () => RaceEventPresentation.CreateEvents(sessions, user).Count);
        table.Measure(scale, "Progression : circuits", () => ProgressionPresentation.GetCircuits(sessions, user).Count);
        table.Measure(scale, "Progression : courbes du circuit par défaut", () => ProgressionPresentation.Create(
            sessions, user, selection.TrackVenue, selection.TrackCourse, selection.Category, ProgressionSettings.Default, TimeZoneInfo.Local).Series.Count);
        table.Measure(scale, "Progression : géométrie SVG", () => ProgressionChartLayout.Create(chart, []).Series.Count);
    }
}
