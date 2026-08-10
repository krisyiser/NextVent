using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NextVent.Core.Messages;
using NextVent.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services;
using NextVent.Services.Interfaces;
using NextVent.Core.Models;
using NextVent.Core.Repositories;
using NextVent.Core.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public record CategoryChipDto(string Name, int Count, string DisplayName);

public partial class ParkedTicketModel : ObservableObject
{
    public string TicketId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = "Público General";
    public DateTime ParkedAt { get; init; } = DateTime.Now;
    public double TotalAmount { get; init; }
    public List<CartItemDto> Lines { get; init; } = new();
}

public partial class PosViewModel : ObservableObject, System.IDisposable
{
    private readonly IProductService _productService;
    private readonly AppDbContext? _db;
    private readonly ICustomerService? _customerService;
    private readonly string _draftCartPath;

    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<ProductDto> FilteredProducts { get; } = [];
    public ObservableCollection<CartItemDto> CartItems { get; } = [];
    public ObservableCollection<CategoryChipDto> CategoryChips { get; } = [];
    public ObservableCollection<CustomerDto> Customers { get; } = [];
    public ObservableCollection<ParkedTicketModel> ParkedTickets { get; } = new();
    private readonly Dictionary<string, double> _originalStockCache = new();

    public bool HasParkedTickets => ParkedTickets.Count > 0;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _selectedCategory = "⭐ Top Ventas";
    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private double _subtotal;
    [ObservableProperty] private double _discountTotal = 0.0;
    [ObservableProperty] private double _tax;
    [ObservableProperty] private double _total;
    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedbackColor))]
    private bool _feedbackIsError;
    public string FeedbackColor => FeedbackIsError ? "#EF4444" : "#10B981";

    public bool NotificationIsError
    {
        get => FeedbackIsError;
        set => FeedbackIsError = value;
    }

    public string NotificationMessage
    {
        get => FeedbackMessage;
        set => FeedbackMessage = value;
    }
    [ObservableProperty] private int _parkedOrdersCount = 0;
    [ObservableProperty] private double _cartWidthPx = 380;
    [ObservableProperty] private string _initialPaymentMode = "Efectivo";

    // Hardware Telemetry & Cashier Status Badges
    [ObservableProperty] private string _activeCashierName = "Alexa S. (Caja 01)";
    [ObservableProperty] private bool _isPrinterOk = true;
    [ObservableProperty] private bool _isScannerOk = true;
    [ObservableProperty] private bool _isDbEncrypted = true;

    [ObservableProperty] private int _cartGridColumn = 1;
    [ObservableProperty] private int _productsGridColumn = 0;
    [ObservableProperty] private string _posGridColumnDefinitions = "*,Auto";
    [ObservableProperty] private Thickness _productsMargin = new(0, 0, 16, 0);

    private readonly IShiftNoteService? _shiftNoteService;
    private readonly IItemKitService? _kitService;
    private readonly ISessionManager? _sessionManager;
    private readonly IUserRepository? _userRepository;
    private readonly IPromotionService? _promotionService;
    private readonly IAuditService? _auditService;
    private readonly IAttendanceService? _attendanceService;
    private readonly DispatcherTimer _debounceTimer;

    public ObservableCollection<ShiftNoteDto> ActiveShiftNotes { get; } = [];
    [ObservableProperty] private string _newShiftNoteText = string.Empty;

    public event Action? OpenCheckoutRequested;
    public event Action? ToggleFullscreenRequested;
    public event Action? LogoutRequested;
    public event Action? OpenCreateItemKitRequested;
    public event Action? OpenShiftNotesRequested;
    public event Action? FocusSearchRequested;
    public event Action? OpenCustomerSelectRequested;
    public event Action? OpenSwitchUserPinRequested;
    public event Action? OpenLockScreenRequested;
    public event Action<string, Action<bool>>? OpenSupervisorPinRequested;
    public event Action<string, string>? ShowAlertRequested;

    private void ShowAlert(string title, string message)
    {
        ShowAlertRequested?.Invoke(title, message);
    }

    public PosViewModel(
        IProductService productService,
        AppDbContext? db = null,
        IShiftNoteService? shiftNoteService = null,
        IItemKitService? kitService = null,
        ICustomerService? customerService = null,
        ISessionManager? sessionManager = null,
        IUserRepository? userRepository = null,
        IPromotionService? promotionService = null,
        IAuditService? auditService = null,
        IAttendanceService? attendanceService = null)
    {
        _productService = productService;
        _db = db;
        _shiftNoteService = shiftNoteService;
        _kitService = kitService;
        _customerService = customerService;
        _sessionManager = sessionManager;
        _userRepository = userRepository;
        _promotionService = promotionService;
        _auditService = auditService;
        _attendanceService = attendanceService;

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _debounceTimer.Tick += async (s, e) =>
        {
            _debounceTimer.Stop();
            await RecalculateCartPromotionsAsync();
        };

        if (_sessionManager != null)
        {
            _sessionManager.CashierChanged += OnCashierChanged;
            if (_sessionManager.CurrentCashier != null)
            {
                OnCashierChanged(_sessionManager.CurrentCashier);
            }
        }

        ThemeService.Instance.CartWidthChanged += width => Dispatcher.UIThread.Post(() => CartWidthPx = width);
        ThemeService.Instance.CartPositionChanged += pos => Dispatcher.UIThread.Post(() => ApplyCartPosition(pos));
        _ = LoadProductsAsync();
        _ = LoadCustomersAsync();
        _ = RefreshParkedCountAsync();
        _ = LoadActiveShiftNotesAsync();

        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string nextVentFolder = Path.Combine(appDataFolder, "NextVent");
        if (!Directory.Exists(nextVentFolder))
        {
            Directory.CreateDirectory(nextVentFolder);
        }
        _draftCartPath = Path.Combine(nextVentFolder, "DraftCart.json");

        CartItems.CollectionChanged += (s, e) => _ = SaveDraftCartAsync();
        _ = RehydrateDraftCartAsync();

        RegisterMessages();
    }

    public void RegisterMessages()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);

        WeakReferenceMessenger.Default.Register<ForceLogoutMessage>(this, (r, m) =>
        {
            HandleForceLogout();
        });
    }

    private void HandleForceLogout()
    {
        Dispatcher.UIThread.Post(() =>
        {
            ClearCart();
            ParkedTickets.Clear();
            ParkedOrdersCount = 0;
            SelectedCustomer = null;

            WeakReferenceMessenger.Default.UnregisterAll(this);
        });
    }

    private void OnCashierChanged(UserModel user)
    {
        ActiveCashierName = $"{user.FullName} ({user.Role})";
        FeedbackMessage = $"Sesión de cajero iniciada: {user.FullName}";
    }

    public void ApplyCartPosition(string position)
    {
        if (position == "Izquierda")
        {
            CartGridColumn = 0;
            ProductsGridColumn = 1;
            PosGridColumnDefinitions = "Auto,*";
            ProductsMargin = new Thickness(16, 0, 0, 0);
        }
        else
        {
            CartGridColumn = 1;
            ProductsGridColumn = 0;
            PosGridColumnDefinitions = "*,Auto";
            ProductsMargin = new Thickness(0, 0, 16, 0);
        }
    }

    public async Task RefreshParkedCountAsync()
    {
        if (_db == null) return;
        try
        {
            var count = await _db.ParkedOrders.CountAsync();
            await Dispatcher.UIThread.InvokeAsync(() => ParkedOrdersCount = count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error counting parked orders");
        }
    }

    public async Task LoadCustomersAsync()
    {
        if (_customerService == null) return;
        try
        {
            var list = await _customerService.GetAllAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Customers.Clear();
                foreach (var c in list) Customers.Add(c);
                if (SelectedCustomer == null && Customers.Count > 0)
                {
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Name.Contains("Público", StringComparison.OrdinalIgnoreCase)) ?? Customers[0];
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading customers in PosViewModel");
        }
    }

    public async Task LoadProductsAsync()
    {
        try
        {
            var list = await _productService.GetAllAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Products.Clear();
                _originalStockCache.Clear();
                foreach (var p in list)
                {
                    _originalStockCache[p.Id] = p.Stock;
                    Products.Add(p);
                }
                BuildCategoryChips();
                FilterProducts();
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading products in PosViewModel");
        }
    }

    private void RefreshCatalogItemState(string productId)
    {
        var index = Products.IndexOf(Products.FirstOrDefault(p => p.Id == productId));
        if (index >= 0 && _originalStockCache.TryGetValue(productId, out double dbStock))
        {
            var oldItem = Products[index];
            var cartItem = CartItems.FirstOrDefault(i => i.Id == productId);
            double cartQty = cartItem?.Quantity ?? 0.0;
            double newStock = Math.Max(0.0, dbStock - cartQty);

            var newItem = oldItem with { Stock = newStock };
            Products[index] = newItem;

            FilterProducts();
        }
    }

    private void BuildCategoryChips()
    {
        CategoryChips.Clear();
        CategoryChips.Add(new CategoryChipDto("⭐ Top Ventas", Products.Count, $"⭐ TOP VENTAS ({Products.Count})"));

        var groups = Products.GroupBy(p => p.Category ?? "General").OrderBy(g => g.Key);
        foreach (var g in groups)
        {
            CategoryChips.Add(new CategoryChipDto(g.Key, g.Count(), $"{g.Key.ToUpper()} ({g.Count()})"));
        }
    }

    [RelayCommand]
    private void SelectCategoryChip(CategoryChipDto chip)
    {
        if (chip == null) return;
        SelectedCategory = chip.Name;
        FilterProducts();
    }

    private void FilterProducts()
    {
        FilteredProducts.Clear();
        var query = SearchQuery.Trim().ToLower();

        var matches = Products.Where(p =>
            (SelectedCategory == "⭐ Top Ventas" || SelectedCategory == "Todos" || p.Category == SelectedCategory) &&
            (string.IsNullOrWhiteSpace(query) ||
             p.Name.ToLower().Contains(query) ||
             (p.Barcode != null && p.Barcode.ToLower().Contains(query))) &&
            p.Stock > 0.0
        );

        foreach (var m in matches)
        {
            FilteredProducts.Add(m);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterProducts();
    }

    [RelayCommand]
    private void FocusSearch() => WeakReferenceMessenger.Default.Send(new FocusSearchMessage());

    [RelayCommand]
    private void OpenCustomerSelect() => OpenCustomerSelectRequested?.Invoke();

    [RelayCommand]
    private void PayCash() => _ = CheckoutAsync("Efectivo");

    [RelayCommand]
    private void PayCard() => _ = CheckoutAsync("TarjetaDebito");

    [RelayCommand]
    private void PayMixed() => _ = CheckoutAsync("Mixto");

    [RelayCommand]
    private void ProcessScanOrSearchSubmit()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            var input = SearchQuery.Trim();
            double quantityMultiplier = 1.0;
            string productQuery = input;

            // ADVANCED PARSER (e.g. "6*750123456" -> Qty: 6, Query: "750123456")
            var parts = input.Split('*', 2);
            if (parts.Length == 2 && double.TryParse(parts[0], out double parsedQty))
            {
                quantityMultiplier = parsedQty;
                productQuery = parts[1].Trim();
            }

            var p = Products.FirstOrDefault(x =>
                (x.Barcode != null && x.Barcode.Equals(productQuery, StringComparison.OrdinalIgnoreCase)) ||
                x.Name.Equals(productQuery, StringComparison.OrdinalIgnoreCase));

            if (p == null)
            {
                // Fallback: search by partial name
                p = Products.FirstOrDefault(x => x.Name.Contains(productQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (p != null)
            {
                var existingItem = CartItems.FirstOrDefault(i => i.Id == p.Id);
                double currentCartQty = existingItem?.Quantity ?? 0.0;
                double projectedQty = currentCartQty + quantityMultiplier;

                if (projectedQty > p.Stock)
                {
                    ShowAlert("Stock Excedido", $"Intentas agregar {projectedQty:N2} pero solo hay {p.Stock:N2} disponibles de {p.Name}. Se ajustará al máximo permitido.");
                    quantityMultiplier = p.Stock - currentCartQty;
                }

                if (quantityMultiplier <= 0)
                {
                    SearchQuery = string.Empty;
                    FeedbackIsError = true;
                    FeedbackMessage = $"El producto {p.Name} ya está en su stock límite en el carrito.";
                    return;
                }

                AddToCartWithQuantity(p, quantityMultiplier);
                SearchQuery = string.Empty;
                FeedbackIsError = false;
                FeedbackMessage = $"¡Agregado {quantityMultiplier:N3}x {p.Name} al ticket!";
            }
            else
            {
                _ = TryAddKitBarcodeAsync(productQuery, quantityMultiplier);
            }
        }
        finally
        {
            WeakReferenceMessenger.Default.Send(new FocusSearchMessage());
        }
    }

    private async Task TryAddKitBarcodeAsync(string barcode, double multiplier)
    {
        if (_kitService == null)
        {
            FeedbackMessage = $"Producto '{barcode}' no encontrado";
            return;
        }

        var kit = await _kitService.GetByBarcodeAsync(barcode);
        if (kit != null)
        {
            foreach (var item in kit.Items)
            {
                var prod = Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod != null)
                {
                    var existingItem = CartItems.FirstOrDefault(i => i.Id == prod.Id);
                    double currentCartQty = existingItem?.Quantity ?? 0.0;
                    double requestedQty = item.Quantity * multiplier;
                    double projectedQty = currentCartQty + requestedQty;

                    if (projectedQty > prod.Stock)
                    {
                        ShowAlert("Stock Excedido en Combo", $"El combo '{kit.Name}' requiere {projectedQty:N2} de {prod.Name} pero solo hay {prod.Stock:N2} disponibles. Se ajustará al máximo permitido.");
                        requestedQty = prod.Stock - currentCartQty;
                    }

                    if (requestedQty > 0)
                    {
                        AddToCartWithQuantity(prod, requestedQty);
                    }
                }
            }
            SearchQuery = string.Empty;
            FeedbackMessage = $"¡Combo / Paquete '{kit.Name}' agregado al ticket!";
        }
        else
        {
            FeedbackMessage = $"Producto o Combo '{barcode}' no encontrado";
        }
    }

    public async Task LoadActiveShiftNotesAsync()
    {
        if (_shiftNoteService == null) return;
        try
        {
            var list = await _shiftNoteService.GetActiveNotesAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ActiveShiftNotes.Clear();
                foreach (var n in list.Where(x => !x.IsResolved)) ActiveShiftNotes.Add(n);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading shift notes");
        }
    }

    [RelayCommand]
    private async Task AddShiftNoteAsync()
    {
        if (_shiftNoteService == null || string.IsNullOrWhiteSpace(NewShiftNoteText)) return;
        try
        {
            await _shiftNoteService.SaveNoteAsync("CAJERO EN TURNO", NewShiftNoteText);
            NewShiftNoteText = string.Empty;
            await LoadActiveShiftNotesAsync();
            FeedbackMessage = "¡Nota de turno registrada correctamente!";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error adding shift note");
        }
    }

    [RelayCommand]
    private async Task ResolveShiftNoteAsync(ShiftNoteDto note)
    {
        if (_shiftNoteService == null || note == null) return;
        try
        {
            await _shiftNoteService.ResolveNoteAsync(note.Id);
            await LoadActiveShiftNotesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resolving shift note");
        }
    }

    [RelayCommand]
    private void OpenCreateItemKitDialog() => OpenCreateItemKitRequested?.Invoke();

    [RelayCommand]
    private void OpenShiftNotesDialog() => OpenShiftNotesRequested?.Invoke();

    [RelayCommand]
    private void AddToCart(ProductDto product)
    {
        AddToCartWithQuantity(product, 1.0);
    }

    private void AddToCartWithQuantity(ProductDto product, double qty)
    {
        if (product == null) return;
        var existing = CartItems.FirstOrDefault(i => i.Id == product.Id);
        
        double currentPrice = GetPriceForCurrentCustomer(product);
        
        if (existing != null)
        {
            double projectedQty = existing.Quantity + qty;
            existing.Quantity = Math.Max(0.0, Math.Min(projectedQty, product.Stock)); // HARD CAP
            
            existing.UnitPrice = currentPrice;
            existing.OriginalUnitPrice = currentPrice;

            if (projectedQty > product.Stock)
            {
                ShowAlert("Stock Insuficiente", $"Solo hay {product.Stock} unidades disponibles de {product.Name}.");
            }
        }
        else
        {
            double safeQty = Math.Max(0.0, Math.Min(qty, product.Stock)); // HARD CAP
            var cartItem = new CartItemDto(product.Id, product.Name, currentPrice, safeQty, product.Unit)
            {
                Category = product.Category ?? "General",
                Cost = product.Cost
            };
            CartItems.Add(cartItem);

            if (qty > product.Stock)
            {
                ShowAlert("Stock Insuficiente", $"Solo se agregaron {safeQty} unidades de {product.Name}.");
            }
        }

        // Calculate and trigger warnings and update card state in real-time
        var finalItem = CartItems.FirstOrDefault(i => i.Id == product.Id);
        if (finalItem != null)
        {
            double remainingStock = product.Stock - finalItem.Quantity;
            if (remainingStock <= product.MinStock && remainingStock > 0)
            {
                FeedbackIsError = true;
                FeedbackMessage = $"⚠️ ALERTA: Quedan {remainingStock:N2} piezas de {product.Name} (Stock Mínimo).";
            }
            else if (remainingStock <= 0)
            {
                FeedbackIsError = true;
                FeedbackMessage = $"⚠️ {product.Name} se ha AGOTADO con esta venta.";
            }
            else
            {
                FeedbackIsError = false;
                FeedbackMessage = $"Agregado: {product.Name}";
            }
        }

        RefreshCatalogItemState(product.Id);
        RecalculateTotal();
    }

    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        RecalculateCartPricesForCustomer();
    }

    private void RecalculateCartPricesForCustomer()
    {
        if (CartItems.Count == 0) return;

        foreach (var item in CartItems)
        {
            var productDef = Products.FirstOrDefault(p => p.Id == item.Id);
            if (productDef != null)
            {
                double basePrice = GetPriceForCurrentCustomer(productDef);
                item.UnitPrice = basePrice;
                item.OriginalUnitPrice = basePrice;
            }
        }
        
        _ = RecalculateCartPromotionsAsync();
    }

    private double GetPriceForCurrentCustomer(ProductDto product)
    {
        if (SelectedCustomer != null && SelectedCustomer.IsWholesale)
        {
            return product.WholesalePrice > 0 ? product.WholesalePrice : product.Price;
        }
        return product.Price;
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItemDto item)
    {
        if (item == null) return;
        var product = Products.FirstOrDefault(p => p.Id == item.Id);
        if (product != null)
        {
            if (item.Quantity + 1 > product.Stock)
            {
                ShowAlert("Stock Insuficiente", $"Solo hay {product.Stock:N2} unidades disponibles de {product.Name}. No se pueden agregar más.");
                return;
            }

            double remainingStock = product.Stock - (item.Quantity + 1);
            if (remainingStock <= product.MinStock && remainingStock > 0)
            {
                FeedbackIsError = true;
                FeedbackMessage = $"⚠️ ALERTA: Quedan {remainingStock:N2} piezas de {product.Name} (Stock Mínimo).";
            }
            else if (remainingStock <= 0)
            {
                FeedbackIsError = true;
                FeedbackMessage = $"⚠️ {product.Name} se ha AGOTADO con esta venta.";
            }
            else
            {
                FeedbackIsError = false;
                FeedbackMessage = $"Agregado: {product.Name}";
            }
        }
        item.Quantity += 1;
        RefreshCatalogItemState(item.Id);
        RecalculateTotal();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItemDto item)
    {
        if (item == null) return;
        if (item.Quantity > 1)
        {
            item.Quantity -= 1;
        }
        else
        {
            CartItems.Remove(item);
        }

        var product = Products.FirstOrDefault(p => p.Id == item.Id);
        if (product != null)
        {
            var cartItem = CartItems.FirstOrDefault(i => i.Id == item.Id);
            double currentCartQty = cartItem?.Quantity ?? 0.0;
            double remainingStock = product.Stock - currentCartQty;

            if (remainingStock <= product.MinStock && remainingStock > 0)
            {
                FeedbackIsError = true;
                FeedbackMessage = $"⚠️ ALERTA: Quedan {remainingStock:N2} piezas de {product.Name} (Stock Mínimo).";
            }
            else if (remainingStock <= 0)
            {
                FeedbackIsError = true;
                FeedbackMessage = $"⚠️ {product.Name} se ha AGOTADO con esta venta.";
            }
            else
            {
                FeedbackIsError = false;
                FeedbackMessage = $"Descontado: {product.Name}";
            }
        }

        RefreshCatalogItemState(item.Id);
        RecalculateTotal();
    }

    [RelayCommand]
    private async Task RemoveFromCartAsync(CartItemDto item)
    {
        if (item == null) return;

        double itemTotalValue = (item.OriginalUnitPrice > 0 ? item.OriginalUnitPrice : item.UnitPrice) * item.Quantity;
        var currentUserId = _sessionManager?.CurrentCashier?.Id.ToString() ?? "cajero_matriz";

        if (_auditService != null)
        {
            var auditEntry = new AuditLogEntity
            {
                UserId = currentUserId,
                ActionType = NextVent.Core.Enums.AuditActionType.CartItemRemoved,
                RiskLevel = itemTotalValue > 500.0 ? NextVent.Core.Enums.RiskLevel.HighRisk : NextVent.Core.Enums.RiskLevel.Warning,
                EntityName = nameof(CartItemDto),
                EntityId = item.ProductId,
                OldValue = $"Qty: {item.Quantity:N2}, Price: {item.OriginalUnitPrice:C}",
                NewValue = "REMOVED_FROM_CART",
                FinancialImpact = itemTotalValue,
                Reason = $"Eliminación de producto '{item.Name}' del carrito antes de cobro"
            };
            await _auditService.LogAsync(auditEntry);
        }

        CartItems.Remove(item);
        RefreshCatalogItemState(item.Id);
        RecalculateTotal();

        FeedbackIsError = false;
        FeedbackMessage = $"Removido del carrito: {item.Name}";
    }

    private void RemoveFromCart(CartItemDto item) => _ = RemoveFromCartAsync(item);

    [RelayCommand]
    public async Task ClearCartAsync()
    {
        if (CartItems.Count == 0) return;

        double totalCartValue = CartItems.Sum(i => i.TotalPrice);
        var currentUserId = _sessionManager?.CurrentCashier?.Id.ToString() ?? "cajero_matriz";

        if (_auditService != null)
        {
            var auditEntry = new AuditLogEntity
            {
                UserId = currentUserId,
                ActionType = NextVent.Core.Enums.AuditActionType.CartCleared,
                RiskLevel = totalCartValue > 500.0 ? NextVent.Core.Enums.RiskLevel.HighRisk : NextVent.Core.Enums.RiskLevel.Warning,
                EntityName = "CartTicket",
                EntityId = "CART_CLEAR",
                OldValue = $"ItemCount: {CartItems.Count}, Total: {totalCartValue:C}",
                NewValue = "CART_CLEARED",
                FinancialImpact = totalCartValue,
                Reason = "Vaciado total del ticket antes de cobro"
            };
            await _auditService.LogAsync(auditEntry);
        }

        var itemsToRefresh = CartItems.ToList();
        CartItems.Clear();
        RecalculateTotal();
        FeedbackIsError = false;
        FeedbackMessage = "Carrito limpiado";

        foreach (var item in itemsToRefresh)
        {
            RefreshCatalogItemState(item.Id);
        }
    }

    public void ClearCart() => _ = ClearCartAsync();

    [RelayCommand]
    private void SwitchCashier()
    {
        FeedbackMessage = "Iniciando cambio de usuario...";
        OpenSwitchUserPinRequested?.Invoke();
    }

    [RelayCommand]
    private void LockTerminal()
    {
        _sessionManager?.LockTerminal();
        FeedbackMessage = "Terminal bloqueada por seguridad (Win+L)";
        OpenLockScreenRequested?.Invoke();
    }

    [RelayCommand]
    private async Task GenerateXReportAsync()
    {
        FeedbackMessage = "Generando Corte Parcial de Caja (Arqueo X)...";
        await Task.Yield();
    }

    [RelayCommand]
    private void GenerateZReport()
    {
        // RBAC: If current cashier is standard CAJERO, prompt for Supervisor / ADMIN PIN first!
        if (_sessionManager?.CurrentCashier?.Role == SystemRole.CAJERO)
        {
            OpenSupervisorPinRequested?.Invoke("Corte Final de Turno (Arqueo Z)", (authorized) =>
            {
                if (authorized)
                {
                    ExecuteZReportCloseout();
                }
                else
                {
                    FeedbackMessage = "Cierre de turno cancelado — Se requiere autorización de ADMIN";
                }
            });
        }
        else
        {
            ExecuteZReportCloseout();
        }
    }

    private void ExecuteZReportCloseout()
    {
        FeedbackMessage = "¡Corte Z completado! Turno cerrado exitosamente.";
    }

    [RelayCommand]
    private async Task RestoreTicketAsync(ParkedTicketModel? ticket)
    {
        if (ticket is null) return;
        CartItems.Clear();
        var outOfStockWarnings = new List<string>();

        foreach (var parkedLine in ticket.Lines)
        {
            var currentProduct = await _productService.GetByIdAsync(parkedLine.ProductId);
            if (currentProduct == null || currentProduct.Stock < parkedLine.Quantity)
            {
                outOfStockWarnings.Add($"- {parkedLine.Name} (Disp: {currentProduct?.Stock ?? 0})");
                parkedLine.Quantity = currentProduct?.Stock ?? 0;
            }

            if (parkedLine.Quantity > 0)
            {
                CartItems.Add(parkedLine);
            }
        }

        if (outOfStockWarnings.Any())
        {
            FeedbackMessage = "Algunos productos del ticket pausado ya no tienen stock suficiente:\n" + string.Join("\n", outOfStockWarnings);
        }
        else
        {
            FeedbackMessage = $"¡Venta {ticket.TicketId} reanudada!";
        }

        await RecalculateCartPromotionsAsync();
        ParkedTickets.Remove(ticket);
        ParkedOrdersCount = ParkedTickets.Count;
        OnPropertyChanged(nameof(CanParkCurrentTicket));
        OnPropertyChanged(nameof(HasParkedTickets));
    }

    [RelayCommand]
    private async Task DiscardParkedTicketAsync(ParkedTicketModel? ticket)
    {
        if (ticket is null) return;

        var currentUserId = _sessionManager?.CurrentCashier?.Id.ToString() ?? "cajero_matriz";

        if (_auditService != null)
        {
            var auditEntry = new AuditLogEntity
            {
                UserId = currentUserId,
                ActionType = NextVent.Core.Enums.AuditActionType.ParkedOrderCancelled,
                RiskLevel = ticket.TotalAmount > 500.0 ? NextVent.Core.Enums.RiskLevel.HighRisk : NextVent.Core.Enums.RiskLevel.Warning,
                EntityName = nameof(ParkedTicketModel),
                EntityId = ticket.TicketId,
                OldValue = $"Lines: {ticket.Lines.Count}, Total: {ticket.TotalAmount:C}",
                NewValue = "PARKED_TICKET_DISCARDED",
                FinancialImpact = ticket.TotalAmount,
                Reason = $"Venta en espera '{ticket.TicketId}' descartada"
            };
            await _auditService.LogAsync(auditEntry);
        }

        ParkedTickets.Remove(ticket);
        ParkedOrdersCount = ParkedTickets.Count;
        OnPropertyChanged(nameof(HasParkedTickets));
        FeedbackMessage = "Venta pausada descartada";
    }

    private void DiscardParkedTicket(ParkedTicketModel? ticket) => _ = DiscardParkedTicketAsync(ticket);

    [RelayCommand]
    private async Task ParkCurrentCartAsync()
    {
        if (CartItems.Count == 0)
        {
            FeedbackIsError = true;
            FeedbackMessage = "El carrito está vacío para pausar";
            return;
        }

        try
        {
            var parked = new ParkedTicketModel
            {
                TicketId = $"Turno #{ParkedTickets.Count + 1}",
                CustomerName = SelectedCustomer?.Name ?? "Público General",
                TotalAmount = Total,
                Lines = new List<CartItemDto>(CartItems)
            };

            ParkedTickets.Add(parked);
            ParkedOrdersCount = ParkedTickets.Count;
            OnPropertyChanged(nameof(HasParkedTickets));

            if (_db != null)
            {
                var json = JsonSerializer.Serialize(
                    CartItems.ToList(),
                    NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListCartItemDto);
                _db.ParkedOrders.Add(new ParkedOrderEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.ToString("s"),
                    ItemsJson = json
                });
                await _db.SaveChangesAsync();
            }

            CartItems.Clear();
            RecalculateTotal();

            FeedbackIsError = false;
            FeedbackMessage = "Venta en espera guardada correctamente.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error parking current cart");
            FeedbackIsError = true;
            FeedbackMessage = "Error al pausar la venta";
        }
    }

    [RelayCommand]
    private async Task ResumeParkedCartAsync()
    {
        if (_db == null) return;
        try
        {
            var last = await _db.ParkedOrders.OrderByDescending(p => p.Timestamp).FirstOrDefaultAsync();
            if (last == null)
            {
                FeedbackMessage = "No hay ventas pausadas en espera";
                return;
            }

            var items = JsonSerializer.Deserialize(
                last.ItemsJson,
                NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListCartItemDto);
            if (items != null)
            {
                CartItems.Clear();
                var outOfStockWarnings = new List<string>();

                foreach (var parkedLine in items)
                {
                    var currentProduct = await _productService.GetByIdAsync(parkedLine.ProductId);
                    if (currentProduct == null || currentProduct.Stock < parkedLine.Quantity)
                    {
                        outOfStockWarnings.Add($"- {parkedLine.Name} (Disp: {currentProduct?.Stock ?? 0})");
                        parkedLine.Quantity = currentProduct?.Stock ?? 0;
                    }

                    if (parkedLine.Quantity > 0)
                    {
                        CartItems.Add(parkedLine);
                    }
                }

                if (outOfStockWarnings.Any())
                {
                    FeedbackIsError = true;
                    FeedbackMessage = "Algunos productos del ticket pausado ya no tienen stock suficiente:\n" + string.Join("\n", outOfStockWarnings);
                }
                else
                {
                    FeedbackIsError = false;
                    FeedbackMessage = "¡Venta reanudada en el carrito!";
                }

                await RecalculateCartPromotionsAsync();
            }

            _db.ParkedOrders.Remove(last);
            await _db.SaveChangesAsync();

            await RefreshParkedCountAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resuming parked cart");
            FeedbackIsError = true;
            FeedbackMessage = "Error al reanudar venta";
        }
    }

    public bool CanParkCurrentTicket => CartItems.Count > 0;
    public bool HasParkedOrders => ParkedTickets.Count > 0;

    private async Task<bool> VerifyAttendanceGuardAsync()
    {
        if (_attendanceService != null && _sessionManager?.CurrentCashier != null)
        {
            // 1. Prioritize Shift Status over pure attendance
            if (_db != null)
            {
                var hasActiveShift = await _db.Shifts
                    .AsNoTracking()
                    .AnyAsync(s => s.IsOpen == 1);
                if (hasActiveShift)
                {
                    return true;
                }
            }

            // 2. Fallback to attendance check (if strict rules apply)
            bool clockedIn = await _attendanceService.HasActiveClockInAsync(_sessionManager.CurrentCashier.Id.ToString());
            if (!clockedIn)
            {
                FeedbackIsError = true;
                FeedbackMessage = "Acceso Denegado: Debes registrar tu entrada en el control de asistencia antes de abrir la caja.";
                return false;
            }
        }
        return true;
    }

    [RelayCommand]
    private async Task CheckoutAsync(string? paymentMethodParam)
    {
        if (CartItems == null || CartItems.Count == 0)
        {
            FeedbackIsError = true;
            FeedbackMessage = "El carrito está vacío. Agregue productos antes de cobrar.";
            return;
        }

        if (!await VerifyAttendanceGuardAsync()) return;

        InitialPaymentMode = paymentMethodParam switch
        {
            "TarjetaDebito" => "Tarjeta Débito/Crédito",
            "Mixto" => "Mixto",
            _ => "Efectivo"
        };

        OpenCheckoutRequested?.Invoke();
    }

    [RelayCommand]
    private void OpenCheckoutDialog()
    {
        _ = CheckoutAsync("Efectivo");
    }

    [RelayCommand(CanExecute = nameof(CanParkCurrentTicket))]
    private void ParkCurrentTicket()
    {
        _ = ParkCurrentCartAsync();
        OnPropertyChanged(nameof(CanParkCurrentTicket));
        OnPropertyChanged(nameof(HasParkedOrders));
        OnPropertyChanged(nameof(HasParkedTickets));
    }

    [RelayCommand] private void ToggleFullscreen() => ToggleFullscreenRequested?.Invoke();
    [RelayCommand] private void Logout() => LogoutRequested?.Invoke();

    private void RecalculateTotal()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async Task RecalculateCartPromotionsAsync()
    {
        if (_promotionService != null && CartItems.Count > 0)
        {
            var evaluated = await _promotionService.EvaluateAndApplyPromotionsAsync(CartItems.ToList());
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                double sub = 0.0;
                double disc = 0.0;

                foreach (var item in evaluated)
                {
                    sub += item.OriginalUnitPrice * item.Quantity;
                    disc += item.AppliedDiscountAmount;
                }

                Subtotal = Math.Round(sub, 2);
                DiscountTotal = Math.Round(disc, 2);
                Total = Math.Max(0.0, Math.Round(sub - disc, 2));
                Tax = Math.Round(Total - (Total / 1.16), 2);

                // Broadcast Reactive Snapshot to Secondary Customer Display
                var snapshot = new NextVent.Core.Messages.CartStateSnapshotMessage(
                    Items: CartItems.ToList().AsReadOnly(),
                    Subtotal: Subtotal,
                    TotalDiscount: DiscountTotal,
                    GrandTotal: Total,
                    LastAddedProductName: CartItems.LastOrDefault()?.Name ?? string.Empty
                );
                WeakReferenceMessenger.Default.Send(snapshot);
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.CustomerDisplayIdleStateMessage(IsIdle: CartItems.Count == 0));
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                double sub = CartItems.Sum(i => (i.OriginalUnitPrice > 0 ? i.OriginalUnitPrice : i.UnitPrice) * i.Quantity);
                double disc = CartItems.Sum(i => i.AppliedDiscountAmount);
                Subtotal = Math.Round(sub, 2);
                DiscountTotal = Math.Round(disc, 2);
                Total = Math.Max(0.0, Math.Round(sub - disc, 2));
                Tax = Math.Round(Total - (Total / 1.16), 2);

                // Broadcast Reactive Snapshot to Secondary Customer Display
                var snapshot = new NextVent.Core.Messages.CartStateSnapshotMessage(
                    Items: CartItems.ToList().AsReadOnly(),
                    Subtotal: Subtotal,
                    TotalDiscount: DiscountTotal,
                    GrandTotal: Total,
                    LastAddedProductName: CartItems.LastOrDefault()?.Name ?? string.Empty
                );
                WeakReferenceMessenger.Default.Send(snapshot);
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.CustomerDisplayIdleStateMessage(IsIdle: CartItems.Count == 0));
            });
        }
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private async Task SaveDraftCartAsync()
    {
        try
        {
            if (CartItems == null || CartItems.Count == 0)
            {
                if (File.Exists(_draftCartPath)) File.Delete(_draftCartPath);
                return;
            }

            var json = JsonSerializer.Serialize(
                CartItems.ToList(),
                NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListCartItemDto);
            await File.WriteAllTextAsync(_draftCartPath, json);
        }
        catch
        {
            // SILENT FAIL: Do NOT let auto-save crashes interrupt the cashier workflow.
            // The UI must remain clean and unblocked.
        }
    }

    private async Task RehydrateDraftCartAsync()
    {
        if (File.Exists(_draftCartPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_draftCartPath);
                var recoveredItems = JsonSerializer.Deserialize(
                    json,
                    NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListCartItemDto);

                if (recoveredItems != null && recoveredItems.Count > 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        foreach (var item in recoveredItems)
                        {
                            CartItems.Add(item);
                        }
                        await RecalculateCartPromotionsAsync();
                    });
                }
            }
            catch
            {
                try { File.Delete(_draftCartPath); } catch {}
            }
        }
    }
}
