using System.Globalization;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The startup loading screen shown instead of the dashboard while the initial import runs.
/// </summary>
public static class StartupLoadingPresentation
{
    /// <summary>
    /// Returns whether the loading screen replaces the dashboard: only during the startup import,
    /// so a reload from Options keeps its own progress bar.
    /// </summary>
    public static bool IsVisible(DashboardState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.IsStartupImportRunning;
    }

    /// <summary>
    /// Returns the share of processed startup files, from 0 to 100.
    /// </summary>
    public static int ProgressPercent(DashboardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // The file count is unknown until the directory has been listed: the wheel stays at the start.
        if (state.FilesToLoad <= 0)
        {
            return 0;
        }

        var percent = (int)Math.Round(state.FilesLoaded * 100d / state.FilesToLoad);
        return Math.Clamp(percent, 0, 100);
    }

    /// <summary>
    /// Returns the caption displayed under the road.
    /// </summary>
    public static string Caption(DashboardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.FilesToLoad <= 0
            ? "Recherche des fichiers de résultats…"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{state.FilesLoaded} / {state.FilesToLoad} fichiers traités");
    }
}
