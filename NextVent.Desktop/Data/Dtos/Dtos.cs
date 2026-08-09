using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

using NextVent.Core.Helpers;
using System.Text.Json.Serialization;

namespace NextVent.Data.Dtos;

public record ProductDto(
    string Id,
    string? Barcode,
    string Name,
    double Cost,
    double Price,
    double WholesalePrice = 0.0,
    int WholesaleThreshold = 0,
    double Stock = 0.0,
    string Category = "General",
    string Unit = "Pza",
    int ExpiresSoon = 0,
    string? CreatedAt = null,
    double PointsRewarded = 1.0,
    double ReorderQuantity = 10.0,
    string LocationRack = "Pasillo 1 - Anaquel A",
    string ClaveSat = "50202306",
    string UnidadSat = "H87",
    double MinStock = 5.0
)
{
    public double CostPrice => Cost;
    public double SalePrice => Price;
}

public record CustomerDto(
    string Id,
    string Nombre,
    string Telefono,
    string Email,
    string Rfc,
    double LimiteCredito,
    double Deuda = 0.0,
    double PuntosSaldo = 0.0,
    double CurrentBalance = 0.0,
    string CustomerCode = ""
)
{
    public string Name => Nombre;
    public string Phone => Telefono;
    public double CreditLimit => LimiteCredito;
    public double Debt => Deuda;
}

public record CustomerPaymentDto(
    string Id,
    string CustomerId,
    string Date,
    double Amount,
    string? PaymentMethod = "Efectivo",
    string? Reference = null
)
{
    public string Method => PaymentMethod ?? "Efectivo";
    public string Notes => Reference ?? "";
};

public record FiscalClientDto(
    string Id,
    string CustomerId,
    string Rfc,
    string RazonSocial,
    string RegimenFiscal,
    string CodigoPostal,
    string UsoCfdi
);

public record PromotionDto(string Id, string Name, double DiscountValue, bool IsActive);

public record UserDto(string Id, string Username, string FullName, string Role, bool IsActive)
{
    public string Nombre => FullName;
    public string Rol => Role;
    public string Estatus => IsActive ? "ACTIVO" : "INACTIVO";
}

public record ShiftDto(
    string Id,
    string StartTime,
    string? EndTime,
    double OpeningBalance,
    double TotalCashSales,
    double TotalCreditSales,
    double ExpectedBalance,
    double? ActualBalance,
    double? Diff,
    int IsOpen
);

public record ParkedOrderDto(string Id, DateTime CreatedAt, string CustomerName, string CartJson, double Total);

public partial class CartItemDto : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string ProductId => Id;
    public string Name { get; set; } = string.Empty;
    public double UnitPrice { get; set; }
    public double OriginalUnitPrice { get; set; }
    public double Cost { get; set; }
    public string Unit { get; set; } = "Pza";
    public string Category { get; set; } = "General";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    [NotifyPropertyChangedFor(nameof(FinalUnitPrice))]
    private double _quantity = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    [NotifyPropertyChangedFor(nameof(FinalUnitPrice))]
    private double _appliedDiscountAmount = 0.0;

    [ObservableProperty]
    private string? _appliedPromotionId;

    [ObservableProperty]
    private string _promotionDescription = string.Empty;

    public double FinalUnitPrice => Math.Max(0.0, OriginalUnitPrice - (AppliedDiscountAmount / Math.Max(1.0, Quantity)));

    public double TotalPrice => Math.Max(0.0, (OriginalUnitPrice * Quantity) - AppliedDiscountAmount);

    public CartItemDto() { }

    public CartItemDto(string id, string name, double unitPrice, double quantity = 1.0, string unit = "Pza")
    {
        Id = id;
        Name = name;
        UnitPrice = unitPrice;
        OriginalUnitPrice = unitPrice;
        Quantity = quantity;
        Unit = unit;
    }
}

public record TicketItemDto(string ProductId, string Name, double UnitPrice, double Cost, double Quantity, string Unit, string Category, double Discount, double TotalPrice);

public record SaleItemSnapshotDto(
    string ProductId,
    string Name,
    double UnitPrice,
    double Cost,
    double Quantity,
    string Unit,
    string Category,
    double Discount,
    double TotalPrice,
    double OriginalUnitPrice = 0.0,
    double AppliedDiscountAmount = 0.0,
    string? AppliedPromotionId = null,
    [property: JsonPropertyName("proratedGlobalDiscountAmount")] double ProratedGlobalDiscountAmount = 0.0,
    [property: JsonPropertyName("taxAmount")] double TaxAmount = 0.0,
    [property: JsonPropertyName("returnedQuantity")] double ReturnedQuantity = 0.0
)
{
    public string Id => ProductId;
    public double AvailableForReturn => Math.Max(0.0, Quantity - ReturnedQuantity);
}

public record SaleDto(
    string Id,
    string Date,
    List<SaleItemSnapshotDto> Items,
    double Total,
    double TotalCost,
    double Profit,
    double PaidAmount,
    double ChangeAmount,
    string PaymentMethod,
    string? CustomerId,
    bool IsCredit,
    bool IsCancelled,
    string? CancelledAt,
    string EstadoFiscal = "PENDIENTE",
    string? UuidSat = null,
    string? SerieFolio = null
)
{
    public string LocalDateDisplay
    {
        get
        {
            if (DateTime.TryParse(Date, out var dt))
            {
                var utcDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                return utcDt.ToBusinessLocalTime().ToString("g");
            }
            return Date;
        }
    }
}

public record SupplierDto(
    string Id,
    string Name,
    string Rfc,
    string Phone,
    string Email,
    string Address,
    string ContactPerson,
    bool IsActive = true
);

public record PurchaseItemDto(
    string Id,
    string PurchaseId,
    string ProductId,
    string ProductName,
    double UnitPrice,
    double Quantity,
    double TotalPrice
);

public record PurchaseDto(
    string Id,
    string SupplierId,
    string SupplierName,
    string InvoiceNumber,
    string Date,
    double TotalCost,
    string Notes,
    List<PurchaseItemDto> Items
);

public record ExpenseDto(
    string Id,
    string Category,
    double Amount,
    string Date,
    string Description,
    string PaymentMethod,
    string RegisteredByUser
);

public record FinancialSummaryDto(
    double TotalRevenue,
    double TotalCostOfGoodsSold,
    double GrossProfit,
    double TotalExpenses,
    double NetProfit
);

public record ItemKitItemDto(
    string Id,
    string ItemKitId,
    string ProductId,
    string ProductName,
    double Quantity
);

public record ItemKitDto(
    string Id,
    string KitBarcode,
    string Name,
    double Price,
    string Description,
    List<ItemKitItemDto> Items
);

public record ShiftNoteDto(
    string Id,
    string CashierName,
    string CreatedAt,
    string NoteText,
    string Category,
    bool IsResolved
);

public record CashierPerformanceDto(
    string CashierName,
    int TicketCount,
    double TotalRevenue,
    double AverageTicketValue,
    double CommissionRatePct,
    double EarnedCommissionAmount
);