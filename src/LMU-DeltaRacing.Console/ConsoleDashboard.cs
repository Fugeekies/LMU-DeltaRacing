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
    /// Displays session details and the outcome of each requested import.
    /// </summary>
    /// <param name="imports">The results of the requested imports.</param>
    public static void Render(IReadOnlyCollection<SessionImportResult> imports)
    {
        AnsiConsole.Write(new Rule("[bold deepskyblue1]LMU DeltaRacing[/]").LeftJustified());
        AnsiConsole.MarkupLine("[dim]Import des résultats et diagnostic OpenTelemetry[/]");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Expand();
        table.AddColumn("État");
        table.AddColumn("Fichier");
        table.AddColumn("Circuit");
        table.AddColumn("Session");
        table.AddColumn("Pilotes");
        table.AddColumn("Détail");

        if (imports.Count == 0)
        {
            table.AddRow(
                "[dim]Aucun XML[/]",
                "[dim]—[/]",
                "[dim]—[/]",
                "[dim]—[/]",
                "[dim]—[/]",
                "[dim]Aucun fichier XML trouvé dans le dossier sélectionné.[/]");
        }
        else
        {
            foreach (var result in imports)
            {
                if (result.Status == SessionImportStatus.Imported)
                {
                    var session = result.Session!;
                    table.AddRow(
                        "[green]Importé[/]",
                        Escape(result.SourceFile),
                        Escape(session.TrackVenue),
                        Escape(session.SessionType),
                        session.Drivers.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Escape(session.TrackCourse));
                }
                else
                {
                    table.AddRow(
                        "[red]Échec[/]",
                        Escape(result.SourceFile),
                        "[dim]—[/]",
                        "[dim]—[/]",
                        "[dim]—[/]",
                        Escape(result.ErrorMessage ?? "Erreur inconnue"));
                }
            }
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Displays chronological lap-time statistics grouped by circuit and vehicle, with charts
    /// of the most recent measured best laps.
    /// </summary>
    /// <param name="sessions">The sessions currently stored in the repository.</param>
    public static void RenderStatistics(IReadOnlyCollection<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        AnsiConsole.Write(new Rule("[bold deepskyblue1]Statistiques et graphiques[/]").LeftJustified());
        if (sessions.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]Aucune session importée à analyser.[/]");
            return;
        }

        var circuitRows = SessionStatistics.ByCircuit(sessions);
        RenderCircuitStatistics(circuitRows);
        RenderCircuitChart(circuitRows);

        var vehicleRows = SessionStatistics.ByCircuitAndCar(sessions);
        RenderVehicleStatistics(vehicleRows);
        RenderVehicleChart(vehicleRows);
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
