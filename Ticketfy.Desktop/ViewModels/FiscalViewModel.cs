using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using System;

namespace Ticketfy.ViewModels;

public partial class FiscalViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IFacturamaService _facturamaService;

    public ObservableCollection<SaleDto> Invoices { get; } = new();

    [ObservableProperty] private string _searchFolio = string.Empty;
    [ObservableProperty] private SaleDto? _selectedSale;

    [ObservableProperty] private string _customerRfc = string.Empty;
    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _customerZipCode = string.Empty;

    public ObservableCollection<string> Regimenes { get; } = new() {
        "601 - General de Ley Personas Morales",
        "603 - Personas Morales con Fines no Lucrativos",
        "605 - Sueldos y Salarios e Ingresos Asimilados a Salarios",
        "606 - Arrendamiento",
        "608 - Demás ingresos",
        "612 - Personas Físicas con Actividades Empresariales y Profesionales",
        "616 - Sin obligaciones fiscales",
        "621 - Incorporación Fiscal",
        "626 - Régimen Simplificado de Confianza"
    };
    [ObservableProperty] private string _selectedRegimen = "616 - Sin obligaciones fiscales";

    public ObservableCollection<string> UsosCfdi { get; } = new() {
        "G01 - Adquisición de mercancias",
        "G03 - Gastos en general",
        "I01 - Construcciones",
        "S01 - Sin efectos fiscales",
        "CP01 - Pagos"
    };
    [ObservableProperty] private string _selectedUsoCfdi = "G03 - Gastos en general";

    public ObservableCollection<string> FormasPago { get; } = new() {
        "01 - Efectivo",
        "02 - Cheque nominativo",
        "03 - Transferencia electrónica de fondos",
        "04 - Tarjeta de crédito",
        "28 - Tarjeta de débito",
        "99 - Por definir"
    };
    [ObservableProperty] private string _selectedFormaPago = "01 - Efectivo";

    public ObservableCollection<string> MetodosPago { get; } = new() {
        "PUE - Pago en una sola exhibición",
        "PPD - Pago en parcialidades o diferido"
    };
    [ObservableProperty] private string _selectedMetodoPago = "PUE - Pago en una sola exhibición";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    public FiscalViewModel(ISaleService saleService, IFacturamaService facturamaService)
    {
        _saleService = saleService;
        _facturamaService = facturamaService;
        _ = LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task SearchSaleAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchFolio)) return;
        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        try
        {
            SelectedSale = await _saleService.GetByIdAsync(SearchFolio.Trim());
            if (SelectedSale == null)
            {
                ErrorMessage = "Venta no encontrada.";
            }
            else
            {
                if (!string.IsNullOrEmpty(SelectedSale.InvoiceId))
                {
                    ErrorMessage = "Esta venta ya ha sido facturada (UUID: " + SelectedSale.InvoiceId + ").";
                    SelectedSale = null;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error buscando venta");
            ErrorMessage = "Error al buscar la venta.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EmitInvoiceAsync()
    {
        if (SelectedSale == null) return;
        if (string.IsNullOrWhiteSpace(CustomerRfc) || string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(CustomerZipCode))
        {
            ErrorMessage = "RFC, Nombre y Código Postal son requeridos.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            var request = new Ticketfy.Core.Models.FacturamaCfdiRequest
            {
                Receiver = new Ticketfy.Core.Models.CfdiReceiver
                {
                    Rfc = CustomerRfc.Trim().ToUpper(),
                    Name = CustomerName.Trim(),
                    TaxZipCode = CustomerZipCode.Trim(),
                    CfdiUse = SelectedUsoCfdi.Substring(0, 3),
                    FiscalRegime = SelectedRegimen.Substring(0, 3)
                },
                PaymentForm = SelectedFormaPago.Substring(0, 2),
                PaymentMethod = SelectedMetodoPago.Substring(0, 3),
                Currency = "MXN",
                CfdiType = "I",
                ExpeditionPlace = "00000" // To be dynamically loaded from settings normally
            };

            foreach (var item in SelectedSale.Items)
            {
                request.Items.Add(new Ticketfy.Core.Models.CfdiItem
                {
                    ProductCode = "01010101", // Default SAT code
                    IdentificationNumber = item.ProductId,
                    Description = item.Name,
                    Unit = "Pieza",
                    UnitCode = "H87", // Default SAT unit
                    UnitPrice = (decimal)item.UnitPrice,
                    Quantity = (decimal)item.Quantity,
                    Subtotal = (decimal)item.TotalPrice,
                    Taxes = new System.Collections.Generic.List<Ticketfy.Core.Models.CfdiTax>
                    {
                        new Ticketfy.Core.Models.CfdiTax { Name = "IVA", Rate = 0.16m, IsRetention = false, Base = (decimal)item.TotalPrice, Total = (decimal)item.TotalPrice * 0.16m }
                    }
                });
            }

            // FacturamaService dinámicamente obtendrá las credenciales desde los ajustes de la empresa.
            var response = await _facturamaService.CreateInvoiceAsync(request);

            if (response != null && !string.IsNullOrEmpty(response.Id))
            {
                SuccessMessage = "Factura emitida exitosamente (UUID: " + response.Id + ")";
                await _saleService.UpdateFiscalStatusAsync(SelectedSale.Id, "TIMBRADO", response.Id, response.Id);
                SelectedSale = null;
                SearchFolio = string.Empty;
                await LoadInvoicesAsync();
            }
            else
            {
                ErrorMessage = "Error al emitir factura, revisa el log para más detalles.";
                await _saleService.UpdateFiscalStatusAsync(SelectedSale.Id, "ERROR AL TIMBRAR", null, null);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error emitiendo factura");
            ErrorMessage = "Error al emitir factura: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
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
                foreach (var sale in sales.Where(s => !string.IsNullOrEmpty(s.InvoiceId) || s.EstadoFiscal == "PENDIENTE" || s.EstadoFiscal == "ERROR AL TIMBRAR" || s.EstadoFiscal == "TIMBRADO"))
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
