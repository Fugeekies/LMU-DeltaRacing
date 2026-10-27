using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Application.Telemetry;
using LMU_DeltaRacing.ConsoleApp;
using LMU_DeltaRacing.ConsoleApp.Configuration;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Infrastructure.Persistence;
using LMU_DeltaRacing.Presentation.Services;
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

// The console takes no argument: like the dashboard, it loads the results directory configured in
// appsettings.json beside the executable, or the one detected automatically.
var executableDirectory = AppContext.BaseDirectory;
var configurationFilePath = Path.Combine(executableDirectory, "appsettings.json");
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = [],
    ContentRootPath = executableDirectory
});

// Only the log settings stop the console: an unreadable results directory is reported in the
// menu so that it can be fixed from the options view.
var optionsValidator = new LmuApplicationOptionsValidator(executableDirectory);
var initialOptions = builder.Configuration
    .GetSection(LmuApplicationOptions.SectionName)
    .Get<LmuApplicationOptions>() ?? new LmuApplicationOptions();
var initialValidation = optionsValidator.Validate(Options.DefaultName, initialOptions);
if (initialValidation.Failed)
{
    AnsiConsole.MarkupLine("[red]La configuration du fichier de log est invalide :[/]");
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
// The console never imports practice sessions (the default of the import service).
builder.Services.AddSingleton<ISessionImportService, SessionImportService>();
builder.Services.AddSingleton(new DashboardSettingsStore(
    (IConfigurationRoot)builder.Configuration,
    configurationFilePath));
builder.Services.AddSingleton<DashboardDataService>();
builder.Services.AddHostedService(serviceProvider =>
    serviceProvider.GetRequiredService<DashboardDataService>());

using var host = builder.Build();
await host.StartAsync();

var application = new ConsoleApplication(
    AnsiConsole.Console,
    host.Services.GetRequiredService<DashboardDataService>(),
    host.Services.GetRequiredService<DashboardSettingsStore>(),
    host.Services.GetRequiredService<LmuApplicationSettingsStore>(),
    host.Services.GetRequiredService<IConfiguration>(),
    host.Services.GetRequiredService<IOptionsMonitor<LmuApplicationOptions>>(),
    host.Services.GetRequiredService<ILogger<ConsoleApplication>>(),
    statisticsCollector.SampleMemory);
var exitCode = await application.RunAsync();

await host.StopAsync();
statisticsCollector.Complete();
return exitCode;
