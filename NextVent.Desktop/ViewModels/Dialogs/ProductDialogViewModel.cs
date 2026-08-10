using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using NextVent.Core.Services;
using NextVent.Data.Entities;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NextVent.ViewModels.Dialogs;

public partial class ProductDialogViewModel : ObservableObject
{
    private readonly IProductService _productService;

    [ObservableProperty]
    private string _barcode = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private double _costPrice;

    [ObservableProperty]
    private double _salePrice;

    [ObservableProperty]
    private int _stock;

    [ObservableProperty]
    private string _category = "General";

    // Sprint K: Attributes & Serial / IMEI
    [ObservableProperty]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    private string _attributesText = string.Empty;

    [ObservableProperty]
    private double _pointsRewarded = 1.0;

    [ObservableProperty]
    private double _reorderQuantity = 10.0;

    [ObservableProperty]
    private string _locationRack = "Pasillo 1 - Anaquel A";

    [ObservableProperty]
    private string _claveSat = "50202306";

    [ObservableProperty]
    private string _unidadSat = "H87";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _title = "Nuevo Producto";

    private string? _editingProductId = null;
    private double _originalStockSnapshot = 0.0;
    private double _originalMinStockSnapshot = 5.0;

    private readonly ISessionManager? _sessionManager;
    private readonly IAuditService? _auditService;

    public event Action? RequestClose;

    public ProductDialogViewModel(IProductService productService, ISessionManager? sessionManager = null, IAuditService? auditService = null)
    {
        _productService = productService;
        _sessionManager = sessionManager;
        _auditService = auditService;
    }

    public void LoadProductForEdit(ProductDto product)
    {
        _editingProductId = product.Id;
        Title = "Editar Producto";
        Barcode = product.Barcode ?? string.Empty;
        Name = product.Name;
        CostPrice = product.Cost;
        SalePrice = product.Price;
        Stock = (int)product.Stock;
        Category = product.Category;
        SerialNumber = product.LocationRack; // Wait, let's keep attributes / serial number empty or set them if present.
        PointsRewarded = product.PointsRewarded;
        ReorderQuantity = product.ReorderQuantity;
        LocationRack = product.LocationRack;
        ClaveSat = product.ClaveSat;
        UnidadSat = product.UnidadSat;

        _originalStockSnapshot = product.Stock;
        _originalMinStockSnapshot = product.MinStock;
    }

    [RelayCommand]
    private void GenerateBarcode()
    {
        Barcode = new Random().Next(10000000, 99999999).ToString();
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
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Barcode))
            {
                ErrorMessage = "Código y nombre son obligatorios.";
                return;
            }

            var dto = new ProductDto(
                _editingProductId ?? Guid.NewGuid().ToString(), Barcode, Name, CostPrice, SalePrice,
                Stock: Stock, Category: Category,
                PointsRewarded: PointsRewarded,
                ReorderQuantity: ReorderQuantity,
                LocationRack: LocationRack,
                ClaveSat: ClaveSat,
                UnidadSat: UnidadSat,
                MinStock: _editingProductId != null ? _originalMinStockSnapshot : 5.0
            );

            if (_editingProductId != null)
            {
                await _productService.UpdateAsync(dto);

                // INJECT SILENT AUDIT FOR MANUAL ADJUSTMENTS
                if (Math.Abs(_originalStockSnapshot - Stock) > 0.0001)
                {
                    double difference = Stock - _originalStockSnapshot;
                    string verb = difference > 0 ? "Incremento" : "Disminución";
                    var currentUserId = _sessionManager?.CurrentCashier?.Id.ToString() ?? "cajero_matriz";

                    if (_auditService != null)
                    {
                        var auditEntry = new AuditLogEntity
                        {
                            UserId = currentUserId,
                            ActionType = NextVent.Core.Enums.AuditActionType.InventoryStockAdjustment,
                            RiskLevel = Math.Abs(difference) > 10.0 ? NextVent.Core.Enums.RiskLevel.HighRisk : NextVent.Core.Enums.RiskLevel.Warning,
                            EntityName = "ProductEntity",
                            EntityId = dto.Id,
                            OldValue = $"Stock: {_originalStockSnapshot:N2}",
                            NewValue = $"Stock: {Stock:N2}",
                            FinancialImpact = Math.Abs(difference) * CostPrice,
                            Reason = $"Ajuste Manual: {Name} | {verb} de {Math.Abs(difference):N2} unidades. (Anterior: {_originalStockSnapshot:N2}, Nuevo: {Stock:N2})"
                        };
                        await _auditService.LogAsync(auditEntry);
                    }
                }
            }
            else
            {
                await _productService.AddAsync(dto);
            }

            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving product");
            ErrorMessage = ex.Message;
        }
    }
}
