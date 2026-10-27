using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LMU_DeltaRacing.ConsoleApp.Configuration;

/// <summary>
/// Persists editable application settings to the appsettings.json beside the executable.
/// </summary>
public sealed class LmuApplicationSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IConfigurationRoot configuration;
    private readonly string configurationFilePath;
    private readonly LmuApplicationOptionsValidator validator;

    /// <summary>
    /// Creates a settings store that validates and reloads changes from appsettings.json.
    /// </summary>
    /// <param name="configuration">The host configuration root to reload after a save.</param>
    /// <param name="configurationFilePath">The appsettings.json path beside the executable.</param>
    /// <param name="validator">The validator used before persisting settings.</param>
    public LmuApplicationSettingsStore(
        IConfigurationRoot configuration,
        string configurationFilePath,
        LmuApplicationOptionsValidator validator)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationFilePath);
        ArgumentNullException.ThrowIfNull(validator);

        this.configuration = configuration;
        this.configurationFilePath = Path.GetFullPath(configurationFilePath);
        this.validator = validator;
    }

    /// <summary>
    /// Saves validated log settings atomically and reloads the active configuration. Only the log
    /// keys are written: the rest of the LMU section belongs to the shared dashboard settings.
    /// </summary>
    /// <param name="options">The settings to persist.</param>
    public void Save(LmuApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var validation = validator.Validate(Options.DefaultName, options);
        if (validation.Failed)
        {
            throw new OptionsValidationException(
                Options.DefaultName,
                typeof(LmuApplicationOptions),
                validation.Failures);
        }

        var root = File.Exists(configurationFilePath)
            ? JsonNode.Parse(File.ReadAllText(configurationFilePath)) as JsonObject ?? new JsonObject()
            : new JsonObject();
        if (root[LmuApplicationOptions.SectionName] is not JsonObject lmuSettings)
        {
            lmuSettings = new JsonObject();
            root[LmuApplicationOptions.SectionName] = lmuSettings;
        }

        lmuSettings[nameof(LmuApplicationOptions.LogFilePath)] = options.LogFilePath;
        lmuSettings[nameof(LmuApplicationOptions.FileMinimumLogLevel)] = options.FileMinimumLogLevel;

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
