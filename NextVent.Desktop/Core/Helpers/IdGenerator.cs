namespace NextVent.Core.Helpers;

/// <summary>
/// Generates prefixed unique identifiers for domain entities.
/// Matches the legacy pattern: PREFIX-{unix_timestamp_ms}
/// </summary>
public static class IdGenerator
{
    /// <summary>Product ID: PROD-{timestamp}</summary>
    public static string NewProductId() => $"PROD-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>Sale ID: SALE-{timestamp}</summary>
    public static string NewSaleId() => $"SALE-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>Payment ID: PAY-{timestamp}</summary>
    public static string NewPaymentId() => $"PAY-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>Shift ID: SHIFT-{timestamp}</summary>
    public static string NewShiftId() => $"SHIFT-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>Attendance ID: ASIST-{timestamp}</summary>
    public static string NewAttendanceId() => $"ASIST-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>Order ID: ORDER-{timestamp}</summary>
    public static string NewOrderId() => $"ORDER-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>Customer ID: CUST-{timestamp}</summary>
    public static string NewCustomerId() => $"CUST-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>User ID: USR-{timestamp}</summary>
    public static string NewUserId() => $"USR-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>Promotion ID: PROMO-{timestamp}</summary>
    public static string NewPromotionId() => $"PROMO-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    /// <summary>Device UUID for licensing: NV-{8_hex_chars}</summary>
    public static string NewDeviceId() => $"NV-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    /// <summary>Bulk product ID with randomness to avoid collisions: PROD-{timestamp}-{random}</summary>
    public static string NewBulkProductId() =>
        $"PROD-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(1000, 9999)}";
}
