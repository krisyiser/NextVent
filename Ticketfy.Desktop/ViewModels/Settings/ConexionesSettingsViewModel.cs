using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Manages hardware connections: COM ports for printers, scanners and scales.
/// </summary>
public partial class ConexionesSettingsViewModel : ObservableObject
{
    private readonly ISettingsService? _settingsService;

    public ObservableCollection<string> ComPortOptions { get; } = [];
    public ObservableCollection<string> BaudRateOptions { get; } = ["9600", "19200", "38400", "57600", "115200"];
    public ObservableCollection<string> PrinterInterfaceOptions { get; } = ["USB (RAW)", "Red (TCP/IP)", "Puerto Serial (COM)"];
    public ObservableCollection<string> ScannerInterfaceOptions { get; } = ["USB HID / Virtual COM", "Red (TCP Socket)"];

    [ObservableProperty] private string _printerInterface = "USB (RAW)";
    [ObservableProperty] private string _printerComPort = string.Empty;
    [ObservableProperty] private string _printerBaudRate = "9600";
    [ObservableProperty] private string _printerTcpIp = string.Empty;
    [ObservableProperty] private int _printerTcpPort = 9100;
    [ObservableProperty] private string _scannerInterface = "USB HID / Virtual COM";
    [ObservableProperty] private string _scannerComPort = string.Empty;
    [ObservableProperty] private string _scaleComPort = string.Empty;
    [ObservableProperty] private string _scaleBaudRate = "9600";
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public ConexionesSettingsViewModel(ISettingsService? settingsService = null)
    {
        _settingsService = settingsService;
        LoadComPorts();
        if (_settingsService != null) _ = LoadAsync();
    }

    private void LoadComPorts()
    {
        try
        {
            foreach (var port in System.IO.Ports.SerialPort.GetPortNames())
                ComPortOptions.Add(port);
        }
        catch { }
    }

    public async Task LoadAsync()
    {
        if (_settingsService == null) return;
        try
        {
            var d = await _settingsService.GetAllAsync();
            if (d.TryGetValue("PrinterInterface", out var pi)) PrinterInterface = pi;
            if (d.TryGetValue("PrinterComPort", out var pc)) PrinterComPort = pc;
            if (d.TryGetValue("PrinterBaudRate", out var pb)) PrinterBaudRate = pb;
            if (d.TryGetValue("PrinterTcpIp", out var ip)) PrinterTcpIp = ip;
            if (d.TryGetValue("PrinterTcpPort", out var pt) && int.TryParse(pt, out var ptv)) PrinterTcpPort = ptv;
            if (d.TryGetValue("ScannerInterface", out var si)) ScannerInterface = si;
            if (d.TryGetValue("ScannerComPort", out var sc)) ScannerComPort = sc;
            if (d.TryGetValue("ScaleComPort", out var scp)) ScaleComPort = scp;
            if (d.TryGetValue("ScaleBaudRate", out var sbr)) ScaleBaudRate = sbr;
        }
        catch (Exception ex) { Log.Error(ex, "ConexionesSettingsViewModel: error loading"); }
    }

    public async Task SaveAsync()
    {
        if (_settingsService == null) return;
        await _settingsService.SetAsync("PrinterInterface", PrinterInterface);
        await _settingsService.SetAsync("PrinterComPort", PrinterComPort);
        await _settingsService.SetAsync("PrinterBaudRate", PrinterBaudRate);
        await _settingsService.SetAsync("PrinterTcpIp", PrinterTcpIp);
        await _settingsService.SetAsync("PrinterTcpPort", PrinterTcpPort.ToString());
        await _settingsService.SetAsync("ScannerInterface", ScannerInterface);
        await _settingsService.SetAsync("ScannerComPort", ScannerComPort);
        await _settingsService.SetAsync("ScaleComPort", ScaleComPort);
        await _settingsService.SetAsync("ScaleBaudRate", ScaleBaudRate);
        FeedbackMessage = "¡Configuración de hardware guardada!";
    }

    [RelayCommand] private async Task Save() => await SaveAsync();
    [RelayCommand] private void RefreshComPorts() { ComPortOptions.Clear(); LoadComPorts(); }
}
