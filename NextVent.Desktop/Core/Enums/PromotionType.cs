namespace NextVent.Core.Enums;

public enum PromotionType
{
    PercentageDiscount = 0,   // e.g., 15% off
    FixedAmountDiscount = 1,  // e.g., $20 off per item
    BuyNGetM = 2,             // e.g., Buy 2 Get 1 Free (NxM)
    VolumeTier = 3,           // e.g., 5 to 9 units @ $18, 10+ units @ $15
    WholesalePrice = 4        // Explicit wholesale price override when Qty >= Threshold
}
