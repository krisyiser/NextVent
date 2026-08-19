using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NextVent.ViewModels.Dialogs;

public partial class CustomerDialogViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _rfc = string.Empty;

    [ObservableProperty]
    private double? _creditLimit;

    [ObservableProperty]
    private string _customerCode = $"CLI-{Random.Shared.Next(1000, 9999)}";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    private string? _editingCustomerId;
    private double _existingDebt;

    public event Action? RequestClose;

    public CustomerDialogViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    public void LoadForEdit(CustomerDto customer)
    {
        _editingCustomerId = customer.Id;
        Name = customer.Name;
        Phone = customer.Phone;
        Email = customer.Email;
        Rfc = customer.Rfc;
        CreditLimit = customer.CreditLimit;
        CustomerCode = customer.CustomerCode;
        _existingDebt = customer.Debt;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "El nombre del cliente es obligatorio.";
                return;
            }

            var dto = new CustomerDto(
                Id: _editingCustomerId ?? Guid.NewGuid().ToString(),
                Nombre: Name.Trim(),
                Telefono: Phone.Trim(),
                Email: Email.Trim(),
                Rfc: Rfc.Trim(),
                LimiteCredito: CreditLimit ?? 0,
                Deuda: _existingDebt,
                CustomerCode: CustomerCode.Trim()
            );

            if (_editingCustomerId != null)
            {
                await _customerService.UpdateAsync(dto);
            }
            else
            {
                await _customerService.AddAsync(dto);
            }
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving customer");
            ErrorMessage = ex.Message;
        }
    }
}
