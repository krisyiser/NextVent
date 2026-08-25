using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Inventory;

/// <summary>
/// Handles inventory CSV imports, snapshots, and checklist printing.
/// Extracted from InventoryViewModel.
/// </summary>
public partial class InventoryActionsViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IExternalCatalogService _externalCatalogService;

    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty] private bool _isFeedbackError = false;

    public event Action? ProductsUpdated;

    public InventoryActionsViewModel(IProductService productService, IExternalCatalogService externalCatalogService)
    {
        _productService = productService;
        _externalCatalogService = externalCatalogService;
    }

    [RelayCommand]
    private async Task ImportProductsFromCsvAsync()
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var storageProvider = desktop.MainWindow.StorageProvider;
                var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Seleccionar Archivo CSV de Inventario",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Archivos CSV (*.csv)") { Patterns = new[] { "*.csv" } }
                    }
                });

                if (files.Count > 0)
                {
                    var file = files[0];
                    using var stream = await file.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    string content = await reader.ReadToEndAsync();

                    int imported = await _productService.ImportFromCsvTextAsync(content);
                    IsFeedbackError = false;
                    FeedbackMessage = $"¡Se importaron correctamente {imported} productos!";
                    ProductsUpdated?.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "InventoryActionsViewModel: error importing CSV");
            IsFeedbackError = true;
            FeedbackMessage = "Error al importar el archivo CSV.";
        }
    }

    public Task ImportCsvCatalogAsync() => ImportProductsFromCsvAsync();

    public async Task PrintChecklistAsync()
    {
        try
        {
            var products = await _productService.GetAllAsync();
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var storageProvider = desktop.MainWindow.StorageProvider;
                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Guardar Checklist de Inventario Físico",
                    DefaultExtension = "csv",
                    SuggestedFileName = $"Checklist_Inventario_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                });

                if (file != null)
                {
                    using var stream = await file.OpenWriteAsync();
                    using var writer = new StreamWriter(stream);
                    await writer.WriteLineAsync("Codigo_SKU,Nombre_Producto,Categoria,Stock_Teorico,Conteo_Fisico,Diferencia,Notas");
                    foreach (var p in products)
                    {
                        await writer.WriteLineAsync($"\"{p.Barcode}\",\"{p.Name}\",\"{p.Category}\",{p.Stock},,,");
                    }
                    IsFeedbackError = false;
                    FeedbackMessage = $"¡Checklist de conteo físico guardado con {products.Count} productos!";
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error printing checklist");
            IsFeedbackError = true;
            FeedbackMessage = "Error al generar el checklist de inventario.";
        }
    }

    public async Task CreateSnapshotAsync()
    {
        try
        {
            var products = await _productService.GetAllAsync();
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var storageProvider = desktop.MainWindow.StorageProvider;
                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Guardar Respaldo / Copia de Seguridad de Inventario",
                    DefaultExtension = "csv",
                    SuggestedFileName = $"Respaldo_Inventario_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                });

                if (file != null)
                {
                    using var stream = await file.OpenWriteAsync();
                    using var writer = new StreamWriter(stream);
                    await writer.WriteLineAsync("Id,Codigo_SKU,Nombre,Costo,Precio,Stock,MinStock,Categoria,Puntos");
                    foreach (var p in products)
                    {
                        await writer.WriteLineAsync($"\"{p.Id}\",\"{p.Barcode}\",\"{p.Name}\",{p.Cost},{p.Price},{p.Stock},{p.MinStock},\"{p.Category}\",{p.PointsRewarded}");
                    }
                    IsFeedbackError = false;
                    FeedbackMessage = $"¡Copia de seguridad del inventario exportada con éxito ({products.Count} productos)!";
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating inventory snapshot");
            IsFeedbackError = true;
            FeedbackMessage = "Error al crear la copia de seguridad del inventario.";
        }
    }

    public async Task ViewSnapshotsAsync()
    {
        try
        {
            var products = await _productService.GetAllAsync();
            double totalCostVal = products.Sum(p => p.Cost * p.Stock);
            double totalSaleVal = products.Sum(p => p.Price * p.Stock);
            int lowStockCount = products.Count(p => p.Stock <= p.MinStock);

            IsFeedbackError = false;
            FeedbackMessage = $"[AUDITORÍA E HISTORIAL] Total Productos: {products.Count} | Valuación Costo: ${totalCostVal:N2} | Valuación Venta: ${totalSaleVal:N2} | Alertas Stock Mínimo: {lowStockCount}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error viewing snapshots");
            IsFeedbackError = true;
            FeedbackMessage = "Error al consultar el historial de auditoría de inventario.";
        }
    }

    [RelayCommand] public void ClearFeedback() => FeedbackMessage = string.Empty;
}
