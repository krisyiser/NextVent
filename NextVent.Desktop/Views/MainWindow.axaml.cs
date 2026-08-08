using Avalonia.Controls;
using Avalonia.Input;
using NextVent.ViewModels;

namespace NextVent.Views;

/// <summary>
/// MainWindow code-behind. Handles global function key shortcuts (F2-F10) for zero-mouse operation and fullscreen toggling.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnGlobalKeyDown;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ToggleFullscreenRequested += ToggleFullscreen;
        }
    }

    private void ToggleFullscreen()
    {
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
    }

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                if (vm.IsDialogOverlayOpen && vm.ActiveDialogViewModel is NextVent.ViewModels.Dialogs.OpenShiftDialogViewModel openShiftVm)
                {
                    openShiftVm.CancelCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Key.F2:
                vm.NavigateToCommand.Execute("pos");
                e.Handled = true;
                break;
            case Key.F3:
                vm.NavigateToCommand.Execute("inventory");
                e.Handled = true;
                break;
            case Key.F4:
                vm.NavigateToCommand.Execute("customers");
                e.Handled = true;
                break;
            case Key.F5:
                vm.NavigateToCommand.Execute("suppliers");
                e.Handled = true;
                break;
            case Key.F6:
                vm.NavigateToCommand.Execute("expenses");
                e.Handled = true;
                break;
            case Key.F7:
                vm.NavigateToCommand.Execute("history");
                e.Handled = true;
                break;
            case Key.F8:
                vm.NavigateToCommand.Execute("promotions");
                e.Handled = true;
                break;
            case Key.F9:
                vm.NavigateToCommand.Execute("fiscal");
                e.Handled = true;
                break;
            case Key.F10:
                vm.NavigateToCommand.Execute("settings");
                e.Handled = true;
                break;
            case Key.F11:
                ToggleFullscreen();
                e.Handled = true;
                break;
        }
    }
}
