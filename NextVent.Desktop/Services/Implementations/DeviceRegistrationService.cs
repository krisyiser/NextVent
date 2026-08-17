using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using NextVent.Core.Helpers;
using NextVent.Core.Services;
using NextVent.Services.Interfaces;
using Serilog;

namespace NextVent.Services.Implementations;

public class BusinessProfile
{
    [JsonPropertyName("businessName")] public string BusinessName { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
}

public class BusinessData
{
    [JsonPropertyName("commercialName")] public string CommercialName { get; set; } = string.Empty;
    [JsonPropertyName("industry")] public string Industry { get; set; } = string.Empty;
}

public class SystemSpecs
{
    [JsonPropertyName("cpuModel")] public string CpuModel { get; set; } = string.Empty;
    [JsonPropertyName("totalRam")] public string TotalRam { get; set; } = string.Empty;
    [JsonPropertyName("primaryStorage")] public string PrimaryStorage { get; set; } = string.Empty;
}

public class SessionInfo
{
    [JsonPropertyName("activeUser")] public string ActiveUser { get; set; } = string.Empty;
    [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
}

public class ProvisionPayload
{
    [JsonPropertyName("hardwareId")] public string HardwareId { get; set; } = string.Empty;
    [JsonPropertyName("localIp")] public string LocalIp { get; set; } = string.Empty;
    [JsonPropertyName("installedVersion")] public string InstalledVersion { get; set; } = string.Empty;
    [JsonPropertyName("business")] public BusinessData Business { get; set; } = new();
    [JsonPropertyName("system")] public SystemSpecs System { get; set; } = new();
    [JsonPropertyName("session")] public SessionInfo Session { get; set; } = new();
}

public class ProvisionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("nodeId")]
    public string? NodeId { get; set; }
    [JsonPropertyName("licenseToken")]
    public string? LicenseToken { get; set; }
}

[JsonSerializable(typeof(ProvisionPayload))]
[JsonSerializable(typeof(ProvisionResponse))]
public partial class TelemetryJsonContext : JsonSerializerContext { }

public class DeviceRegistrationService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService? _settingsService;
    private readonly ISessionManager? _sessionManager;
    private const string NEXTVENT_API_KEY = "nv_sk_valcore_5f8a9"; 
    private const string API_URL = "https://api.valcore/api/v1/nodes/provision";

    public DeviceRegistrationService(ISettingsService? settingsService = null, ISessionManager? sessionManager = null)
    {
        _settingsService = settingsService;
        _sessionManager = sessionManager;

        // PARCHE SSL: Aceptar el certificado autofirmado de Traefik / Tailscale
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NEXTVENT_API_KEY);
    }

    public async Task RegisterNodeAsync(BusinessProfile profile)
    {
        await PingServerAsync(profile);
    }

    public async Task PingServerAsync(BusinessProfile profile)
    {
        try
        {
            var machineId = HardwareIdentityHelper.GetMotherboardUUID() ?? "DEV-MACHINE-" + Environment.MachineName;
            var localIp = NetworkHelper.GetLocalIpAddress() ?? "127.0.0.1";

            var payload = new ProvisionPayload
            {
                HardwareId = machineId,
                LocalIp = localIp,
                InstalledVersion = "1.0.0"
            };

            // 1. Lectura de Hardware Profunda (AOT-Safe)
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var cpuName = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null) as string;
                    payload.System.CpuModel = string.IsNullOrWhiteSpace(cpuName) ? "Unknown CPU" : cpuName.Trim();
                }
                else
                {
                    payload.System.CpuModel = "Unknown CPU";
                }

                // RAM (AOT-Safe)
                var memInfo = GC.GetGCMemoryInfo();
                payload.System.TotalRam = $"{(memInfo.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024)):F1} GB";

                // Storage (AOT-Safe)
                var mainDrive = System.IO.DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == System.IO.DriveType.Fixed);
                if (mainDrive != null)
                {
                    payload.System.PrimaryStorage = $"Disco Local - {(mainDrive.TotalSize / (1024.0 * 1024 * 1024)):F0} GB";
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to read hardware specs");
            }

            // 2. Lectura de Entorno de Negocio
            if (_settingsService != null)
            {
                var name = await _settingsService.GetAsync("EmpresaNombreComercial");
                payload.Business.CommercialName = string.IsNullOrEmpty(name) ? profile.BusinessName : name;
                
                var industry = await _settingsService.GetAsync("EmpresaGiroComercial");
                payload.Business.Industry = string.IsNullOrEmpty(industry) ? "No especificado" : industry;
            }
            else
            {
                payload.Business.CommercialName = string.IsNullOrEmpty(profile.BusinessName) ? "Negocio Prueba" : profile.BusinessName;
            }

            // 3. Lectura de Sesión Activa
            if (_sessionManager?.CurrentCashier != null)
            {
                payload.Session.ActiveUser = _sessionManager.CurrentCashier.FullName ?? _sessionManager.CurrentCashier.Username ?? "Unknown";
                payload.Session.Role = _sessionManager.CurrentCashier.Role.ToString();
            }

            // Fire and Forget
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Aumentado a 5s para el primer handshake TLS
                    
                    // Uso estricto del Source Generator para AOT
                    var response = await _httpClient.PostAsJsonAsync(API_URL, payload, TelemetryJsonContext.Default.ProvisionPayload, cts.Token);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorText = await response.Content.ReadAsStringAsync(cts.Token);
                        Log.Warning($"Fallo en Phone Home. Status: {response.StatusCode}. Detalle: {errorText}");

                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            if (System.IO.File.Exists("license.jwt"))
                            {
                                System.IO.File.Delete("license.jwt");
                                Log.Warning("❌ LICENCIA REVOCADA POR EL SERVIDOR (Kill Switch Activo).");
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                                    {
                                        if (desktop.MainWindow != null)
                                        {
                                            desktop.MainWindow.Content = new NextVent.Views.LicenseLockedView { DataContext = new NextVent.ViewModels.LicenseLockedViewModel() };
                                        }
                                    }
                                });
                            }
                        }
                    }
                    else
                    {
                        Log.Information("✅ TELEMETRÍA ENVIADA CON ÉXITO AL NEXTVENT HUB.");
                        try 
                        {
                            var jsonResponse = await response.Content.ReadFromJsonAsync(TelemetryJsonContext.Default.ProvisionResponse, cts.Token);
                            if (jsonResponse != null)
                            {
                                if (!string.IsNullOrEmpty(jsonResponse.LicenseToken))
                                {
                                    await System.IO.File.WriteAllTextAsync("license.jwt", jsonResponse.LicenseToken, cts.Token);
                                    Log.Information("✅ LICENCIA ACTUALIZADA DESDE NEXTVENT HUB.");
                                }
                                else
                                {
                                    if (System.IO.File.Exists("license.jwt"))
                                    {
                                        System.IO.File.Delete("license.jwt");
                                        Log.Warning("❌ LICENCIA REVOCADA POR EL SERVIDOR (Kill Switch Activo).");
                                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                        {
                                            if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                                            {
                                                if (desktop.MainWindow != null && !(desktop.MainWindow.Content is NextVent.Views.LicenseLockedView))
                                                {
                                                    desktop.MainWindow.Content = new NextVent.Views.LicenseLockedView { DataContext = new NextVent.ViewModels.LicenseLockedViewModel() };
                                                }
                                            }
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Log.Error(innerEx, "Error al procesar la respuesta de la licencia.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error crítico en Phone Home: {ex.Message}", ex);
                }
            });
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error en inicialización de Phone Home");
        }
    }
}
