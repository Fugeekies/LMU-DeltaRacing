using System.Reflection;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The version of the running application, stamped at publish time from the release tag.
/// </summary>
public static class ApplicationVersion
{
    /// <summary>
    /// Gets the version of this build; a local build keeps the development version of the project file.
    /// </summary>
    public static string Current { get; } =
        typeof(ApplicationVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? string.Empty;

    /// <summary>
    /// Returns a version written like a release tag, for example <c>v0.1.2</c>.
    /// </summary>
    public static string Display(string version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return $"v{version}";
    }

    /// <summary>
    /// Reads a release version or tag (<c>0.1.2</c> or <c>v0.1.2</c>). Anything else, such as a
    /// development version, is not a release and cannot be compared with one.
    /// </summary>
    public static bool TryParseRelease(string? value, out Version? version)
    {
        version = null;
        if (value is null
            || !Version.TryParse(value.StartsWith('v') ? value[1..] : value, out var parsed)
            || parsed.Build < 0
            || parsed.Revision >= 0)
        {
            return false;
        }

        version = parsed;
        return true;
    }
}
