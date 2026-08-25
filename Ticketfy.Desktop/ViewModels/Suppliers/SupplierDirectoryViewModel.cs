using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Suppliers;

/// <summary>
/// Manages supplier directory CRUD operations with instant search, edit, and delete capabilities.
/// </summary>
public partial class SupplierDirectoryViewModel : ObservableObject
{
    private readonly ISupplierService _supplierService;
    private List<SupplierDto> _allSuppliers = [];

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];

    [ObservableProperty] private string _searchQuery = string.Empty;

    [ObservableProperty] private string _supplierName = string.Empty;
    [ObservableProperty] private string _supplierRfc = string.Empty;
    [ObservableProperty] private string _supplierPhone = string.Empty;
    [ObservableProperty] private string _supplierEmail = string.Empty;
    [ObservableProperty] private string _supplierAddress = string.Empty;
    [ObservableProperty] private string _supplierContact = string.Empty;

    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty] private bool _isEditing = false;
    [ObservableProperty] private string _formTitle = "Registrar Nuevo Proveedor";
    [ObservableProperty] private string _saveButtonText = "GUARDAR PROVEEDOR";

    private SupplierDto? _editingSupplier = null;

    public event Action? SuppliersUpdated;

    public SupplierDirectoryViewModel(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public async Task LoadSuppliersAsync()
    {
        try
        {
            var list = await _supplierService.GetAllAsync();
            _allSuppliers = list.OrderBy(x => x.Name).ToList();
            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SupplierDirectoryViewModel: error loading suppliers");
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        Suppliers.Clear();
        var q = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;

        foreach (var s in _allSuppliers)
        {
            if (string.IsNullOrWhiteSpace(q) ||
                s.Name.ToLowerInvariant().Contains(q) ||
                (!string.IsNullOrEmpty(s.Rfc) && s.Rfc.ToLowerInvariant().Contains(q)) ||
                (!string.IsNullOrEmpty(s.Phone) && s.Phone.Contains(q)) ||
                (!string.IsNullOrEmpty(s.ContactPerson) && s.ContactPerson.ToLowerInvariant().Contains(q)))
            {
                Suppliers.Add(s);
            }
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
            string cleanPhone = string.IsNullOrWhiteSpace(SupplierPhone) ? string.Empty : new string(SupplierPhone.Where(char.IsDigit).ToArray());

            if (IsEditing && _editingSupplier != null)
            {
                var updatedSupplier = new SupplierDto(
                    _editingSupplier.Id,
                    SupplierName.Trim(),
                    SupplierRfc.Trim(),
                    cleanPhone,
                    SupplierEmail.Trim(),
                    SupplierAddress.Trim(),
                    SupplierContact.Trim(),
                    true
                );

                await _supplierService.UpdateAsync(updatedSupplier);
                FeedbackMessage = "Proveedor actualizado con éxito";
                CancelEdit();
            }
            else
            {
                var newSupplier = new SupplierDto(
                    Guid.NewGuid().ToString(),
                    SupplierName.Trim(),
                    SupplierRfc.Trim(),
                    cleanPhone,
                    SupplierEmail.Trim(),
                    SupplierAddress.Trim(),
                    SupplierContact.Trim(),
                    true
                );

                await _supplierService.CreateAsync(newSupplier);
                FeedbackMessage = "Proveedor guardado con éxito";
                ClearForm();
            }

            await LoadSuppliersAsync();
            SuppliersUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SupplierDirectoryViewModel: error saving supplier");
            FeedbackMessage = "Error al guardar el proveedor";
        }
    }

    [RelayCommand]
    public void StartEdit(SupplierDto supplier)
    {
        if (supplier == null) return;
        _editingSupplier = supplier;
        SupplierName = supplier.Name;
        SupplierRfc = supplier.Rfc ?? string.Empty;
        SupplierPhone = supplier.Phone ?? string.Empty;
        SupplierEmail = supplier.Email ?? string.Empty;
        SupplierAddress = supplier.Address ?? string.Empty;
        SupplierContact = supplier.ContactPerson ?? string.Empty;

        IsEditing = true;
        FormTitle = "Editar Proveedor";
        SaveButtonText = "GUARDAR CAMBIOS";
        FeedbackMessage = string.Empty;
    }

    [RelayCommand]
    public void CancelEdit()
    {
        _editingSupplier = null;
        IsEditing = false;
        FormTitle = "Registrar Nuevo Proveedor";
        SaveButtonText = "GUARDAR PROVEEDOR";
        ClearForm();
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync(SupplierDto supplier)
    {
        if (supplier == null) return;
        if (supplier.Name.ToUpper().Contains("GENERAL") || supplier.Id == "sup_compra_general")
        {
            FeedbackMessage = "No se puede eliminar el proveedor del sistema 'COMPRA GENERAL'.";
            return;
        }

        try
        {
            await _supplierService.DeleteAsync(supplier.Id);
            if (_editingSupplier?.Id == supplier.Id)
            {
                CancelEdit();
            }
            FeedbackMessage = "Proveedor eliminado con éxito";
            await LoadSuppliersAsync();
            SuppliersUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SupplierDirectoryViewModel: error deleting supplier");
            FeedbackMessage = "Error al eliminar el proveedor";
        }
    }

    private void ClearForm()
    {
        SupplierName = string.Empty;
        SupplierRfc = string.Empty;
        SupplierPhone = string.Empty;
        SupplierEmail = string.Empty;
        SupplierAddress = string.Empty;
        SupplierContact = string.Empty;
    }
}
