using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NextVent.ViewModels.Dialogs;

public partial class PromotionDialogViewModel : ObservableObject
{
    private readonly IPromotionService _promotionService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private double _discountValue;

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
                ErrorMessage = "El nombre de la promoción es obligatorio.";
                return;
            }

            var dto = new PromotionDto(Guid.NewGuid().ToString(), Name, DiscountValue, IsActive);
            await _promotionService.SaveAsync(dto);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving promotion");
            ErrorMessage = ex.Message;
        }
    }
}
