using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Ticketfy.ViewModels;

public partial class SettingsViewModel
{
    // ── CONEXIONES & HARDWARE ──────────────────────────────────────────────
    [ObservableProperty] private string _printerPort = "COM1";
    [ObservableProperty] private string _barcodeScannerMode = "USB-HID (Teclado emulado)";
    [ObservableProperty] private string _scalePort = "Desconectado";
    [ObservableProperty] private string _cashDrawerPort = "Conectado a Impresora RJ11 (Pin 2)";

    public ObservableCollection<string> PrinterPortOptions { get; } = ["COM1", "COM2", "COM3", "COM4", "USB001", "USB002", "Red LAN / IP"];
    public ObservableCollection<string> ScannerModeOptions { get; } = ["USB-HID (Teclado emulado)", "Puerto Serie Virtual (COM)", "Bluetooth SPP"];
    public ObservableCollection<string> ScalePortOptions { get; } = ["Desconectado", "COM1", "COM2", "COM3", "COM4", "USB Serial"];
    public ObservableCollection<string> CashDrawerPortOptions { get; } = [
        "Conectado a Impresora RJ11 (Pin 2)", "Conectado a Impresora RJ11 (Pin 5)", "Puerto COM Directo", "Desconectado"
    ];
}
