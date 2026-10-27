using System.Collections.Concurrent;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.ConsoleApp.Configuration;
using LMU_DeltaRacing.ConsoleApp.Views;
using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.Presentation.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LMU_DeltaRacing.ConsoleApp;

/// <summary>
/// Runs the interactive console: initial import progress, main menu and the home, "my races" and
/// options views. Data and settings come from the services shared with the dashboard.
/// </summary>
public sealed class ConsoleApplication
{
    private const string HomeChoice = "Accueil";
    private const string MyRacesChoice = "Mes courses";
    private const string OptionsChoice = "Options";
    private const string QuitChoice = "Quitter";
    private const string BackChoice = "Retour";
    private const string ChangeFiltersChoice = "Changer les filtres";
    private const string AddMergeChoice = "Ajouter une fusion";

    private readonly IAnsiConsole console;
    private readonly DashboardDataService data;
    private readonly DashboardSettingsStore settingsStore;
    private readonly LmuApplicationSettingsStore logSettingsStore;
    private readonly IConfiguration configuration;
    private readonly IOptionsMonitor<LmuApplicationOptions> logOptions;
    private readonly ILogger<ConsoleApplication> logger;
    private readonly Action? onProgress;
    private readonly ConcurrentQueue<SessionImportResult> liveImports = new();
    private bool myRacesFiltersSelected;
    private string? trackFilter;
    private string? categoryFilter;
    private string? optionsFeedback;

    /// <summary>
    /// Creates the interactive console.
    /// </summary>
    /// <param name="console">The terminal used for rendering and prompts.</param>
    /// <param name="data">The shared results service, already started.</param>
    /// <param name="settingsStore">The store of the settings shared with the dashboard.</param>
    /// <param name="logSettingsStore">The store of the console log settings.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logOptions">The console log settings.</param>
    /// <param name="logger">The logger of the console session.</param>
    /// <param name="onProgress">Called while results load, for example to sample memory.</param>
    public ConsoleApplication(
        IAnsiConsole console,
        DashboardDataService data,
        DashboardSettingsStore settingsStore,
        LmuApplicationSettingsStore logSettingsStore,
        IConfiguration configuration,
        IOptionsMonitor<LmuApplicationOptions> logOptions,
        ILogger<ConsoleApplication> logger,
        Action? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(logSettingsStore);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logOptions);
        ArgumentNullException.ThrowIfNull(logger);

