namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The outcome of the update check made at startup.
/// </summary>
public enum UpdateCheckStatus
{
    /// <summary>
    /// The check has not finished yet.
    /// </summary>
    Pending,

    /// <summary>
    /// The running version is the latest release, or a more recent one.
    /// </summary>
    UpToDate,

    /// <summary>
    /// A newer release is published.
    /// </summary>
    UpdateAvailable,

    /// <summary>
    /// GitHub could not be reached or its answer could not be used.
    /// </summary>
    Failed,

    /// <summary>
    /// The check is turned off in the options.
    /// </summary>
    Disabled,

    /// <summary>
    /// A local build has no release number to compare.
    /// </summary>
    DevelopmentBuild
}

/// <summary>
/// The state of the update check.
/// </summary>
/// <param name="Status">The outcome of the check.</param>
/// <param name="LatestVersion">The latest release without its <c>v</c> prefix, when it is known.</param>
public sealed record UpdateCheckResult(UpdateCheckStatus Status, string? LatestVersion);
