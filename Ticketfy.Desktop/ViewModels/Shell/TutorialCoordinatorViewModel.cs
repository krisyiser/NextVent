using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Ticketfy.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Shell;

/// <summary>
/// Manages tutorial overlay lifecycle: sidebar tour and per-module first-time tours.
/// Decoupled from navigation and dialog coordination.
/// </summary>
public partial class TutorialCoordinatorViewModel : ObservableObject
{
    private readonly ITutorialService _tutorialService;

    [ObservableProperty] private TutorialOverlayViewModel _activeTutorialVm = null!;

    public event Action? SidebarTourCompleted;

    public TutorialCoordinatorViewModel(ITutorialService tutorialService)
    {
        _tutorialService = tutorialService;

        var sidebarVm = new TutorialOverlayViewModel(tutorialService, "Sidebar");
        sidebarVm.TutorialCompleted += () => SidebarTourCompleted?.Invoke();
        ActiveTutorialVm = sidebarVm;
    }

    /// <summary>
    /// Launches the sidebar onboarding tour on first login.
    /// </summary>
    public Task TryStartSidebarTourAsync()
        => ActiveTutorialVm.TryStartAsync(TutorialStepCatalog.BuildSidebarSteps());

    /// <summary>
    /// Launches a per-module tour the first time the module is opened.
    /// Swaps ActiveTutorialVm on the UI thread so MainWindow picks it up.
    /// </summary>
    public async Task LaunchModuleTourAsync(string moduleKey)
    {
        await Task.Delay(350); // let the module view settle

        var steps = TutorialStepCatalog.BuildModuleSteps(moduleKey);
        if (steps.Count == 0) return;

        var freshVm = new TutorialOverlayViewModel(_tutorialService, moduleKey);
        freshVm.TutorialCompleted += () => { };

        await freshVm.TryStartAsync(steps);

        if (freshVm.IsVisible)
        {
            Dispatcher.UIThread.Post(() => ActiveTutorialVm = freshVm);
        }
    }
}
