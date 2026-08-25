using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Dialogs;

public partial class PromotionDialogViewModel : ObservableObject
{
    private readonly IPromotionService _promotionService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _discountValueText = string.Empty;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event Action? RequestClose;

    public PromotionDialogViewModel(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "El nombre de la promoción u oferta es obligatorio.";
                return;
            }

            if (string.IsNullOrWhiteSpace(DiscountValueText) || !double.TryParse(DiscountValueText, out double parsedVal) || parsedVal <= 0)
            {
                ErrorMessage = "El valor del descuento es obligatorio y debe ser un número válido mayor a 0.";
                return;
            }

            ErrorMessage = string.Empty;
            var dto = new PromotionDto(Guid.NewGuid().ToString(), Name, parsedVal, IsActive);
            await _promotionService.SaveAsync(dto);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving promotion");
            ErrorMessage = "Error interno al guardar la promoción. Verifique los datos ingresados.";
        }
    }
}
