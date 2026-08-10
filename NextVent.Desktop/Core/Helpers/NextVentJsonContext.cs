using System.Collections.Generic;
using System.Text.Json.Serialization;
using NextVent.Data.Dtos;

namespace NextVent.Desktop.Core.Helpers;

/// <summary>
/// Source-generated JSON serialization context for reflection-free AOT compilation.
/// </summary>
[JsonSerializable(typeof(List<CartItemDto>))]
public partial class NextVentJsonContext : JsonSerializerContext
{
}
