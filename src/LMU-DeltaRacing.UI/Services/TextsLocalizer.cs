using LMU_DeltaRacing.Presentation.Localization;
using Microsoft.Extensions.Localization;

namespace LMU_DeltaRacing.UI.Services;

/// <summary>
/// Gives the Razor pages the texts of <see cref="Texts"/> through <see cref="IStringLocalizer{T}"/>.
/// </summary>
/// <remarks>
/// The standard resource factory derives the resource name from the assembly name, which has
/// dashes where the namespace has underscores: reading <see cref="Texts"/> keeps one source.
/// </remarks>
public sealed class TextsLocalizer : IStringLocalizer<Texts>
{
    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            var value = Texts.Find(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = Texts.Find(name);
            return new LocalizedString(
                name,
                value is null ? name : Texts.Format(name, arguments),
                resourceNotFound: value is null);
        }
    }

    /// <inheritdoc />
    /// <remarks>Not used by the pages: listing every text has no use on screen.</remarks>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
