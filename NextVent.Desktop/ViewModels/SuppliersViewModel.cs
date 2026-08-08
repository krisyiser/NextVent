using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly ISupplierService _supplierService;
    private readonly IPurchaseService _purchaseService;
    private readonly IProductService _productService;

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<PurchaseDto> Purchases { get; } = [];
    public ObservableCollection<ProductDto> AvailableProducts { get; } = [];
    public ObservableCollection<PurchaseItemDto> DraftPurchaseItems { get; } = [];

    // Supplier Form
    [ObservableProperty] private string _supplierName = string.Empty;
    [ObservableProperty] private string _supplierRfc = string.Empty;
    [ObservableProperty] private string _supplierPhone = string.Empty;
    [ObservableProperty] private string _supplierEmail = string.Empty;
    [ObservableProperty] private string _supplierAddress = string.Empty;
    [ObservableProperty] private string _supplierContact = string.Empty;

    // Purchase Order Form
    [ObservableProperty] private SupplierDto? _selectedSupplierForPurchase;
    [ObservableProperty] private string _invoiceNumber = string.Empty;
    [ObservableProperty] private ProductDto? _selectedProductForPurchase;
    [ObservableProperty] private double _purchaseUnitPrice;
    [ObservableProperty] private double _purchaseQuantity = 1;
    [ObservableProperty] private double _totalPurchaseCost;

    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty] private bool _isRegisteringPurchase = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmPurchaseCommand))]
    private bool _isSubmitting = false;

    [ObservableProperty]
    private string _submitButtonText = "PROCESAR ENTRADA Y REABASTECER INVENTARIO";

    public SuppliersViewModel(ISupplierService supplierService, IPurchaseService purchaseService, IProductService productService)
    {
        _supplierService = supplierService;
        _purchaseService = purchaseService;
        _productService = productService;
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var suppliers = await _supplierService.GetAllAsync();
            var purchases = await _purchaseService.GetAllAsync();
            var products = await _productService.GetAllAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Suppliers.Clear();
                foreach (var s in suppliers) Suppliers.Add(s);

                Purchases.Clear();
                foreach (var p in purchases) Purchases.Add(p);

                AvailableProducts.Clear();
                foreach (var pr in products) AvailableProducts.Add(pr);

                if (Suppliers.Count > 0) SelectedSupplierForPurchase = Suppliers[0];
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading suppliers data");
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
            Suppliers.Add(created);

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
            Log.Error(ex, "Error creating supplier");
            FeedbackMessage = "Error al guardar el proveedor";
        }
    }

    [RelayCommand]
    private void AddItemToPurchaseDraft()
    {
        if (SelectedProductForPurchase == null) return;
        if (PurchaseQuantity <= 0 || PurchaseUnitPrice < 0) return;

        var total = PurchaseUnitPrice * PurchaseQuantity;
        DraftPurchaseItems.Add(new PurchaseItemDto(
            Guid.NewGuid().ToString(),
            string.Empty,
            SelectedProductForPurchase.Id,
            SelectedProductForPurchase.Name,
            PurchaseUnitPrice,
            PurchaseQuantity,
            total
        ));

        TotalPurchaseCost = DraftPurchaseItems.Sum(i => i.TotalPrice);
        ConfirmPurchaseCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveItemFromPurchaseDraft(PurchaseItemDto item)
    {
        DraftPurchaseItems.Remove(item);
        TotalPurchaseCost = DraftPurchaseItems.Sum(i => i.TotalPrice);
        ConfirmPurchaseCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirmPurchase()
    {
        return !IsSubmitting && DraftPurchaseItems.Any();
    }

    [RelayCommand(CanExecute = nameof(CanConfirmPurchase))]
    private async Task ConfirmPurchaseAsync()
    {
        if (SelectedSupplierForPurchase == null)
        {
            FeedbackMessage = "Seleccione un proveedor";
            return;
        }

        if (DraftPurchaseItems.Count == 0)
        {
            FeedbackMessage = "Agregue al menos un producto a la compra";
            return;
        }

        try
        {
            IsSubmitting = true;
            SubmitButtonText = "PROCESANDO ENTRADA...";

            var purchaseDto = new PurchaseDto(
                Guid.NewGuid().ToString(),
                SelectedSupplierForPurchase.Id,
                SelectedSupplierForPurchase.Name,
                string.IsNullOrWhiteSpace(InvoiceNumber) ? $"FAC-{DateTime.Now:fff}" : InvoiceNumber,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalPurchaseCost,
                "Entrada directa de inventario",
                DraftPurchaseItems.ToList()
            );

            var registered = await _purchaseService.RegisterPurchaseAsync(purchaseDto);
            Purchases.Insert(0, registered);

            DraftPurchaseItems.Clear();
            TotalPurchaseCost = 0;
            InvoiceNumber = string.Empty;
            IsRegisteringPurchase = false;

            FeedbackMessage = "¡Compra y reabastecimiento registrados correctamente!";
            await _productService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error registering purchase order");
            FeedbackMessage = "Error al procesar la entrada de mercancía";
        }
        finally
        {
            IsSubmitting = false;
            SubmitButtonText = "PROCESAR ENTRADA Y REABASTECER INVENTARIO";
            ConfirmPurchaseCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task GenerateSuggestedPurchaseOrderAsync(string supplierId)
    {
        var targetSupplier = Suppliers.FirstOrDefault(s => s.Id == supplierId) ?? SelectedSupplierForPurchase;
        if (targetSupplier != null)
        {
            SelectedSupplierForPurchase = targetSupplier;
        }

        var lowStockProducts = AvailableProducts
            .Where(p => p.Stock <= p.MinStock)
            .ToList();

        if (lowStockProducts.Count == 0)
        {
            FeedbackMessage = "No hay productos con stock por debajo del mínimo.";
            return;
        }

        DraftPurchaseItems.Clear();

        foreach (var p in lowStockProducts)
        {
            double qtyToOrder = Math.Max(10.0, (p.MinStock * 2) - p.Stock);
            double itemTotal = p.Cost * qtyToOrder;

            DraftPurchaseItems.Add(new PurchaseItemDto(
                Guid.NewGuid().ToString(),
                string.Empty,
                p.Id,
                p.Name,
                p.Cost,
                qtyToOrder,
                itemTotal
            ));
        }

        TotalPurchaseCost = DraftPurchaseItems.Sum(i => i.TotalPrice);
        IsRegisteringPurchase = true;
        ConfirmPurchaseCommand.NotifyCanExecuteChanged();
        FeedbackMessage = $"¡Orden sugerida generada con {DraftPurchaseItems.Count} productos con stock crítico!";
        await Task.CompletedTask;
    }
}
