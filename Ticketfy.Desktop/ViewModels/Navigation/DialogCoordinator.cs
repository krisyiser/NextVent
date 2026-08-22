using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Ticketfy.ViewModels.Navigation;

/// <summary>
/// Manages the single active dialog overlay.
/// Provides a clean interface so ViewModels can request dialogs
/// without knowing about MainWindow state.
/// </summary>
public partial class DialogCoordinator : ObservableObject
{
    [ObservableProperty] private ObservableObject? _activeDialogViewModel = null;
    [ObservableProperty] private bool _isDialogOverlayOpen = false;

    /// <summary>Shows a dialog by setting it as the active overlay.</summary>
    public void ShowDialog(ObservableObject viewModel)
    {
        ActiveDialogViewModel = viewModel;
        IsDialogOverlayOpen = true;
    }

    /// <summary>Clears the active dialog and closes the overlay.</summary>
    public void CloseDialog()
    {
        ActiveDialogViewModel = null;
        IsDialogOverlayOpen = false;
    }

    /// <summary>Convenience: shows dialog and auto-closes when RequestClose fires.</summary>
    public void ShowDialog(ObservableObject viewModel, Action? onClose)
    {
        ShowDialog(viewModel);
    }
}
