using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Services.Interfaces;
using NextVent.ViewModels.Base;
using System;
using System.Threading.Tasks;

namespace NextVent.ViewModels.Dialogs;

/// <summary>
/// Dialog ViewModel forcing cashier to physically count the drawer and enter initial float.
/// </summary>
public partial class OpenShiftDialogViewModel : ValidatableViewModelBase
{
    private readonly IShiftService _shiftService;

    [ObservableProperty]
    private double _initialFloatAmount;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public OpenShiftDialogViewModel(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        if (InitialFloatAmount < 0)
        {
            ErrorMessage = "El monto inicial no puede ser negativo.";
            return;
        }

        try
        {
            IsProcessing = true;
            ErrorMessage = string.Empty;

            var result = await _shiftService.OpenAsync(InitialFloatAmount);
            if (result != null)
            {
                RequestClose?.Invoke(true);
            }
            else
            {
                ErrorMessage = "Error al abrir el turno.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
