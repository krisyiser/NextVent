using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Services;
using Ticketfy.Services.Implementations;
using Ticketfy.Services.Interfaces;
using Ticketfy.Services.Audit;
using Ticketfy.Services.Auth;
using Ticketfy.Services.Security;
using Ticketfy.ViewModels.Dialogs;
using Ticketfy.Core.Models;
using Ticketfy.Core.Repositories;
using Ticketfy.Core.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private ObservableObject _activeViewModel;
    [ObservableProperty] private ObservableObject? _activeDialogViewModel = null;
    [ObservableProperty] private bool _isDialogOverlayOpen = false;

    [ObservableProperty]
    private bool _isSplashScreenVisible = true;

    [ObservableProperty] private bool _isLocked = false;
    [ObservableProperty] private string _unlockPin = string.Empty;
    [ObservableProperty] private string _unlockErrorMessage = string.Empty;

    [ObservableProperty] private bool _isUpdateAvailable = false;
    [ObservableProperty] private bool _isUpdateReady = false;
    [ObservableProperty] private double _updateProgress = 0;
    
    // Auto-Updater Feedback Properties
    [ObservableProperty] private bool _isUpdateUpToDate = false;
    [ObservableProperty] private bool _isUpdateFailed = false;
    [ObservableProperty] private string _updateErrorMessage = string.Empty;
    
    public ObservableObject CurrentView
    {
        get => ActiveViewModel;
        set => ActiveViewModel = value;
    }

    public string CurrentAppVersion => Ticketfy.Core.Helpers.AppVersionHelper.DisplayVersion;
    public string FullAppVersionTitle => Ticketfy.Core.Helpers.AppVersionHelper.FullTitle;

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
    private readonly Ticketfy.Services.Interfaces.IBackupService _backupService;
    private readonly IAttendanceService _attendanceService;
    private readonly SatBillingQueueService _satBillingQueue;
    private readonly IUserService _authService;
    private readonly IAuditService _auditService;
    private readonly DeviceRegistrationService _deviceRegistrationService;
    private readonly AutoUpdateService _autoUpdateService;
    private readonly Ticketfy.Services.Interfaces.ITutorialService _tutorialService;

    /// <summary>Currently active tutorial overlay (Sidebar tour or per-Module tour).</summary>
    [ObservableProperty] private TutorialOverlayViewModel _activeTutorialVm = null!;

    public MainWindowViewModel()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(appDataFolder, "ticketfy", "Database");
        if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
        string dbPath = Path.Combine(appFolder, "ticketfy.db");

        string securePassword = Ticketfy.Services.Security.SecurityManager.GetMasterKey();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath};Password={securePassword};Cache=Shared;Mode=ReadWriteCreate;")
            .AddInterceptors(new Ticketfy.Data.Interceptors.SlowQueryInterceptor())
            .Options;

        _db = new AppDbContext(options);
        var _dbContextFactory = new Ticketfy.Data.AppDbContextFactoryImpl<AppDbContext>(options);

        var auditOptions = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite($"Data Source={Path.Combine(appFolder, "audit_logs.db")};")
            .Options;
        var _auditContextFactory = new Ticketfy.Data.AppDbContextFactoryImpl<AuditDbContext>(auditOptions);

        var _auditArchivingWorker = new Ticketfy.Services.Implementations.AuditArchivingWorker(_auditContextFactory);
        _ = _auditArchivingWorker.StartAsync(System.Threading.CancellationToken.None);

        var _coOccurrenceQueue = new Ticketfy.Services.Implementations.CoOccurrenceQueue();
        var _coOccurrenceWorker = new Ticketfy.Services.Implementations.CoOccurrenceWorker(_coOccurrenceQueue, _dbContextFactory);
        _ = _coOccurrenceWorker.StartAsync(System.Threading.CancellationToken.None);

        var _alertWorker = new Ticketfy.Services.Implementations.AlertBackgroundWorker(_dbContextFactory);
        _ = _alertWorker.StartAsync(System.Threading.CancellationToken.None);

        _printerService = new EscPosPrinterService();
        _backupService = new Ticketfy.Services.Implementations.BackupService();
        _satBillingQueue = new SatBillingQueueService();
        _ = _satBillingQueue.StartAsync(System.Threading.CancellationToken.None);

        _productService = new ProductService(_db);
        _customerService = new CustomerService(_db, _printerService);
        _authService = new UserService(_db);
        _auditService = new Ticketfy.Services.Audit.AuditService(_auditContextFactory);
        _saleService = new SaleService(_dbContextFactory, _coOccurrenceQueue, _auditService);
        _promotionService = new PromotionService(_db);
        _giftcardService = new GiftcardService(_db);
        var supplierService = new SupplierService(_db);
        var purchaseService = new PurchaseService(_db);
        var expenseService = new ExpenseService(_db, _printerService);
        var userService = new UserService(_db);
        var settingsService = new SettingsService(_db);
        _tutorialService = new Ticketfy.Services.Implementations.TutorialService(settingsService);
        var sidebarTutorialVm = new TutorialOverlayViewModel(_tutorialService, "Sidebar");
        sidebarTutorialVm.TutorialCompleted += () => { };
        ActiveTutorialVm = sidebarTutorialVm;
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
        _historyVm = new HistoryViewModel(_saleService, _printerService, _db, settingsService);
        _promotionsVm = new PromotionsViewModel(_promotionService);
        var facturamaService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IFacturamaService>(App.Current!.Services!);
        _fiscalVm = new FiscalViewModel(_saleService, facturamaService);
        _cashierPerformanceVm = new CashierPerformanceViewModel(performanceAnalyticsService, attendanceService);
        _settingsVm = new SettingsViewModel(userService, settingsService);
        _suppliersVm = new SuppliersViewModel(supplierService, purchaseService, _productService, _printerService);
        _expensesVm = new ExpensesViewModel(expenseService, shiftService);
        _userRepository = userRepository;

        _satBillingQueue = new SatBillingQueueService();
        _ = _satBillingQueue.StartAsync(System.Threading.CancellationToken.None);

        _autoUpdateService = new AutoUpdateService();
        _autoUpdateService.UpdateAvailableEvent += () => IsUpdateAvailable = true;
        _autoUpdateService.DownloadProgressChangedEvent += (progress) => UpdateProgress = progress;
        _autoUpdateService.UpdateReadyToInstallEvent += () =>
        {
            IsUpdateAvailable = false;
            IsUpdateReady = true;
        };
        _autoUpdateService.UpdateFailedEvent += (msg) =>
        {
            IsUpdateAvailable = false;
            IsUpdateFailed = true;
            UpdateErrorMessage = $"Fallo al buscar actualizaciones: {msg}";
            Task.Delay(5000).ContinueWith(_ => Dispatcher.UIThread.Post(() => IsUpdateFailed = false));
        };
        _autoUpdateService.UpdateUpToDateEvent += () =>
        {
            IsUpdateUpToDate = true;
            Task.Delay(3000).ContinueWith(_ => Dispatcher.UIThread.Post(() => IsUpdateUpToDate = false));
        };
        _autoUpdateService.StartPeriodicChecks(TimeSpan.FromHours(4));

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
            WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());

            // Launch Sidebar Tour on first login ever
            _ = ActiveTutorialVm.TryStartAsync(BuildSidebarTourSteps());
        };

        // ── Wire Dynamic Sidebar Layout Changes ──
        ThemeService.Instance.SidebarPositionChanged += pos => Dispatcher.UIThread.Post(() => ApplySidebarLayout(pos));

        // ── Wire Session & Header Commands ──
        _posVm.OpenSwitchUserPinRequested += () =>
        {
            var dialog = new SwitchUserPinDialogViewModel(userRepository, sessionManager, () =>
            {
                CloseDialog();
                WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());
            });
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.OpenLockScreenRequested += () =>
        {
            var dialog = new LockScreenDialogViewModel(userRepository, sessionManager, () =>
            {
                CloseDialog();
                WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());
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
                WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());
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
                WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());
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
                WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());
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
                WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());
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
                WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());
            };
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.ToggleFullscreenRequested += () => ToggleFullscreenRequested?.Invoke();
        _posVm.LogoutRequested += () => 
        {
            _loginVm.Username = string.Empty;
            _loginVm.Password = string.Empty;
            _loginVm.ErrorMessage = string.Empty;
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
            double currentVal = Ticketfy.ViewModels.InventoryViewModel.LoadDefaultMinStock();
            var dialog = new Ticketfy.ViewModels.Dialogs.ConfigureMinStockDialogViewModel(currentVal);
            dialog.Saved += (newVal) =>
            {
                Ticketfy.ViewModels.InventoryViewModel.SaveDefaultMinStock(newVal);
                _inventoryVm.ShowOnlyLowStock = true;
                _ = _inventoryVm.LoadProductsAsync();
            };
            dialog.RequestClose += () => CloseDialog();
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _inventoryVm.OpenManageCategoriesRequested += () =>
        {
            var dialog = new Ticketfy.ViewModels.Dialogs.ManageCategoriesDialogViewModel(_db);
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

        _posVm.OpenAddCustomerRequested += () =>
        {
            var dialog = new CustomerDialogViewModel(_customerService);
            dialog.RequestClose += () =>
            {
                CloseDialog();
                _ = _posVm.Cart.LoadCustomersAsync();
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

        // ── Wire POS Header X-Report (Parcial) & Z-Report (Final) Cashup ──
        _posVm.OpenPartialCashupRequested += () =>
        {
            var dialog = new CashupDialogViewModel(_db, _shiftService, _sessionManager, _printerService, _backupService, attendanceService: _attendanceService, isFinalZCut: false);
            dialog.RequestClose += CloseDialog;
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _posVm.OpenFinalCashupRequested += () =>
        {
            var dialog = new CashupDialogViewModel(_db, _shiftService, _sessionManager, _printerService, _backupService, attendanceService: _attendanceService, isFinalZCut: true);
            dialog.RequestClose += CloseDialog;
            ActiveDialogViewModel = dialog;
            IsDialogOverlayOpen = true;
        };

        _activeViewModel = _loginVm;
        _ = InitializeApplicationStateAsync();

        WeakReferenceMessenger.Default.Register<ForceLogoutMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _loginVm.Username = string.Empty;
                _loginVm.Password = string.Empty;
                _loginVm.ErrorMessage = string.Empty;
                ActiveViewModel = _loginVm;
                CloseDialog();
            });
        });

        WeakReferenceMessenger.Default.Register<Ticketfy.Core.Messages.UserDeletedMessage>(this, (r, m) =>
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
    private void ApplyUpdateAndRestart()
    {
        _autoUpdateService.ApplyUpdatesAndRestart();
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        // Indicate to user that we are checking via UI if needed, or simply trigger the silent check.
        // It will raise UpdateAvailableEvent if an update is found.
        IsUpdateFailed = false;
        IsUpdateUpToDate = false;
        await _autoUpdateService.CheckAndDownloadUpdatesAsync();
    }

    private void ExecuteLogout()
    {
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
            WeakReferenceMessenger.Default.Send(new Ticketfy.Core.Messages.FocusSearchMessage());
            _ = LaunchModuleTourAsync("Module.POS");
        }
        else if (ActiveViewModel == _inventoryVm)
        {
            _ = _inventoryVm.LoadProductsAsync();
            _ = LaunchModuleTourAsync("Module.Inventory");
        }
        else if (ActiveViewModel == _customersVm)
        {
            _ = _customersVm.LoadCustomersAsync();
            _ = LaunchModuleTourAsync("Module.Customers");
        }
        else if (ActiveViewModel == _historyVm)
        {
            _ = _historyVm.LoadSalesAsync();
            _ = LaunchModuleTourAsync("Module.History");
        }
        else if (ActiveViewModel == _promotionsVm)
        {
            _ = _promotionsVm.LoadPromotionsAsync();
            _ = LaunchModuleTourAsync("Module.Promotions");
        }
        else if (ActiveViewModel == _fiscalVm)
        {
            _ = _fiscalVm.LoadInvoicesCommand.ExecuteAsync(null);
        }
        else if (ActiveViewModel == _suppliersVm)
        {
            _ = _suppliersVm.LoadDataAsync();
            _ = LaunchModuleTourAsync("Module.Suppliers");
        }
        else if (ActiveViewModel == _expensesVm)
        {
            _ = _expensesVm.LoadExpensesAsync();
            _ = LaunchModuleTourAsync("Module.Expenses");
        }
        else if (ActiveViewModel == _settingsVm)
        {
            _ = _settingsVm.LoadUsersAsync();
            _ = LaunchModuleTourAsync("Module.Settings");
        }
        else if (ActiveViewModel == _cashierPerformanceVm)
        {
            _ = _cashierPerformanceVm.LoadReportsAsync();
        }
    }

    /// <summary>
    /// Reuses the window-level TutorialVm to display a per-module tour the first time the module is opened.
    /// </summary>
    private async Task LaunchModuleTourAsync(string moduleKey)
    {
        await Task.Delay(350); // Let the module view settle before showing the overlay
        var steps = BuildModuleTourSteps(moduleKey);
        if (steps.Count == 0) return;

        // Re-initialize TutorialVm with the new step key so it checks completion independently
        var freshVm = new TutorialOverlayViewModel(_tutorialService, moduleKey);
        freshVm.TutorialCompleted += () => { }; // no-op: no additional action after module tour
        await freshVm.TryStartAsync(steps);

        if (freshVm.IsVisible)
        {
            // Swap TutorialVm on the UI thread so MainWindow.axaml picks it up
            Dispatcher.UIThread.Post(() => ActiveTutorialVm = freshVm);
        }
    }


    private static List<Ticketfy.Core.Models.TutorialStep> BuildSidebarTourSteps()
    {
        return new()
        {
            new("📊 Ventas (POS)",
                "Aquí procesas tus ventas diarias, cobras a clientes y abres o cierras turnos de caja.",
                TargetName: "NavPosBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
            new("📦 Inventario",
                "Administra todo tu catálogo: agrega productos, actualiza precios y controla el stock.",
                TargetName: "NavInventoryBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
            new("👥 Clientes",
                "Gestiona clientes, consulta deudas a crédito y genera estados de cuenta.",
                TargetName: "NavCustomersBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
            new("🚚 Proveedores",
                "Registra tus proveedores y lleva el control de pedidos y compras de mercancía.",
                TargetName: "NavSuppliersBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
            new("💸 Gastos",
                "Registra gastos operativos (luz, renta, sueldos) y monitorea tu utilidad neta real.",
                TargetName: "NavExpensesBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
            new("📋 Historial",
                "Consulta todas las ventas anteriores, realiza devoluciones e historial de cortes.",
                TargetName: "NavHistoryBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
            new("🏷️ Promociones",
                "Crea descuentos automáticos, kits de productos y ofertas por tiempo limitado.",
                TargetName: "NavPromotionsBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
            new("⚙️ Ajustes",
                "Configura impresoras, usuarios, tema visual y parámetros del sistema.",
                TargetName: "NavSettingsBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
        };
    }

    private static List<Ticketfy.Core.Models.TutorialStep> BuildModuleTourSteps(string moduleKey)
    {
        return moduleKey switch
        {
            "Module.POS" => new()
            {
                new("👤 Botón de Usuario",
                    "Cambia de cajero activo, bloquea la terminal o realiza cortes de turno desde este menú.",
                    TargetName: "PosUserButton", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
                new("🔍 Buscador de Productos",
                    "Ingresa el código de barras, SKU o nombre para agregar productos al carrito al instante.",
                    TargetName: "PosSearchBorder", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
                new("🛒 Ticket de Venta",
                    "Aquí aparecen los productos agregados a la venta actual, sus cantidades, precios y el total a cobrar.",
                    TargetName: "PosCartSection", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Left),
                new("👥 Agregar Clientes",
                    "Selecciona o agrega un cliente para consultar su saldo a crédito o asignar la venta.",
                    TargetName: "PosCustomerSelector", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Left),
                new("⏸️ Pausar Compra",
                    "Pausa la venta actual para atender a otro cliente y reanúdala cuando desees.",
                    TargetName: "PosPauseButton", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
                new("📝 Notas del Turno",
                    "Registra recordatorios o avisos importantes entre cajeros durante el turno.",
                    TargetName: "PosNotesButton", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
            },
            "Module.Inventory" => new()
            {
                new("📋 Productos",
                    "Consulta la lista completa de tus artículos, precios, categoría y existencias de stock.",
                    TargetName: "InventoryDataGridBorder", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
                new("🔍 Buscador",
                    "Filtra y busca productos rápidamente por nombre, SKU o código de barras.",
                    TargetName: "InventorySearchBorder", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
                new("➕ Nuevo Producto",
                    "Registra nuevos artículos en el catálogo ingresando su precio, costo y stock inicial.",
                    TargetName: "AddProductBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
                new("💾 Copia de Seguridad",
                    "Genera un respaldo instantáneo del inventario y existencias actuales.",
                    TargetName: "InventoryBackupBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
                new("📜 Historial",
                    "Revisa el historial de respaldos y movimientos pasados del catálogo.",
                    TargetName: "InventoryHistoryBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
            },
            "Module.Customers" => new()
            {
                new("🔍 Buscador de Clientes",
                    "Busca rápidamente a cualquier cliente por su nombre o número de teléfono.",
                    TargetName: "CustomersSearchBorder", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
                new("👥 Directorio de Clientes",
                    "Consulta saldos pendientes, crédito disponible, abonos y estados de cuenta.",
                    TargetName: "CustomersDataGridBorder", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
                new("➕ Nuevo Cliente",
                    "Registra nuevos clientes ingresando su nombre, teléfono y límite de crédito.",
                    TargetName: "AddCustomerBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
            },
            "Module.History" => new()
            {
                new("📊 Área de Ventas y Rendimiento",
                    "Monitorea las ventas totales acumuladas, el rendimiento individual de cajeros y las horas pico de tráfico.",
                    TargetName: "HistorySalesAreaSection", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
                new("📋 Registros",
                    "Consulta la bitácora completa de transacciones pasadas, reimprime tickets y revisa cortes de caja.",
                    TargetName: "HistoryRecordsSection", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
            },
            "Module.Suppliers" => new()
            {
                new("📦 Nueva Orden de Compra",
                    "Formulario completo para seleccionar proveedor, número de factura y reabastecer inventario.",
                    TargetName: "PurchaseOrderForm", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
                new("➕ Agregar Producto",
                    "Ingresa el producto, costo unitario y cantidad para añadirlo al borrador de la orden.",
                    TargetName: "AddPurchaseItemRow", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
                new("✅ Procesar Entrada",
                    "Guarda la orden de compra y actualiza automáticamente el stock en tu catálogo de inventario.",
                    TargetName: "ConfirmPurchaseBtn", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
                new("🚚 Directorio",
                    "Registra y administra a tus proveedores, RFC, teléfonos y datos de contacto.",
                    TargetName: "SuppliersDirectoryContainer", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
                new("📜 Historial",
                    "Consulta todas las remisiones, órdenes de compra pasadas y tickets de entradas.",
                    TargetName: "PurchaseHistoryContainer", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
            },
            "Module.Expenses" => new()
            {
                new("💸 Ingresar Gastos",
                    "Ingresa el concepto, monto y categoría del gasto para aplicarlo como egreso de caja.",
                    TargetName: "ExpenseEntryForm", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Right),
                new("📜 Historial",
                    "Consulta la lista completa de egresos registrados, importes y fechas correspondientes.",
                    TargetName: "ExpenseHistoryContainer", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Left),
                new("📊 Balance",
                    "Revisa en tiempo real la utilidad neta real, ingresos, egresos y total disponible en caja.",
                    TargetName: "FinancialBalanceSection", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
            },
            "Module.Promotions" => new()
            {
                new("🏷️ Descuentos y Kits",
                    "Crea promociones de porcentaje, monto fijo o kits de productos que se aplican automáticamente en POS.",
                    TargetName: "PromotionsDataGridBorder", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Top),
            },
            "Module.Settings" => new()
            {
                new("👥 Configuración del Sistema",
                    "Administra los parámetros globales, impresoras, usuarios y temas del sistema.",
                    TargetName: "SettingsCard", AnchorSide: Ticketfy.Core.Models.TutorialAnchorSide.Bottom),
            },
            _ => new()
        };
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
                    "Se detectó un turno del día anterior que no fue cerrado correctamente. Debe realizar el Corte final antes de iniciar uno nuevo. ¿Proceder al corte ciego?",
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
        try
        {
            // Always dismiss splash screen to prevent UI freeze
            _ = DismissSplashScreenAsync();

            var licenseService = new Ticketfy.Services.Security.LicenseEnforcementService();
            if (licenseService.IsSystemLocked())
            {
                // KILL SWITCH ACTIVADO
                ActiveViewModel = new LicenseLockedViewModel();
                return;
            }

            bool hasUsers = await _userRepository.HasAnyUsersAsync();

        if (!hasUsers)
        {
            // Route to First-Time Setup (OOBE) Wizard
            var dialogService = new DialogService(async (vm) => { return null; }, () => {});
            var settingsService = new Ticketfy.Services.Implementations.SettingsService(_db);

            Action finishSetupAction = () => { ActiveViewModel = _loginVm; };
            
            Action navigateToAdditionalUsersAction = () => 
            {
                ActiveViewModel = new SetupAdditionalUsersViewModel(_userRepository, finishSetupAction);
            };

            Action navigateToBusinessDataAction = () => 
            {
                ActiveViewModel = new SetupBusinessDataViewModel(settingsService, navigateToAdditionalUsersAction);
            };

            ActiveViewModel = new FirstTimeSetupViewModel(_userRepository, dialogService, navigateToBusinessDataAction);
        }
        else
        {
            // Route to standard Login
            ActiveViewModel = _loginVm;
        }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to initialize application state. This is why the splash screen hangs.");
            _ = DismissSplashScreenAsync();
            UnlockErrorMessage = $"CRITICAL ERROR: {ex.Message}";
        }
    }

    private async Task DismissSplashScreenAsync()
    {
        await Task.Delay(3000); // 3 seconds animated splash
        IsSplashScreenVisible = false;
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
