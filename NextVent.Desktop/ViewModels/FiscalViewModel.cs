using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using System;

namespace NextVent.ViewModels;

public partial class FiscalViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IFacturamaService _facturamaService;

    public ObservableCollection<SaleDto> Invoices { get; } = new();

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    public FiscalViewModel(ISaleService saleService, IFacturamaService facturamaService)
    {
        _saleService = saleService;
        _facturamaService = facturamaService;
        _ = LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var sales = await _saleService.GetHistoryAsync(100);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Invoices.Clear();
                foreach (var sale in sales.Where(s => !string.IsNullOrEmpty(s.InvoiceId) || s.EstadoFiscal == "PENDIENTE" || s.EstadoFiscal == "ERROR AL TIMBRAR"))
                {
                    Invoices.Add(sale);
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading invoices in FiscalViewModel");
            ErrorMessage = "No se pudieron cargar los registros fiscales.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
