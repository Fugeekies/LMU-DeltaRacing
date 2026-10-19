using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Domain.Entities;
using Spectre.Console;

namespace LMU_DeltaRacing.ConsoleApp;

/// <summary>
/// Renders the result import summary in the terminal.
/// </summary>
public static class ConsoleDashboard
{
    /// <summary>
    /// Displays the application title before loading configuration and result files.
    /// </summary>
    public static void RenderStartupTitle()
    {
        AnsiConsole.Write(new Rule("[bold deepskyblue1]LMU DeltaRacing[/]").LeftJustified());
        AnsiConsole.MarkupLine("[dim]Import des résultats LMU — Statistiques et graphiques[/]");
    }

    /// <summary>
    /// Displays a compact import outcome after the loading progress completes.
    /// </summary>
    /// <param name="imports">The outcome of each requested file import.</param>
    /// <param name="totalFiles">The number of files presented to the importer.</param>
    public static void RenderImportSummary(
        IReadOnlyCollection<SessionImportResult> imports,
        int totalFiles)
    {
        ArgumentNullException.ThrowIfNull(imports);
        ArgumentOutOfRangeException.ThrowIfNegative(totalFiles);

        var successfulImports = imports.Count(result => result.Status == SessionImportStatus.Imported);
        var skippedPracticeImports = imports.Count(result => result.Status == SessionImportStatus.SkippedPractice);
        var failedImports = imports
            .Where(result => result.Status == SessionImportStatus.Failed)
            .ToArray();

        if (totalFiles == 0)
        {
            AnsiConsole.MarkupLine("[dim]Aucun résultat à charger.[/]");
            return;
        }

        AnsiConsole.MarkupLine(
            $"Chargement terminé : [green]{successfulImports} réussi(s)[/], " +
            $"[dim]{skippedPracticeImports} session(s) Practice ignorée(s)[/], " +
            $"[red]{failedImports.Length} échec(s)[/].");
        foreach (var failure in failedImports)
        {
            AnsiConsole.MarkupLine(
                $"[red]Échec[/] [bold]{Escape(failure.SourceFile)}[/] : " +
                Escape(failure.ErrorMessage ?? "Erreur inconnue"));
        }
    }

    /// <summary>
    /// Reports the outcome of a result file imported while directory monitoring is active.
    /// </summary>
    /// <param name="result">The outcome of the newly observed file.</param>
    public static void RenderDynamicImportResult(SessionImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status == SessionImportStatus.Imported)
        {
            var session = result.Session!;
            AnsiConsole.MarkupLine(
                $"[green]Nouveau résultat importé[/] : {Escape(result.SourceFile)} — " +
                $"{Escape(session.TrackVenue)} / {Escape(session.SessionType)}.");
            return;
        }

        if (result.Status == SessionImportStatus.SkippedPractice)
        {
            AnsiConsole.MarkupLine(
                $"[dim]Session Practice ignorée[/] : {Escape(result.SourceFile)}.");
            return;
        }

