using System.Globalization;
using System.Resources;

namespace LMU_DeltaRacing.Presentation.Localization;

/// <summary>
/// Reads the texts shown to the user from <c>Resources/Texts.resx</c>, in the current UI culture.
/// </summary>
/// <remarks>
/// French is the neutral language of the resource: a missing translation falls back to French.
/// The type also names the resource for <c>IStringLocalizer&lt;Texts&gt;</c> in the Razor pages.
/// </remarks>
public sealed class Texts
{
    private static readonly ResourceManager Resources = new(
        "LMU_DeltaRacing.Presentation.Resources.Texts",
        typeof(Texts).Assembly);

    private Texts()
    {
    }

    /// <summary>
    /// Gets the text of a key in the current UI culture, or <see langword="null"/> when the key is unknown.
    /// </summary>
    /// <param name="key">The key of the text, such as <c>MyRaces.Title</c>.</param>
    public static string? Find(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Resources.GetString(key, CultureInfo.CurrentUICulture);
    }

    /// <summary>
    /// Gets the text of a key in the current UI culture. An unknown key is returned as is, so that
    /// the screen shows what is missing instead of failing; a test checks that every key exists.
    /// </summary>
    /// <param name="key">The key of the text, such as <c>MyRaces.Title</c>.</param>
    public static string Get(string key) => Find(key) ?? key;

    /// <summary>
    /// Gets the text of a key and fills its placeholders (<c>{0}</c>, <c>{1}</c>…) with the culture
    /// of the current thread.
    /// </summary>
    /// <param name="key">The key of the text.</param>
    /// <param name="arguments">The values of the placeholders.</param>
    public static string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    /// <summary>
    /// Gets the singular (<c>key.One</c>) or plural (<c>key.Other</c>) text of a count, with the
    /// count as its first placeholder: <c>1 course</c>, <c>3 courses</c>.
    /// </summary>
    /// <remarks>
    /// French puts 0 in the singular (<c>0 course</c>); English, German, Italian and Spanish only 1.
    /// </remarks>
    /// <param name="key">The key of the text, without its <c>.One</c> or <c>.Other</c> suffix.</param>
    /// <param name="count">The counted number.</param>
    /// <param name="arguments">The values of the other placeholders, from <c>{1}</c>.</param>
    public static string Plural(string key, int count, params object?[] arguments)
    {
        var isSingular = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr"
            ? count is 0 or 1 or -1
            : count is 1 or -1;
        return Format($"{key}.{(isSingular ? "One" : "Other")}", [count, .. arguments]);
    }
}
