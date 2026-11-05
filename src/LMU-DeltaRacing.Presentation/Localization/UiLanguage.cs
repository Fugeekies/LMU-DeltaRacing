using System.Globalization;

namespace LMU_DeltaRacing.Presentation.Localization;

/// <summary>
/// The languages of the interface and the choice of the one shown (<c>LMU:Language</c>).
/// </summary>
public static class UiLanguage
{
    /// <summary>
    /// The language of the texts of reference, in which the resource is written.
    /// </summary>
    public const string French = "fr";

    /// <summary>
    /// The language used when neither the setting nor Windows gives a supported one.
    /// </summary>
    public const string Fallback = "en";

    /// <summary>
    /// Gets the supported languages, by their two-letter code, in the order of the Options list.
    /// </summary>
    public static IReadOnlyList<string> Supported { get; } = ["en", "fr", "de", "it", "es"];

    /// <summary>
    /// Gets the language to show: the configured one when supported, otherwise the language of
    /// Windows when supported, otherwise English.
    /// </summary>
    /// <param name="configured">The value of <c>LMU:Language</c>, empty for automatic.</param>
    /// <param name="windowsCulture">The culture of the Windows user interface.</param>
    public static string Resolve(string? configured, CultureInfo windowsCulture)
    {
        ArgumentNullException.ThrowIfNull(windowsCulture);

        var setting = configured?.Trim().ToLowerInvariant();
        if (setting is not null && Supported.Contains(setting))
        {
            return setting;
        }

        var windows = windowsCulture.TwoLetterISOLanguageName;
        return Supported.Contains(windows) ? windows : Fallback;
    }

    /// <summary>
    /// Gets the culture of a supported language. English is British, so that dates read day first.
    /// </summary>
    /// <param name="language">A supported two-letter code.</param>
    public static CultureInfo Culture(string language) => CultureInfo.GetCultureInfo(language switch
    {
        "en" => "en-GB",
        "de" => "de-DE",
        "it" => "it-IT",
        "es" => "es-ES",
        _ => "fr-FR"
    });

    /// <summary>
    /// Gets the name of a language written in that language, as listed in Options.
    /// </summary>
    /// <param name="language">A supported two-letter code.</param>
    public static string NativeName(string language) => language switch
    {
        "en" => "English",
        "de" => "Deutsch",
        "it" => "Italiano",
        "es" => "Español",
        _ => "Français"
    };

    /// <summary>
    /// Gets the language of the texts shown now, from the current UI culture.
    /// </summary>
    public static string Current
    {
        get
        {
            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Supported.Contains(language) ? language : French;
        }
    }

    /// <summary>
    /// Shows the interface in a language from now on, for every thread that has not chosen its own
    /// culture. Numbers keep the culture of Windows: only texts, ordinals, dates and month names
    /// follow the language.
    /// </summary>
    /// <param name="language">A supported two-letter code.</param>
    public static void Apply(string language) =>
        CultureInfo.DefaultThreadCurrentUICulture = Culture(language);
}
