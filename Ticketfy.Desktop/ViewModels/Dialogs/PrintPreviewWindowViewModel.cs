using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ticketfy.ViewModels.Dialogs;

public partial class PrintPreviewWindowViewModel : ObservableObject
{
    public event Action<bool>? RequestClose;

    [ObservableProperty] private string _documentName = "Documento";

    public PrintPreviewWindowViewModel(string documentName)
    {
        DocumentName = documentName;
    }

    [RelayCommand]
    private void Confirm()
    {
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
