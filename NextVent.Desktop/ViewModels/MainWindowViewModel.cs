using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Services;
using NextVent.Services.Implementations;
using NextVent.Services.Interfaces;
using NextVent.Services.Audit;
using NextVent.Services.Auth;
using NextVent.Services.Security;
using NextVent.ViewModels.Dialogs;
using NextVent.Core.Models;
using NextVent.Core.Repositories;
using NextVent.Core.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private ObservableObject _activeViewModel;
    [ObservableProperty] private ObservableObject? _activeDialogViewModel = null;
    [ObservableProperty] private bool _isDialogOverlayOpen = false;

    [ObservableProperty] private string _sidebarDockPosition = "Left";
    [ObservableProperty] private double _sidebarWidth = 80;
    [ObservableProperty] private double _sidebarHeight = double.NaN;
    [ObservableProperty] private string _sidebarOrientation = "Vertical";

    public event Action? ToggleFullscreenRequested;

    private readonly PosViewModel _posVm;
    private readonly InventoryViewModel _inventoryVm;
    private readonly CustomersViewModel _customersVm;
    private readonly HistoryViewModel _historyVm;
    private readonly PromotionsViewModel _promotionsVm;
    private readonly FiscalViewModel _fiscalVm;
    private readonly SettingsViewModel _settingsVm;
    private readonly SuppliersViewModel _suppliersVm;
    private readonly ExpensesViewModel _expensesVm;
    private readonly CashierPerformanceViewModel _cashierPerformanceVm;
    private readonly LoginViewModel _loginVm;

    private readonly ISaleService _saleService;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IPromotionService _promotionService;
    private readonly IEscPosPrinterService _printerService;
    private readonly IGiftcardService _giftcardService;
    private readonly AppDbContext _db;
    private readonly IShiftService _shiftService;
    private readonly ISessionManager _sessionManager;

    public MainWindowViewModel()
    {
        var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        _db = new AppDbContext(options);

        _printerService = new EscPosPrinterService();
        _productService = new ProductService(_db);
        _customerService = new CustomerService(_db, _printerService);
        _saleService = new SaleService(_db);
        _promotionService = new PromotionService(_db);
        _giftcardService = new GiftcardService(_db);
        var supplierService = new SupplierService(_db);
        var purchaseService = new PurchaseService(_db);
        var expenseService = new ExpenseService(_db, _printerService);
        var userService = new UserService(_db);
        var settingsService = new SettingsService(_db);
        var shiftNoteService = new ShiftNoteService(_db);
        var kitService = new ItemKitService(_db);

        var userRepository = new UserRepository(_db);
        var sessionManager = new SessionManager();
        _sessionManager = sessionManager;
        var auditService = new AuditService(_db);
        var securityService = new SecurityInterceptionService(userRepository);
        var attendanceService = new AttendanceService(_db);
        var performanceAnalyticsService = new PerformanceAnalyticsService(_db);
        var authService = new AuthService(userService);
        var shiftService = new ShiftService(_db);
        _shiftService = shiftService;

        securityService.RequestSupervisorPinDialog += (title, callback) =>
        {
            var dialog = new SupervisorPinDialogViewModel(userRepository, title, callback);
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm = new PosViewModel(_productService, _db, shiftNoteService, kitService, _customerService, sessionManager, userRepository, _promotionService, auditService, attendanceService);
        _inventoryVm = new InventoryViewModel(_productService, purchaseService);
        _customersVm = new CustomersViewModel(_customerService);
        _historyVm = new HistoryViewModel(_saleService, _printerService);
        _promotionsVm = new PromotionsViewModel(_promotionService);
        _fiscalVm = new FiscalViewModel();
        _cashierPerformanceVm = new CashierPerformanceViewModel(performanceAnalyticsService, attendanceService);
        _settingsVm = new SettingsViewModel(userService, settingsService);
        _ = _settingsVm.LoadSavedSettingsAsync();
        _suppliersVm = new SuppliersViewModel(supplierService, purchaseService, _productService);
        _expensesVm = new ExpensesViewModel(expenseService);
        _loginVm = new LoginViewModel(authService);
        _loginVm.LoginSuccessful += async () =>
        {
            ActiveViewModel = _posVm;
            await ValidateShiftStatusAsync();
        };

        // ── Wire Dynamic Sidebar Layout Changes ──
        ThemeService.Instance.SidebarPositionChanged += pos => Dispatcher.UIThread.Post(() => ApplySidebarLayout(pos));

        // ── Wire Session & Header Commands ──
        _posVm.OpenSwitchUserPinRequested += () =>
        {
            var dialog = new SwitchUserPinDialogViewModel(userRepository, sessionManager, CloseDialog);
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.OpenLockScreenRequested += () =>
        {
            var dialog = new LockScreenDialogViewModel(userRepository, sessionManager, CloseDialog);
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.OpenSupervisorPinRequested += (title, callback) =>
        {
            var dialog = new SupervisorPinDialogViewModel(userRepository, title, (authorized, user) =>
            {
                CloseDialog();
                callback?.Invoke(authorized);
            });
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        // ── Wire POS Dialog & Fullscreen / Logout Events ──
        _posVm.OpenCheckoutRequested += () =>
        {
            var dialog = new CheckoutDialogViewModel(
                _saleService, _customerService, _printerService,
                _posVm.CartItems.ToList(), _posVm.Total,
                async () =>
                {
                    _posVm.ClearCart();
                    await _posVm.LoadProductsAsync();
                    await _historyVm.LoadSalesAsync();
                    await _historyVm.LoadCashierPerformanceAsync();
                },
                _giftcardService);

            dialog.RequestClose += CloseDialog;
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.OpenCreateItemKitRequested += () =>
        {
            var dialog = new ItemKitDialogViewModel(kitService, _productService);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _posVm.LoadProductsAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.OpenShiftNotesRequested += () =>
        {
            var dialog = new ShiftNotesDialogViewModel(shiftNoteService);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _posVm.LoadActiveShiftNotesAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.ToggleFullscreenRequested += () => ToggleFullscreenRequested?.Invoke();
        _posVm.LogoutRequested += () => ActiveViewModel = _loginVm;

        // ── Wire History Cashup & Return Dialog Events ──
        _historyVm.OpenCashupRequested += () =>
        {
            var dialog = new CashupDialogViewModel(_db);
            dialog.RequestClose += CloseDialog;
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _historyVm.OpenReturnRequested += (sale) =>
        {
            var dialog = new ReturnDialogViewModel(_saleService, sale);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _historyVm.LoadSalesAsync();
                _ = _historyVm.LoadCashierPerformanceAsync();
                _ = _posVm.LoadProductsAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        // ── Wire Inventory Add Product Dialog Event ──
        _inventoryVm.OpenAddProductRequested += () =>
        {
            var dialog = new ProductDialogViewModel(_productService);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _inventoryVm.LoadProductsAsync();
                _ = _posVm.LoadProductsAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        // ── Wire Customers Add Customer, Payment & Statement Dialog Events ──
        _customersVm.OpenAddCustomerRequested += () =>
        {
            var dialog = new CustomerDialogViewModel(_customerService);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _customersVm.LoadCustomersAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _customersVm.OpenAddPaymentRequested += (customer) =>
        {
            var dialog = new PaymentDialogViewModel(_customerService, customer.Id, customer.Debt);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _customersVm.LoadCustomersAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _customersVm.OpenStatementRequested += (customer) =>
        {
            var dialog = new CustomerStatementDialogViewModel(_db, customer.Id, customer.Name, customer.Rfc, customer.Debt);
            dialog.RequestClose += CloseDialog;
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        // ── Wire Promotions Add Promotion Dialog Event ──
        _promotionsVm.OpenAddPromotionRequested += () =>
        {
            var dialog = new PromotionDialogViewModel(_promotionService);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _promotionsVm.LoadPromotionsAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _activeViewModel = _posVm;
        _ = _posVm.LoadProductsAsync();
        _ = ValidateShiftStatusAsync();
    }

    private void ApplySidebarLayout(string position)
    {
        switch (position)
        {
            case "Derecha":
                SidebarDockPosition = "Right";
                SidebarWidth = 80;
                SidebarHeight = double.NaN;
                SidebarOrientation = "Vertical";
                break;
            case "Arriba (Top Bar)":
            case "Arriba":
            case "Arriba (Banner)":
            case "Barra Superior Flotante":
                SidebarDockPosition = "Top";
                SidebarWidth = double.NaN;
                SidebarHeight = 64;
                SidebarOrientation = "Horizontal";
                break;
            case "Abajo (Bottom Bar)":
            case "Abajo":
            case "Abajo (Footer)":
                SidebarDockPosition = "Bottom";
                SidebarWidth = double.NaN;
                SidebarHeight = 64;
                SidebarOrientation = "Horizontal";
                break;
            case "Izquierda":
            default:
                SidebarDockPosition = "Left";
                SidebarWidth = 80;
                SidebarHeight = double.NaN;
                SidebarOrientation = "Vertical";
                break;
        }
    }

    [RelayCommand]
    private void TriggerPosCheckout()
    {
        if (ActiveViewModel == _posVm)
        {
            _posVm.OpenCheckoutDialogCommand.Execute(null);
        }
    }

    private void CloseDialog()
    {
        ActiveDialogViewModel = null;
        IsDialogOverlayOpen = false;
    }

    [RelayCommand]
    private async Task NavigateTo(string target)
    {
        if (ActiveViewModel == _loginVm || IsDialogOverlayOpen)
        {
            return;
        }

        ActiveViewModel = target.ToLower() switch
        {
            "pos" => _posVm,
            "inventory" => _inventoryVm,
            "customers" => _customersVm,
            "suppliers" => _suppliersVm,
            "proveedores" => _suppliersVm,
            "expenses" => _expensesVm,
            "gastos" => _expensesVm,
            "history" => _historyVm,
            "promotions" => _promotionsVm,
            "fiscal" => _fiscalVm,
            "settings" => _settingsVm,
            "performance" => _cashierPerformanceVm,
            "rendimiento" => _cashierPerformanceVm,
            "cashierperformance" => _cashierPerformanceVm,
            _ => _posVm
        };

        if (ActiveViewModel == _posVm)
        {
            _ = _posVm.LoadProductsAsync();
            await ValidateShiftStatusAsync();
        }
        else if (ActiveViewModel == _inventoryVm) _ = _inventoryVm.LoadProductsAsync();
        else if (ActiveViewModel == _customersVm) _ = _customersVm.LoadCustomersAsync();
        else if (ActiveViewModel == _historyVm) _ = _historyVm.LoadSalesAsync();
        else if (ActiveViewModel == _promotionsVm) _ = _promotionsVm.LoadPromotionsAsync();
        else if (ActiveViewModel == _suppliersVm) _ = _suppliersVm.LoadDataAsync();
        else if (ActiveViewModel == _expensesVm) _ = _expensesVm.LoadExpensesAsync();
        else if (ActiveViewModel == _settingsVm) _ = _settingsVm.LoadUsersAsync();
        else if (ActiveViewModel == _cashierPerformanceVm) _ = _cashierPerformanceVm.LoadReportsAsync();
    }

    private async Task ValidateShiftStatusAsync()
    {
        var activeShift = await _shiftService.GetActiveAsync();
        if (activeShift != null)
        {
            if (DateTime.TryParse(activeShift.StartTime, out var startTime) && startTime.Date < DateTime.UtcNow.Date)
            {
                // Orphaned Shift Recovery (Z-Cut Ciego)
                var confirmVm = new ConfirmDialogViewModel(
                    "Turno Suspendido Detectado",
                    "Se detectó un turno del día anterior que no fue cerrado correctamente. Debe realizar el Corte de Caja Z antes de iniciar uno nuevo. ¿Proceder al corte ciego?",
                    (confirmed) =>
                    {
                        if (confirmed)
                        {
                            var blindCashupVm = new CashupDialogViewModel(_db, _shiftService, isFinalZCut: true, isBlindMode: true);
                            blindCashupVm.RequestClose += () =>
                            {
                                CloseDialog();
                                _ = ValidateShiftStatusAsync();
                            };
                            ActiveDialogViewModel = blindCashupVm;
                            IsDialogOverlayOpen = true;
                        }
                        else
                        {
                            CloseDialog();
                            ActiveViewModel = _loginVm;
                        }
                    }
                );
                ActiveDialogViewModel = confirmVm;
                IsDialogOverlayOpen = true;
            }
        }
        else
        {
            // Open Shift Gating
            var openShiftVm = new OpenShiftDialogViewModel(_shiftService);
            openShiftVm.RequestClose += (success) =>
            {
                CloseDialog();
                if (!success)
                {
                    _sessionManager.SwitchCashier(null!);
                    ActiveViewModel = _loginVm;
                }
            };
            ActiveDialogViewModel = openShiftVm;
            IsDialogOverlayOpen = true;
        }
    }
}