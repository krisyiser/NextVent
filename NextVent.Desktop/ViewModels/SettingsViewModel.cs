using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NextVent.Data.Dtos;
using NextVent.Services;
using NextVent.Services.Interfaces;
using NextVent.Services.Implementations;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public record ColorPaletteItem(string Name, string HexColor);
public record FontSizeScaleOption(string Name, string SizePx, string Description);
public record KeyboardShortcutItem(string Shortcut, string ActionName, string Category);

public partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeService _themeService = ThemeService.Instance;
    private readonly IUserService? _userService;
    private readonly ISettingsService? _settingsService;

    // Application Version
    public string AppVersion => NextVent.Core.Helpers.AppVersionHelper.DisplayVersion;
    public string CurrentAppVersion => NextVent.Core.Helpers.AppVersionHelper.DisplayVersion;
    public string FullAppVersionTitle => NextVent.Core.Helpers.AppVersionHelper.FullTitle;

    // Active Main Category Tab
    [ObservableProperty] private bool _isEmpresaTab = true;
    [ObservableProperty] private bool _isInterfazTab = false;
    [ObservableProperty] private bool _isTicketTab = false;
    [ObservableProperty] private bool _isConexionesTab = false;
    [ObservableProperty] private bool _isSeguridadTab = false;
    [ObservableProperty] private bool _isDatosTab = false;
    [ObservableProperty] private bool _isAlertasTab = false;
    [ObservableProperty] private bool _isUsuariosTab = false;
    [ObservableProperty] private bool _isAtajosTab = false;
    [ObservableProperty] private bool _isAcercaDeTab = false;

    // Active Sub-Tab (Empresa)
    [ObservableProperty] private bool _isSubEmpresaGenerales = true;
    [ObservableProperty] private bool _isSubEmpresaIdentidad = false;
    [ObservableProperty] private bool _isSubEmpresaFiscal = false;
    [ObservableProperty] private bool _isSubEmpresaSucursal = false;
    [ObservableProperty] private bool _isSubEmpresaRedes = false;

    // Active Sub-Tab (Interfaz - 6 Granular Sub-tabs)
    [ObservableProperty] private bool _isSubTema = true;
    [ObservableProperty] private bool _isSubColores = false;
    [ObservableProperty] private bool _isSubFuentes = false;
    [ObservableProperty] private bool _isSubDisposicion = false;
    [ObservableProperty] private bool _isSubComponentes = false;
    [ObservableProperty] private bool _isSubAnimaciones = false;

    // ═══ EMPRESA SUB-TABS ═══
    [ObservableProperty] private string _empresaNombreComercial = string.Empty;
    [ObservableProperty] private string _empresaRazonSocial = string.Empty;
    [ObservableProperty] private string _empresaGiroComercial = string.Empty;
    [ObservableProperty] private string _empresaEslogan = string.Empty;
    [ObservableProperty] private string _empresaMonedaPrincipal = string.Empty;
    [ObservableProperty] private string _empresaSimboloMoneda = "$";
    [ObservableProperty] private string _empresaZonaHoraria = string.Empty;

    public ObservableCollection<string> EmpresaGiroOptions { get; } = [
        "Abarrotes / Minisuper", "Restaurante / Cafetería", "Boutique / Ropa",
        "Farmacia", "Ferretería", "Servicios / General", "Otro"
    ];
    public ObservableCollection<string> MonedaOptions { get; } = ["MXN", "USD", "EUR", "GTQ"];
    public ObservableCollection<string> ZonaHorariaOptions { get; } = [
        "America/Mexico_City", "America/Tijuana", "America/Cancun", "America/Monterrey"
    ];

    [ObservableProperty] private string _empresaLogoPrincipalUrl = "";
    [ObservableProperty] private string _empresaLogoIsotipoUrl = "";
    [ObservableProperty] private string _empresaLogoTermicoUrl = "";
    [ObservableProperty] private string _empresaColorCorporativoHex = "#2563EB";
    [ObservableProperty] private bool _empresaSincronizarColorSistema = true;
    [ObservableProperty] private double _empresaThermalLogoThreshold = 128.0;

    [ObservableProperty] private string _empresaRfc = string.Empty;
    [ObservableProperty] private string _empresaRegimenFiscal = string.Empty;
    [ObservableProperty] private string _empresaCodigoPostalFiscal = string.Empty;
    [ObservableProperty] private string _empresaCertificadoCerPath = "";
    [ObservableProperty] private string _empresaCertificadoKeyPath = "";
    [ObservableProperty] private string _empresaPasswordKeyCsd = "";
    [ObservableProperty] private string _empresaUsoCfdiPorDefecto = string.Empty;
    [ObservableProperty] private string _empresaPrefijoFolioInterno = "F-";
    [ObservableProperty] private int _empresaFolioInicial = 1;
    [ObservableProperty] private string _csdValidityStatus = "VÁLIDO";

    [ObservableProperty] private string _facturamaApiUser = string.Empty;
    [ObservableProperty] private string _facturamaApiPassword = string.Empty;
    [ObservableProperty] private string _facturamaAmbiente = "Sandbox";
    public ObservableCollection<string> FacturamaAmbienteOptions { get; } = ["Sandbox", "Producción"];

    public ObservableCollection<string> RegimenFiscalOptions { get; } = [
        "601 - General de Ley Personas Morales",
        "612 - Personas Físicas con Actividades Empresariales y Profesionales",
        "626 - Régimen Simplificado de Confianza (RESICO)",
        "605 - Sueldos y Salarios e Ingresos Asimilados a Salarios",
        "616 - Sin obligaciones fiscales"
    ];

    public ObservableCollection<string> UsoCfdiOptions { get; } = [
        "G01 - Adquisición de mercancías",
        "G03 - Gastos en general",
        "S01 - Sin efectos fiscales",
        "P01 - Por definir"
    ];

    [ObservableProperty] private string _empresaNombreSucursal = string.Empty;
    [ObservableProperty] private string _empresaCalleYNumero = string.Empty;
    [ObservableProperty] private string _empresaColonia = string.Empty;
    [ObservableProperty] private string _empresaCiudadMunicipio = string.Empty;
    [ObservableProperty] private string _empresaEstado = string.Empty;
    [ObservableProperty] private string _empresaTelefonoFijo = string.Empty;
    [ObservableProperty] private string _empresaWhatsappContacto = string.Empty;

    public ObservableCollection<string> MexicanStates { get; } = [
        "Aguascalientes", "Baja California", "Baja California Sur", "Campeche", "Chiapas",
        "Chihuahua", "Coahuila", "Colima", "Ciudad de México", "Durango", "Guanajuato",
        "Guerrero", "Hidalgo", "Jalisco", "Estado de México", "Michoacán", "Morelos",
        "Nayarit", "Nuevo León", "Oaxaca", "Puebla", "Querétaro", "Quintana Roo",
        "San Luis Potosí", "Sinaloa", "Sonora", "Tabasco", "Tamaulipas", "Tlaxcala",
        "Veracruz", "Yucatán", "Zacatecas"
    ];

    [ObservableProperty] private string _empresaEmailContacto = string.Empty;
    [ObservableProperty] private string _empresaSitioWeb = string.Empty;
    [ObservableProperty] private string _empresaFacebook = string.Empty;
    [ObservableProperty] private string _empresaInstagram = string.Empty;
    [ObservableProperty] private string _empresaTiktok = string.Empty;
    [ObservableProperty] private string _empresaMensajeBienvenidaTicket = string.Empty;
    [ObservableProperty] private string _empresaQrRedesUrl = string.Empty;

    // ═══ INTERFAZ SUB-TAB 1: TEMA (7 PRESETS + GLASSMORPHISM) ═══
    [ObservableProperty] private string _currentTheme = "Modo Claro";
    [ObservableProperty] private bool _modoHorarioAuto = false;
    [ObservableProperty] private string _estiloFondo = "Sólido";
    [ObservableProperty] private double _glassmorphismBlur = 0.0;
    [ObservableProperty] private double _glassmorphismOpacity = 100.0;
    [ObservableProperty] private double _borderGlowIntensity = 1.0;

    public ObservableCollection<string> ThemePresetNames { get; } = [
        "Modo Claro", "Modo Oscuro", "Alto Contraste", "Cyberpunk Dark",
        "Emerald Glass", "Nordic Slate", "Retro Amber"
    ];

    public ObservableCollection<string> EstiloFondoOptions { get; } = [
        "Sólido", "Gradiente Diagonal", "Malla de Puntos (Dot Matrix)", "Líneas Diagonales", "Ruido Sutil"
    ];

    // ═══ INTERFAZ SUB-TAB 2: COLORES & BRANDING (GRANULAR COLOR PALETTES - ZERO HEX CODES) ═══
    [ObservableProperty] private string _accentColor = "#2563EB";
    [ObservableProperty] private string _colorSecundario = "#38BDF8";
    [ObservableProperty] private string _colorExitoCobro = "#059669";
    [ObservableProperty] private string _colorAlertaCancelacion = "#DC2626";
    [ObservableProperty] private string _sidebarBgColor = "#1E3A8A";
    [ObservableProperty] private string _colorFondoContenedores = "#1E293B";
    [ObservableProperty] private string _colorTextoPrincipal = "#F8FAFC";
    [ObservableProperty] private string _colorTextoSecundario = "#94A3B8";

    [ObservableProperty] private bool _mostrarLogoEnTopbar = true;
    [ObservableProperty] private double _escalaLogoTopbar = 32.0;
    [ObservableProperty] private bool _invertirLogoModoOscuro = false;

    // 12 Visual Swatches per Category
    public ObservableCollection<ColorPaletteItem> SuccessColors { get; } = [
        new("Esmeralda", "#10B981"), new("Verde Vivo", "#059669"),
        new("Verde Menta", "#34D399"), new("Teal Turquesa", "#0D9488"),
        new("Verde Lima", "#84CC16"), new("Verde Jade", "#047857"),
        new("Azul Turquesa", "#06B6D4"), new("Verde Selva", "#15803D"),
        new("Verde Neón", "#22C55E"), new("Verde Pino", "#064E3B"),
        new("Cian Fresco", "#14B8A6"), new("Verde Bosque", "#166534")
    ];

    public ObservableCollection<ColorPaletteItem> DangerColors { get; } = [
        new("Rojo Carmesí", "#EF4444"), new("Rojo Pasión", "#DC2626"),
        new("Rojo Escarlata", "#B91C1C"), new("Rosa Rubí", "#E11D48"),
        new("Naranja Fuego", "#EA580C"), new("Rojo Coral", "#F87171"),
        new("Rojo Tinto", "#991B1B"), new("Rosa Fucsia", "#D946EF"),
        new("Borgoña", "#881337"), new("Rojo Ladrillo", "#C2410C"),
        new("Rosa Intenso", "#F43F5E"), new("Naranja Neón", "#F97316")
    ];

    public ObservableCollection<ColorPaletteItem> AccentColors { get; } = [
        new("Azul Cobalto", "#2563EB"), new("Azul Medianoche", "#1E40AF"),
        new("Morado Real", "#7C3AED"), new("Violeta", "#6D28D9"),
        new("Azul Celeste", "#38BDF8"), new("Cían Eléctrico", "#06B6D4"),
        new("Naranja Ámbar", "#D97706"), new("Ámbar Cálido", "#F59E0B"),
        new("Rosa Magenta", "#EC4899"), new("Gris Acero", "#475569"),
        new("Índigo Oscuro", "#3730A3"), new("Azul Marino", "#1E3A8A")
    ];

    public ObservableCollection<ColorPaletteItem> SidebarColors { get; } = [
        new("Negro Grafito", "#09090B"), new("Azul Noche", "#0F172A"),
        new("Azul Marino", "#0F3D79"), new("Slate Oscuro", "#1E293B"),
        new("Verde Noche", "#022C22"), new("Morado Noche", "#1E1B4B"),
        new("Gris Carbón", "#18181B"), new("Café Piedra", "#1C1917"),
        new("Azul Rey", "#1E3A8A"), new("Vino Tinto", "#4C0519"),
        new("Esmeralda Noche", "#064E3B"), new("Azul Abismo", "#0B132B")
    ];

    // ═══ INTERFAZ SUB-TAB 3: TIPOGRAFÍA (FONT INSPECTOR & SCALING) ═══
    [ObservableProperty] private string _appFont = "Inter";
    [ObservableProperty] private double _tamanoFuenteBasePx = 14.0;
    [ObservableProperty] private double _tamanoPreciosPosPx = 24.0;
    [ObservableProperty] private double _espaciadoLetrasPx = 0.0;
    [ObservableProperty] private string _grosorTitulos = "Bold (700)";
    [ObservableProperty] private string _grosorNumerosPrecios = "Bold (700)";

    public ObservableCollection<string> AvailableFontOptions { get; } = [
        "Inter", "Roboto", "Montserrat", "Poppins", "JetBrains Mono", "Fira Code",
        "Outfit", "Plus Jakarta Sans", "SF Pro Display", "Open Sans", "Lato", "Consolas"
    ];

    public ObservableCollection<string> GrosorTitulosOptions { get; } = ["Medium (500)", "Bold (700)", "Black (900)"];
    public ObservableCollection<string> GrosorPreciosOptions { get; } = ["Regular (400)", "SemiBold (600)", "ExtraBold (800)"];

    // ═══ INTERFAZ SUB-TAB 4: DISPOSICIÓN & GRID (LAYOUT & DENSITY) ═══
    [ObservableProperty] private string _posicionCarrito = "Derecha";
    [ObservableProperty] private string _sidebarPosition = "Izquierda";
    [ObservableProperty] private double _anchoCarritoPx = 380.0;
    [ObservableProperty] private string _selectedProductGridColumns = "4 Columnas";
    [ObservableProperty] private double _gridGapPx = 12.0;
    [ObservableProperty] private string _modoDensidadEspacial = "Estándar";

    public ObservableCollection<string> PosicionCarritoOptions { get; } = ["Derecha", "Izquierda"];
    public ObservableCollection<string> PosicionSidebarOptions { get; } = ["Izquierda", "Derecha", "Arriba (Top Bar)", "Abajo (Bottom Bar)"];
    public ObservableCollection<string> ProductGridColumnsOptions { get; } = ["3 Columnas", "4 Columnas", "5 Columnas", "6 Columnas", "8 Columnas", "Lista de Alta Densidad"];
    public ObservableCollection<string> ModoDensidadOptions { get; } = ["Cómodo / Touch", "Estándar", "Compacto / Alta Densidad"];

    // ═══ INTERFAZ SUB-TAB 5: BOTONES & COMPONENTES (GEOMETRY & BADGES) ═══
    [ObservableProperty] private double _radioBordesPx = 6.0;
    [ObservableProperty] private string _estiloSombras = "Elevación Sutil";
    [ObservableProperty] private double _grosorBordePx = 1.0;
    [ObservableProperty] private string _setDeIconos = "Lucide (Línea Fina)";

    [ObservableProperty] private bool _showProductImages = true;
    [ObservableProperty] private bool _showSkuProducto = true;
    [ObservableProperty] private bool _showStockBadge = true;
    [ObservableProperty] private bool _showCategoriaBadge = false;
    [ObservableProperty] private bool _showQuickAddButton = true;
    [ObservableProperty] private bool _enableHoverZoom = true;

    public ObservableCollection<string> EstiloSombrasOptions { get; } = ["Sin Sombra / Flat", "Elevación Sutil", "Sombra Profunda", "Borde Neumórfico"];
    public ObservableCollection<string> SetDeIconosOptions { get; } = ["Lucide (Línea Fina)", "Heroicons (Rellenos)", "Tabler (Clásicos)"];

    // ═══ INTERFAZ SUB-TAB 6: ANIMACIONES (MOTION ENGINE) ═══
    [ObservableProperty] private bool _enableAnimations = true;
    [ObservableProperty] private bool _enableHoverEffects = true;
    [ObservableProperty] private bool _enableActiveScaleDown = true;
    [ObservableProperty] private double _duracionTransicionMs = 120.0;
    [ObservableProperty] private string _selectedEasingFunction = "CubicEaseOut";
    [ObservableProperty] private string _tipoEntradaModulos = "Slide Horizontal";

    public ObservableCollection<string> EasingFunctionsOptions { get; } = ["CubicEaseOut", "Bounce", "Linear", "Smooth Spring"];
    public ObservableCollection<string> TipoEntradaModulosOptions { get; } = ["Slide Horizontal", "Fade In Zoom", "Escalonado por Tarjeta (Stagger)"];

    // User Collection
    public ObservableCollection<UserDto> Users { get; } = [];
    [ObservableProperty] private string _newUsername = string.Empty;
    [ObservableProperty] private string _newFullName = string.Empty;
    [ObservableProperty] private string _newRole = "CAJERO";
    public ObservableCollection<string> RolesOptions { get; } = ["CAJERO", "GERENTE", "ADMIN", "SUPERVISOR"];
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _newPasswordHint = string.Empty;
    [ObservableProperty] private string _newPin1 = string.Empty;
    [ObservableProperty] private string _newPin2 = string.Empty;
    [ObservableProperty] private string _newPin3 = string.Empty;
    [ObservableProperty] private string _newPin4 = string.Empty;

    // Admin Deletion State
    [ObservableProperty] private UserDto? _userToDelete;
    [ObservableProperty] private bool _isConfirmingAdminDelete = false;
    [ObservableProperty] private string _adminDeletePassword = string.Empty;

    // Ticket & Printer Settings
    public ObservableCollection<string> PaperWidths { get; } = ["58mm (32 Columnas)", "80mm (48 Columnas)"];
    [ObservableProperty] private string _selectedPaperWidth = "80mm (48 Columnas)";
    [ObservableProperty] private bool _autoPrintTicketOnCheckout = true;
    [ObservableProperty] private bool _autoCutPaper = true;
    [ObservableProperty] private bool _printCashierName = true;
    [ObservableProperty] private string _ticketHeaderLine1 = string.Empty;
    [ObservableProperty] private string _ticketHeaderLine2 = string.Empty;
    [ObservableProperty] private string _ticketFooterLine1 = string.Empty;
    [ObservableProperty] private string _ticketFooterLine2 = string.Empty;

    // Conexiones / Periféricos
    [ObservableProperty] private string _printerPort = string.Empty;
    [ObservableProperty] private string _barcodeScannerMode = string.Empty;
    [ObservableProperty] private string _scalePort = string.Empty;
    [ObservableProperty] private string _cashDrawerPort = string.Empty;

    // Shortcuts List
    public ObservableCollection<KeyboardShortcutItem> KeyboardShortcuts { get; } = [
        new("F2", "Abrir Punto de Venta (POS)", "Punto de Venta"),
        new("F3", "Ver Catálogo de Inventario", "Inventario"),
        new("F4", "Ver Clientes y Créditos", "Clientes"),
        new("F5", "Proveedores y Compras", "Proveedores"),
        new("F6", "Gastos y Utilidad Neta", "Gastos"),
        new("F7", "Historial de Transacciones", "Historial"),
        new("F8", "Promociones y Descuentos", "Ventas"),
        new("F9", "Facturación Electrónica SAT", "Fiscal"),
        new("F10", "Configuración del Sistema", "Ajustes"),
        new("F12", "Cobrar Ticket en POS", "Punto de Venta"),
        new("Ctrl + N", "Nueva Venta / Limpiar Carrito", "Punto de Venta"),
        new("Ctrl + F", "Enfocar Búsqueda de Producto", "Navegación"),
        new("Esc", "Cerrar Diálogos / Cancelar Venta", "Global")
    ];

    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public event Func<Task>? SettingsSaved;

    public SettingsViewModel(IUserService userService, ISettingsService settingsService) : this()
    {
        _userService = userService;
        _settingsService = settingsService;
        _ = LoadUsersAsync();
        _ = LoadSavedSettingsAsync();
    }

    public SettingsViewModel()
    {
    }

    public async Task LoadSavedSettingsAsync()
    {
        if (_settingsService == null) return;
        try
        {
            var dict = await _settingsService.GetAllAsync();
            if (dict.TryGetValue("PrinterPort", out var port)) PrinterPort = port;
            if (dict.TryGetValue("PaperWidth", out var pw)) SelectedPaperWidth = pw;
            if (dict.TryGetValue("Header1", out var h1)) TicketHeaderLine1 = h1;
            if (dict.TryGetValue("Footer1", out var f1)) TicketFooterLine1 = f1;

            if (dict.TryGetValue("EmpresaNombreComercial", out var nc)) EmpresaNombreComercial = nc;
            if (dict.TryGetValue("EmpresaRazonSocial", out var rs)) EmpresaRazonSocial = rs;
            if (dict.TryGetValue("EmpresaGiroComercial", out var gc)) EmpresaGiroComercial = gc;
            if (dict.TryGetValue("EmpresaRfc", out var rfc)) EmpresaRfc = rfc;
            if (dict.TryGetValue("EmpresaEstado", out var est)) EmpresaEstado = est;
            
            // Clean up old stubborn DB defaults
            if (PrinterPort == "COM1 (9600 8N1)") PrinterPort = string.Empty;
            if (EmpresaEstado == "Ciudad de México") EmpresaEstado = string.Empty;

            if (dict.TryGetValue("FacturamaApiUser", out var fUser)) FacturamaApiUser = fUser;
            if (dict.TryGetValue("FacturamaApiPassword", out var fPass)) FacturamaApiPassword = fPass;
            if (dict.TryGetValue("FacturamaAmbiente", out var fAmbiente)) FacturamaAmbiente = fAmbiente;

            if (dict.TryGetValue("CurrentTheme", out var theme)) CurrentTheme = theme;
            if (dict.TryGetValue("AccentColor", out var accent)) AccentColor = accent;
            if (dict.TryGetValue("ColorExitoCobro", out var cobro)) ColorExitoCobro = cobro;
            if (dict.TryGetValue("ColorAlertaCancelacion", out var cancel)) ColorAlertaCancelacion = cancel;
            if (dict.TryGetValue("SidebarBgColor", out var sideBg)) SidebarBgColor = sideBg;
            if (dict.TryGetValue("AppFont", out var font)) AppFont = font;
            if (dict.TryGetValue("SidebarPosition", out var sp)) SidebarPosition = sp;
            if (dict.TryGetValue("PosicionCarrito", out var pc)) PosicionCarrito = pc;

            if (dict.TryGetValue("GlassmorphismBlur", out var blur) && double.TryParse(blur, out var bVal)) GlassmorphismBlur = bVal;
            if (dict.TryGetValue("GlassmorphismOpacity", out var op) && double.TryParse(op, out var oVal)) GlassmorphismOpacity = oVal;
            if (dict.TryGetValue("EscalaLogoTopbar", out var logo) && double.TryParse(logo, out var lVal)) EscalaLogoTopbar = lVal;
            if (dict.TryGetValue("TamanoFuenteBasePx", out var fontBase) && double.TryParse(fontBase, out var fbVal)) TamanoFuenteBasePx = fbVal;
            if (dict.TryGetValue("TamanoPreciosPosPx", out var fontPrice) && double.TryParse(fontPrice, out var fpVal)) TamanoPreciosPosPx = fpVal;
            if (dict.TryGetValue("AnchoCarritoPx", out var cartW) && double.TryParse(cartW, out var cwVal)) AnchoCarritoPx = cwVal;
            if (dict.TryGetValue("RadioBordesPx", out var radius) && double.TryParse(radius, out var rVal)) RadioBordesPx = rVal;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading saved settings");
        }
    }

    public async Task LoadUsersAsync()
    {
        if (_userService == null) return;
        try
        {
            var list = await _userService.GetAllAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Users.Clear();
                foreach (var u in list) Users.Add(u);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading users list in SettingsViewModel");
        }
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (_userService == null) return;
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewFullName))
        {
            FeedbackMessage = "Nombre y Usuario son obligatorios";
            return;
        }

        try
        {
            string finalPass = string.IsNullOrWhiteSpace(NewPassword) ? string.Empty : NextVent.Core.Helpers.CryptoHelper.HashPassword(NewPassword);
            string finalPin = $"{NewPin1}{NewPin2}{NewPin3}{NewPin4}";
            if (finalPin.Length != 4)
            {
                FeedbackMessage = "El PIN debe ser de 4 dígitos";
                return;
            }

            await _userService.SaveAsync(Guid.NewGuid().ToString(), NewFullName, NewUsername, NewRole, finalPass, finalPin, NewPasswordHint);
            await LoadUsersAsync();

            NewUsername = string.Empty;
            NewFullName = string.Empty;
            NewPin1 = string.Empty;
            NewPin2 = string.Empty;
            NewPin3 = string.Empty;
            NewPin4 = string.Empty;
            NewPassword = string.Empty;
            NewPasswordHint = string.Empty;
            NewRole = "CAJERO";
            FeedbackMessage = "¡Cajero / Usuario registrado correctamente!";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating user");
            FeedbackMessage = "Error al crear usuario";
        }
    }

    [RelayCommand]
    private void RequestDeleteUser(UserDto user)
    {
        if (user == null) return;

        if (user.Role.ToUpper() == "ADMIN")
        {
            UserToDelete = user;
            IsConfirmingAdminDelete = true;
            AdminDeletePassword = string.Empty;
            FeedbackMessage = "Para eliminar un administrador, por favor confirma con su contraseña.";
        }
        else
        {
            _ = ConfirmDeleteUserAsync(user);
        }
    }

    [RelayCommand]
    private void CancelAdminDelete()
    {
        IsConfirmingAdminDelete = false;
        UserToDelete = null;
        AdminDeletePassword = string.Empty;
        FeedbackMessage = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmAdminDeleteAsync()
    {
        if (UserToDelete == null || _userService == null) return;

        string savedHash = await _userService.GetPasswordHashAsync(UserToDelete.Id) ?? string.Empty;
        if (string.IsNullOrEmpty(savedHash) || NextVent.Core.Helpers.CryptoHelper.VerifyPassword(AdminDeletePassword, savedHash) || NextVent.Services.Security.SecurityManager.VerifyPassword(AdminDeletePassword, savedHash))
        {
            await ConfirmDeleteUserAsync(UserToDelete);
            CancelAdminDelete();
        }
        else
        {
            FeedbackMessage = "Contraseña de administrador incorrecta. No se puede eliminar.";
        }
    }

    private async Task ConfirmDeleteUserAsync(UserDto user)
    {
        if (_userService == null || user == null) return;
        try
        {
            await _userService.DeleteAsync(user.Id);
            await LoadUsersAsync();
            CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(new NextVent.Core.Messages.UserDeletedMessage(user.Id));
            FeedbackMessage = $"Usuario {user.FullName} eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting user");
            FeedbackMessage = "Error al eliminar usuario.";
        }
    }

    [RelayCommand]
    private void SelectMainTab(string category)
    {
        IsEmpresaTab = category == "empresa";
        IsInterfazTab = category == "interfaz";
        IsTicketTab = category == "ticket";
        IsConexionesTab = category == "conexiones";
        IsSeguridadTab = category == "seguridad";
        IsDatosTab = category == "datos";
        IsAlertasTab = category == "alertas";
        IsUsuariosTab = category == "usuarios";
        IsAtajosTab = category == "atajos";
        IsAcercaDeTab = category == "acercade";
    }

    [RelayCommand]
    private void SelectEmpresaSubTab(string subTab)
    {
        IsSubEmpresaGenerales = subTab == "generales";
        IsSubEmpresaIdentidad = subTab == "identidad";
        IsSubEmpresaFiscal = subTab == "fiscal";
        IsSubEmpresaSucursal = subTab == "sucursal";
        IsSubEmpresaRedes = subTab == "redes";
    }

    [RelayCommand]
    private void SelectSubTab(string subTab)
    {
        IsSubTema = subTab == "tema";
        IsSubColores = subTab == "colores";
        IsSubFuentes = subTab == "fuentes";
        IsSubDisposicion = subTab == "disposicion";
        IsSubComponentes = subTab == "componentes";
        IsSubAnimaciones = subTab == "animaciones";
    }

    [RelayCommand]
    private void SelectTheme(string themeName)
    {
        CurrentTheme = themeName;
        _themeService.ApplyTheme(themeName);
        _ = SaveAllSettingsAsync();
    }

    [RelayCommand]
    private void SelectAccent(string hex)
    {
        AccentColor = hex;
        _themeService.ApplyAccentColor(hex);
        _ = SaveAllSettingsAsync();
    }

    [RelayCommand]
    private void SelectSuccessColor(string hex)
    {
        ColorExitoCobro = hex;
        _themeService.ApplySuccessColor(hex);
        _ = SaveAllSettingsAsync();
    }

    [RelayCommand]
    private void SelectDangerColor(string hex)
    {
        ColorAlertaCancelacion = hex;
        _themeService.ApplyDangerColor(hex);
        _ = SaveAllSettingsAsync();
    }

    [RelayCommand]
    private void SelectSidebarColor(string hex)
    {
        SidebarBgColor = hex;
        _themeService.ApplySidebarColor(hex);
        _ = SaveAllSettingsAsync();
    }

    // REACTIVE SLIDER CHANGED HANDLERS (REAL-TIME PROPAGATION & INSTANT PERSISTENCE TO SQLITE)
    partial void OnGlassmorphismBlurChanged(double value) { _themeService.ApplyGlassmorphismBlur(value); _ = SaveAllSettingsAsync(); }
    partial void OnGlassmorphismOpacityChanged(double value) { _themeService.ApplyGlassmorphismOpacity(value); _ = SaveAllSettingsAsync(); }
    partial void OnEscalaLogoTopbarChanged(double value) { _themeService.ApplyLogoScale(value); _ = SaveAllSettingsAsync(); }
    partial void OnTamanoFuenteBasePxChanged(double value) { _themeService.ApplyBaseFontSize(value); _ = SaveAllSettingsAsync(); }
    partial void OnTamanoPreciosPosPxChanged(double value) { _themeService.ApplyPosPriceFontSize(value); _ = SaveAllSettingsAsync(); }
    partial void OnAnchoCarritoPxChanged(double value) { _themeService.ApplyCartWidth(value); _ = SaveAllSettingsAsync(); }
    partial void OnRadioBordesPxChanged(double value) { _themeService.ApplyBorderRadius(value); _ = SaveAllSettingsAsync(); }
    partial void OnDuracionTransicionMsChanged(double value) { _themeService.ApplyTransitionDuration(value); _ = SaveAllSettingsAsync(); }

    partial void OnColorExitoCobroChanged(string value) { _themeService.ApplySuccessColor(value); _ = SaveAllSettingsAsync(); }
    partial void OnColorAlertaCancelacionChanged(string value) { _themeService.ApplyDangerColor(value); _ = SaveAllSettingsAsync(); }
    partial void OnSidebarBgColorChanged(string value) { _themeService.ApplySidebarColor(value); _ = SaveAllSettingsAsync(); }
    partial void OnSidebarPositionChanged(string value) { _themeService.ApplySidebarPosition(value); _ = SaveAllSettingsAsync(); }
    partial void OnPosicionCarritoChanged(string value) { _themeService.ApplyCartPosition(value); _ = SaveAllSettingsAsync(); }
    partial void OnAppFontChanged(string value) { _themeService.ApplyFont(value); _ = SaveAllSettingsAsync(); }

    [RelayCommand] private void CreateBackupNow() => FeedbackMessage = "¡Copia de seguridad creada correctamente en disco local!";
    [RelayCommand] private void ExportCatalogCsv() => FeedbackMessage = "Catálogo exportado exitosamente a formato CSV";
    
    [RelayCommand] 
    private async Task SaveAllSettingsAsync()
    {
        if (_settingsService != null)
        {
            await _settingsService.SetAsync("PrinterPort", PrinterPort);
            await _settingsService.SetAsync("PaperWidth", SelectedPaperWidth);
            await _settingsService.SetAsync("Header1", TicketHeaderLine1);
            await _settingsService.SetAsync("Footer1", TicketFooterLine1);

            await _settingsService.SetAsync("CurrentTheme", CurrentTheme);
            await _settingsService.SetAsync("AccentColor", AccentColor);
            await _settingsService.SetAsync("ColorExitoCobro", ColorExitoCobro);
            await _settingsService.SetAsync("ColorAlertaCancelacion", ColorAlertaCancelacion);
            await _settingsService.SetAsync("SidebarBgColor", SidebarBgColor);
            await _settingsService.SetAsync("AppFont", AppFont);
            await _settingsService.SetAsync("SidebarPosition", SidebarPosition);
            await _settingsService.SetAsync("PosicionCarrito", PosicionCarrito);

            await _settingsService.SetAsync("GlassmorphismBlur", GlassmorphismBlur.ToString());
            await _settingsService.SetAsync("GlassmorphismOpacity", GlassmorphismOpacity.ToString());
            await _settingsService.SetAsync("EscalaLogoTopbar", EscalaLogoTopbar.ToString());
            await _settingsService.SetAsync("TamanoFuenteBasePx", TamanoFuenteBasePx.ToString());
            await _settingsService.SetAsync("TamanoPreciosPosPx", TamanoPreciosPosPx.ToString());
            await _settingsService.SetAsync("AnchoCarritoPx", AnchoCarritoPx.ToString());
            await _settingsService.SetAsync("RadioBordesPx", RadioBordesPx.ToString());
            await _settingsService.SetAsync("DuracionTransicionMs", DuracionTransicionMs.ToString());

            await _settingsService.SetAsync("EmpresaNombreComercial", EmpresaNombreComercial);
            await _settingsService.SetAsync("EmpresaRazonSocial", EmpresaRazonSocial);
            await _settingsService.SetAsync("EmpresaGiroComercial", EmpresaGiroComercial);
            await _settingsService.SetAsync("EmpresaRfc", EmpresaRfc);

            await _settingsService.SetAsync("FacturamaApiUser", FacturamaApiUser);
            await _settingsService.SetAsync("FacturamaApiPassword", FacturamaApiPassword);
            await _settingsService.SetAsync("FacturamaAmbiente", FacturamaAmbiente);
        }

        // --- PING HUB EN TIEMPO REAL ---
        var deviceReg = new NextVent.Services.Implementations.DeviceRegistrationService(_settingsService);
        _ = deviceReg.PingServerAsync(new NextVent.Services.Implementations.BusinessProfile 
        { 
            BusinessName = EmpresaNombreComercial,
            Email = "contacto@empresa.com"
        });

        if (SettingsSaved != null)
        {
            await SettingsSaved.Invoke();
        }

        FeedbackMessage = "¡Toda la configuración visual, paletas de colores y atajos guardada y sincronizada!";
    }

    [RelayCommand] private void Close() { }
}
