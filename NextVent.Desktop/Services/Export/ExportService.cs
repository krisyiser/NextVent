using System.Text;
using System.Text.Json;
using Serilog;

namespace NextVent.Services.Export;

/// <summary>
/// Exports POS data as JSON or CSV files using native file system.
/// Replaces export.ts Tauri dialog + FS calls with System.IO.
/// </summary>
public sealed class ExportService
{
    /// <summary>
    /// Exports a collection of objects as a formatted JSON file.
    /// </summary>
    public async Task ExportJsonAsync<T>(IEnumerable<T> data, string filePath)
    {
        var json = JsonSerializer.Serialize(data, typeof(IEnumerable<T>), NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        Log.Information("Exported JSON: {Path} ({Count} records)", filePath, data.Count());
    }

    /// <summary>
    /// Exports a collection of objects as a CSV file.
    /// Uses reflection to extract property names as headers.
    /// </summary>
    public async Task ExportCsvAsync<T>(IEnumerable<T> data, string filePath) where T : class
    {
        var items = data.ToList();
        if (items.Count == 0) return;

        var props = typeof(T).GetProperties();
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(",", props.Select(p => $"\"{p.Name}\"")));

        // Rows
        foreach (var item in items)
        {
            var values = props.Select(p =>
            {
                var val = p.GetValue(item)?.ToString() ?? string.Empty;
                return $"\"{val.Replace("\"", "\"\"")}\"";
            });
            sb.AppendLine(string.Join(",", values));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        Log.Information("Exported CSV: {Path} ({Count} records)", filePath, items.Count);
    }
}