        AnsiConsole.MarkupLine(
            $"[red]Échec d'import[/] : {Escape(result.SourceFile)} — " +
            Escape(result.ErrorMessage ?? "Erreur inconnue"));
    }

    /// <summary>
    /// Displays chronological lap-time statistics grouped by circuit and vehicle, with charts
    /// of the most recent measured best laps.
    /// </summary>
    /// <param name="sessions">The sessions currently stored in the repository.</param>
    public static void RenderStatistics(
        IReadOnlyCollection<Session> sessions,
        StatisticsTableSelection selection)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        AnsiConsole.Write(new Rule("[bold deepskyblue1]Statistiques et graphiques[/]").LeftJustified());
        var sessionsWithoutPractice = sessions
            .Where(session => !session.SessionType.StartsWith("Practice", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        AnsiConsole.MarkupLine("[dim]Les sessions Practice sont exclues des statistiques.[/]");
        if (sessionsWithoutPractice.Length == 0)
        {
            AnsiConsole.MarkupLine("[dim]Aucune session hors Practice à analyser.[/]");
            return;
        }

        if (selection is StatisticsTableSelection.Circuit or StatisticsTableSelection.Both)
        {
            var circuitRows = SessionStatistics.ByCircuit(sessionsWithoutPractice);
            RenderCircuitStatistics(circuitRows);
            RenderCircuitChart(circuitRows);
        }

        if (selection is StatisticsTableSelection.CircuitAndCar or StatisticsTableSelection.Both)
        {
            var vehicleRows = SessionStatistics.ByCircuitAndCar(sessionsWithoutPractice);
            RenderVehicleStatistics(vehicleRows);
            RenderVehicleChart(vehicleRows);
        }
    }

    private static void RenderCircuitStatistics(IReadOnlyList<CircuitStatisticsRow> rows)
    {
        AnsiConsole.MarkupLine("[bold]Vue par circuit[/]");
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Expand();
        table.AddColumn("Circuit");
        table.AddColumn("Date");
        table.AddColumn("Session");
        table.AddColumn("Meilleur tour");
        table.AddColumn("Delta");

        foreach (var row in rows)
        {
            table.AddRow(
                Escape($"{row.TrackVenue} — {row.TrackCourse}"),
                row.StartedAt.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                Escape(row.SessionType),
                FormatLap(row.BestLap),
                FormatDelta(row.Delta));
        }

        AnsiConsole.Write(table);
    }

    private static void RenderVehicleStatistics(IReadOnlyList<CircuitVehicleStatisticsRow> rows)
    {
        AnsiConsole.MarkupLine("[bold]Vue par circuit et voiture (classe + équipe)[/]");
        if (rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]Aucune donnée de pilote disponible pour cette vue.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Expand();
        table.AddColumn("Circuit");
        table.AddColumn("Classe");
        table.AddColumn("Équipe");
        table.AddColumn("Date");
        table.AddColumn("Session");
        table.AddColumn("Meilleur tour");
        table.AddColumn("Delta");

        foreach (var row in rows)
        {
            table.AddRow(
                Escape($"{row.TrackVenue} — {row.TrackCourse}"),
                Escape(row.CarClass),
                Escape(row.TeamName),
                row.StartedAt.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                Escape(row.SessionType),
                FormatLap(row.BestLap),
                FormatDelta(row.Delta));
        }

        AnsiConsole.Write(table);
    }

    private static void RenderCircuitChart(IReadOnlyList<CircuitStatisticsRow> rows)
    {
        var latestMeasurements = rows
            .Where(row => row.BestLap is not null)
            .GroupBy(row => (row.TrackVenue, row.TrackCourse))
            .Select(group => group.OrderBy(row => row.StartedAt).Last())
            .ToArray();
        if (latestMeasurements.Length == 0)
        {
            AnsiConsole.MarkupLine("[dim]Aucun tour valide à représenter par circuit.[/]");
            return;
        }

        var chart = new BarChart()
            .Width(80)
            .Label("[bold]Meilleur tour valide le plus récent par circuit (secondes)[/]")
            .CenterLabel();
        foreach (var row in latestMeasurements)
        {
            chart.AddItem(
                Escape($"{row.TrackVenue} — {row.TrackCourse}"),
                row.BestLap!.Value.TotalSeconds,
                Color.DeepSkyBlue1);
        }

        AnsiConsole.Write(chart);
    }

    private static void RenderVehicleChart(IReadOnlyList<CircuitVehicleStatisticsRow> rows)
    {
        var latestMeasurements = rows
            .Where(row => row.BestLap is not null)
            .GroupBy(row => (row.TrackVenue, row.TrackCourse, row.CarClass, row.TeamName))
            .Select(group => group.OrderBy(row => row.StartedAt).Last())
            .ToArray();
        if (latestMeasurements.Length == 0)
        {
            AnsiConsole.MarkupLine("[dim]Aucun tour valide à représenter par voiture.[/]");
            return;
        }

        var chart = new BarChart()
            .Width(80)
            .Label("[bold]Meilleur tour valide le plus récent par voiture (secondes)[/]")
            .CenterLabel();
        foreach (var row in latestMeasurements)
        {
            chart.AddItem(
                Escape($"{row.TrackVenue} — {row.TrackCourse} — {row.CarClass} — {row.TeamName}"),
                row.BestLap!.Value.TotalSeconds,
                Color.DeepSkyBlue1);
        }

        AnsiConsole.Write(chart);
    }

    private static string FormatLap(TimeSpan? time) =>
        time?.ToString(@"m\:ss\.fff", System.Globalization.CultureInfo.InvariantCulture) ?? "[dim]—[/]";

    private static string FormatDelta(TimeSpan? delta)
    {
        if (delta is null)
        {
            return "[dim]—[/]";
        }

        var formatted = delta.Value.ToString(@"m\:ss\.fff", System.Globalization.CultureInfo.InvariantCulture);
        if (delta.Value > TimeSpan.Zero)
        {
            return $"[red]+{formatted}[/]";
        }

        return delta.Value < TimeSpan.Zero
            ? $"[green]{formatted}[/]"
            : $"[yellow]+{formatted}[/]";
    }

    private static string Escape(string? value) => Markup.Escape(value ?? string.Empty);
}

/// <summary>
/// Identifies which statistics tables and charts are displayed after import.
/// </summary>
public enum StatisticsTableSelection
{
    /// <summary>
    /// Displays statistics grouped by circuit.
    /// </summary>
    Circuit,

    /// <summary>
    /// Displays statistics grouped by circuit and vehicle class/team.
    /// </summary>
    CircuitAndCar,

    /// <summary>
    /// Displays both statistics views.
    /// </summary>
    Both
}
