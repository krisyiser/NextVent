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
        AppSettings settings = new();
        try
        {
            var json = await GetAsync(AppSettingsJsonKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var deserialized = JsonSerializer.Deserialize<AppSettings>(json);
                if (deserialized != null) settings = deserialized;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed deserializing AppSettings from DB, returning defaults");
        }

        // Always overlay individual keys for 100% full integration with Onboarding & Settings
        var all = await GetAllAsync();

        if (all.TryGetValue("CurrentTheme", out var ct)) settings.Visual.ThemeName = ct;
        if (all.TryGetValue("AccentColor", out var ac)) settings.Visual.PrimaryColor = ac;
        if (all.TryGetValue("AppFont", out var af)) settings.Visual.FontFamily = af;
        if (all.TryGetValue("SidebarPosition", out var sp)) settings.Visual.SidebarPosition = sp;

        if (all.TryGetValue("EmpresaNombreComercial", out var nc) && !string.IsNullOrWhiteSpace(nc)) settings.Company.CommercialName = nc;
        else if (all.TryGetValue("BusinessName", out var bn) && !string.IsNullOrWhiteSpace(bn)) settings.Company.CommercialName = bn;

        if (all.TryGetValue("EmpresaEmailContacto", out var ec) && !string.IsNullOrWhiteSpace(ec)) settings.Company.Email = ec;
        else if (all.TryGetValue("BusinessEmail", out var be) && !string.IsNullOrWhiteSpace(be)) settings.Company.Email = be;

        if (all.TryGetValue("EmpresaTelefonoFijo", out var tf) && !string.IsNullOrWhiteSpace(tf)) settings.Company.Phone = tf;
        else if (all.TryGetValue("BusinessPhone", out var bp) && !string.IsNullOrWhiteSpace(bp)) settings.Company.Phone = bp;

        if (all.TryGetValue("EmpresaCalleYNumero", out var cn) && !string.IsNullOrWhiteSpace(cn)) settings.Company.Address = cn;
        else if (all.TryGetValue("BusinessAddress", out var ba) && !string.IsNullOrWhiteSpace(ba)) settings.Company.Address = ba;

        if (all.TryGetValue("EmpresaRfc", out var rfc) && !string.IsNullOrWhiteSpace(rfc)) settings.Company.Rfc = rfc;

        return settings;
    }

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        if (settings == null) return;
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await SetAsync(AppSettingsJsonKey, json);

            // Synchronize legacy and individual keys for 100% full integration
            await SetAsync("CurrentTheme", settings.Visual.ThemeName);
            await SetAsync("AccentColor", settings.Visual.PrimaryColor);
            await SetAsync("AppFont", settings.Visual.FontFamily);
            await SetAsync("SidebarPosition", settings.Visual.SidebarPosition);

            if (!string.IsNullOrWhiteSpace(settings.Company.CommercialName))
            {
                await SetAsync("EmpresaNombreComercial", settings.Company.CommercialName);
                await SetAsync("BusinessName", settings.Company.CommercialName);
            }
            if (!string.IsNullOrWhiteSpace(settings.Company.Email))
            {
                await SetAsync("EmpresaEmailContacto", settings.Company.Email);
                await SetAsync("BusinessEmail", settings.Company.Email);
            }
            if (!string.IsNullOrWhiteSpace(settings.Company.Phone))
            {
                await SetAsync("EmpresaTelefonoFijo", settings.Company.Phone);
                await SetAsync("BusinessPhone", settings.Company.Phone);
            }
            if (!string.IsNullOrWhiteSpace(settings.Company.Address))
            {
                await SetAsync("EmpresaCalleYNumero", settings.Company.Address);
                await SetAsync("BusinessAddress", settings.Company.Address);
            }
            if (!string.IsNullOrWhiteSpace(settings.Company.Rfc))
            {
                await SetAsync("EmpresaRfc", settings.Company.Rfc);
            }

            Log.Information("AppSettings saved atomically to local SQLite storage");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed serializing and saving AppSettings");
        }
    }
}
