using LMU_DeltaRacing.Application.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LMU_DeltaRacing.ConsoleApp.Views;

/// <summary>
/// Renders the files imported by the watcher since the main menu was last shown.
/// </summary>
public static class ImportNotice
{
    /// <summary>
    /// Creates the notice of the new results and failures, or returns <see langword="null"/> when
    /// there is nothing to report. Ignored practice files are not reported: the console never shows them.
    /// </summary>
    /// <param name="imports">The watcher imports, oldest first.</param>
    public static IRenderable? Create(IReadOnlyCollection<SessionImportResult> imports)
    {
        ArgumentNullException.ThrowIfNull(imports);

        var lines = new List<IRenderable>();
        var sessions = imports
            .Where(result => result.Status == SessionImportStatus.Imported && result.Session is not null)
            .Select(result => result.Session!)
            .ToArray();
        if (sessions.Length > 0)
        {
            var label = sessions.Length > 1
                ? $"{sessions.Length} nouveaux résultats"
                : "1 nouveau résultat";
            var details = string.Join(
                " · ",
                sessions.Select(session => $"{session.TrackVenue} — {session.SessionType}"));
            lines.Add(new Markup(
                $" {ConsoleTheme.Colored("+", ConsoleTheme.Gain)} {Markup.Escape(label)} : {Markup.Escape(details)}"));
        }

        foreach (var failure in imports.Where(result => result.Status == SessionImportStatus.Failed))
        {
            lines.Add(new Markup(
                $" {ConsoleTheme.Colored("✖ Échec", ConsoleTheme.Loss)} : "
                + Markup.Escape($"{failure.SourceFile} — {failure.ErrorMessage ?? "Erreur inconnue"}")));
        }

        return lines.Count == 0 ? null : new Rows(lines);
    }
}
