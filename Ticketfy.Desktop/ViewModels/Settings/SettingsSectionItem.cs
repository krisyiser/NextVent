using CommunityToolkit.Mvvm.ComponentModel;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Navigation item representing an isolated operational axis section in SettingsView.
/// Eliminates Z-index leaks by providing strongly-typed ViewModel instances for TransitioningContentControl.
/// </summary>
public partial class SettingsSectionItem : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _subtitle = string.Empty;
    [ObservableProperty] private string _iconKind = "CogOutline";
    [ObservableProperty] private ObservableObject _viewModel;
    [ObservableProperty] private bool _isSelected;

    public SettingsSectionItem(string title, string subtitle, string iconKind, ObservableObject viewModel, bool isSelected = false)
    {
        _title = title;
        _subtitle = subtitle;
        _iconKind = iconKind;
        _viewModel = viewModel;
        _isSelected = isSelected;
    }
}
