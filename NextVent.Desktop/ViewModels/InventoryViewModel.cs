using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace NextVent.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly IProductService _productService;
    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<ProductDto> FilteredProducts { get; } = [];

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty] private bool _isFeedbackError = false;
    [ObservableProperty] private bool _showOnlyLowStock = false;
    [ObservableProperty] private bool _isIntelligencePanelVisible = false;

    public double TotalStockCost => FilteredProducts.Sum(p => p.Cost * p.Stock);
    public double TotalStockSale => FilteredProducts.Sum(p => p.Price * p.Stock);

    private readonly IPurchaseService? _purchaseService;
    private readonly IPredictiveIntelligenceService? _predictiveService;
    private readonly IExternalCatalogService _externalCatalogService;
    private CancellationTokenSource? _searchCts;
    
    public ObservableCollection<PredictiveAlertDto> UrgentRestockAlerts { get; } = [];

    public event Action? OpenAddProductRequested;
    public event Action<NextVent.ViewModels.Dialogs.ProductDialogParameters>? OpenProductDialogWithParamsRequested;
    public event Action<ProductDto>? OpenEditProductRequested;
    public event Action? OpenConfigureLowStockRequested;
    public event Action? OpenManageCategoriesRequested;

    private static readonly string SettingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".gemini", "antigravity-ide", "pos_settings.json"
    );

    public static double LoadDefaultMinStock()
    {
        try
        {
            if (System.IO.File.Exists(SettingsPath))
            {
                var text = System.IO.File.ReadAllText(SettingsPath);
                if (double.TryParse(text.Trim(), out double val))
                {
                    return val;
                }
            }
        }
        catch { }
        return 5.0;
    }

    public static void SaveDefaultMinStock(double val)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(SettingsPath);
            if (dir != null && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(SettingsPath, val.ToString());
        }
        catch { }
    }

    [ObservableProperty] private bool _isLoading = false;

    public InventoryViewModel(IProductService productService, IExternalCatalogService externalCatalogService, IPurchaseService? purchaseService = null, IPredictiveIntelligenceService? predictiveService = null)
    {
        _productService = productService;
        _externalCatalogService = externalCatalogService;
        _purchaseService = purchaseService;
        _predictiveService = predictiveService;
        _ = LoadProductsAsync();
    }

    public async Task LoadProductsAsync()
    {
        try
        {
            var items = await _productService.GetAllAsync();
            
            var alerts = _predictiveService != null ? await _predictiveService.GetUrgentRestockAlertsAsync() : new();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Products.Clear();
                foreach (var item in items) Products.Add(item);
                ApplyFilter();
                
                UrgentRestockAlerts.Clear();
                foreach (var alert in alerts.Take(3)) UrgentRestockAlerts.Add(alert);
                IsIntelligencePanelVisible = UrgentRestockAlerts.Count > 0;
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading inventory");
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        _ = ExecuteDebouncedSearchAsync(value, _searchCts.Token);
    }

    private async Task ExecuteDebouncedSearchAsync(string query, CancellationToken token)
    {
        try
        {
            await Task.Delay(300, token);
            if (!token.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() => ApplyFilter());
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in debounced search");
        }
    }

    partial void OnShowOnlyLowStockChanged(bool value) => ApplyFilter();

    [RelayCommand]
    private void EditProduct(ProductDto product)
    {
        if (product == null) return;
        OpenEditProductRequested?.Invoke(product);
    }

    [RelayCommand]
    private async Task DeleteProductAsync(ProductDto product)
    {
        if (product == null) return;
        try
        {
            await _productService.DeleteAsync(product.Id);
            await LoadProductsAsync();
            IsFeedbackError = false;
            FeedbackMessage = $"Producto '{product.Name}' eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting product");
            IsFeedbackError = true;
            FeedbackMessage = $"Error al eliminar producto: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleLowStockFilter()
    {
        if (ShowOnlyLowStock)
        {
            ShowOnlyLowStock = false;
        }
        else
        {
            OpenConfigureLowStockRequested?.Invoke();
        }
    }

    [RelayCommand]
    private void OpenManageCategoriesDialog() => OpenManageCategoriesRequested?.Invoke();

    private void ApplyFilter()
    {
        FilteredProducts.Clear();
        var q = SearchQuery.Trim().ToLower();

        var matches = Products.AsEnumerable();
        if (ShowOnlyLowStock)
        {
            double defaultMin = LoadDefaultMinStock();
            matches = matches.Where(p => p.Stock <= (p.MinStock > 0.0 ? p.MinStock : defaultMin)); // Stock Mínimo threshold
        }
        if (!string.IsNullOrEmpty(q))
        {
            matches = matches.Where(p => (p.Barcode != null && p.Barcode.ToLower().Contains(q)) || p.Name.ToLower().Contains(q) || p.Category.ToLower().Contains(q));
        }

        foreach (var m in matches) FilteredProducts.Add(m);
        
        OnPropertyChanged(nameof(TotalStockCost));
        OnPropertyChanged(nameof(TotalStockSale));
    }

    [RelayCommand]
    private void OpenAddProductDialog() => OpenAddProductRequested?.Invoke();

    [RelayCommand]
    private async Task ImportCsvCatalogAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider != null)
            {
                var options = new FilePickerOpenOptions
                {
                    Title = "Seleccionar CSV de Inventario",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Archivos CSV (*.csv)")
                        {
                            Patterns = new[] { "*.csv" }
                        }
                    }
                };

                try
                {
                    var files = await storageProvider.OpenFilePickerAsync(options);
                    if (files != null && files.Count > 0)
                    {
                        var filePath = files[0].Path.LocalPath;
                        if (System.IO.File.Exists(filePath))
                        {
                            var text = await System.IO.File.ReadAllTextAsync(filePath);
                            int count = await _productService.ImportFromCsvTextAsync(text);
                            await LoadProductsAsync();
                            IsFeedbackError = false;
                            FeedbackMessage = $"¡Se importaron e integraron {count} productos del archivo CSV exitosamente!";
                        }
                        else
                        {
                            IsFeedbackError = true;
                            FeedbackMessage = "El archivo seleccionado no existe.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error importing CSV catalog");
                    IsFeedbackError = true;
                    FeedbackMessage = $"Error al importar catálogo CSV: {ex.Message}";
                }
            }
        }
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync(string barcode)
    {
        IsLoading = true;

        var localProduct = await _productService.GetByBarcodeAsync(barcode);

        if (localProduct != null)
        {
            // El producto existe, procesar normalmente (abrir edición)
            OpenEditProductRequested?.Invoke(localProduct);
            IsLoading = false;
            return;
        }

        // El producto NO existe. Consultar la nube silenciosamente.
        var externalProduct = await _externalCatalogService.FetchProductByBarcodeAsync(barcode);
        IsLoading = false;

        if (externalProduct != null)
        {
            // Se encontró en internet. Pre-llenar el formulario.
            OpenProductDialogWithParamsRequested?.Invoke(new NextVent.ViewModels.Dialogs.ProductDialogParameters 
            { 
                IsEditMode = false,
                PreFilledData = externalProduct,
                ShowAutoFillBanner = true
            });
        }
        else
        {
            // No hay internet o no existe en la API. Abrir vacío tradicional.
            OpenProductDialogWithParamsRequested?.Invoke(new NextVent.ViewModels.Dialogs.ProductDialogParameters 
            { 
                IsEditMode = false,
                PreFilledBarcode = barcode
            });
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
                IsFeedbackError = false;
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
                IsFeedbackError = false;
                FeedbackMessage = $"¡Orden de Reabastecimiento '{order.InvoiceNumber}' generada exitosamente en Compras para {lowStockItems.Count} productos!";
            }
            else
            {
                IsFeedbackError = false;
                FeedbackMessage = $"Se identificaron {lowStockItems.Count} productos en bajo stock sugeridos para compra.";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating auto purchase orders");
            IsFeedbackError = true;
            FeedbackMessage = "Error al generar orden de compra automática";
        }
    }

    [RelayCommand]
    private async Task CreateSnapshotAsync()
    {
        try
        {
            var svc = new NextVent.Services.Implementations.InventorySnapshotService();
            var snap = await svc.CreateSnapshotAsync($"Punto de Guardado Manual - {DateTime.Now:dd/MM/yyyy hh:mm tt}");
            IsFeedbackError = false;
            FeedbackMessage = $"¡Guardado exitoso!";
        }
        catch (Exception ex)
        {
            IsFeedbackError = true;
            FeedbackMessage = "Error al crear el punto de guardado.";
            Log.Error(ex, "Failed to create snapshot from UI.");
        }
    }

    [RelayCommand]
    private void ViewSnapshots()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var win = new NextVent.Views.InventorySnapshotsWindow
            {
                DataContext = new NextVent.ViewModels.InventorySnapshotsViewModel()
            };
            win.ShowDialog(desktop.MainWindow);
        }
    }

    [RelayCommand]
    private async Task PrintChecklistAsync()
    {
        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        if (desktop?.MainWindow == null) return;

        var vm = new NextVent.ViewModels.Dialogs.PrintPreviewWindowViewModel("Checklist de Inventario Físico");
        var win = new NextVent.Views.Dialogs.PrintPreviewWindow { DataContext = vm };
        
        var confirmed = await win.ShowDialog<bool>(desktop.MainWindow);
        if (!confirmed) return;

        try
        {
            var printerSvc = new NextVent.Services.Implementations.EscPosPrinterService();
            bool result = await printerSvc.PrintInventoryChecklistAsync(Products.ToList());
            
            if (result)
            {
                IsFeedbackError = false;
                FeedbackMessage = "Imprimiendo checklist de inventario físico...";
            }
            else
            {
                IsFeedbackError = true;
                FeedbackMessage = "Error de comunicación con la impresora térmica.";
            }
        }
        catch (Exception ex)
        {
            IsFeedbackError = true;
            FeedbackMessage = "Ocurrió un error al intentar imprimir el checklist.";
            Log.Error(ex, "Failed to print inventory checklist.");
        }
    }

    [RelayCommand]
    private void ClearFeedback()
    {
        FeedbackMessage = string.Empty;
    }
}
