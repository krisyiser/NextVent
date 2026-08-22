using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Ticketfy.ViewModels;

public partial class SettingsViewModel
{
    // ── TICKET & thermal printing options ──────────────────────────────────
    [ObservableProperty] private string _selectedPaperWidth = "80mm (Estándar POS)";
    [ObservableProperty] private bool _autoPrintTicketOnCheckout = true;
    [ObservableProperty] private bool _autoCutPaper = true;
    [ObservableProperty] private string _ticketHeaderLine1 = "TICKETFY! ENTERPRISE POS";
    [ObservableProperty] private string _ticketHeaderLine2 = "GRACIAS POR SU PREFERENCIA";
    [ObservableProperty] private string _ticketFooterLine1 = "CONSERVE ESTE TICKET PARA CUALQUIER ACLARACIÓN";
    [ObservableProperty] private string _ticketFooterLine2 = "www.ticketfy.pos";

    public ObservableCollection<string> PaperWidths { get; } = ["58mm (Compacto)", "80mm (Estándar POS)"];

    // ── INTERFAZ COMPONENTES EXTRA ──────────────────────────────────────────
    [ObservableProperty] private double _grosorBordePx = 1.0;
    [ObservableProperty] private bool _showStockBadge = true;
    [ObservableProperty] private bool _showSkuProducto = true;
    [ObservableProperty] private bool _showQuickAddButton = true;
}
