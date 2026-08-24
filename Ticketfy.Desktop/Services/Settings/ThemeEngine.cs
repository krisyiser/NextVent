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
                if (app.Resources.TryGetValue(key, out var existing) && existing is SolidColorBrush solidBrush)
                {
                    solidBrush.Color = colorVal;
                }
                else
                {
                    app.Resources[key] = new SolidColorBrush(colorVal);
                }
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
                if (desktop.MainWindow.Resources.TryGetValue(key, out var winExisting) && winExisting is SolidColorBrush winBrush)
                {
                    winBrush.Color = winColorVal;
                }
                else
                {
                    desktop.MainWindow.Resources[key] = new SolidColorBrush(winColorVal);
                }
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
            // 1. Theme Presets & Palette
            switch (vis.ThemeName)
            {
                case "Modo Claro":
                case "Light":
                    SetResource("BgPrimaryBrush", Color.Parse("#F4F6F8"));
                    SetResource("BgSecondaryBrush", Color.Parse("#FFFFFF"));
                    SetResource("BgTertiaryBrush", Color.Parse("#F3F4F6"));
                    SetResource("BorderBrush", Color.Parse("#E5E7EB"));
                    SetResource("TextPrimaryBrush", Color.Parse("#111827"));
                    SetResource("TextSecondaryBrush", Color.Parse("#4B5563"));
                    SetResource("TextMutedBrush", Color.Parse("#9CA3AF"));
                    SetResource("SidebarBgBrush", Color.Parse("#0F172A"));
                    break;

                case "Alto Contraste":
                    SetResource("BgPrimaryBrush", Color.Parse("#000000"));
                    SetResource("BgSecondaryBrush", Color.Parse("#000000"));
                    SetResource("BgTertiaryBrush", Color.Parse("#121212"));
                    SetResource("BorderBrush", Color.Parse("#FFFFFF"));
                    SetResource("TextPrimaryBrush", Color.Parse("#FFFFFF"));
                    SetResource("TextSecondaryBrush", Color.Parse("#FACC15"));
                    SetResource("TextMutedBrush", Color.Parse("#E5E7EB"));
                    break;

                case "Cyberpunk Dark":
                    SetResource("BgPrimaryBrush", Color.Parse("#09090B"));
                    SetResource("BgSecondaryBrush", Color.Parse("#18181B"));
                    SetResource("BgTertiaryBrush", Color.Parse("#27272A"));
                    SetResource("BorderBrush", Color.Parse("#8B5CF6"));
                    SetResource("TextPrimaryBrush", Color.Parse("#F4F4F5"));
                    SetResource("TextSecondaryBrush", Color.Parse("#38BDF8"));
                    SetResource("TextMutedBrush", Color.Parse("#A1A1AA"));
                    break;

                case "Emerald Glass":
                    SetResource("BgPrimaryBrush", Color.Parse("#022C22"));
                    SetResource("BgSecondaryBrush", Color.Parse("#064E3B"));
                    SetResource("BgTertiaryBrush", Color.Parse("#047857"));
                    SetResource("BorderBrush", Color.Parse("#34D399"));
                    SetResource("TextPrimaryBrush", Color.Parse("#ECFDF5"));
                    SetResource("TextSecondaryBrush", Color.Parse("#A7F3D0"));
                    SetResource("TextMutedBrush", Color.Parse("#6EE7B7"));
                    break;

                case "Nordic Slate":
                    SetResource("BgPrimaryBrush", Color.Parse("#1E293B"));
                    SetResource("BgSecondaryBrush", Color.Parse("#334155"));
                    SetResource("BgTertiaryBrush", Color.Parse("#475569"));
                    SetResource("BorderBrush", Color.Parse("#94A3B8"));
                    SetResource("TextPrimaryBrush", Color.Parse("#F8FAFC"));
                    SetResource("TextSecondaryBrush", Color.Parse("#CBD5E1"));
                    SetResource("TextMutedBrush", Color.Parse("#94A3B8"));
                    break;

                case "Modo Oscuro":
                default:
                    SetResource("BgPrimaryBrush", Color.Parse("#090D16"));
                    SetResource("BgSecondaryBrush", Color.Parse("#1E293B"));
                    SetResource("BgTertiaryBrush", Color.Parse("#0F172A"));
                    SetResource("BorderBrush", Color.Parse("#334155"));
                    SetResource("TextPrimaryBrush", Color.Parse("#F8FAFC"));
                    SetResource("TextSecondaryBrush", Color.Parse("#94A3B8"));
                    SetResource("TextMutedBrush", Color.Parse("#64748B"));
                    break;
            }

            // 2. Custom Brand Accents
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

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopWin && desktopWin.MainWindow != null)
            {
                desktopWin.MainWindow.FontFamily = fontFam;
                desktopWin.MainWindow.FontSize = vis.FontSizeScale;
            }

            // 5. Spatial Density & Cart Layout
            SetResource("PosCartWidth", vis.PosCartWidth);
            SetResource("ControlSpacing", vis.Density switch
            {
                UIDensity.Compact => new Thickness(4, 2),
                UIDensity.Touch => new Thickness(14, 10),
                _ => new Thickness(8, 6)
            });

            // 6. Glassmorphism & Effects
            SetResource("GlassmorphismBlurRadius", vis.GlassmorphismBlur);
            if (vis.GlassmorphismOpacity < 100.0)
            {
                byte alpha = (byte)Math.Clamp((int)(255.0 * (vis.GlassmorphismOpacity / 100.0)), 40, 255);
                var secCol = Color.Parse("#1E293B");
                SetResource("BgSecondaryBrush", Color.FromArgb(alpha, secCol.R, secCol.G, secCol.B));
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
