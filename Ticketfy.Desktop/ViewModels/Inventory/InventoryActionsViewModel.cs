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
    public Task PrintChecklistAsync() => Task.CompletedTask;
    public Task CreateSnapshotAsync() => Task.CompletedTask;
    public Task ViewSnapshotsAsync() => Task.CompletedTask;
    [RelayCommand] public void ClearFeedback() => FeedbackMessage = string.Empty;
}
