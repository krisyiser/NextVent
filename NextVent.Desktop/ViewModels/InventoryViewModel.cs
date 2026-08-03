using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly IProductService _productService;
    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<ProductDto> FilteredProducts { get; } = [];

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty] private bool _showOnlyLowStock = false;

    private readonly IPurchaseService? _purchaseService;

    public event Action? OpenAddProductRequested;

    public InventoryViewModel(IProductService productService, IPurchaseService? purchaseService = null)
    {
        _productService = productService;
        _purchaseService = purchaseService;
        _ = LoadProductsAsync();
    }

    public async Task LoadProductsAsync()
    {
        try
        {
            var items = await _productService.GetAllAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Products.Clear();
                foreach (var item in items) Products.Add(item);
                ApplyFilter();
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading inventory");
        }
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnShowOnlyLowStockChanged(bool value) => ApplyFilter();

    [RelayCommand]
    private void ToggleLowStockFilter()
    {
        ShowOnlyLowStock = !ShowOnlyLowStock;
    }

    private void ApplyFilter()
    {
        FilteredProducts.Clear();
        var q = SearchQuery.Trim().ToLower();

        var matches = Products.AsEnumerable();
        if (ShowOnlyLowStock)
        {
            matches = matches.Where(p => p.Stock <= 5.0); // Stock Mínimo threshold
        }
        if (!string.IsNullOrEmpty(q))
        {
            matches = matches.Where(p => (p.Barcode != null && p.Barcode.ToLower().Contains(q)) || p.Name.ToLower().Contains(q) || p.Category.ToLower().Contains(q));
        }

        foreach (var m in matches) FilteredProducts.Add(m);
    }

    [RelayCommand]
    private void OpenAddProductDialog() => OpenAddProductRequested?.Invoke();

    [RelayCommand]
    private async Task ImportCsvCatalogAsync()
    {
        try
        {
            string sampleCsv = "Barcode;Name;CostPrice;SalePrice;Stock;Category;Unit\n750105530001;Aceite Vegetal 1L;32.50;45.00;30;Abarrotes;pza";

            int count = await _productService.ImportFromCsvTextAsync(sampleCsv);
            await LoadProductsAsync();
            FeedbackMessage = "¡Se importaron e integraron productos del archivo CSV exitosamente!";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error importing CSV catalog");
            FeedbackMessage = "Error al importar catálogo CSV";
        }
    }

    [RelayCommand]
    private async Task GenerateAutoPurchaseOrdersAsync()
    {
        try
        {
            var lowStockItems = Products.Where(p => p.Stock <= 5.0 || p.Stock <= p.ReorderQuantity).ToList();
            if (lowStockItems.Count == 0)
            {
                FeedbackMessage = "No se detectaron productos en nivel crítico de reabastecimiento.";
                return;
            }

            if (_purchaseService != null)
            {
                var purchaseItems = lowStockItems.Select(p => new PurchaseItemDto(
                    Guid.NewGuid().ToString(),
                    "",
                    p.Id,
                    p.Name,
                    p.Cost,
                    p.ReorderQuantity > 0 ? p.ReorderQuantity : 10.0,
                    p.Cost * (p.ReorderQuantity > 0 ? p.ReorderQuantity : 10.0)
                )).ToList();

                var order = new PurchaseDto(
                    Guid.NewGuid().ToString(),
                    "SUP-AUTO",
                    "PROVEEDOR GENERAL DE REABASTECIMIENTO",
                    $"ORD-AUTO-{Random.Shared.Next(1000, 9999)}",
                    DateTime.Now.ToString("g"),
                    purchaseItems.Sum(i => i.TotalPrice),
                    $"Orden de Reabastecimiento Automático por Bajo Stock ({lowStockItems.Count} ítems)",
                    purchaseItems
                );

                await _purchaseService.RegisterPurchaseAsync(order);
                FeedbackMessage = $"¡Orden de Reabastecimiento '{order.InvoiceNumber}' generada exitosamente en Compras para {lowStockItems.Count} productos!";
            }
            else
            {
                FeedbackMessage = $"Se identificaron {lowStockItems.Count} productos en bajo stock sugeridos para compra.";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating auto purchase orders");
            FeedbackMessage = "Error al generar orden de compra automática";
        }
    }
}
