using System.Collections.Generic;
using System.Text.Json.Serialization;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;

namespace NextVent.Desktop.Core.Helpers;

/// <summary>
/// Source-generated JSON serialization context for reflection-free AOT compilation.
/// </summary>
[JsonSerializable(typeof(List<CartItemDto>))]
[JsonSerializable(typeof(CartItemDto))]
[JsonSerializable(typeof(List<SaleItemSnapshotDto>))]
[JsonSerializable(typeof(SaleItemSnapshotDto))]
[JsonSerializable(typeof(SaleEntity))]
[JsonSerializable(typeof(AuditLogEntity))]
[JsonSerializable(typeof(ParkedOrderEntity))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class NextVentJsonContext : JsonSerializerContext
{
}
