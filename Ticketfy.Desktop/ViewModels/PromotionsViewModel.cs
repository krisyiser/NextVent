using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels;

public partial class PromotionsViewModel : ObservableObject
{
    private readonly IPromotionService _promotionService;
    public ObservableCollection<PromotionDto> Promotions { get; } = [];

    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public event Action? OpenAddPromotionRequested;
    public event Action? OpenCreateItemKitRequested;

    public PromotionsViewModel(IPromotionService promotionService)
    {
        _promotionService = promotionService;
        _ = LoadPromotionsAsync();
    }

    public async Task LoadPromotionsAsync()
    {
        try
        {
            var items = await _promotionService.GetAllAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Promotions.Clear();
                foreach (var item in items) Promotions.Add(item);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading promotions");
        }
    }

    [RelayCommand]
    private void OpenAddPromotionDialog() => OpenAddPromotionRequested?.Invoke();

    [RelayCommand]
    private void OpenCreateItemKitDialog() => OpenCreateItemKitRequested?.Invoke();

    [RelayCommand]
    private async Task ToggleStatusAsync(PromotionDto promo)
    {
        if (promo == null) return;
        try
        {
            var updated = promo with { IsActive = !promo.IsActive };
            await _promotionService.SaveAsync(updated);
            FeedbackMessage = promo.IsActive ? "Promoción desactivada" : "Promoción activada";
            await LoadPromotionsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error toggling promotion status");
        }
    }

    [RelayCommand]
    private async Task DeletePromotionAsync(PromotionDto promo)
    {
        if (promo == null) return;
        try
        {
            await _promotionService.DeleteAsync(promo.Id);
            Promotions.Remove(promo);
            FeedbackMessage = "Promoción eliminada con éxito";
            await LoadPromotionsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting promotion");
        }
    }
}
