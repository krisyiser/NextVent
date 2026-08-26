using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using System;

namespace Ticketfy.ViewModels.Dialogs;

public partial class BulkWeightDialogViewModel : ObservableObject
{
    private readonly ProductDto _product;
    private bool _isUpdatingInternally = false;

    public ProductDto Product => _product;
    public string ProductName => _product.Name;
    public double UnitPrice => _product.Price;
    public string UnitLabel => string.IsNullOrWhiteSpace(_product.Unit) ? "Kg" : _product.Unit;

    [ObservableProperty]
    private double _quantityInGrams = 1000.0;

    [ObservableProperty]
    private double _quantityInKilos = 1.0;

    [ObservableProperty]
    private double _moneyAmount;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public double ResultQuantity => QuantityInKilos;
    public bool WasConfirmed { get; private set; } = false;

    public event Action<bool, double>? RequestCloseWithResult;

    public BulkWeightDialogViewModel(ProductDto product)
    {
        _product = product;
        _moneyAmount = product.Price;
        _quantityInKilos = 1.0;
        _quantityInGrams = 1000.0;
    }

    partial void OnQuantityInGramsChanged(double value)
    {
        if (_isUpdatingInternally) return;
        _isUpdatingInternally = true;
        try
        {
            QuantityInKilos = Math.Round(value / 1000.0, 4);
            MoneyAmount = Math.Round(QuantityInKilos * UnitPrice, 2);
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    partial void OnQuantityInKilosChanged(double value)
    {
        if (_isUpdatingInternally) return;
        _isUpdatingInternally = true;
        try
        {
            QuantityInGrams = Math.Round(value * 1000.0, 1);
            MoneyAmount = Math.Round(value * UnitPrice, 2);
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    partial void OnMoneyAmountChanged(double value)
    {
        if (_isUpdatingInternally || UnitPrice <= 0) return;
        _isUpdatingInternally = true;
        try
        {
            double kilos = Math.Round(value / UnitPrice, 4);
            QuantityInKilos = kilos;
            QuantityInGrams = Math.Round(kilos * 1000.0, 1);
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    [RelayCommand]
    private void SetPresetGrams(object? param)
    {
        if (param == null) return;
        if (param is double d)
        {
            QuantityInGrams = d;
        }
        else if (double.TryParse(param.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
        {
            QuantityInGrams = parsed;
        }
    }

    [RelayCommand]
    private void SetPresetMoney(object? param)
    {
        if (param == null) return;
        if (param is double d)
        {
            MoneyAmount = d;
        }
        else if (double.TryParse(param.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
        {
            MoneyAmount = parsed;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (QuantityInKilos <= 0)
        {
            ErrorMessage = "Ingrese una cantidad o peso mayor a cero";
            return;
        }

        WasConfirmed = true;
        RequestCloseWithResult?.Invoke(true, QuantityInKilos);
    }

    [RelayCommand]
    private void Cancel()
    {
        WasConfirmed = false;
        RequestCloseWithResult?.Invoke(false, 0.0);
    }
}
