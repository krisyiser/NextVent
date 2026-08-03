using System;

namespace NextVent.Core.Models;

public class ShiftMovementSlipModel
{
    public string Folio { get; set; } = string.Empty;
    public string MovementTypeLabel { get; set; } = "MOVIMIENTO DE CAJA";
    public double Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CashierName { get; set; } = "Cajero Matrix";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
