using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Suppliers;

/// <summary>
/// Manages supplier directory CRUD operations.
/// Extracted from SuppliersViewModel.
/// </summary>
public partial class SupplierDirectoryViewModel : ObservableObject
{
    private readonly ISupplierService _supplierService;

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];

    [ObservableProperty] private string _supplierName = string.Empty;
    [ObservableProperty] private string _supplierRfc = string.Empty;
    [ObservableProperty] private string _supplierPhone = string.Empty;
    [ObservableProperty] private string _supplierEmail = string.Empty;
    [ObservableProperty] private string _supplierAddress = string.Empty;
    [ObservableProperty] private string _supplierContact = string.Empty;
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public SupplierDirectoryViewModel(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public async Task LoadSuppliersAsync()
    {
        try
        {
            var list = await _supplierService.GetAllAsync();
            Suppliers.Clear();
            foreach (var s in list.OrderBy(x => x.Name)) Suppliers.Add(s);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SupplierDirectoryViewModel: error loading suppliers");
        }
    }

    [RelayCommand]
    private async Task SaveSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(SupplierName))
        {
            FeedbackMessage = "El nombre del proveedor es obligatorio";
            return;
        }

        try
        {
            var newSupplier = new SupplierDto(
                Guid.NewGuid().ToString(),
                SupplierName,
                SupplierRfc,
                SupplierPhone,
                SupplierEmail,
                SupplierAddress,
                SupplierContact,
                true
            );

            var created = await _supplierService.CreateAsync(newSupplier);
            await LoadSuppliersAsync();

            SupplierName = string.Empty;
            SupplierRfc = string.Empty;
            SupplierPhone = string.Empty;
            SupplierEmail = string.Empty;
            SupplierAddress = string.Empty;
            SupplierContact = string.Empty;

            FeedbackMessage = "Proveedor guardado con éxito";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SupplierDirectoryViewModel: error creating supplier");
            FeedbackMessage = "Error al guardar el proveedor";
        }
    }
}
