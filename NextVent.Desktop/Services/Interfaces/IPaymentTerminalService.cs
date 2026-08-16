using NextVent.Core.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace NextVent.Services.Interfaces;

public class PaymentResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
    public string CardBrand { get; set; } = string.Empty; // Visa, Mastercard, AMEX
    public string Last4Digits { get; set; } = string.Empty;
}

public interface IPaymentTerminalService
{
    // Inicia la intención de cobro en la terminal física
    Task<Result<PaymentResponse>> ProcessPaymentAsync(decimal amount, string referenceId, CancellationToken cancellationToken);
    
    // Permite cancelar el cobro desde la PC si el cliente se arrepiente
    Task<Result> CancelPaymentIntentAsync(string referenceId);
}
