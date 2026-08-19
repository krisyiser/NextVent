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
    private readonly IEscPosPrinterService? _printerService;

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
    [ObservableProperty] private string _purchaseUnitPrice = string.Empty;
    [ObservableProperty] private string _purchaseQuantity = "1";
    [ObservableProperty] private double _totalPurchaseCost;

    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty] private bool _isRegisteringPurchase = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmPurchaseCommand))]
    private bool _isSubmitting = false;

    [ObservableProperty]
    private string _submitButtonText = "PROCESAR ENTRADA Y REABASTECER INVENTARIO";

    public SuppliersViewModel(ISupplierService supplierService, IPurchaseService purchaseService, IProductService productService, IEscPosPrinterService? printerService = null)
    {
        _supplierService = supplierService;
        _purchaseService = purchaseService;
        _productService = productService;
        _printerService = printerService;
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var suppliers = await _supplierService.GetAllAsync();
            var purchases = await _purchaseService.GetAllAsync();
            var products = await _productService.GetAllAsync();
            if (suppliers.Count == 0)
            {
#if DEBUG
                await SeedDemoDataAsync(products.ToList());
                suppliers = await _supplierService.GetAllAsync();
                purchases = await _purchaseService.GetAllAsync();
#endif
            }

            // Ensure "Proveedor General" exists
            var generalSupplier = suppliers.FirstOrDefault(s => s.Name == "Proveedor General (Compras Libres)");
            if (generalSupplier == null)
            {
                generalSupplier = await _supplierService.CreateAsync(new SupplierDto(
                    Guid.NewGuid().ToString(),
                    "Proveedor General (Compras Libres)",
                    "N/A",
                    "N/A",
                    "N/A",
                    "Local/Desconocido",
                    "Compras Varias",
                    true
                ));
                suppliers.Add(generalSupplier);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Suppliers.Clear();
                foreach (var s in suppliers.OrderBy(x => x.Name)) Suppliers.Add(s);

                Purchases.Clear();
                foreach (var p in purchases.OrderByDescending(x => x.Date)) Purchases.Add(p);

                AvailableProducts.Clear();
                foreach (var pr in products.OrderBy(x => x.Name)) AvailableProducts.Add(pr);

                SelectedSupplierForPurchase = generalSupplier;
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
        
        if (!double.TryParse(PurchaseQuantity, out double qty) || qty <= 0) return;
        if (!double.TryParse(PurchaseUnitPrice, out double price) || price < 0) return;

        var total = price * qty;
        DraftPurchaseItems.Add(new PurchaseItemDto(
            Guid.NewGuid().ToString(),
            string.Empty,
            SelectedProductForPurchase.Id,
            SelectedProductForPurchase.Name,
            price,
            qty,
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

    private async Task SeedDemoDataAsync(System.Collections.Generic.List<ProductDto> availableProducts)
    {
        string[] supplierNames = [ "Bimbo de México", "Coca-Cola Femsa", "Sabritas S.A.", "Lala Corporativo", "Grupo Modelo" ];
        var createdSuppliers = new System.Collections.Generic.List<SupplierDto>();

        for (int i = 0; i < 5; i++)
        {
            var s = new SupplierDto(
                Guid.NewGuid().ToString(),
                supplierNames[i],
                $"RFC000{i}XXX",
                $"555123456{i}",
                $"contacto@{supplierNames[i].Replace(" ", "").ToLower()}.com",
                "Av. Principal " + (i + 1) * 100,
                "Vendedor " + (i + 1),
                true
            );
            var created = await _supplierService.CreateAsync(s);
            createdSuppliers.Add(created);
        }

        var rand = new Random();
        for (int i = 0; i < 10; i++)
        {
            var supplier = createdSuppliers[rand.Next(createdSuppliers.Count)];
            var items = new System.Collections.Generic.List<PurchaseItemDto>();

            int numItems = rand.Next(1, 4);
            double totalCost = 0;
            for (int j = 0; j < numItems; j++)
            {
                var prod = availableProducts.Count > 0 
                    ? availableProducts[rand.Next(availableProducts.Count)] 
                    : new ProductDto(Guid.NewGuid().ToString(), "0000", "Producto Falso", 10, 15);

                double qty = rand.Next(5, 50);
                double cost = prod.Cost;
                items.Add(new PurchaseItemDto(Guid.NewGuid().ToString(), "", prod.Id, prod.Name, cost, qty, qty * cost));
                totalCost += qty * cost;
            }

            var p = new PurchaseDto(
                Guid.NewGuid().ToString(),
                supplier.Id,
                supplier.Name,
                $"FAC-2024-{rand.Next(1000, 9999)}",
                DateTime.Now.AddDays(-rand.Next(1, 30)).ToString("yyyy-MM-dd HH:mm:ss"),
                totalCost,
                "Compra de reabastecimiento demo",
                items
            );

            await _purchaseService.RegisterPurchaseAsync(p);
        }
    }

    /// <summary>
    /// Generates and queues an ESC/POS purchase receipt for the selected purchase record.
    /// </summary>
    [RelayCommand]
    private async Task PrintPurchaseTicketAsync(PurchaseDto? purchase)
    {
        if (purchase == null || _printerService == null)
        {
            FeedbackMessage = "No hay impresora configurada o registro de compra seleccionado.";
            return;
        }

        try
        {
            await _printerService.PrintPurchaseOrderAsync(purchase, "ImpresoraTickets");
            FeedbackMessage = $"Ticket de compra {purchase.InvoiceNumber} enviado a impresora.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error printing purchase ticket");
            FeedbackMessage = "Error al imprimir el ticket de compra.";
        }
    }
}
