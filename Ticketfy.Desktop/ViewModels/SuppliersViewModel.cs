using CommunityToolkit.Mvvm.ComponentModel;
using Ticketfy.Services.Interfaces;
using Ticketfy.ViewModels.Suppliers;
using System;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels;

/// <summary>
/// Modular coordinator for the Suppliers screen.
/// Composes Directory, Purchase Order, and Purchase History sub-ViewModels.
/// </summary>
public partial class SuppliersViewModel : ObservableObject
{
    public SupplierDirectoryViewModel DirectoryVM { get; }
    public PurchaseOrderViewModel OrderVM { get; }
    public PurchaseHistoryViewModel HistoryVM { get; }

    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public SuppliersViewModel(ISupplierService supplierService, IPurchaseService purchaseService, IProductService productService, IEscPosPrinterService? printerService = null)
    {
        DirectoryVM = new SupplierDirectoryViewModel(supplierService);
        OrderVM = new PurchaseOrderViewModel(purchaseService, productService);
        HistoryVM = new PurchaseHistoryViewModel(purchaseService, printerService);

        OrderVM.PurchaseConfirmed += async _ => await HistoryVM.LoadPurchasesAsync();
        DirectoryVM.SuppliersUpdated += async () => await OrderVM.LoadCatalogAsync(System.Linq.Enumerable.ToList(DirectoryVM.Suppliers));
        _ = LoadAllDataAsync();
    }

    public async Task LoadAllDataAsync()
    {
        await DirectoryVM.LoadSuppliersAsync();
        await HistoryVM.LoadPurchasesAsync();
        await OrderVM.LoadCatalogAsync(System.Linq.Enumerable.ToList(DirectoryVM.Suppliers));
    }

    public Task LoadDataAsync() => LoadAllDataAsync();
}
