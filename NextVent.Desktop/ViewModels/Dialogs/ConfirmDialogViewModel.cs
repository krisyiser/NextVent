using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NextVent.ViewModels.Dialogs;

public partial class ConfirmDialogViewModel : ObservableObject
{
    private readonly Action<bool> _callback;

    [ObservableProperty] private string _title = "Confirmación";
    [ObservableProperty] private string _message = "¿Está seguro de realizar esta acción?";

    public ConfirmDialogViewModel(string title, string message, Action<bool> callback)
    {
        Title = title;
        Message = message;
        _callback = callback;
    }

    [RelayCommand]
    private void Confirm()
    {
        _callback?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        _callback?.Invoke(false);
    }
}
