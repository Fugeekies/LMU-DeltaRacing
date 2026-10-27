using System.Text.Json;
using System.Text.Json.Nodes;
using LMU_DeltaRacing.Presentation.Models;
using Microsoft.Extensions.Configuration;

namespace LMU_DeltaRacing.Presentation.Services;

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
    /// Saves the results directory atomically. An empty value restores automatic detection.
    /// </summary>
    public void SaveResultsDirectory(string resultsDirectory)
    {
        ArgumentNullException.ThrowIfNull(resultsDirectory);
        var root = LoadSettings();
        GetLmuSettings(root)["ResultsDirectory"] = resultsDirectory;

        Save(root);
    }

    /// <summary>
    /// Saves whether the results watcher starts with the application.
    /// </summary>
    public void SaveWatchOnStartup(bool watchOnStartup)
    {
        var root = LoadSettings();
        GetLmuSettings(root)["WatchResultsOnStartup"] = watchOnStartup;

        Save(root);
    }

    /// <summary>
    /// Saves the driver merges as a list of { From, To } objects. A list is used instead of a
    /// name-keyed object because configuration keys cannot contain every character of a name.
    /// </summary>
    public void SaveDriverMerges(IEnumerable<DriverMerge> merges)
    {
        ArgumentNullException.ThrowIfNull(merges);
        var root = LoadSettings();
        var array = new JsonArray();
        foreach (var merge in merges)
        {
            array.Add(new JsonObject
            {
                ["From"] = merge.From,
                ["To"] = merge.To
            });
        }

        GetLmuSettings(root)["DriverMerges"] = array;
        Save(root);
    }

    /// <summary>
    /// Saves whether practice sessions are loaded (they are mostly empty and slow down loading).
    /// </summary>
    public void SaveLoadPracticeSessions(bool loadPracticeSessions)
    {
        var root = LoadSettings();
        GetLmuSettings(root)["LoadPracticeSessions"] = loadPracticeSessions;

        Save(root);
    }

    /// <summary>
    /// Saves the selected driver name to application settings.
    /// </summary>
    public void SaveUserName(string userName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        var root = LoadSettings();
        GetLmuSettings(root)["UserName"] = userName.Trim();

        Save(root);
    }

    /// <summary>
    /// Saves the chart settings of the progression page; enum values are written by name.
    /// </summary>
    public void SaveProgressionSettings(ProgressionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var root = LoadSettings();
        GetLmuSettings(root)["Progression"] = new JsonObject
        {
            ["Granularity"] = settings.Granularity.ToString(),
            ["Aggregate"] = settings.Aggregate.ToString(),
            ["Mode"] = settings.Mode.ToString(),
            ["Session"] = settings.Session.ToString()
        };

        Save(root);
    }

    private JsonObject LoadSettings() =>
        File.Exists(configurationFilePath)
            ? JsonNode.Parse(File.ReadAllText(configurationFilePath)) as JsonObject
                ?? throw new JsonException("The application settings file must contain a JSON object.")
            : new JsonObject();

    private static JsonObject GetLmuSettings(JsonObject root)
    {
        var lmuSettings = root["LMU"] as JsonObject;
        if (lmuSettings is null)
        {
            lmuSettings = new JsonObject();
            root["LMU"] = lmuSettings;
        }

        return lmuSettings;
    }

    private void Save(JsonObject root)
    {
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
