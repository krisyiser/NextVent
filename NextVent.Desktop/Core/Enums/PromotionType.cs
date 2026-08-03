namespace NextVent.Core.Enums;

/// <summary>
/// Promotion engine rule types.
/// Product: discount on a specific product.
/// Category: discount on all products in a category.
/// MultiBuy: N×M promotion (e.g., buy 3 pay 2).
/// </summary>
public enum PromotionType
{
    Product = 0,
    Category = 1,
    MultiBuy = 2
}
