using FluentAssertions;
using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the startup loading screen visibility, progress and caption.
/// </summary>
[TestClass]
public sealed class StartupLoadingPresentationTests
{
    [TestMethod]
    public void Screen_is_visible_only_during_the_startup_import()
    {
        var loading = CreateState(isLoading: true, filesToLoad: 10, filesLoaded: 3);

        StartupLoadingPresentation.IsVisible(loading with { IsStartupImportRunning = true }).Should().BeTrue();
        StartupLoadingPresentation.IsVisible(loading)
            .Should().BeFalse("a reload from Options keeps the dashboard visible");
    }

    [TestMethod]
    public void Progress_is_the_share_of_processed_files()
    {
        StartupLoadingPresentation.ProgressPercent(CreateState(true, 3, 1)).Should().Be(33);
        StartupLoadingPresentation.ProgressPercent(CreateState(true, 3, 2)).Should().Be(67);
        StartupLoadingPresentation.ProgressPercent(CreateState(true, 4, 4)).Should().Be(100);
    }

    [TestMethod]
    public void Progress_starts_at_zero_while_the_files_are_not_counted_and_never_exceeds_100()
    {
        StartupLoadingPresentation.ProgressPercent(CreateState(true, 0, 0)).Should().Be(0);
        StartupLoadingPresentation.ProgressPercent(CreateState(true, 2, 5)).Should().Be(100);
    }

    [TestMethod]
    public void Caption_counts_processed_files_once_they_are_known()
    {
        StartupLoadingPresentation.Caption(CreateState(true, 0, 0)).Should().Be("Recherche des fichiers de résultats…");
        StartupLoadingPresentation.Caption(CreateState(true, 170, 42)).Should().Be("42 / 170 fichiers traités");
    }

    private static DashboardState CreateState(bool isLoading, int filesToLoad, int filesLoaded) =>
        new(isLoading, filesToLoad, filesLoaded, false, null, [], [], null);
}
