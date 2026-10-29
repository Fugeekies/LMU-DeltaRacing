using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.Presentation.Services;
using Microsoft.AspNetCore.Components;

namespace LMU_DeltaRacing.UI.Components;

/// <summary>
/// Base of the pages that show the imported results: keeps <see cref="State"/> in sync with the
/// data service and lets the page recalculate its data after each change.
/// </summary>
public abstract class DashboardPage : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the shared results service.
    /// </summary>
    [Inject]
    protected DashboardDataService DashboardData { get; set; } = null!;

    /// <summary>
    /// Gets the dashboard state the page is rendered with.
    /// </summary>
    protected DashboardState State { get; private set; } = DashboardState.Initial;

    /// <inheritdoc />
    public void Dispose()
    {
        DashboardData.StateChanged -= OnDashboardStateChanged;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        DashboardData.StateChanged += OnDashboardStateChanged;
        RefreshState();
    }

    /// <summary>
    /// Recalculates the page data from <see cref="State"/>; called at start and after each change.
    /// </summary>
    protected abstract void OnStateChanged();

    private void RefreshState()
    {
        State = DashboardData.Snapshot;
        OnStateChanged();
    }

    // The service raises its event on import threads: the page is updated on its own dispatcher.
    private void OnDashboardStateChanged() =>
        _ = InvokeAsync(() =>
        {
            RefreshState();
            StateHasChanged();
        });
}
