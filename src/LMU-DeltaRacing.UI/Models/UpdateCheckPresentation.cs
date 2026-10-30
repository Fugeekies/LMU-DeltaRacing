namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// What the Options page shows about the update check.
/// </summary>
public static class UpdateCheckPresentation
{
    /// <summary>
    /// The installer of the latest release: GitHub redirects this address to the current file, so
    /// the link never has to change. The installer keeps its name from one release to the next.
    /// </summary>
    public const string DownloadUrl =
        "https://github.com/Fugeekies/LMU-DeltaRacing/releases/latest/download/LMU-DeltaRacing-Setup.exe";

    /// <summary>
    /// The page that lists every release.
    /// </summary>
    public const string ReleasesUrl = "https://github.com/Fugeekies/LMU-DeltaRacing/releases";

    /// <summary>
    /// Returns the sentence that describes the state of the check.
    /// </summary>
    public static string Label(UpdateCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            UpdateCheckStatus.Pending => "Vérification en cours…",
            UpdateCheckStatus.UpToDate => "Vous utilisez la dernière version.",
            UpdateCheckStatus.UpdateAvailable =>
                $"Nouvelle version disponible : {ApplicationVersion.Display(result.LatestVersion ?? string.Empty)}",
            UpdateCheckStatus.Failed => "Impossible de vérifier les mises à jour.",
            UpdateCheckStatus.Disabled => "Vérification désactivée.",
            UpdateCheckStatus.DevelopmentBuild => "Version de développement : pas de vérification.",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, "Unknown update check status.")
        };
    }

    /// <summary>
    /// Returns whether the download button is offered: only when a newer release exists.
    /// </summary>
    public static bool OffersDownload(UpdateCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Status == UpdateCheckStatus.UpdateAvailable;
    }

    /// <summary>
    /// Returns whether the releases page is offered: a failed check says nothing about the latest
    /// version, so the user can still compare by hand.
    /// </summary>
    public static bool OffersReleasesPage(UpdateCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Status == UpdateCheckStatus.Failed;
    }

    /// <summary>
    /// Returns the class of the status dot, green only when the application is known to be up to date.
    /// </summary>
    public static string DotClass(UpdateCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Status == UpdateCheckStatus.UpToDate ? "is-live" : "is-idle";
    }
}
