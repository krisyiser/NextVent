namespace NextVent.Core.Helpers;

/// <summary>
/// Generates prefixed unique identifiers for domain entities.
/// Matches the legacy pattern: PREFIX-{unix_timestamp_ms}
/// </summary>
public static class IdGenerator
{
    /// <summary>Product ID: PROD-{timestamp}</summary>
    public static string NewProductId() => $"PROD-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>Sale ID: SALE-{timestamp}</summary>
    public static string NewSaleId() => $"SALE-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>Payment ID: PAY-{timestamp}</summary>
    public static string NewPaymentId() => $"PAY-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>Shift ID: SHIFT-{timestamp}</summary>
    public static string NewShiftId() => $"SHIFT-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>Attendance ID: ASIST-{timestamp}</summary>
    public static string NewAttendanceId() => $"ASIST-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>Order ID: ORDER-{timestamp}</summary>
    public static string NewOrderId() => $"ORDER-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>Customer ID: CUST-{timestamp}</summary>
    public static string NewCustomerId() => $"CUST-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>User ID: USR-{timestamp}</summary>
    public static string NewUserId() => $"USR-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>Promotion ID: PROMO-{timestamp}</summary>
    public static string NewPromotionId() => $"PROMO-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    /// <summary>Device UUID for licensing: NV-{8_hex_chars}</summary>
    public static string NewDeviceId() => $"NV-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    /// <summary>Bulk product ID with randomness to avoid collisions: PROD-{timestamp}-{random}</summary>
    public static string NewBulkProductId() =>
        $"PROD-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-{Random.Shared.Next(1000, 9999)}";
}
