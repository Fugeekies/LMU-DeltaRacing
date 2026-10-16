using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Application.Telemetry;
using LMU_DeltaRacing.ConsoleApp;
using LMU_DeltaRacing.ConsoleApp.Configuration;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Spectre.Console;

var executableDirectory = AppContext.BaseDirectory;
var configurationFilePath = Path.Combine(executableDirectory, "appsettings.json");
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = [],
    ContentRootPath = executableDirectory
});

var optionsValidator = new LmuApplicationOptionsValidator(executableDirectory);
var initialOptions = builder.Configuration
    .GetSection(LmuApplicationOptions.SectionName)
    .Get<LmuApplicationOptions>() ?? new LmuApplicationOptions();
var initialValidation = optionsValidator.Validate(Options.DefaultName, initialOptions);
if (initialValidation.Failed)
{
    AnsiConsole.MarkupLine("[red]La configuration est invalide :[/]");
    foreach (var failure in initialValidation.Failures)
    {
        AnsiConsole.MarkupLine($"[red]- {Markup.Escape(failure)}[/]");
    }

    return 1;
}

var statisticsFilePath = Path.Combine(executableDirectory, "lmu-deltaracing-stats.json");
using var statisticsCollector = new RunStatisticsCollector(statisticsFilePath);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AddProcessor(serviceProvider =>
        new SimpleLogRecordExportProcessor(serviceProvider.GetRequiredService<FileLogExporter>()));
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("LMU-DeltaRacing.Console"))
    .WithTracing(tracing => tracing
        .AddSource(ApplicationTelemetry.ActivitySourceName)
        .AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug))
    .WithMetrics(metrics => metrics
        .AddMeter(ApplicationTelemetry.MeterName)
        .AddRuntimeInstrumentation()
        .AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug));

builder.Services.AddSingleton(optionsValidator);
builder.Services.AddSingleton<IValidateOptions<LmuApplicationOptions>>(
    serviceProvider => serviceProvider.GetRequiredService<LmuApplicationOptionsValidator>());
builder.Services.AddOptions<LmuApplicationOptions>()
    .Bind(builder.Configuration.GetSection(LmuApplicationOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<FileLogExporter>();
builder.Services.AddSingleton(serviceProvider => new LmuApplicationSettingsStore(
    (IConfigurationRoot)builder.Configuration,
    configurationFilePath,
    serviceProvider.GetRequiredService<LmuApplicationOptionsValidator>()));
builder.Services.AddSingleton<IResultXmlParser, XmlResultParser>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
builder.Services.AddSingleton<ISessionImportService, SessionImportService>();

using var host = builder.Build();
await host.StartAsync();

var logger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("LMU-DeltaRacing.Configuration");
var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<LmuApplicationOptions>>();
var settingsStore = host.Services.GetRequiredService<LmuApplicationSettingsStore>();
var importer = host.Services.GetRequiredService<ISessionImportService>();
var sessionRepository = host.Services.GetRequiredService<ISessionRepository>();
var imports = new List<SessionImportResult>();
IReadOnlyList<string> filesToImport = args;
var statisticsTableSelection = StatisticsTableSelection.Both;

if (filesToImport.Count == 0)
{
    var shouldImport = false;
    while (!shouldImport)
    {
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Que souhaitez-vous faire ?")
                .AddChoices(
                    "Importer les résultats",
                    "Choisir les tableaux statistiques",
                    "Modifier la configuration",
                    "Quitter"));

        if (action == "Quitter")
        {
            await host.StopAsync();
            statisticsCollector.Complete();
            return 0;
        }

        if (action == "Modifier la configuration")
        {
            var current = optionsMonitor.CurrentValue;
            var resolvedResultsDirectory = optionsValidator.ResolveResultsDirectory(current);
            AnsiConsole.MarkupLine(
                $"[dim]Laisser le chemin des résultats vide pour utiliser : {Markup.Escape(resolvedResultsDirectory)}[/]");

            var resultsDirectory = AnsiConsole.Prompt(
                new TextPrompt<string>("Dossier des résultats XML :")
                    .DefaultValue(current.ResultsDirectory)
                    .AllowEmpty());
            var logFilePath = AnsiConsole.Prompt(
                new TextPrompt<string>("Chemin du fichier de log JSON (relatif à l'exécutable ou absolu) :")
                    .DefaultValue(current.LogFilePath));
            var logLevels = Enum.GetNames<LogLevel>();
            var currentLogLevel = Enum.TryParse<LogLevel>(
                current.FileMinimumLogLevel,
                ignoreCase: true,
                out var parsedLogLevel)
                ? parsedLogLevel.ToString()
                : nameof(LogLevel.Debug);
            var orderedLevels = logLevels
                .OrderByDescending(level => level == currentLogLevel)
                .ThenBy(level => Array.IndexOf(logLevels, level));
            var fileMinimumLogLevel = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Niveau minimal écrit dans le fichier de log :")
                    .AddChoices(orderedLevels));

            try
            {
                settingsStore.Save(new LmuApplicationOptions
                {
                    ResultsDirectory = resultsDirectory,
                    LogFilePath = logFilePath,
                    FileMinimumLogLevel = fileMinimumLogLevel
                });
                AnsiConsole.MarkupLine("[green]Configuration enregistrée dans appsettings.json.[/]");
            }
            catch (OptionsValidationException exception)
            {
                logger.LogWarning(exception, "Rejected invalid application configuration");
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogError(exception, "Could not save application configuration");
                AnsiConsole.MarkupLine($"[red]Impossible d'enregistrer la configuration : {Markup.Escape(exception.Message)}[/]");
            }

            continue;
        }

        if (action == "Choisir les tableaux statistiques")
        {
            var selectedTable = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Quels tableaux statistiques afficher après l'import ?")
                    .AddChoices("Les deux tableaux", "Par circuit", "Par circuit et voiture"));

            statisticsTableSelection = selectedTable switch
            {
                "Par circuit" => StatisticsTableSelection.Circuit,
                "Par circuit et voiture" => StatisticsTableSelection.CircuitAndCar,
                _ => StatisticsTableSelection.Both
            };
            AnsiConsole.MarkupLine(
                $"[green]Sélection enregistrée : {Markup.Escape(selectedTable)}.[/]");
            continue;
        }

        shouldImport = true;
    }

    var activeOptions = optionsMonitor.CurrentValue;
    var resolvedImportDirectory = optionsValidator.ResolveResultsDirectory(activeOptions);
    try
    {
        filesToImport = XmlFileDiscovery.GetXmlFiles(resolvedImportDirectory);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        logger.LogError(exception, "Could not read results directory {ResultsDirectory}", resolvedImportDirectory);
        AnsiConsole.MarkupLine($"[red]Impossible de lire le dossier : {Markup.Escape(exception.Message)}[/]");
        await host.StopAsync();
        statisticsCollector.Complete();
        return 1;
    }
}

foreach (var filePath in filesToImport)
{
    statisticsCollector.SampleMemory();
    imports.Add(importer.ImportFile(filePath));
    statisticsCollector.SampleMemory();
}

ConsoleDashboard.Render(imports);
ConsoleDashboard.RenderStatistics(sessionRepository.GetAll(), statisticsTableSelection);
await host.StopAsync();
statisticsCollector.Complete();

return imports.Any(result => result.Status == SessionImportStatus.Failed) ? 1 : 0;
