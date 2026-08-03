using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels.Dialogs;

public partial class ReturnDialogViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly SaleDto _sale;

    [ObservableProperty] private string _saleId = string.Empty;
    [ObservableProperty] private string _saleDate = string.Empty;
    [ObservableProperty] private string _returnReason = "Producto defectuoso";
    [ObservableProperty] private string _refundMethod = "Efectivo";
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public ObservableCollection<SaleItemSnapshotDto> Items { get; } = [];
    public ObservableCollection<string> Reasons { get; } = [
        "Producto defectuoso", "Caducidad cercana", "Cambio de opinión del cliente", "Error en cobro de caja"
    ];
    public ObservableCollection<string> RefundMethods { get; } = ["Efectivo", "Monedero Electrónico", "Tarjeta"];

    [ObservableProperty] private SaleItemSnapshotDto? _selectedItem;
    [ObservableProperty] private double _returnQuantity = 1.0;

    public event Action? RequestClose;

    public ReturnDialogViewModel(ISaleService saleService, SaleDto sale)
    {
        _saleService = saleService;
        _sale = sale;
        SaleId = sale.Id;
        SaleDate = sale.Date;

        if (sale.Items != null)
        {
            foreach (var item in sale.Items) Items.Add(item);
            SelectedItem = Items.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task ProcessReturnAsync()
    {
        if (SelectedItem == null || ReturnQuantity <= 0)
        {
            FeedbackMessage = "Seleccione un producto y cantidad a devolver";
            return;
        }

        if (ReturnQuantity > SelectedItem.Quantity)
        {
            FeedbackMessage = $"La cantidad máxima a devolver es {SelectedItem.Quantity}";
            return;
        }

        try
        {
            var success = await _saleService.ProcessPartialReturnAsync(
                _sale.Id,
                SelectedItem.ProductId,
                ReturnQuantity,
                ReturnReason,
                RefundMethod
            );

            if (success)
            {
                RequestClose?.Invoke();
            }
            else
            {
                FeedbackMessage = "Error al procesar la devolución parcial";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing partial return");
            FeedbackMessage = "Error al devolver producto";
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
