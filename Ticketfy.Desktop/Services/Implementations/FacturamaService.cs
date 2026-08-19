using System;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Ticketfy.Core.Models;
using Ticketfy.Services.Interfaces;
using Serilog;

namespace Ticketfy.Services.Implementations;

public class FacturamaService : IFacturamaService
{
    private readonly HttpClient _httpClient;
    public FacturamaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FacturamaCfdiResponse?> CreateInvoiceAsync(FacturamaCfdiRequest request)
    {
        try
        {
            string securePassword = Ticketfy.Services.Security.SecurityManager.GetMasterKey();
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Ticketfy.Data.AppDbContext>()
                .UseSqlite($"Data Source={System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ticketfy", "Database", "ticketfy.db")};Password={securePassword};")
                .Options;
            
            using var tempCtx = new Ticketfy.Data.AppDbContext(options);
            var userSetting = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(tempCtx.Settings, s => s.Key == "FacturamaApiUser");
            var passSetting = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(tempCtx.Settings, s => s.Key == "FacturamaApiPassword");
            var ambSetting = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(tempCtx.Settings, s => s.Key == "FacturamaAmbiente");

            string user = userSetting?.Value ?? "";
            string pass = passSetting?.Value ?? "";
            string ambiente = ambSetting?.Value ?? "Sandbox";

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                throw new Exception("Credenciales de Facturama no configuradas en Ajustes -> Empresa.");
            }

            string baseUrl = ambiente == "Producción" ? "https://api.facturama.mx/" : "https://apisandbox.facturama.mx/";
            _httpClient.BaseAddress = new Uri(baseUrl);

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

            var response = await _httpClient.PostAsJsonAsync(
                "api/3/cfdis", 
                request, 
                FacturamaJsonContext.Default.FacturamaCfdiRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Log.Error($"Error CFDI: {errorMsg}");
                throw new Exception($"Rechazo del SAT/Facturama: {errorMsg}");
            }

            return await response.Content.ReadFromJsonAsync(FacturamaJsonContext.Default.FacturamaCfdiResponse);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fallo en timbrado");
            throw; // Propagate exception to UI
        }
    }
}
