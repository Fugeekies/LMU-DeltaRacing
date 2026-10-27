using System.Globalization;
using LMU_DeltaRacing.Presentation.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LMU_DeltaRacing.ConsoleApp.Views;

/// <summary>
/// The settings shown by the options view, read from the configuration when the view opens.
/// </summary>
/// <param name="ConfiguredDirectory">The configured results directory; empty for automatic detection.</param>
/// <param name="DirectoryStatus">The check of the configured directory.</param>
/// <param name="WatchOnStartup">Whether the watcher starts with the application.</param>
/// <param name="LogFilePath">The configured log file path.</param>
/// <param name="FileMinimumLogLevel">The minimum level written to the log file.</param>
/// <param name="MergesWithSharedSessions">The number of driver merges whose drivers met in a session.</param>
public sealed record OptionsSettings(
    string ConfiguredDirectory,
    ResultsDirectoryStatus DirectoryStatus,
    bool WatchOnStartup,
    string LogFilePath,
    string FileMinimumLogLevel,
    int MergesWithSharedSessions);

/// <summary>
/// Renders the current settings and the import state, as the options page of the dashboard.
/// </summary>
public static class OptionsView
{
    /// <summary>
    /// The number of recent sessions listed below the settings.
    /// </summary>
    public const int RecentSessionCount = 10;

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>
    /// Creates the options view.
    /// </summary>
    /// <param name="state">The current dashboard state.</param>
    /// <param name="settings">The settings read from the configuration.</param>
    public static IRenderable Create(DashboardState state, OptionsSettings settings)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settings);

        var driverSessions = DashboardPresentation.OrderSessions(
            DriverProfile.FilterSessions(state.Sessions, state.UserName));
        var lines = new List<IRenderable>
        {
            CreateSettingsTable(state, settings),
            Text.Empty,
            CreateImportSummary(driverSessions, state.ImportFailures.Count)
        };

        foreach (var failure in state.ImportFailures)
        {
            lines.Add(new Markup(
                $" {ConsoleTheme.Colored("✖", ConsoleTheme.Loss)} "
                + Markup.Escape($"{failure.SourceFile} — {failure.ErrorMessage ?? "Erreur inconnue"}")));
        }

        if (!string.IsNullOrWhiteSpace(state.UserName))
        {
            lines.Add(Text.Empty);
            lines.Add(new Markup($" [bold]Sessions récentes de {Markup.Escape(state.UserName)}[/]"));
            lines.Add(driverSessions.Count == 0
                ? new Markup(ConsoleTheme.MutedMarkup(" Aucune session"))
                : CreateRecentSessions(driverSessions, state.UserName));
        }

        return new Rows(lines);
    }

    private static Table CreateSettingsTable(DashboardState state, OptionsSettings settings)
    {
        var table = new Table().Border(TableBorder.Square).HideHeaders();
        table.AddColumn(string.Empty);
        table.AddColumn(string.Empty);

        var directory = string.IsNullOrWhiteSpace(settings.ConfiguredDirectory)
            ? "Détection automatique"
            : settings.ConfiguredDirectory;
        var resolved = settings.DirectoryStatus.DirectoryPath is { } path
            && !string.Equals(path, settings.ConfiguredDirectory, StringComparison.OrdinalIgnoreCase)
            ? Environment.NewLine + ConsoleTheme.MutedMarkup(path)
            : string.Empty;
        table.AddRow("Dossier des résultats", Markup.Escape(directory) + resolved);
        table.AddRow("État du dossier", settings.DirectoryStatus.IsValid
            ? Markup.Escape($"🟢 Dossier valide · {settings.DirectoryStatus.XmlFileCount} fichier(s) XML")
            : Markup.Escape($"🔴 {settings.DirectoryStatus.ErrorMessage ?? "Dossier introuvable ou illisible"}"));
        table.AddRow("Pilote principal", Markup.Escape(FormatDriver(state)));
        table.AddRow("Fusions de pilotes", Markup.Escape(FormatMerges(state.DriverMerges.Count, settings.MergesWithSharedSessions)));
        table.AddRow("Démarrage automatique", settings.WatchOnStartup ? "Oui" : "Non");
        table.AddRow("Fichier de log", Markup.Escape($"{settings.LogFilePath} · {settings.FileMinimumLogLevel}"));
        return table;
    }

    /// <summary>
    /// Formats a driver as in the driver list of the options page: name, share and file count.
    /// </summary>
    /// <param name="driver">The driver frequency.</param>
    public static string FormatFrequency(DriverFrequency driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        return $"{driver.Name} — {driver.Percentage.ToString("P1", French)} "
            + $"({driver.AppearanceCount}/{driver.TotalResults} fichiers)";
    }

    private static string FormatDriver(DashboardState state)
    {
        if (string.IsNullOrWhiteSpace(state.UserName))
        {
            return "Pilote non défini";
        }

        var frequency = state.DriverFrequencies.FirstOrDefault(driver =>
            string.Equals(driver.Name, state.UserName, StringComparison.OrdinalIgnoreCase));
        return frequency is null
            ? $"{state.UserName} (absent des résultats chargés)"
            : FormatFrequency(frequency);
    }

    private static string FormatMerges(int merges, int mergesWithSharedSessions) =>
        merges == 0
            ? "Aucune"
            : mergesWithSharedSessions > 0
                ? $"{merges} (dont {mergesWithSharedSessions} avec sessions en commun)"
                : merges.ToString(CultureInfo.InvariantCulture);

    private static Markup CreateImportSummary(IReadOnlyList<LMU_DeltaRacing.Domain.Entities.Session> sessions, int failures)
    {
        var last = sessions.FirstOrDefault();
        var lastSession = last is null
            ? "Aucune session"
            : $"{last.TrackVenue} {DisplayFormat.DateTime(last.StartedAt)}";
        var failureText = failures.ToString(CultureInfo.InvariantCulture);
        return new Markup(
            Markup.Escape($" Sessions importées {sessions.Count} · Dernière session {lastSession} · Fichiers en échec ")
            + (failures > 0 ? ConsoleTheme.Colored(failureText, ConsoleTheme.Loss) : failureText));
    }

    private static Table CreateRecentSessions(
        IReadOnlyList<LMU_DeltaRacing.Domain.Entities.Session> sessions,
        string userName)
    {
        var table = new Table().Border(TableBorder.Square);
        table.AddColumn("DATE / HEURE");
        table.AddColumn("CIRCUIT");
        table.AddColumn("ÉVÉNEMENT");
        table.AddColumn("SESSION");
        table.AddColumn("VOITURE");

        foreach (var session in sessions.Take(RecentSessionCount))
        {
            var circuit = string.Equals(session.TrackVenue, session.TrackCourse, StringComparison.OrdinalIgnoreCase)
                ? session.TrackVenue
                : $"{session.TrackVenue} — {session.TrackCourse}";
            var car = DriverProfile.FindDriver(session, userName)?.CarType;
            table.AddRow(
                DisplayFormat.DateTime(session.StartedAt),
                Markup.Escape(circuit),
                Markup.Escape(session.TrackEvent),
                Markup.Escape(session.SessionType),
                Markup.Escape(string.IsNullOrWhiteSpace(car) ? DisplayFormat.Missing : car));
        }

        return table;
    }
}
