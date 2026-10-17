using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace LMU_DeltaRacing.UI.Services;

/// <summary>
/// Saves dashboard settings to the application configuration file.
/// </summary>
public sealed class DashboardSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IConfigurationRoot configuration;
    private readonly string configurationFilePath;

    /// <summary>
    /// Creates a store for the application settings file.
    /// </summary>
    public DashboardSettingsStore(IConfigurationRoot configuration, string configurationFilePath)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationFilePath);

        this.configuration = configuration;
        this.configurationFilePath = Path.GetFullPath(configurationFilePath);
    }

    /// <summary>
    /// Saves the results directory and startup-watcher preference atomically.
    /// </summary>
    public void SaveResultsSettings(string resultsDirectory, bool watchOnStartup)
    {
        ArgumentNullException.ThrowIfNull(resultsDirectory);
        var root = File.Exists(configurationFilePath)
            ? JsonNode.Parse(File.ReadAllText(configurationFilePath)) as JsonObject
                ?? throw new JsonException("The application settings file must contain a JSON object.")
            : new JsonObject();
        var lmuSettings = root["LMU"] as JsonObject;
        if (lmuSettings is null)
        {
            lmuSettings = new JsonObject();
            root["LMU"] = lmuSettings;
        }
        lmuSettings["ResultsDirectory"] = resultsDirectory;
        lmuSettings["WatchResultsOnStartup"] = watchOnStartup;

        var temporaryPath = $"{configurationFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, root.ToJsonString(JsonOptions));
            File.Move(temporaryPath, configurationFilePath, overwrite: true);
            configuration.Reload();
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
