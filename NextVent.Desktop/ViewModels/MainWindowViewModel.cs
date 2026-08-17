using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

    [ObservableProperty] private bool _isLocked = false;
    [ObservableProperty] private string _unlockPin = string.Empty;
    [ObservableProperty] private string _unlockErrorMessage = string.Empty;

    public ObservableObject CurrentView
    {
        get => ActiveViewModel;
        set => ActiveViewModel = value;
    }

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
    private readonly IPrintDispatcherService _printDispatcherService;
    private readonly IGiftcardService _giftcardService;
    private readonly AppDbContext _db;
    private readonly IShiftService _shiftService;
    private readonly ISessionManager _sessionManager;
    private readonly IUserRepository _userRepository;
    private readonly NextVent.Services.Interfaces.IBackupService _backupService;
    private readonly IAttendanceService _attendanceService;
    private readonly SatBillingQueueService _satBillingQueue;
    private readonly IUserService _authService;
    private readonly IAuditService _auditService;
    private readonly DeviceRegistrationService _deviceRegistrationService;

    public MainWindowViewModel()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(appDataFolder, "NextVent", "Database");
        if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
        string dbPath = Path.Combine(appFolder, "nextvent.db");

        string securePassword = NextVent.Services.Security.SecurityManager.GetMasterKey();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath};Password={securePassword};Cache=Shared;Mode=ReadWriteCreate;")
            .AddInterceptors(new NextVent.Data.Interceptors.SlowQueryInterceptor())
            .Options;

        _db = new AppDbContext(options);
        var _dbContextFactory = new NextVent.Data.AppDbContextFactoryImpl<AppDbContext>(options);

        var auditOptions = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite($"Data Source={Path.Combine(appFolder, "audit_logs.db")};")
            .Options;
        var _auditContextFactory = new NextVent.Data.AppDbContextFactoryImpl<AuditDbContext>(auditOptions);

        var _auditArchivingWorker = new NextVent.Services.Implementations.AuditArchivingWorker(_auditContextFactory);
        _ = _auditArchivingWorker.StartAsync(System.Threading.CancellationToken.None);

        var _coOccurrenceQueue = new NextVent.Services.Implementations.CoOccurrenceQueue();
        var _coOccurrenceWorker = new NextVent.Services.Implementations.CoOccurrenceWorker(_coOccurrenceQueue, _dbContextFactory);
        _ = _coOccurrenceWorker.StartAsync(System.Threading.CancellationToken.None);

        var _alertWorker = new NextVent.Services.Implementations.AlertBackgroundWorker(_dbContextFactory);
        _ = _alertWorker.StartAsync(System.Threading.CancellationToken.None);

        _printerService = new EscPosPrinterService();
        _backupService = new NextVent.Services.Implementations.BackupService();
        _satBillingQueue = new SatBillingQueueService();
        _ = _satBillingQueue.StartAsync(System.Threading.CancellationToken.None);

        _productService = new ProductService(_db);
        _customerService = new CustomerService(_db, _printerService);
        _authService = new UserService(_db);
        _auditService = new NextVent.Services.Audit.AuditService(_auditContextFactory);
        _saleService = new SaleService(_dbContextFactory, _coOccurrenceQueue, _auditService);
        _promotionService = new PromotionService(_db);
        _giftcardService = new GiftcardService(_db);
        var supplierService = new SupplierService(_db);
        var purchaseService = new PurchaseService(_db);
        var expenseService = new ExpenseService(_db, _printerService);
        var userService = new UserService(_db);
        var settingsService = new SettingsService(_db);
        var terminalService = new MercadoPagoTerminalService(new System.Net.Http.HttpClient(), settingsService);
        var shiftNoteService = new ShiftNoteService(_db);
        var kitService = new ItemKitService(_db);
        var predictiveService = new PredictiveIntelligenceService(_dbContextFactory);

        var certificateGenerator = new CertificateGeneratorService();
        _printDispatcherService = new PrintDispatcherService(_printerService, certificateGenerator, _dbContextFactory, settingsService);

        var userRepository = new UserRepository(_db);
        var sessionManager = new SessionManager();
        _sessionManager = sessionManager;
        
        _deviceRegistrationService = new DeviceRegistrationService(settingsService, _sessionManager);
        _sessionManager.LockStateChanged += (locked) =>
        {
            IsLocked = locked;
        };
        _sessionManager.CashierChanged += (user) =>
        {
            _ = _deviceRegistrationService.PingServerAsync(new BusinessProfile());
        };
        IsLocked = _sessionManager.IsTerminalLocked;

        var securityService = new SecurityInterceptionService(userRepository);
        var attendanceService = new AttendanceService(_db);
        _attendanceService = attendanceService;
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

        var externalCatalogService = new ExternalCatalogService(new System.Net.Http.HttpClient());

        _posVm = new PosViewModel(_productService, _db, shiftNoteService, kitService, _customerService, sessionManager, userRepository, _promotionService, _auditService, attendanceService, predictiveService, externalCatalogService);
        _inventoryVm = new InventoryViewModel(_productService, externalCatalogService, purchaseService, predictiveService);
        _customersVm = new CustomersViewModel(_customerService);
        _historyVm = new HistoryViewModel(_saleService, _printerService);
        _promotionsVm = new PromotionsViewModel(_promotionService);
        _fiscalVm = new FiscalViewModel();
        _cashierPerformanceVm = new CashierPerformanceViewModel(performanceAnalyticsService, attendanceService);
        _settingsVm = new SettingsViewModel(userService, settingsService);
        _ = _settingsVm.LoadSavedSettingsAsync();
        _suppliersVm = new SuppliersViewModel(supplierService, purchaseService, _productService);
        _expensesVm = new ExpensesViewModel(expenseService);
        _userRepository = userRepository;

        _satBillingQueue = new SatBillingQueueService();
        _ = _satBillingQueue.StartAsync(System.Threading.CancellationToken.None);

        var dialogService = new DialogService(async (vmObj) =>
        {
            if (vmObj is string str && str == "LOCK_SCREEN")
            {
                sessionManager.LockTerminal();
                return null;
            }
            ActiveDialogViewModel = vmObj as ObservableObject;
            IsDialogOverlayOpen = true;
            return null;
        }, () =>
        {
            CloseDialog();
        });

        _loginVm = new LoginViewModel(authService, sessionManager, dialogService);
        _loginVm.LoginSuccessful += async () =>
        {
            ActiveViewModel = _posVm;
            _posVm.RegisterMessages();
            
            // Ping Telemetry on Login
            _ = _deviceRegistrationService.PingServerAsync(new BusinessProfile());

            _ = _posVm.LoadProductsAsync();
            await ValidateShiftStatusAsync();
            WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
        };

        // ── Wire Dynamic Sidebar Layout Changes ──
        ThemeService.Instance.SidebarPositionChanged += pos => Dispatcher.UIThread.Post(() => ApplySidebarLayout(pos));

        // ── Wire Session & Header Commands ──
        _posVm.OpenSwitchUserPinRequested += () =>
        {
            var dialog = new SwitchUserPinDialogViewModel(userRepository, sessionManager, () =>
            {
                CloseDialog();
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
            });
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.OpenLockScreenRequested += () =>
        {
            var dialog = new LockScreenDialogViewModel(userRepository, sessionManager, () =>
            {
                CloseDialog();
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
            });
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.OpenSupervisorPinRequested += (title, callback) =>
        {
            var dialog = new SupervisorPinDialogViewModel(userRepository, title, (authorized, user) =>
            {
                CloseDialog();
                callback?.Invoke(authorized);
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
            });
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        // Temporary fix: ShowAlertRequested might not be wired in PosViewModel
        // If we really need it, we should bubble it from Catalog/Cart/Header
        // But let's skip for now or wire it to Cart / Catalog if added.

        _posVm.Catalog.OpenProductDialogWithParamsRequested += (parameters) =>
        {
            var dialog = new ProductDialogViewModel(_productService, _db, _sessionManager, _auditService);
            dialog.LoadFromParameters(parameters);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _inventoryVm.LoadProductsAsync();
                _ = _posVm.LoadProductsAsync();
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        // ── Wire POS Dialog & Fullscreen / Logout Events ──
        _posVm.OpenCheckoutRequested += () =>
        {
            var dialog = new CheckoutDialogViewModel(
                _saleService, _customerService, _printDispatcherService, terminalService,
                _posVm.Cart.CartState.Items.ToList(), _posVm.Cart.CartState.Total,
                async () =>
                {
                    _posVm.Cart.ClearCartCommand.Execute(null);
                    await _posVm.LoadProductsAsync();
                    await _posVm.Cart.LoadCustomersAsync();
                    await _historyVm.LoadSalesAsync();
                    await _historyVm.LoadCashierPerformanceAsync();
                },
                _giftcardService,
                preselectedCustomer: _posVm.Cart.SelectedCustomer);

            dialog.RequestClose += () =>
            {
                CloseDialog();
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                dialog.PaymentMethod = _posVm.Cart.InitialPaymentMode;
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        };

        _promotionsVm.OpenCreateItemKitRequested += () =>
        {
            var dialog = new ItemKitDialogViewModel(kitService, _productService);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _posVm.LoadProductsAsync();
                _ = _promotionsVm.LoadPromotionsAsync();
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
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
                _ = _posVm.Header.LoadActiveShiftNotesAsync();
                WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.ToggleFullscreenRequested += () => ToggleFullscreenRequested?.Invoke();
        _posVm.LogoutRequested += () => 
        {
            ActiveViewModel = _loginVm;
            // Ping Telemetry on Logout
            _ = _deviceRegistrationService.PingServerAsync(new BusinessProfile());
        };

        // ── Wire History Cashup & Return Dialog Events ──
        _historyVm.OpenCashupRequested += () =>
        {
            var dialog = new CashupDialogViewModel(_db, _shiftService, _sessionManager, _printerService, _backupService, attendanceService: _attendanceService);
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

        _historyVm.OpenSupervisorPinRequested += (title, callback) =>
        {
            var dialog = new SupervisorPinDialogViewModel(userRepository, title, (authorized, user) =>
            {
                CloseDialog();
                callback?.Invoke(authorized);
            });
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        // ── Wire Inventory Add & Edit Product Dialog Events ──
        _inventoryVm.OpenAddProductRequested += () =>
        {
            var dialog = new ProductDialogViewModel(_productService, _db, _sessionManager, _auditService);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _inventoryVm.LoadProductsAsync();
                _ = _posVm.LoadProductsAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _inventoryVm.OpenProductDialogWithParamsRequested += (parameters) =>
        {
            var dialog = new ProductDialogViewModel(_productService, _db, _sessionManager, _auditService);
            dialog.LoadFromParameters(parameters);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _inventoryVm.LoadProductsAsync();
                _ = _posVm.LoadProductsAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _inventoryVm.OpenEditProductRequested += (product) =>
        {
            var dialog = new ProductDialogViewModel(_productService, _db, _sessionManager, _auditService);
            dialog.LoadProductForEdit(product);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _inventoryVm.LoadProductsAsync();
                _ = _posVm.LoadProductsAsync();
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _inventoryVm.OpenConfigureLowStockRequested += () =>
        {
            double currentVal = NextVent.ViewModels.InventoryViewModel.LoadDefaultMinStock();
            var dialog = new NextVent.ViewModels.Dialogs.ConfigureMinStockDialogViewModel(currentVal);
            dialog.Saved += (newVal) =>
            {
                NextVent.ViewModels.InventoryViewModel.SaveDefaultMinStock(newVal);
                _inventoryVm.ShowOnlyLowStock = true;
                _ = _inventoryVm.LoadProductsAsync();
            };
            dialog.RequestClose += () => CloseDialog();
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _inventoryVm.OpenManageCategoriesRequested += () =>
        {
            var dialog = new NextVent.ViewModels.Dialogs.ManageCategoriesDialogViewModel(_db);
            dialog.CategoriesUpdated += () =>
            {
                _ = _inventoryVm.LoadProductsAsync();
                _ = _posVm.LoadProductsAsync();
            };
            dialog.RequestClose += () => CloseDialog();
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

        _customersVm.OpenEditCustomerRequested += (customer) =>
        {
            var dialog = new CustomerDialogViewModel(_customerService);
            dialog.LoadForEdit(customer);
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

        _activeViewModel = _loginVm;
        _ = InitializeApplicationStateAsync();

        WeakReferenceMessenger.Default.Register<ForceLogoutMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                ActiveViewModel = _loginVm;
                CloseDialog();
            });
        });

        WeakReferenceMessenger.Default.Register<NextVent.Core.Messages.UserDeletedMessage>(this, (r, m) =>
        {
            if (_sessionManager.CurrentCashier?.Id.ToString() == m.UserId)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    WeakReferenceMessenger.Default.Send(new ForceLogoutMessage());
                });
            }
        });
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
            _posVm.Cart.CheckoutCommand.Execute("Efectivo");
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
        if (ActiveViewModel == _loginVm || 
            IsDialogOverlayOpen || 
            ActiveViewModel is LicenseLockedViewModel || 
            ActiveViewModel is FirstTimeSetupViewModel)
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
            WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.FocusSearchMessage());
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
            if (DateTime.TryParse(activeShift.StartTime, out var startTime))
            {
                var localStartTime = startTime.ToLocalTime();
                if (localStartTime.Date < DateTime.Today)
                {
                // Orphaned Shift Recovery (Z-Cut Ciego)
                var confirmVm = new ConfirmDialogViewModel(
                    "Turno Suspendido Detectado",
                    "Se detectó un turno del día anterior que no fue cerrado correctamente. Debe realizar el Corte de Caja Z antes de iniciar uno nuevo. ¿Proceder al corte ciego?",
                    (confirmed) =>
                    {
                        if (confirmed)
                        {
                            var blindCashupVm = new CashupDialogViewModel(_db, _shiftService, _sessionManager, _printerService, _backupService, isFinalZCut: true, isBlindMode: true, attendanceService: _attendanceService);
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
        }
        else
        {
            // Open Shift Gating
            var openShiftVm = new OpenShiftDialogViewModel(_shiftService, _attendanceService, _sessionManager);
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

    public async Task InitializeApplicationStateAsync()
    {
        var licenseService = new NextVent.Services.Security.LicenseEnforcementService();
        if (licenseService.IsSystemLocked())
        {
            // KILL SWITCH ACTIVADO
            ActiveViewModel = new LicenseLockedViewModel();
            return;
        }

        bool hasUsers = await _userRepository.HasAnyUsersAsync();

        if (!hasUsers)
        {
            // Route to First-Time Setup (OOBE)
            ActiveViewModel = new FirstTimeSetupViewModel(_userRepository, new DialogService(async (vm) => { return null; }, () => {}), () =>
            {
                ActiveViewModel = _loginVm;
            });
        }
        else
        {
            // Route to standard Login
            ActiveViewModel = _loginVm;
        }
    }

    public async Task<int> GetIdleTimeoutMinutesAsync()
    {
        var settingsService = new SettingsService(_db);
        var val = await settingsService.GetAsync("IdleTimeoutMinutes");
        if (int.TryParse(val, out var minutes) && minutes > 0)
        {
            return minutes;
        }
        return 5; // default 5 minutes
    }

    [RelayCommand]
    private async Task UnlockTerminalAsync()
    {
        UnlockErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(UnlockPin))
        {
            UnlockErrorMessage = "El PIN es obligatorio.";
            return;
        }

        var user = await _userRepository.ValidateAnyPinAsync(UnlockPin);
        if (user != null)
        {
            _sessionManager.UnlockTerminal();
            UnlockPin = string.Empty;
        }
        else
        {
            UnlockErrorMessage = "PIN incorrecto.";
        }
    }

    public async Task TriggerAutoLockAsync()
    {
        if (_sessionManager.CurrentCashier != null && !IsLocked)
        {
            var dialog = new LockScreenDialogViewModel(_userRepository, _sessionManager, CloseDialog);
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        }
        await Task.CompletedTask;
    }
}
