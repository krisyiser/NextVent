using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

using Ticketfy.Core.Helpers;
using Ticketfy.Core.Enums;
using System.Text.Json.Serialization;

namespace Ticketfy.Data.Dtos;

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
    double PointsRewarded = 0.0,
    double ReorderQuantity = 10.0,
    string LocationRack = "Pasillo 1 - Anaquel A",
    string SatProductCode = "01010101",
    string SatUnitCode = "H87",
    double MinStock = 5.0,
    string? DefaultSupplierId = null,
    bool IsBulk = false,
    bool IsKit = false
)
{
    public double CostPrice => Cost;
    public double SalePrice => Price;
    public bool IsOutOfStock => Stock <= 0.0;
    public bool IsAvailable => Stock > 0.0;
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
    /// <summary>Remaining credit headroom = LimiteCredito - Deuda. Always >= 0.</summary>
    public double AvailableCredit => Math.Max(0.0, LimiteCredito - Deuda);
    public bool IsWholesale => Name.Contains("Mayorista", StringComparison.OrdinalIgnoreCase);
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

public record PromotionDto(
    string Id,
    string Name,
    double DiscountValue,
    bool IsActive,
    Ticketfy.Core.Enums.PromotionType StrategyType = Ticketfy.Core.Enums.PromotionType.PercentageDiscount,
    string? TargetProductId = null,
    string TargetCategory = "",
    double MinQuantity = 1.0,
    double FreeQuantity = 0.0
);

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
    public string SatProductCode { get; set; } = "01010101";
    public string SatUnitCode { get; set; } = "H87";
    public double PointsRewarded { get; set; } = 0.0;

    private double _quantity = 1.0;
    public double Quantity
    {
        get => _quantity;
        private set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(FinalUnitPrice));
                OnPropertyChanged(nameof(IsFractional));
                OnPropertyChanged(nameof(IsNotFractional));
            }
        }
    }

    public (bool Success, string ErrorMessage) IncreaseQuantity(double amountToAdd, double absoluteDbStock)
    {
        if (Quantity + amountToAdd > absoluteDbStock)
        {
            return (false, $"Stock físico insuficiente. Límite: {absoluteDbStock}");
        }

        Quantity += amountToAdd;
        return (true, string.Empty);
    }

    public void DecreaseQuantity(double amountToSubtract)
    {
        if (Quantity - amountToSubtract >= 1)
        {
            Quantity -= amountToSubtract;
        }
    }

    public void OverrideQuantity(double newQuantity, double absoluteDbStock)
    {
        if (newQuantity <= absoluteDbStock)
        {
            Quantity = newQuantity;
        }
    }

    public bool IsFractional => Quantity % 1.0 != 0.0;
    public bool IsNotFractional => !IsFractional;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    [NotifyPropertyChangedFor(nameof(FinalUnitPrice))]
    private double _appliedDiscountAmount = 0.0;

    [ObservableProperty]
    private string? _appliedPromotionId;

    [ObservableProperty]
    private string _promotionDescription = string.Empty;

    public double FinalUnitPrice => Math.Max(0.0, OriginalUnitPrice - (AppliedDiscountAmount / Math.Max(1.0, Quantity)));

    public double TotalPrice => GetLineTotal();

    public double GetLineTotal() 
    {
        return Math.Max(0.0, (OriginalUnitPrice * Quantity) - AppliedDiscountAmount);
    }

    public CartItemDto() { }

    public CartItemDto(string id, string name, double unitPrice, double quantity = 1.0, string unit = "Pza", double pointsRewarded = 0.0)
    {
        Id = id;
        Name = name;
        UnitPrice = unitPrice;
        OriginalUnitPrice = unitPrice;
        Quantity = quantity;
        Unit = unit;
        PointsRewarded = pointsRewarded;
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
    string SatProductCode = "01010101",
    string SatUnitCode = "H87",
    [property: JsonPropertyName("proratedGlobalDiscountAmount")] double ProratedGlobalDiscountAmount = 0.0,
    [property: JsonPropertyName("taxAmount")] double TaxAmount = 0.0,
    [property: JsonPropertyName("returnedQuantity")] double ReturnedQuantity = 0.0,
    [property: JsonPropertyName("pointsRewarded")] double PointsRewarded = 0.0
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
    string? SerieFolio = null,
    SaleStatus Status = SaleStatus.Completed,
    string? InvoiceId = null,
    string? InvoiceStatus = null,
    string? CashierUserId = null,
    string? CashierName = null
)
{
    public string LocalDateDisplay => DateTimeExtensions.ToLocalDisplayString(Date);

    public string PaymentStatusDisplay
    {
        get
        {
            if (IsCancelled) return "CANCELADO";
            if (Items != null && System.Linq.Enumerable.Any(Items, i => i.ReturnedQuantity > 0))
            {
                bool allReturned = System.Linq.Enumerable.All(Items, i => i.ReturnedQuantity >= i.Quantity);
                return allReturned ? "DEVUELTO TOTAL" : "DEVOLUCIÓN PARCIAL";
            }
            return IsCredit ? "CRÉDITO (PENDIENTE)" : "COBRADO";
        }
    }

    public string PaymentStatusColor
    {
        get
        {
            if (IsCancelled) return "#EF4444"; // Red
            if (Items != null && System.Linq.Enumerable.Any(Items, i => i.ReturnedQuantity > 0)) return "#F59E0B"; // Amber
            return IsCredit ? "#3B82F6" : "#10B981"; // Blue / Green
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
    double NetProfit,
    double CashRevenue = 0,
    double CashExpenses = 0,
    double CardRevenue = 0
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
