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
}
