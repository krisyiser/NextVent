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
            // 1. RADICAL BRAND PRESETS WITH VALCORE HOVER CONTRAST PROTOCOL
            switch (vis.ThemeName)
            {
                case "Modo Claro":
                case "Light":
                    SetResource("BgPrimaryBrush", Color.Parse("#F8FAFC"));
                    SetResource("BgSecondaryBrush", Color.Parse("#FFFFFF"));
                    SetResource("BgTertiaryBrush", Color.Parse("#F1F5F9"));
                    SetResource("HoverBgBrush", Color.Parse("#E2E8F0")); // Contrast slate tint (NEVER disappears into white!)
                    SetResource("BorderBrush", Color.Parse("#CBD5E1"));
                    SetResource("TextPrimaryBrush", Color.Parse("#0F172A"));
                    SetResource("TextSecondaryBrush", Color.Parse("#334155"));
                    SetResource("TextMutedBrush", Color.Parse("#64748B"));
                    SetResource("SidebarBgBrush", Color.Parse(string.IsNullOrWhiteSpace(vis.SidebarBgColor) ? "#F1F5F9" : vis.SidebarBgColor));
                    break;

                case "Alto Contraste":
                    SetResource("BgPrimaryBrush", Color.Parse("#000000"));
                    SetResource("BgSecondaryBrush", Color.Parse("#000000"));
                    SetResource("BgTertiaryBrush", Color.Parse("#141414"));
                    SetResource("HoverBgBrush", Color.Parse("#262626"));
                    SetResource("BorderBrush", Color.Parse("#FACC15"));
                    SetResource("TextPrimaryBrush", Color.Parse("#FFFFFF"));
                    SetResource("TextSecondaryBrush", Color.Parse("#FACC15"));
                    SetResource("TextMutedBrush", Color.Parse("#E5E7EB"));
                    SetResource("SidebarBgBrush", Color.Parse(string.IsNullOrWhiteSpace(vis.SidebarBgColor) ? "#000000" : vis.SidebarBgColor));
                    break;

                case "Cyberpunk Dark":
                    SetResource("BgPrimaryBrush", Color.Parse("#07050E"));
                    SetResource("BgSecondaryBrush", Color.Parse("#130E26"));
                    SetResource("BgTertiaryBrush", Color.Parse("#1F163D"));
                    SetResource("HoverBgBrush", Color.Parse("#2D1C54"));
                    SetResource("BorderBrush", Color.Parse("#8B5CF6"));
                    SetResource("TextPrimaryBrush", Color.Parse("#F5F3FF"));
                    SetResource("TextSecondaryBrush", Color.Parse("#C084FC"));
                    SetResource("TextMutedBrush", Color.Parse("#A78BFA"));
                    SetResource("SidebarBgBrush", Color.Parse(string.IsNullOrWhiteSpace(vis.SidebarBgColor) ? "#0B0719" : vis.SidebarBgColor));
                    break;

                case "Emerald Glass":
                    SetResource("BgPrimaryBrush", Color.Parse("#021F17"));
                    SetResource("BgSecondaryBrush", Color.Parse("#053729"));
                    SetResource("BgTertiaryBrush", Color.Parse("#0A4D3A"));
                    SetResource("HoverBgBrush", Color.Parse("#0F624C"));
                    SetResource("BorderBrush", Color.Parse("#10B981"));
                    SetResource("TextPrimaryBrush", Color.Parse("#ECFDF5"));
                    SetResource("TextSecondaryBrush", Color.Parse("#A7F3D0"));
                    SetResource("TextMutedBrush", Color.Parse("#6EE7B7"));
                    SetResource("SidebarBgBrush", Color.Parse(string.IsNullOrWhiteSpace(vis.SidebarBgColor) ? "#011711" : vis.SidebarBgColor));
                    break;

                case "Nordic Slate":
                    SetResource("BgPrimaryBrush", Color.Parse("#1E293B"));
                    SetResource("BgSecondaryBrush", Color.Parse("#27354A"));
                    SetResource("BgTertiaryBrush", Color.Parse("#334155"));
                    SetResource("HoverBgBrush", Color.Parse("#475569"));
                    SetResource("BorderBrush", Color.Parse("#475569"));
                    SetResource("TextPrimaryBrush", Color.Parse("#F1F5F9"));
                    SetResource("TextSecondaryBrush", Color.Parse("#CBD5E1"));
                    SetResource("TextMutedBrush", Color.Parse("#94A3B8"));
                    SetResource("SidebarBgBrush", Color.Parse(string.IsNullOrWhiteSpace(vis.SidebarBgColor) ? "#0F172A" : vis.SidebarBgColor));
                    break;

                case "Modo Oscuro":
                default:
                    SetResource("BgPrimaryBrush", Color.Parse("#090D16"));
                    SetResource("BgSecondaryBrush", Color.Parse("#151D2A"));
                    SetResource("BgTertiaryBrush", Color.Parse("#1E293B"));
                    SetResource("HoverBgBrush", Color.Parse("#27354A")); // Elevated dark slate hover
                    SetResource("BorderBrush", Color.Parse("#334155"));
                    SetResource("TextPrimaryBrush", Color.Parse("#F8FAFC"));
                    SetResource("TextSecondaryBrush", Color.Parse("#94A3B8"));
                    SetResource("TextMutedBrush", Color.Parse("#64748B"));
                    SetResource("SidebarBgBrush", Color.Parse(string.IsNullOrWhiteSpace(vis.SidebarBgColor) ? "#0B111E" : vis.SidebarBgColor));
                    break;
            }

            // 2. Custom Brand Accents
            if (!string.IsNullOrWhiteSpace(vis.PrimaryColor)) SetResource("AccentPrimaryBrush", Color.Parse(vis.PrimaryColor));
            if (!string.IsNullOrWhiteSpace(vis.SuccessColor)) SetResource("AccentSuccessBrush", Color.Parse(vis.SuccessColor));
            if (!string.IsNullOrWhiteSpace(vis.DangerColor)) SetResource("AccentDangerBrush", Color.Parse(vis.DangerColor));

            // 3. Geometry & Corner Radiuses
            SetResource("AppCornerRadius", new CornerRadius(vis.CornerRadius));
            SetResource("CardCornerRadius", new CornerRadius(vis.CornerRadius * 1.5));

            // 4. Typography & Font Scaling
            var cleanFont = vis.FontFamily.Split(',')[0].Trim();
            var fontFam = new FontFamily($"{cleanFont}, Inter, Segoe UI, sans-serif");
            SetResource("AppFontFamily", fontFam);
            SetResource("AppBaseFontSize", vis.FontSizeScale);
            SetResource("PosPriceFontSize", vis.PosPriceFontSize);

            // 5. Spatial Density & Cart Layout
            SetResource("PosCartWidth", vis.PosCartWidth);
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

            // 6. Animations & Transitions
            SetResource("TransitionDuration", vis.EnableAnimations ? TimeSpan.FromSeconds(0.15) : TimeSpan.Zero);

            // 7. Glassmorphism & Effects
            SetResource("GlassmorphismBlurRadius", vis.GlassmorphismBlur);
            if (vis.GlassmorphismOpacity < 100.0)
            {
                byte alpha = (byte)Math.Clamp((int)(255.0 * (vis.GlassmorphismOpacity / 100.0)), 40, 255);
                var secCol = Color.Parse("#151D2A");
                SetResource("BgSecondaryBrush", Color.FromArgb(alpha, secCol.R, secCol.G, secCol.B));
            }

            // 8. Force Immediate Visual Measure & Visual Tree Invalidation in MainWindow
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopWin && desktopWin.MainWindow != null)
            {
                desktopWin.MainWindow.FontFamily = fontFam;
                desktopWin.MainWindow.FontSize = vis.FontSizeScale;
                desktopWin.MainWindow.InvalidateMeasure();
                desktopWin.MainWindow.InvalidateVisual();
            }

            SettingsApplied?.Invoke(settings);
            Log.Information("ThemeEngine applied app settings cleanly. Theme: {Theme}, Font: {Font}", vis.ThemeName, cleanFont);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ThemeEngine failed applying settings");
        }
    }
}
