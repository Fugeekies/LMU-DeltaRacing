using FluentAssertions;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies what the Options page says about the update check.
/// </summary>
[TestClass]
public sealed class UpdateCheckPresentationTests
{
    [TestMethod]
    public void The_download_link_targets_the_installer_of_the_latest_release()
    {
        UpdateCheckPresentation.DownloadUrl.Should().Be(
            "https://github.com/Fugeekies/LMU-DeltaRacing/releases/latest/download/LMU-DeltaRacing-Setup.exe");
    }

    [TestMethod]
    [DataRow(UpdateCheckStatus.Pending, null, "Vérification en cours…")]
    [DataRow(UpdateCheckStatus.UpToDate, "0.1.0", "Vous utilisez la dernière version.")]
    [DataRow(UpdateCheckStatus.UpdateAvailable, "0.2.0", "Nouvelle version disponible : v0.2.0")]
    [DataRow(UpdateCheckStatus.Failed, null, "Impossible de vérifier les mises à jour.")]
    [DataRow(UpdateCheckStatus.Disabled, null, "Vérification désactivée.")]
    [DataRow(UpdateCheckStatus.DevelopmentBuild, null, "Version de développement : pas de vérification.")]
    public void Each_status_has_its_label(UpdateCheckStatus status, string? latestVersion, string expected)
    {
        UpdateCheckPresentation.Label(new UpdateCheckResult(status, latestVersion)).Should().Be(expected);
    }

    [TestMethod]
    public void Only_a_new_release_offers_the_download()
    {
        foreach (var status in Enum.GetValues<UpdateCheckStatus>())
        {
            UpdateCheckPresentation.OffersDownload(new UpdateCheckResult(status, "0.2.0"))
                .Should().Be(status == UpdateCheckStatus.UpdateAvailable);
        }
    }

    [TestMethod]
    public void Only_a_failed_check_offers_the_releases_page()
    {
        foreach (var status in Enum.GetValues<UpdateCheckStatus>())
        {
            UpdateCheckPresentation.OffersReleasesPage(new UpdateCheckResult(status, null))
                .Should().Be(status == UpdateCheckStatus.Failed);
        }
    }

    [TestMethod]
    public void The_status_dot_is_green_only_when_up_to_date()
    {
        foreach (var status in Enum.GetValues<UpdateCheckStatus>())
        {
            UpdateCheckPresentation.DotClass(new UpdateCheckResult(status, null))
                .Should().Be(status == UpdateCheckStatus.UpToDate ? "is-live" : "is-idle");
        }
    }
}
