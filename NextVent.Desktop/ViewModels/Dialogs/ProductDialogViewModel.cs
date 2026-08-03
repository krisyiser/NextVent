using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
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

    public event Action? RequestClose;

    public ProductDialogViewModel(IProductService productService)
    {
        _productService = productService;
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
                Guid.NewGuid().ToString(), Barcode, Name, CostPrice, SalePrice,
                Stock: Stock, Category: Category,
                PointsRewarded: PointsRewarded,
                ReorderQuantity: ReorderQuantity,
                LocationRack: LocationRack,
                ClaveSat: ClaveSat,
                UnidadSat: UnidadSat
            );
            await _productService.AddAsync(dto);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving product");
            ErrorMessage = ex.Message;
        }
    }
}
