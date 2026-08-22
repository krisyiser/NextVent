using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Services;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Manages theme, colors, typography, layout density, component geometry, and animations.
/// Encapsulates theme configuration and communicates with ThemeService.
/// </summary>
public partial class InterfazSettingsViewModel : ObservableObject
{
    private readonly ThemeService _themeService = ThemeService.Instance;
    private readonly ISettingsService? _settingsService;

    // Sub-tab selection inside Interfaz
    [ObservableProperty] private bool _isSubTema = true;
    [ObservableProperty] private bool _isSubColores = false;
    [ObservableProperty] private bool _isSubFuentes = false;
    [ObservableProperty] private bool _isSubDisposicion = false;
    [ObservableProperty] private bool _isSubComponentes = false;
    [ObservableProperty] private bool _isSubAnimaciones = false;

    // Theme & Background
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

    // Colors
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

    // Typography
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

    // Layout
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

    // Components & Geometry
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

    // Animations
    [ObservableProperty] private bool _enableAnimations = true;
    [ObservableProperty] private bool _enableHoverEffects = true;
    [ObservableProperty] private bool _enableActiveScaleDown = true;
    [ObservableProperty] private double _duracionTransicionMs = 120.0;
    [ObservableProperty] private string _selectedEasingFunction = "CubicEaseOut";
    [ObservableProperty] private string _tipoEntradaModulos = "Slide Horizontal";

    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public InterfazSettingsViewModel(ISettingsService? settingsService = null)
    {
        _settingsService = settingsService;
        if (_settingsService != null) _ = LoadAsync();
    }

    [RelayCommand]
    private void SelectSubTab(string tab)
    {
        IsSubTema        = tab == "tema";
        IsSubColores     = tab == "colores";
        IsSubFuentes     = tab == "fuentes";
        IsSubDisposicion = tab == "disposicion";
        IsSubComponentes = tab == "componentes";
        IsSubAnimaciones = tab == "animaciones";
    }

    public async Task LoadAsync()
    {
        if (_settingsService == null) return;
        try
        {
            var d = await _settingsService.GetAllAsync();
            if (d.TryGetValue("CurrentTheme", out var ct)) CurrentTheme = ct;
            if (d.TryGetValue("AccentColor", out var ac)) AccentColor = ac;
            if (d.TryGetValue("AppFont", out var af)) AppFont = af;
            if (d.TryGetValue("SidebarPosition", out var sp)) SidebarPosition = sp;
        }
        catch (Exception ex) { Log.Error(ex, "InterfazSettingsViewModel: error loading"); }
    }

    public async Task SaveAsync()
    {
        if (_settingsService == null) return;
        try
        {
            await _settingsService.SetAsync("CurrentTheme", CurrentTheme);
            await _settingsService.SetAsync("AccentColor", AccentColor);
            await _settingsService.SetAsync("AppFont", AppFont);
            await _settingsService.SetAsync("SidebarPosition", SidebarPosition);

            _themeService.ApplyTheme(CurrentTheme);
            _themeService.ApplyAccentColor(AccentColor);
            _themeService.ApplySidebarPosition(SidebarPosition);

            FeedbackMessage = "¡Ajustes de interfaz guardados correctamente!";
        }
        catch (Exception ex) { Log.Error(ex, "InterfazSettingsViewModel: error saving"); }
    }

    [RelayCommand] private async Task Save() => await SaveAsync();
}
