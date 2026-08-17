using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NextVent.Core.Models;

public class FacturamaCfdiRequest
{
    [JsonPropertyName("Receiver")] public CfdiReceiver Receiver { get; set; } = new();
    [JsonPropertyName("CfdiType")] public string CfdiType { get; set; } = "I";
    [JsonPropertyName("PaymentForm")] public string PaymentForm { get; set; } = "01";
    [JsonPropertyName("PaymentMethod")] public string PaymentMethod { get; set; } = "PUE";
    [JsonPropertyName("Currency")] public string Currency { get; set; } = "MXN";
    [JsonPropertyName("ExpeditionPlace")] public string ExpeditionPlace { get; set; } = "";
    [JsonPropertyName("Items")] public List<CfdiItem> Items { get; set; } = new();
}

public class CfdiReceiver
{
    [JsonPropertyName("Rfc")] public string Rfc { get; set; } = "";
    [JsonPropertyName("Name")] public string Name { get; set; } = "";
    [JsonPropertyName("CfdiUse")] public string CfdiUse { get; set; } = "G03";
    [JsonPropertyName("FiscalRegime")] public string FiscalRegime { get; set; } = "616";
    [JsonPropertyName("TaxZipCode")] public string TaxZipCode { get; set; } = "";
}

public class CfdiItem
{
    [JsonPropertyName("ProductCode")] public string ProductCode { get; set; } = ""; // Viene del DB local
    [JsonPropertyName("IdentificationNumber")] public string IdentificationNumber { get; set; } = "";
    [JsonPropertyName("Description")] public string Description { get; set; } = "";
    [JsonPropertyName("Unit")] public string Unit { get; set; } = "Pieza";
    [JsonPropertyName("UnitCode")] public string UnitCode { get; set; } = ""; // Viene del DB local
    [JsonPropertyName("UnitPrice")] public decimal UnitPrice { get; set; }
    [JsonPropertyName("Quantity")] public decimal Quantity { get; set; }
    [JsonPropertyName("Subtotal")] public decimal Subtotal { get; set; }
    [JsonPropertyName("Taxes")] public List<CfdiTax> Taxes { get; set; } = new();
}

public class CfdiTax
{
    [JsonPropertyName("Name")] public string Name { get; set; } = "IVA";
    [JsonPropertyName("IsRetention")] public bool IsRetention { get; set; } = false;
    [JsonPropertyName("Rate")] public decimal Rate { get; set; } = 0.16m;
    [JsonPropertyName("Total")] public decimal Total { get; set; }
    [JsonPropertyName("Base")] public decimal Base { get; set; }
}

public class FacturamaCfdiResponse
{
    [JsonPropertyName("Id")] public string Id { get; set; } = "";
    [JsonPropertyName("Status")] public string Status { get; set; } = "";
}

[JsonSerializable(typeof(FacturamaCfdiRequest))]
[JsonSerializable(typeof(FacturamaCfdiResponse))]
public partial class FacturamaJsonContext : JsonSerializerContext { }
