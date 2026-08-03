using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Serilog;
using System;

namespace NextVent.Services;

public sealed class ThemeService
{
    public static ThemeService Instance { get; } = new();

    public event Action<string>? SidebarPositionChanged;
    public event Action<string>? CartPositionChanged;
    public event Action<double>? CartWidthChanged;
    public event Action<double>? BorderWidthChanged;

    private Color _baseSecondaryColor = Color.Parse("#FFFFFF");
    private Color _baseTertiaryColor = Color.Parse("#F3F4F6");
    private double _currentOpacityPct = 100.0;

    private static void SetDynamicResource(string key, object resourceValue)
    {
        var app = Application.Current;
        if (app != null)
        {
            if (resourceValue is Color colorVal)
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
                app.Resources[key] = resourceValue;
            }
        }

        if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            if (resourceValue is Color winColorVal)
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
                desktop.MainWindow.Resources[key] = resourceValue;
            }
        }
    }

    public void ApplyTheme(string themeName)
    {
        try
        {
            switch (themeName)
            {
                case "Modo Claro":
                case "Light":
                    _baseSecondaryColor = Color.Parse("#FFFFFF");
                    _baseTertiaryColor = Color.Parse("#F3F4F6");
                    SetDynamicResource("BgPrimaryBrush", Color.Parse("#F4F6F8"));
                    SetDynamicResource("BorderBrush", Color.Parse("#E5E7EB"));
                    SetDynamicResource("TextPrimaryBrush", Color.Parse("#111827"));
                    SetDynamicResource("TextSecondaryBrush", Color.Parse("#4B5563"));
                    SetDynamicResource("TextMutedBrush", Color.Parse("#9CA3AF"));
                    SetDynamicResource("SidebarBgBrush", Color.Parse("#0F172A"));
                    break;

                case "Modo Oscuro":
                case "Dark":
                    _baseSecondaryColor = Color.Parse("#1E293B");
                    _baseTertiaryColor = Color.Parse("#334155");
                    SetDynamicResource("BgPrimaryBrush", Color.Parse("#0F172A"));
                    SetDynamicResource("BorderBrush", Color.Parse("#475569"));
                    SetDynamicResource("TextPrimaryBrush", Color.Parse("#F8FAFC"));
                    SetDynamicResource("TextSecondaryBrush", Color.Parse("#94A3B8"));
                    SetDynamicResource("TextMutedBrush", Color.Parse("#64748B"));
                    SetDynamicResource("SidebarBgBrush", Color.Parse("#09090B"));
                    break;

                case "Alto Contraste":
                    _baseSecondaryColor = Color.Parse("#000000");
                    _baseTertiaryColor = Color.Parse("#121212");
                    SetDynamicResource("BgPrimaryBrush", Color.Parse("#000000"));
                    SetDynamicResource("BorderBrush", Color.Parse("#FFFFFF"));
                    SetDynamicResource("TextPrimaryBrush", Color.Parse("#FFFFFF"));
                    SetDynamicResource("TextSecondaryBrush", Color.Parse("#FACC15"));
                    SetDynamicResource("TextMutedBrush", Color.Parse("#E5E7EB"));
                    SetDynamicResource("AccentPrimaryBrush", Color.Parse("#FACC15"));
                    break;

                case "Cyberpunk Dark":
                    _baseSecondaryColor = Color.Parse("#18181B");
                    _baseTertiaryColor = Color.Parse("#27272A");
                    SetDynamicResource("BgPrimaryBrush", Color.Parse("#09090B"));
                    SetDynamicResource("BorderBrush", Color.Parse("#8B5CF6"));
                    SetDynamicResource("TextPrimaryBrush", Color.Parse("#F4F4F5"));
                    SetDynamicResource("TextSecondaryBrush", Color.Parse("#38BDF8"));
                    SetDynamicResource("TextMutedBrush", Color.Parse("#A1A1AA"));
                    SetDynamicResource("AccentPrimaryBrush", Color.Parse("#EC4899"));
                    break;

                case "Emerald Glass":
                    _baseSecondaryColor = Color.Parse("#064E3B");
                    _baseTertiaryColor = Color.Parse("#047857");
                    SetDynamicResource("BgPrimaryBrush", Color.Parse("#022C22"));
                    SetDynamicResource("BorderBrush", Color.Parse("#34D399"));
                    SetDynamicResource("TextPrimaryBrush", Color.Parse("#ECFDF5"));
                    SetDynamicResource("TextSecondaryBrush", Color.Parse("#A7F3D0"));
                    SetDynamicResource("TextMutedBrush", Color.Parse("#6EE7B7"));
                    SetDynamicResource("AccentPrimaryBrush", Color.Parse("#10B981"));
                    break;

                case "Nordic Slate":
                    _baseSecondaryColor = Color.Parse("#334155");
                    _baseTertiaryColor = Color.Parse("#475569");
                    SetDynamicResource("BgPrimaryBrush", Color.Parse("#1E293B"));
                    SetDynamicResource("BorderBrush", Color.Parse("#94A3B8"));
                    SetDynamicResource("TextPrimaryBrush", Color.Parse("#F8FAFC"));
                    SetDynamicResource("TextSecondaryBrush", Color.Parse("#CBD5E1"));
                    SetDynamicResource("TextMutedBrush", Color.Parse("#94A3B8"));
                    SetDynamicResource("AccentPrimaryBrush", Color.Parse("#38BDF8"));
                    break;

                case "Retro Amber":
                    _baseSecondaryColor = Color.Parse("#292524");
                    _baseTertiaryColor = Color.Parse("#44403C");
                    SetDynamicResource("BgPrimaryBrush", Color.Parse("#1C1917"));
                    SetDynamicResource("BorderBrush", Color.Parse("#D97706"));
                    SetDynamicResource("TextPrimaryBrush", Color.Parse("#FDE68A"));
                    SetDynamicResource("TextSecondaryBrush", Color.Parse("#F59E0B"));
                    SetDynamicResource("TextMutedBrush", Color.Parse("#78350F"));
                    SetDynamicResource("AccentPrimaryBrush", Color.Parse("#F59E0B"));
                    break;
            }

            ApplyGlassmorphismOpacity(_currentOpacityPct);
            Log.Information("Applied real-time theme: {Theme}", themeName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply theme {Theme}", themeName);
        }
    }

    public void ApplyAccentColor(string hexColor)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hexColor)) return;
            SetDynamicResource("AccentPrimaryBrush", Color.Parse(hexColor));
            Log.Information("Applied real-time accent color: {Hex}", hexColor);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply accent color {Hex}", hexColor);
        }
    }

    public void ApplySuccessColor(string hexColor)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hexColor)) return;
            SetDynamicResource("AccentSuccessBrush", Color.Parse(hexColor));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply success color {Hex}", hexColor);
        }
    }

    public void ApplyDangerColor(string hexColor)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hexColor)) return;
            SetDynamicResource("AccentDangerBrush", Color.Parse(hexColor));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply danger color {Hex}", hexColor);
        }
    }

    public void ApplySidebarColor(string hexColor)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hexColor)) return;
            SetDynamicResource("SidebarBgBrush", Color.Parse(hexColor));
            Log.Information("Applied real-time sidebar color: {Hex}", hexColor);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply sidebar color {Hex}", hexColor);
        }
    }

    public void ApplyFont(string fontName)
    {
        try
        {
            var cleanFont = fontName.Split('(')[0].Trim();
            var fontFam = new FontFamily($"{cleanFont}, Inter, Segoe UI, sans-serif");

            var app = Application.Current;
            if (app != null)
            {
                app.Resources["AppFontFamily"] = fontFam;
            }

            if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                desktop.MainWindow.Resources["AppFontFamily"] = fontFam;
                desktop.MainWindow.FontFamily = fontFam;
            }
            Log.Information("Applied real-time font family: {Font}", cleanFont);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply font family {Font}", fontName);
        }
    }

    public void ApplyBorderRadius(double radiusPx)
    {
        try
        {
            SetDynamicResource("AppCornerRadius", new CornerRadius(radiusPx));
            Log.Information("Applied real-time border radius: {Radius}px", radiusPx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply border radius {Radius}", radiusPx);
        }
    }

    public void ApplyBaseFontSize(double fontSizePx)
    {
        try
        {
            SetDynamicResource("AppBaseFontSize", fontSizePx);
            SetDynamicResource("ControlContentThemeFontSize", fontSizePx);

            var app = Application.Current;
            if (app != null)
            {
                app.Resources["AppBaseFontSize"] = fontSizePx;
                app.Resources["ControlContentThemeFontSize"] = fontSizePx;
            }

            if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                desktop.MainWindow.FontSize = fontSizePx;
                desktop.MainWindow.Resources["AppBaseFontSize"] = fontSizePx;
                desktop.MainWindow.Resources["ControlContentThemeFontSize"] = fontSizePx;
            }

            Log.Information("Applied global system-wide font size: {Size}px", fontSizePx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply base font size {Size}", fontSizePx);
        }
    }

    public void ApplyPosPriceFontSize(double fontSizePx)
    {
        try
        {
            SetDynamicResource("PosPriceFontSize", fontSizePx);
            Log.Information("Applied real-time POS price font size: {Size}px", fontSizePx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply POS price font size {Size}", fontSizePx);
        }
    }

    public void ApplyCartWidth(double widthPx)
    {
        try
        {
            SetDynamicResource("PosCartWidth", widthPx);
            CartWidthChanged?.Invoke(widthPx);
            Log.Information("Applied real-time POS cart width: {Width}px", widthPx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply cart width {Width}", widthPx);
        }
    }

    public void ApplyLogoScale(double scalePx)
    {
        try
        {
            SetDynamicResource("AppLogoHeight", scalePx);
            Log.Information("Applied real-time logo scale: {Scale}px", scalePx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply logo scale {Scale}", scalePx);
        }
    }

    public void ApplyGlassmorphismBlur(double blurPx)
    {
        try
        {
            SetDynamicResource("GlassmorphismBlurRadius", blurPx);
            Log.Information("Applied real-time glassmorphism blur: {Blur}px", blurPx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply glassmorphism blur {Blur}", blurPx);
        }
    }

    public void ApplyGlassmorphismOpacity(double opacityPct)
    {
        try
        {
            _currentOpacityPct = opacityPct;
            byte alpha = (byte)Math.Clamp((int)(255.0 * (opacityPct / 100.0)), 30, 255);

            Color secTransparent = Color.FromArgb(alpha, _baseSecondaryColor.R, _baseSecondaryColor.G, _baseSecondaryColor.B);
            Color tertTransparent = Color.FromArgb(alpha, _baseTertiaryColor.R, _baseTertiaryColor.G, _baseTertiaryColor.B);

            SetDynamicResource("BgSecondaryBrush", secTransparent);
            SetDynamicResource("BgTertiaryBrush", tertTransparent);

            Log.Information("Applied real-time glassmorphism opacity: {Opacity}% (Alpha: {Alpha})", opacityPct, alpha);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply glassmorphism opacity {Opacity}", opacityPct);
        }
    }

    public void ApplyTransitionDuration(double durationMs)
    {
        try
        {
            SetDynamicResource("TransitionDurationMs", durationMs);
            Log.Information("Applied real-time transition duration: {Duration}ms", durationMs);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply transition duration {Duration}", durationMs);
        }
    }

    public void ApplySidebarPosition(string position)
    {
        SidebarPositionChanged?.Invoke(position);
    }

    public void ApplyCartPosition(string position)
    {
        CartPositionChanged?.Invoke(position);
    }
}
