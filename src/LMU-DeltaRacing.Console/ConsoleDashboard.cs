using LMU_DeltaRacing.Application.Models;
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

    private static string Escape(string? value) => Markup.Escape(value ?? string.Empty);
}
