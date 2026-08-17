using System.Collections.Generic;
using NextVent.Data.Dtos;

namespace NextVent.Core.Messages;

public record CartStateSnapshotMessage(
    IReadOnlyList<CartItemDto> Items,
    double Subtotal,
    double TotalDiscount,
    double GrandTotal,
    string LastAddedProductName
);

public record CustomerDisplayIdleStateMessage(bool IsIdle);

public record FocusSearchMessage();

public record UserDeletedMessage(string UserId);
