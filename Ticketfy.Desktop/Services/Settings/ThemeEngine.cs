using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Ticketfy.Core.Models.Settings;
using Serilog;
using System;

namespace Ticketfy.Services.Settings;

/// <summary>
/// High-performance reactive ThemeEngine under Protocol Valcore v4.0.
/// Direct manipulation of Application.Current.Resources for zero-latency UI customizer updates.
/// Enforces Valcore UX Hover Protocol with solid contrast tones and accent border strokes.
/// Registers all reactive visual customization parameters as dynamic resources.
/// Enforces Valcore Mandatory Input Border Standard (Universal Visible Outline).
/// </summary>
public sealed class ThemeEngine
{
    public static ThemeEngine Instance { get; } = new();

    public event Action<AppSettings>? SettingsApplied;

    private static void SetResource(string key, object value)
    {
        var app = Application.Current;
        if (app != null)
        {
            if (value is Color colorVal)
            {
                var brush = new SolidColorBrush(colorVal);
                app.Resources[key] = brush;
            }
            else
            {
                app.Resources[key] = value;
            }
        }

        if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            if (value is Color winColorVal)
            {
                var winBrush = new SolidColorBrush(winColorVal);
                desktop.MainWindow.Resources[key] = winBrush;
            }
            else
            {
                desktop.MainWindow.Resources[key] = value;
            }
        }
    }

    public void Apply(AppSettings settings)
    {
        if (settings == null) return;
        var vis = settings.Visual;

        try
        {
            // 1. RADICAL BRAND PRESETS WITH VALCORE HOVER & MANDATORY BORDER CONTRAST PROTOCOL
            switch (vis.ThemeName)
            {
                case "Modo Oscuro":
                case "Dark":
                    SetResource("BgPrimaryBrush", Color.Parse("#090D16"));
                    SetResource("BgSecondaryBrush", Color.Parse("#151D2A"));
                    SetResource("BgTertiaryBrush", Color.Parse("#1E293B"));
                    SetResource("HoverBgBrush", Color.Parse("#27354A"));
                    SetResource("BorderBrush", Color.Parse("#475569"));
                    SetResource("TextPrimaryBrush", Color.Parse("#F8FAFC"));
                    SetResource("TextSecondaryBrush", Color.Parse("#94A3B8"));
                    SetResource("TextMutedBrush", Color.Parse("#64748B"));
                    SetResource("SidebarBgBrush", Color.Parse(string.IsNullOrWhiteSpace(vis.SidebarBgColor) ? "#0B111E" : vis.SidebarBgColor));
                    break;

                case "Modo Claro":
                case "Light":
                default:
                    SetResource("BgPrimaryBrush", Color.Parse("#F8FAFC"));
                    SetResource("BgSecondaryBrush", Color.Parse("#FFFFFF"));
                    SetResource("BgTertiaryBrush", Color.Parse("#F1F5F9"));
                    SetResource("HoverBgBrush", Color.Parse("#E2E8F0"));
                    SetResource("BorderBrush", Color.Parse("#94A3B8")); // High-contrast slate border stroke (NEVER disappears into white!)
                    SetResource("TextPrimaryBrush", Color.Parse("#0F172A"));
                    SetResource("TextSecondaryBrush", Color.Parse("#334155"));
                    SetResource("TextMutedBrush", Color.Parse("#64748B"));
                    SetResource("SidebarBgBrush", Color.Parse(string.IsNullOrWhiteSpace(vis.SidebarBgColor) ? "#F1F5F9" : vis.SidebarBgColor));
                    break;
            }

            // 2. Custom Brand Accents & Sidebar Overrides
            if (!string.IsNullOrWhiteSpace(vis.PrimaryColor)) SetResource("AccentPrimaryBrush", Color.Parse(vis.PrimaryColor));
            if (!string.IsNullOrWhiteSpace(vis.SuccessColor)) SetResource("AccentSuccessBrush", Color.Parse(vis.SuccessColor));
            if (!string.IsNullOrWhiteSpace(vis.DangerColor)) SetResource("AccentDangerBrush", Color.Parse(vis.DangerColor));
            if (!string.IsNullOrWhiteSpace(vis.SidebarBgColor)) SetResource("SidebarBgBrush", Color.Parse(vis.SidebarBgColor));

            // 3. Geometry & Corner Radiuses
            SetResource("AppCornerRadius", new CornerRadius(vis.CornerRadius));
            SetResource("CardCornerRadius", new CornerRadius(vis.CornerRadius * 1.5));

            // 4. Typography & Font Scaling
            var cleanFont = vis.FontFamily.Split(',')[0].Trim();
            var fontFam = new FontFamily($"{cleanFont}, Inter, Segoe UI, sans-serif");
            SetResource("AppFontFamily", fontFam);
            SetResource("AppBaseFontSize", vis.FontSizeScale);
            SetResource("PosPriceFontSize", vis.PosPriceFontSize);

            // 5. Spatial Density & Cart Layout & Logo Scale
            SetResource("PosCartWidth", vis.PosCartWidth);
            SetResource("EscalaLogoTopbar", vis.EscalaLogoTopbar);
            switch (vis.Density)
            {
                case UIDensity.Compact:
                    SetResource("ControlPadding", new Thickness(8, 4));
                    SetResource("ControlMinHeight", 30.0);
                    SetResource("ControlSpacing", 4.0);
                    break;
                case UIDensity.Touch:
                    SetResource("ControlPadding", new Thickness(18, 12));
                    SetResource("ControlMinHeight", 48.0);
                    SetResource("ControlSpacing", 14.0);
                    break;
                case UIDensity.Comfortable:
                default:
                    SetResource("ControlPadding", new Thickness(14, 10));
                    SetResource("ControlMinHeight", 38.0);
                    SetResource("ControlSpacing", 8.0);
                    break;
            }

            // 6. Component Visibilities & Flags
            SetResource("ShowStockBadgeResource", vis.ShowStockBadge);
            SetResource("ShowQuickAddButtonResource", vis.ShowQuickAddButton);
            SetResource("ShowBarcodeIconResource", vis.ShowSkuProducto);

            // 7. Animations & Transitions
            SetResource("TransitionDuration", vis.EnableAnimations ? TimeSpan.FromSeconds(0.15) : TimeSpan.Zero);

            // 8. Glassmorphism & Effects
            SetResource("GlassmorphismBlurRadius", vis.GlassmorphismBlur);
            if (vis.GlassmorphismOpacity < 100.0)
            {
                byte alpha = (byte)Math.Clamp((int)(255.0 * (vis.GlassmorphismOpacity / 100.0)), 40, 255);
                var secCol = Color.Parse("#151D2A");
                SetResource("BgSecondaryBrush", Color.FromArgb(alpha, secCol.R, secCol.G, secCol.B));
            }

            // 9. Force Immediate Visual Measure & Visual Tree Invalidation in MainWindow
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopWin && desktopWin.MainWindow != null)
            {
                desktopWin.MainWindow.FontFamily = fontFam;
                desktopWin.MainWindow.FontSize = vis.FontSizeScale;
                desktopWin.MainWindow.InvalidateMeasure();
                desktopWin.MainWindow.InvalidateVisual();
            }

            SettingsApplied?.Invoke(settings);
            Log.Information("ThemeEngine applied app settings cleanly. Theme: {Theme}, Font: {Font}, CartWidth: {Width}", vis.ThemeName, cleanFont, vis.PosCartWidth);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ThemeEngine failed applying settings");
        }
    }
}
