using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Ticketfy.Core.Helpers;
using Ticketfy.Services.Interfaces;
using Polly;

namespace Ticketfy.Services.Implementations;

public class PaymentIntentRequest
{
    public decimal amount { get; set; }
    public string description { get; set; } = string.Empty;
}

[JsonSerializable(typeof(PaymentIntentRequest))]
[JsonSerializable(typeof(PaymentIntentStatusResponse))]
public partial class MercadoPagoJsonContext : JsonSerializerContext
{
}

public static class SettingsServiceExtensions
{
    public static string GetTerminalDeviceId(this ISettingsService settings)
    {
        // For testing we return a mock value if not configured
        var task = settings.GetAsync("TerminalDeviceId");
        task.Wait();
        return task.Result ?? "PDV_01";
    }

    public static string GetApiToken(this ISettingsService settings)
    {
        var task = settings.GetAsync("PaymentApiToken");
        task.Wait();
        return task.Result ?? "TEST-123456789-TOKEN";
    }
}

public class PaymentIntentStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public PaymentResponseData? Payment { get; set; }
}

public class PaymentResponseData
{
    public string Id { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string CardLastFourDigits { get; set; } = string.Empty;
}

public class MercadoPagoTerminalService : IPaymentTerminalService
{
    private readonly HttpClient _httpClient;
    private readonly string _deviceId;
    private readonly string _accessToken;

    public MercadoPagoTerminalService(HttpClient httpClient, ISettingsService settings)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.mercadopago.com");
        _deviceId = settings.GetTerminalDeviceId();
        _accessToken = settings.GetApiToken();
        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        }
    }

    public async Task<Result<PaymentResponse>> ProcessPaymentAsync(decimal amount, string referenceId, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Enviar el monto a la terminal física
            var intentRequest = new PaymentIntentRequest { amount = Math.Round(amount, 2), description = $"Venta {referenceId}" };
            var response = await _httpClient.PostAsJsonAsync($"/point/integration-api/devices/{_deviceId}/payment-intents", intentRequest, MercadoPagoJsonContext.Default.PaymentIntentRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return Result<PaymentResponse>.Failure("Error al comunicar con la terminal TPV.");
            }

            // 2. Hacer Polling con Polly (Revisar estado cada 2 segundos)
            var retryPolicy = Policy
                .HandleResult<string>(status => status == "PROCESSING" || status == "WAITING_FOR_CARD" || status == "INITIAL")
                .WaitAndRetryAsync(30, _ => TimeSpan.FromSeconds(2)); // Esperar máximo 60 segundos

            var finalStatus = await retryPolicy.ExecuteAsync(async () => 
            {
                try 
                {
                    var statusResponse = await _httpClient.GetFromJsonAsync<PaymentIntentStatusResponse>($"/point/integration-api/payment-intents/{referenceId}", MercadoPagoJsonContext.Default.PaymentIntentStatusResponse, cancellationToken);
                    return statusResponse?.Status ?? "UNKNOWN";
                }
                catch (OperationCanceledException)
                {
                    return "CANCELLED";
                }
            });

            if (finalStatus == "FINISHED") 
            {
                // Fetch final details
                var statusResponse = await _httpClient.GetFromJsonAsync<PaymentIntentStatusResponse>($"/point/integration-api/payment-intents/{referenceId}", MercadoPagoJsonContext.Default.PaymentIntentStatusResponse, cancellationToken);
                return Result<PaymentResponse>.Success(new PaymentResponse 
                { 
                    TransactionId = statusResponse?.Payment?.Id ?? "UNKNOWN",
                    AuthorizationCode = statusResponse?.Payment?.AuthorizationCode ?? "000000",
                    CardBrand = statusResponse?.Payment?.PaymentMethodId ?? "CARD",
                    Last4Digits = statusResponse?.Payment?.CardLastFourDigits ?? "****"
                });
            }
            
            return Result<PaymentResponse>.Failure("El cobro fue rechazado o cancelado en la terminal.");
        }
        catch (OperationCanceledException)
        {
            return Result<PaymentResponse>.Failure("Operación cancelada por el usuario.");
        }
        catch (Exception ex)
        {
            return Result<PaymentResponse>.Failure($"Error de red: {ex.Message}");
        }
    }

    public async Task<Result> CancelPaymentIntentAsync(string referenceId)
    {
        try
        {
            // Llamada a la API para cancelar la luz de la terminal física
            await _httpClient.DeleteAsync($"/point/integration-api/devices/{_deviceId}/payment-intents/{referenceId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error cancelando: {ex.Message}");
        }
    }
}
