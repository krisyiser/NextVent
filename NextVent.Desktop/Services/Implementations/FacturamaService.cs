using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using NextVent.Core.Models;
using NextVent.Services.Interfaces;
using Serilog;

namespace NextVent.Services.Implementations;

public class FacturamaService : IFacturamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public FacturamaService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://apisandbox.facturama.mx/");
    }

    public async Task<FacturamaCfdiResponse?> CreateInvoiceAsync(FacturamaCfdiRequest request, string user, string pass)
    {
        try
        {
            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

            var response = await _httpClient.PostAsJsonAsync(
                "api/3/cfdis", 
                request, 
                FacturamaJsonContext.Default.FacturamaCfdiRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                _logger.Error($"Error CFDI: {errorMsg}");
                throw new Exception($"Rechazo del SAT/Facturama: {errorMsg}");
            }

            return await response.Content.ReadFromJsonAsync(FacturamaJsonContext.Default.FacturamaCfdiResponse);
        }
        catch (Exception ex)
        {
            _logger.Error("Fallo en timbrado", ex);
            throw; // Propagate exception to UI
        }
    }
}