        this.console = console;
        this.data = data;
        this.settingsStore = settingsStore;
        this.logSettingsStore = logSettingsStore;
        this.configuration = configuration;
        this.logOptions = logOptions;
        this.logger = logger;
        this.onProgress = onProgress;
    }

    /// <summary>
    /// Shows the initial import, then the main menu until the user quits.
    /// </summary>
    /// <returns>1 when result files failed to import, 0 otherwise.</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        // Watcher imports arrive on another thread: they are queued and shown between two menus,
        // because writing while a prompt is displayed would break it.
        data.LiveImportCompleted += liveImports.Enqueue;
        try
        {
            console.Write(new Rule("[bold deepskyblue1]LMU DeltaRacing[/]").LeftJustified());
            await ShowLoadingAsync(Task.CompletedTask, cancellationToken).ConfigureAwait(false);
            RenderLoadSummary(data.Snapshot);

            var firstMenu = true;
            while (true)
            {
                if (!firstMenu)
                {
                    console.Clear();
                }

                firstMenu = false;
                RenderHeader(withImports: true);
                var choice = console.Prompt(new SelectionPrompt<string>()
                    .Title("Que voulez-vous afficher ?")
                    .AddChoices(HomeChoice, MyRacesChoice, OptionsChoice, QuitChoice));

                switch (choice)
                {
                    case HomeChoice:
                        ShowHome();
                        break;
                    case MyRacesChoice:
                        ShowMyRaces();
                        break;
                    case OptionsChoice:
                        await ShowOptionsAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        return data.Snapshot.ImportFailures.Count > 0 ? 1 : 0;
                }
            }
        }
        finally
        {
            data.LiveImportCompleted -= liveImports.Enqueue;
        }
    }

    private void ShowHome()
    {
        var state = data.Snapshot;
        console.Clear();
        RenderHeader(withImports: false);
        console.Write(HomeView.Create(HomePresentation.Create(state.Sessions, state.UserName), state.UserName));
        console.WriteLine();
        console.Prompt(new SelectionPrompt<string>().AddChoices(BackChoice));
    }

    private void ShowMyRaces()
    {
        while (true)
        {
            var state = data.Snapshot;
            var records = MyRacesPresentation.CreateRecords(state.Sessions, state.UserName);
            if (!myRacesFiltersSelected && records.Count > 0)
            {
                SelectMyRacesFilters(records);
            }

            console.Clear();
            RenderHeader(withImports: false);
            console.Write(MyRacesView.Create(records, trackFilter, categoryFilter));
            console.WriteLine();

            var choices = records.Count > 0 ? new[] { ChangeFiltersChoice, BackChoice } : [BackChoice];
            if (console.Prompt(new SelectionPrompt<string>().AddChoices(choices)) == BackChoice)
            {
                return;
            }

            myRacesFiltersSelected = false;
        }
    }

    private void SelectMyRacesFilters(IReadOnlyList<MyRaceRecord> records)
    {
        var track = console.Prompt(new SelectionPrompt<string>()
            .Title("Circuit :")
            .EnableSearch()
            .UseConverter(Markup.Escape)
            .AddChoices([MyRacesView.AllTracks, .. MyRacesFilters.GetTracks(records)]));
        var category = console.Prompt(new SelectionPrompt<string>()
            .Title("Catégorie :")
            .UseConverter(Markup.Escape)
            .AddChoices([MyRacesView.AllCategories, .. MyRacesFilters.GetCategories(records)]));

        trackFilter = track == MyRacesView.AllTracks ? null : track;
        categoryFilter = category == MyRacesView.AllCategories ? null : category;
        myRacesFiltersSelected = true;
    }

    private async Task ShowOptionsAsync(CancellationToken cancellationToken)
    {
        const string directoryChoice = "Dossier des résultats";
        const string driverChoice = "Pilote principal";
        const string mergeChoice = "Fusion de pilotes";
        const string startupChoice = "Démarrage automatique";
        const string logChoice = "Fichier de log";

        while (true)
        {
            var state = data.Snapshot;
            console.Clear();
            RenderHeader(withImports: false);
            console.Write(OptionsView.Create(state, ReadOptionsSettings(state)));
            console.WriteLine();
            if (optionsFeedback is not null)
            {
                console.MarkupLine($" {Markup.Escape(optionsFeedback)}");
                console.WriteLine();
                optionsFeedback = null;
            }

            var choice = console.Prompt(new SelectionPrompt<string>()
                .Title("Que voulez-vous modifier ?")
                .AddChoices(directoryChoice, driverChoice, mergeChoice, startupChoice, logChoice, BackChoice));
            switch (choice)
            {
                case directoryChoice:
                    await ChangeResultsDirectoryAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case driverChoice:
                    ChangeDriver();
                    break;
                case mergeChoice:
                    ManageDriverMerges();
                    break;
                case startupChoice:
                    ChangeWatchOnStartup();
                    break;
                case logChoice:
                    ChangeLogSettings();
                    break;
                default:
                    return;
            }
        }
    }

    /// <summary>
    /// Validates the folder, then saves and loads it in one step like the "Apply" button of the
    /// dashboard, so the folder in use is always the one stored in appsettings.json.
    /// </summary>
    private async Task ChangeResultsDirectoryAsync(CancellationToken cancellationToken)
    {
        var directory = console.Prompt(new TextPrompt<string>("Dossier des résultats (vide = détection automatique) :")
            .DefaultValue(configuration["LMU:ResultsDirectory"] ?? string.Empty)
            .AllowEmpty()).Trim();
        var status = data.CheckResultsDirectory(directory);
        if (!status.IsValid)
        {
            optionsFeedback = $"🔴 {status.ErrorMessage ?? "Dossier introuvable ou illisible"} Rien n'a été enregistré.";
            return;
        }

        console.MarkupLine(Markup.Escape($"🟢 Dossier valide · {status.XmlFileCount} fichier(s) XML · {status.DirectoryPath}"));
        if (!console.Confirm("Appliquer ?"))
        {
            return;
        }

        try
        {
            settingsStore.SaveResultsDirectory(directory);
            var reload = data.ReloadResultsAsync(directory, cancellationToken);
            await ShowLoadingAsync(reload, cancellationToken).ConfigureAwait(false);
            status = await reload.ConfigureAwait(false);
            optionsFeedback = status.IsValid
                ? $"Dossier enregistré et chargé : {status.XmlFileCount} fichier(s) traité(s). La surveillance est active."
                : status.ErrorMessage;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            logger.LogError(exception, "Could not save the results directory");
            optionsFeedback = $"Impossible d'enregistrer le dossier : {exception.Message}";
        }
    }

    private void ChangeDriver()
    {
        var state = data.Snapshot;
        if (state.DriverFrequencies.Count == 0)
        {
            optionsFeedback = "Aucun pilote dans les résultats chargés.";
            return;
        }

        var drivers = state.DriverFrequencies
            .OrderByDescending(driver => string.Equals(driver.Name, state.UserName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var driver = console.Prompt(new SelectionPrompt<DriverFrequency>()
            .Title("Pilote utilisé dans les vues de résultats :")
            .EnableSearch()
            .UseConverter(frequency => Markup.Escape(OptionsView.FormatFrequency(frequency)))
            .AddChoices(drivers));
        try
        {
            data.SelectUserName(driver.Name);
            optionsFeedback = $"Profil pilote enregistré : {driver.Name}.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException
                or System.Text.Json.JsonException or InvalidOperationException)
        {
            logger.LogError(exception, "Could not save the selected driver");
            optionsFeedback = $"Impossible d'enregistrer le pilote sélectionné : {exception.Message}";
        }
    }

    private void ManageDriverMerges()
    {
        var merges = data.Snapshot.DriverMerges;
        var choices = new List<string> { AddMergeChoice };
        choices.AddRange(merges.Select(FormatMerge));
        choices.Add(BackChoice);

        var choice = console.Prompt(new SelectionPrompt<string>()
            .Title("Fusion de pilotes")
            .UseConverter(Markup.Escape)
            .AddChoices(choices));
        if (choice == BackChoice)
        {
            return;
        }

        if (choice == AddMergeChoice)
        {
            AddDriverMerge();
            return;
        }

        var merge = merges[choices.IndexOf(choice) - 1];
        if (!console.Confirm(Markup.Escape($"Annuler la fusion {merge.From} → {merge.To} ?"), defaultValue: false))
        {
            return;
        }

        SaveMerges(DriverMerges.Remove(merges, merge.From), $"Fusion de {merge.From} annulée.");
    }

    private void AddDriverMerge()
    {
        var names = data.Snapshot.DriverFrequencies.Select(driver => driver.Name).ToArray();
        if (names.Length < 2)
        {
            optionsFeedback = "Il faut au moins deux pilotes dans les résultats chargés pour les fusionner.";
            return;
        }

        var from = console.Prompt(new SelectionPrompt<string>()
            .Title("Ancien nom :")
            .EnableSearch()
            .UseConverter(Markup.Escape)
            .AddChoices(names));
        var to = console.Prompt(new SelectionPrompt<string>()
            .Title("Nom conservé :")
            .EnableSearch()
            .UseConverter(Markup.Escape)
            .AddChoices(names.Where(name => !string.Equals(name, from, StringComparison.OrdinalIgnoreCase))));
        console.MarkupLine(Markup.Escape($"Ancien nom : {from} · Nom conservé : {to}"));

        // One person cannot drive twice in a session: shared sessions hint at two different people.
        var sharedSessions = data.CountSharedSessions(from, to);
        if (sharedSessions > 0)
        {
            console.MarkupLine(ConsoleTheme.Colored(
                $"⚠ Ces deux pilotes ont participé ensemble à {sharedSessions} session(s) : "
                + "ce sont probablement deux personnes différentes.",
                ConsoleTheme.Loss));
            if (!console.Confirm("Fusionner quand même ?", defaultValue: false))
            {
                return;
            }
        }

        try
        {
            SaveMerges(DriverMerges.Add(data.Snapshot.DriverMerges, from, to), $"{from} est fusionné avec {to}.");
        }
        catch (ArgumentException exception)
        {
            optionsFeedback = $"Impossible de fusionner les pilotes : {exception.Message}";
        }
    }

    private void SaveMerges(IReadOnlyList<DriverMerge> merges, string feedback)
    {
        try
        {
            data.SetDriverMerges(merges);
            optionsFeedback = feedback;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            logger.LogError(exception, "Could not save the driver merges");
            optionsFeedback = $"Impossible d'enregistrer les fusions : {exception.Message}";
        }
    }

    private string FormatMerge(DriverMerge merge)
    {
        var sharedSessions = data.CountSharedSessions(merge.From, merge.To);
        return sharedSessions > 0
            ? $"{merge.From} → {merge.To}  ⚠ {sharedSessions} session(s) en commun"
            : $"{merge.From} → {merge.To}";
    }

    private void ChangeWatchOnStartup()
    {
        var watchOnStartup = console.Confirm(
            "Lancer la surveillance au démarrage ?",
            configuration.GetValue("LMU:WatchResultsOnStartup", true));
        try
        {
            settingsStore.SaveWatchOnStartup(watchOnStartup);
            optionsFeedback = "Préférence de démarrage enregistrée. Elle s'applique au prochain démarrage.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            logger.LogError(exception, "Could not save the watcher startup preference");
            optionsFeedback = $"Impossible d'enregistrer la préférence de démarrage : {exception.Message}";
        }
    }

    private void ChangeLogSettings()
    {
        var current = logOptions.CurrentValue;
        var logFilePath = console.Prompt(
            new TextPrompt<string>("Chemin du fichier de log JSON (relatif à l'exécutable ou absolu) :")
                .DefaultValue(current.LogFilePath));
        var levels = Enum.GetNames<LogLevel>();
        var level = console.Prompt(new SelectionPrompt<string>()
            .Title("Niveau minimal écrit dans le fichier de log :")
            .AddChoices(levels.OrderByDescending(name =>
                string.Equals(name, current.FileMinimumLogLevel, StringComparison.OrdinalIgnoreCase))));
        try
        {
            logSettingsStore.Save(new LmuApplicationOptions
            {
                LogFilePath = logFilePath,
                FileMinimumLogLevel = level
            });
            optionsFeedback = "Réglages du fichier de log enregistrés.";
        }
        catch (OptionsValidationException exception)
        {
            logger.LogWarning(exception, "Rejected invalid log settings");
            optionsFeedback = exception.Message;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Could not save the log settings");
            optionsFeedback = $"Impossible d'enregistrer les réglages du log : {exception.Message}";
        }
    }

    private OptionsSettings ReadOptionsSettings(DashboardState state)
    {
        var configuredDirectory = configuration["LMU:ResultsDirectory"] ?? string.Empty;
        var current = logOptions.CurrentValue;
        return new OptionsSettings(
            configuredDirectory,
            data.CheckResultsDirectory(configuredDirectory),
            configuration.GetValue("LMU:WatchResultsOnStartup", true),
            current.LogFilePath,
            current.FileMinimumLogLevel,
            state.DriverMerges.Count(merge => data.CountSharedSessions(merge.From, merge.To) > 0));
    }

    /// <summary>
    /// Shows a progress bar until the results are loaded and <paramref name="operation"/> ends.
    /// </summary>
    private async Task ShowLoadingAsync(Task operation, CancellationToken cancellationToken)
    {
        await console.Progress()
            .AutoClear(true)
            .StartAsync(async context =>
            {
                var task = context.AddTask("[deepskyblue1]Chargement des résultats[/]", maxValue: 1);
                while (true)
                {
                    var state = data.Snapshot;
                    task.MaxValue = Math.Max(1, state.FilesToLoad);
                    task.Value = state.FilesLoaded;
                    onProgress?.Invoke();
                    if (!state.IsLoading && operation.IsCompleted)
                    {
                        task.Value = task.MaxValue;
                        return;
                    }

                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                }
            })
            .ConfigureAwait(false);
    }

    private void RenderLoadSummary(DashboardState state)
    {
        if (!string.IsNullOrWhiteSpace(state.ErrorMessage))
        {
            console.MarkupLine(ConsoleTheme.Colored(
                $"Les résultats n'ont pas pu être chargés : {state.ErrorMessage}",
                ConsoleTheme.Loss));
            console.MarkupLine(ConsoleTheme.MutedMarkup("Vérifiez le dossier des résultats dans Options."));
        }
        else
        {
            // Every file is either imported, ignored (practice) or failed.
            var failures = state.ImportFailures.Count;
            var skippedPractice = Math.Max(0, state.FilesLoaded - state.Sessions.Count - failures);
            console.MarkupLine(
                $"{state.Sessions.Count} session(s) importée(s) · {skippedPractice} essai(s) ignoré(s) · "
                + (failures > 0 ? ConsoleTheme.Colored($"{failures} échec(s)", ConsoleTheme.Loss) : "0 échec(s)"));
            foreach (var failure in state.ImportFailures)
            {
                console.MarkupLine(
                    $"{ConsoleTheme.Colored("✖", ConsoleTheme.Loss)} "
                    + Markup.Escape($"{failure.SourceFile} — {failure.ErrorMessage ?? "Erreur inconnue"}"));
            }
        }

        console.WriteLine();
    }

    private void RenderHeader(bool withImports)
    {
        console.Write(StatusBanner.Create(data.Snapshot));
        if (!withImports)
        {
            return;
        }

        var imports = new List<SessionImportResult>();
        while (liveImports.TryDequeue(out var result))
        {
            imports.Add(result);
        }

        if (ImportNotice.Create(imports) is IRenderable notice)
        {
            console.Write(notice);
            console.WriteLine();
        }
    }
}
