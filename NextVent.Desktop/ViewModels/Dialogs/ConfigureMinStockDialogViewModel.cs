using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NextVent.ViewModels.Dialogs;

public partial class ConfigureMinStockDialogViewModel : ObservableObject
{
    [ObservableProperty] private double _minStockValue;

    public event Action<double>? Saved;
    public event Action? RequestClose;

    public ConfigureMinStockDialogViewModel(double currentVal)
    {
        MinStockValue = currentVal;
    }

    [RelayCommand]
    private void Save()
    {
        Saved?.Invoke(MinStockValue);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
