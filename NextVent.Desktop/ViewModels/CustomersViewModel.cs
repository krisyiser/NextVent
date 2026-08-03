using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public partial class CustomersViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;
    public ObservableCollection<CustomerDto> Customers { get; } = [];

    [ObservableProperty] private string _searchQuery = string.Empty;
    public event Action? OpenAddCustomerRequested;
    public event Action<CustomerDto>? OpenAddPaymentRequested;
    public event Action<CustomerDto>? OpenStatementRequested;

    public CustomersViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
        _ = LoadCustomersAsync();
    }

    public async Task LoadCustomersAsync()
    {
        try
        {
            var items = await _customerService.GetAllAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Customers.Clear();
                foreach (var item in items) Customers.Add(item);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading customers");
        }
    }

    [RelayCommand]
    private void OpenAddCustomerDialog() => OpenAddCustomerRequested?.Invoke();

    [RelayCommand]
    private void OpenAddPaymentDialog(CustomerDto customer)
    {
        if (customer != null) OpenAddPaymentRequested?.Invoke(customer);
    }

    [RelayCommand]
    private void OpenStatementDialog(CustomerDto customer)
    {
        if (customer != null) OpenStatementRequested?.Invoke(customer);
    }
}
