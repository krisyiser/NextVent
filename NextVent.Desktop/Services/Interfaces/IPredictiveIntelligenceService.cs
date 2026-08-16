using System.Collections.Generic;
using System.Threading.Tasks;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

public class PredictiveAlertDto
{
    public string Message { get; set; } = string.Empty;
    public decimal DaysRemaining { get; set; }
}

public interface IPredictiveIntelligenceService
{
    // Obtiene el producto más vendido junto con el producto escaneado
    Task<ProductDto?> GetTopCorrelatedProductAsync(string sourceProductId, List<string> currentCartProductIds);

    // Obtiene alertas de resurtido urgente (cadena de suministro)
    Task<List<PredictiveAlertDto>> GetUrgentRestockAlertsAsync();
}
