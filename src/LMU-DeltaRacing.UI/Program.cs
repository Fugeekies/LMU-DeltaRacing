using System.ComponentModel;
using System.Diagnostics;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Infrastructure.Persistence;
using LMU_DeltaRacing.UI.Components;
using LMU_DeltaRacing.UI.Logging;
using LMU_DeltaRacing.UI.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
var configuredLogLevel = builder.Configuration["LMU:FileMinimumLogLevel"] ?? "Debug";
if (!Enum.TryParse<LogLevel>(configuredLogLevel, ignoreCase: true, out var minimumLogLevel)
    || !Enum.IsDefined(minimumLogLevel))
{
    throw new InvalidOperationException(
        $"LMU:FileMinimumLogLevel must be a valid log severity; received '{configuredLogLevel}'.");
}

var configuredLogPath = builder.Configuration["LMU:LogFilePath"] ?? "lmu-deltaracing.log";
var logFilePath = Path.GetFullPath(configuredLogPath, AppContext.BaseDirectory);
var logDirectory = Path.GetDirectoryName(logFilePath)
    ?? throw new InvalidOperationException("LMU:LogFilePath must include a parent directory.");
if (!Directory.Exists(logDirectory))
{
    throw new DirectoryNotFoundException($"The configured log directory does not exist: '{logDirectory}'.");
}

builder.Logging.AddProvider(new JsonFileLoggerProvider(logFilePath, minimumLogLevel));
builder.WebHost.UseUrls("http://127.0.0.1:0");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<IResultXmlParser, XmlResultParser>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
builder.Services.AddSingleton<ISessionImportService, SessionImportService>();
builder.Services.AddSingleton<DashboardDataService>();
builder.Services.AddHostedService(serviceProvider =>
    serviceProvider.GetRequiredService<DashboardDataService>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.StartAsync();
var addresses = app.Services.GetRequiredService<IServer>()
    .Features.Get<IServerAddressesFeature>()?.Addresses;
var dashboardUrl = addresses?.SingleOrDefault(address => address.StartsWith(
    "http://127.0.0.1:",
    StringComparison.Ordinal));
if (dashboardUrl is null)
{
    throw new InvalidOperationException("The local dashboard did not receive a loopback address.");
}

if (app.Configuration.GetValue("LMU:OpenBrowser", defaultValue: true))
{
    try
    {
        using var browser = Process.Start(new ProcessStartInfo(dashboardUrl)
        {
            UseShellExecute = true
        });
        if (browser is null)
        {
            app.Logger.LogWarning("The dashboard is available at {DashboardUrl}, but no browser was launched", dashboardUrl);
        }
    }
    catch (Exception exception) when (
        exception is Win32Exception or InvalidOperationException or UnauthorizedAccessException)
    {
        app.Logger.LogError(
            exception,
            "Could not open the default browser; the local dashboard is available at {DashboardUrl}",
            dashboardUrl);
    }
}

await app.WaitForShutdownAsync();
