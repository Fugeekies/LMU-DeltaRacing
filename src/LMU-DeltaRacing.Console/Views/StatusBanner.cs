using LMU_DeltaRacing.Presentation.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LMU_DeltaRacing.ConsoleApp.Views;

/// <summary>
/// Renders the status line shown above the main menu and at the top of each view.
/// </summary>
public static class StatusBanner
{
    /// <summary>
    /// Creates the banner: driver, results and watcher state, driver sessions and import failures.
    /// </summary>
    /// <param name="state">The current dashboard state.</param>
    public static IRenderable Create(DashboardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var driver = string.IsNullOrWhiteSpace(state.UserName)
            ? ConsoleTheme.MutedMarkup("Pilote non défini")
            : $"[bold]{Markup.Escape(state.UserName)}[/]";
        var status = !string.IsNullOrWhiteSpace(state.ErrorMessage)
            ? "🔴 Résultats indisponibles"
            : state.IsWatching
                ? "🟢 Surveillance active"
                : "🟡 Surveillance inactive";
        var sessions = DriverProfile.FilterSessions(state.Sessions, state.UserName).Count;
        var failures = state.ImportFailures.Count;
        var failureText = $"{failures} {(failures > 1 ? "échecs" : "échec")}";

        return new Markup(
            $" [bold deepskyblue1]LMU DeltaRacing[/] ─ {driver} ─ {status} ─ "
            + $"{sessions} {(sessions > 1 ? "sessions" : "session")} ─ "
            + (failures > 0 ? ConsoleTheme.Colored(failureText, ConsoleTheme.Loss) : failureText)
            + Environment.NewLine);
    }
}
