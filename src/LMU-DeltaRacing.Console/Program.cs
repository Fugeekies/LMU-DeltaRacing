using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Application.Telemetry;
using LMU_DeltaRacing.ConsoleApp;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Spectre.Console;

IReadOnlyList<string> filesToImport = args;
if (filesToImport.Count == 0)
{
    string resultsDirectory;
#if DEBUG
    try
    {
        resultsDirectory = XmlFileDiscovery.FindTestAssetsResultsDirectory(AppContext.BaseDirectory);
    }
    catch (DirectoryNotFoundException exception)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
        return 1;
    }

    AnsiConsole.MarkupLine(
        $"[dim]Mode Debug : utilisation des XML de test dans {Markup.Escape(resultsDirectory)}[/]");
#else
    resultsDirectory = AnsiConsole.Ask<string>(
        "Chemin du dossier contenant les résultats XML ?");
#endif

    try
    {
        filesToImport = XmlFileDiscovery.GetXmlFiles(resultsDirectory);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        AnsiConsole.MarkupLine($"[red]Impossible de lire le dossier : {Markup.Escape(exception.Message)}[/]");
        return 1;
    }
}

var builder = Host.CreateApplicationBuilder();
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AddConsoleExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("LMU-DeltaRacing.Console"))
    .WithTracing(tracing => tracing
        .AddSource(ApplicationTelemetry.ActivitySourceName)
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(ApplicationTelemetry.MeterName)
        .AddRuntimeInstrumentation()
        .AddConsoleExporter());

builder.Services.AddSingleton<IResultXmlParser, XmlResultParser>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
builder.Services.AddSingleton<ISessionImportService, SessionImportService>();

using var host = builder.Build();
await host.StartAsync();

var logger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("LMU-DeltaRacing.Import");
var importer = host.Services.GetRequiredService<ISessionImportService>();
var imports = new List<SessionImportResult>();

foreach (var filePath in filesToImport)
{
    var result = importer.ImportFile(filePath);
    imports.Add(result);

    if (result.Status == SessionImportStatus.Imported)
    {
        logger.LogInformation(
            "Imported {SourceFile}: {TrackVenue}, session {SessionType}, {DriverCount} drivers",
            result.SourceFile,
            result.Session!.TrackVenue,
            result.Session.SessionType,
            result.Session.Drivers.Count);
    }
    else
    {
        logger.LogWarning(
            "Could not import {SourceFile}: {ErrorMessage}",
            result.SourceFile,
            result.ErrorMessage);
    }
}

ConsoleDashboard.Render(imports);
await host.StopAsync();

return imports.Any(result =>
    result.Status == SessionImportStatus.Failed)
    ? 1
    : 0;
