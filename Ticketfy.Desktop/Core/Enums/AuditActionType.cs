namespace Ticketfy.Core.Enums;

public enum AuditActionType
{
    CartItemRemoved = 0,
    CartCleared = 1,
    ParkedOrderCancelled = 2,
    PriceOverride = 3,
    ManualDiscountExceeded = 4,
    InventoryStockAdjustment = 5,
    ShiftDrawerOpenedManually = 6,
    UserLoginFailed = 7
}
