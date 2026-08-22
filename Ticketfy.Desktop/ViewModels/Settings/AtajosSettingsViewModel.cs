using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Static keyboard shortcuts reference. No persistence needed — data is fixed.
/// </summary>
public partial class AtajosSettingsViewModel : ObservableObject
{
    public List<KeyboardShortcut> Shortcuts { get; } = new()
    {
        new("F1", "Ayuda contextual"),
        new("F2", "Ir a Punto de Venta"),
        new("F3", "Ir a Inventario"),
        new("F4", "Ir a Clientes"),
        new("F5", "Ir a Proveedores"),
        new("F6", "Ir a Gastos"),
        new("F7", "Ir a Historial"),
        new("F8", "Ir a Promociones"),
        new("F9", "Ir a Facturación SAT"),
        new("F10", "Ir a Ajustes"),
        new("F11", "Pantalla Completa"),
        new("Ctrl+B", "Abrir buscador de producto"),
        new("Ctrl+P", "Imprimir ticket o reporte activo"),
        new("Esc", "Cerrar diálogo activo"),
    };

    public record KeyboardShortcut(string Key, string Description);
}
