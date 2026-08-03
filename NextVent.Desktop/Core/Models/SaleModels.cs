using System;
using System.Collections.Generic;
using NextVent.Core.Enums;
using NextVent.Data.Dtos;

namespace NextVent.Core.Models;

public record SaleCreationDto(
    string? CustomerId,
    List<CartItemDto> Items,
    double Total,
    double CreditAmount,
    PaymentMethod PaymentMethod,
    string? GiftcardNumber = null,
    double GiftcardAmount = 0
);

public class SaleResultModel
{
    public bool IsSuccess { get; set; }
    public string SaleId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
