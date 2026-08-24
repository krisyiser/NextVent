using System;
using System.Text.Json.Serialization;

namespace Ticketfy.Core.Models.Settings;

public enum ThemeMode
{
    Light,
    Dark,
    System,
    HighContrast,
    Cyberpunk,
    Emerald,
    Nordic,
    Amber
}

public enum UIDensity
{
    Compact,
    Comfortable,
    Touch
}

/// <summary>
/// Unified Root Settings POCO representing the entire application configuration and UI customization state.
/// Designed under Protocol Valcore v4.0 for zero-fragmentation reactive state management.
/// </summary>
public sealed class AppSettings
{
    public VisualCustomizationConfig Visual { get; set; } = new();
    public CompanyProfileConfig Company { get; set; } = new();
    public TicketLayoutConfig Ticket { get; set; } = new();
    public HardwareConfig Hardware { get; set; } = new();
    public SystemSecurityConfig System { get; set; } = new();
}

public sealed class VisualCustomizationConfig
{
    public ThemeMode Mode { get; set; } = ThemeMode.Dark;
    public string ThemeName { get; set; } = "Modo Oscuro";
    public string PrimaryColor { get; set; } = "#3B82F6";
    public string AccentColor { get; set; } = "#38BDF8";
    public string SuccessColor { get; set; } = "#10B981";
    public string DangerColor { get; set; } = "#EF4444";
    public string SidebarBgColor { get; set; } = "#0F172A";
    public double CornerRadius { get; set; } = 6.0;
    public double FontSizeScale { get; set; } = 14.0;
    public double PosPriceFontSize { get; set; } = 24.0;
    public string FontFamily { get; set; } = "Inter";
    public bool EnableAnimations { get; set; } = true;
    public double TransitionDurationMs { get; set; } = 120.0;
    public UIDensity Density { get; set; } = UIDensity.Comfortable;
    public double GlassmorphismBlur { get; set; } = 0.0;
    public double GlassmorphismOpacity { get; set; } = 100.0;
    public double PosCartWidth { get; set; } = 380.0;
    public string SidebarPosition { get; set; } = "Izquierda";
    public string CartPosition { get; set; } = "Derecha";
    public bool ShowStockBadge { get; set; } = true;
    public bool ShowSkuProducto { get; set; } = true;
    public bool ShowQuickAddButton { get; set; } = true;
    public bool ShowProductImages { get; set; } = true;
    public double GrosorBordePx { get; set; } = 1.0;
}

public sealed class CompanyProfileConfig
{
    public string CommercialName { get; set; } = "TICKETFY! DEMO STORE";
    public string LegalName { get; set; } = "TICKETFY ENTERPRISE S.A. DE C.V.";
    public string Rfc { get; set; } = "XAXX010101000";
    public string FiscalRegime { get; set; } = "601 - General de Ley Personas Morales";
    public string ZipCode { get; set; } = "06000";
    public string Phone { get; set; } = "5512345678";
    public string Email { get; set; } = "contacto@valcore.cloud";
    public string Address { get; set; } = "Av. Insurgentes Sur 1234, CDMX";
    public string LogoPath { get; set; } = string.Empty;
    public string Website { get; set; } = "https://valcore.cloud";
}

public sealed class TicketLayoutConfig
{
    public string HeaderText { get; set; } = "¡Gracias por su compra!";
    public string FooterText { get; set; } = "Conserve este ticket para cualquier aclaración.";
    public string PaperWidthMm { get; set; } = "80mm";
    public bool AutoPrintReceipt { get; set; } = true;
    public bool ShowTaxBreakdown { get; set; } = true;
    public bool PrintLogo { get; set; } = true;
}

public sealed class HardwareConfig
{
    public string PrinterPort { get; set; } = "LPT1 / Direct Raw";
    public string ScannerPort { get; set; } = "HID Barcode Scanner";
    public string ScalePort { get; set; } = "COM1 (RS-232)";
    public bool EnableCashDrawerTrigger { get; set; } = true;
}

public sealed class SystemSecurityConfig
{
    public bool EnablePinLock { get; set; } = true;
    public int AutoLockTimeoutMinutes { get; set; } = 15;
    public string BackupPath { get; set; } = "C:\\TicketfyData\\Backups";
    public bool AutoBackupOnClose { get; set; } = true;
    public bool EnableAuditLogs { get; set; } = true;
}
