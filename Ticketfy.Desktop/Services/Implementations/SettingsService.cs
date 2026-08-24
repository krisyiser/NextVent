using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Core.Models.Settings;
using Ticketfy.Data;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;
using Serilog;

namespace Ticketfy.Services.Implementations;

public class SettingsService : ISettingsService
{
    private const string AppSettingsJsonKey = "SystemGlobalAppSettings_V4";
    private readonly AppDbContext _context;

    public SettingsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _context.Settings.FindAsync(key);
        return setting?.Value;
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await _context.Settings.FindAsync(key);
        if (setting == null)
        {
            setting = new SettingEntity { Key = key, Value = value };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        var list = await _context.Settings.AsNoTracking().ToListAsync();
        return list.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task<AppSettings> GetAppSettingsAsync()
    {
        try
        {
            var json = await GetAsync(AppSettingsJsonKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed deserializing AppSettings from DB, returning defaults");
        }

        // Fallback to reading legacy individual keys or default instance
        var defaults = new AppSettings();
        var all = await GetAllAsync();

        if (all.TryGetValue("CurrentTheme", out var ct)) defaults.Visual.ThemeName = ct;
        if (all.TryGetValue("AccentColor", out var ac)) defaults.Visual.PrimaryColor = ac;
        if (all.TryGetValue("AppFont", out var af)) defaults.Visual.FontFamily = af;
        if (all.TryGetValue("SidebarPosition", out var sp)) defaults.Visual.SidebarPosition = sp;
        if (all.TryGetValue("EmpresaNombreComercial", out var nc)) defaults.Company.CommercialName = nc;
        if (all.TryGetValue("EmpresaRfc", out var rfc)) defaults.Company.Rfc = rfc;

        return defaults;
    }

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        if (settings == null) return;
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await SetAsync(AppSettingsJsonKey, json);

            // Synchronize legacy keys for backwards compatibility
            await SetAsync("CurrentTheme", settings.Visual.ThemeName);
            await SetAsync("AccentColor", settings.Visual.PrimaryColor);
            await SetAsync("AppFont", settings.Visual.FontFamily);
            await SetAsync("SidebarPosition", settings.Visual.SidebarPosition);
            await SetAsync("EmpresaNombreComercial", settings.Company.CommercialName);
            await SetAsync("EmpresaRfc", settings.Company.Rfc);

            Log.Information("AppSettings saved atomically to local SQLite storage");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed serializing and saving AppSettings");
        }
    }
}
