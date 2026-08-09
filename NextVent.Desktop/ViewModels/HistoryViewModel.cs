using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using NextVent.Core.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public record HourlySalesDto(string HourLabel, int TicketCount, double TotalRevenue, bool IsPeakHour);

public partial class HistoryViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IEscPosPrinterService _printerService;

    public ObservableCollection<SaleDto> Sales { get; } = [];
    public ObservableCollection<HourlySalesDto> HourlyReport { get; } = [];
    public ObservableCollection<CashierPerformanceDto> CashierPerformanceReport { get; } = [];

    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty] private string _peakHourLabel = "Ninguna";
    [ObservableProperty] private double _peakHourRevenue = 0.0;
    [ObservableProperty] private double _totalDayRevenue = 0.0;
    [ObservableProperty] private DateTimeOffset? _startDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private DateTimeOffset? _endDate = DateTimeOffset.Now.Date.AddDays(1).AddTicks(-1);
    [ObservableProperty] private bool _isLoading = false;

    public event Action? OpenCashupRequested;
    public event Action<SaleDto>? OpenReturnRequested;

    public HistoryViewModel(ISaleService saleService, IEscPosPrinterService printerService)
    {
        _saleService = saleService;
        _printerService = printerService;
        _ = LoadSalesAsync();
        _ = LoadCashierPerformanceAsync();
    }

    public Task LoadSalesAsync() => FetchSalesHistoryAsync();

    [RelayCommand]
    private async Task FetchSalesHistoryAsync()
    {
        IsLoading = true;
        try
        {
            DateTime start = StartDate?.DateTime ?? DateTime.Today;
            DateTime end = EndDate?.DateTime ?? DateTime.Today.AddDays(1).AddTicks(-1);

            DateTime utcStart = start.ToBusinessUtcTime();
            DateTime utcEnd = end.ToBusinessUtcTime();

            var salesList = await _saleService.GetSalesByDateRangeAsync(utcStart, utcEnd);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Sales.Clear();
                foreach (var sale in salesList.Take(500))
                {
                    Sales.Add(sale);
                }

                CalculateHourlyReport(Sales.ToList());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching date-filtered sales history");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadCashierPerformanceAsync()
    {
        try
        {
            var list = await _saleService.GetCashierPerformanceReportAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CashierPerformanceReport.Clear();
                foreach (var c in list) CashierPerformanceReport.Add(c);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading cashier performance");
        }
    }

    private void CalculateHourlyReport(List<SaleDto> salesList)
    {
        HourlyReport.Clear();
        var validSales = salesList.Where(s => !s.IsCancelled).ToList();
        TotalDayRevenue = validSales.Sum(s => s.Total);

        var groups = validSales
            .GroupBy(s =>
            {
                if (DateTime.TryParse(s.Date, out var dt))
                {
                    var utcDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    return utcDt.ToBusinessLocalTime().Hour;
                }
                return 0;
            })
            .OrderBy(g => g.Key)
            .ToList();

        double maxRev = 0.0;
        int peakHour = -1;

        foreach (var g in groups)
        {
            var sum = g.Sum(s => s.Total);
            if (sum > maxRev)
            {
                maxRev = sum;
                peakHour = g.Key;
            }
        }

        if (peakHour >= 0)
        {
            PeakHourLabel = $"{peakHour:D2}:00 - {(peakHour + 1):D2}:00";
            PeakHourRevenue = maxRev;
        }

        foreach (var g in groups)
        {
            var h = g.Key;
            var isPeak = h == peakHour;
            HourlyReport.Add(new HourlySalesDto($"{h:D2}:00 - {(h + 1):D2}:00", g.Count(), g.Sum(s => s.Total), isPeak));
        }
    }

    [RelayCommand]
    private async Task RePrintTicketAsync(SaleDto sale)
    {
        if (sale == null) return;
        try
        {
            var success = await _printerService.PrintTicketAsync(sale, "COM1");
            FeedbackMessage = success ? "¡Ticket enviado a la impresora térmica ESC/POS!" : "Error enviando a la impresora";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reprinting ticket");
            FeedbackMessage = "Error al imprimir ticket";
        }
    }

    [RelayCommand]
    private async Task CancelSaleAsync(SaleDto sale)
    {
        if (sale == null || sale.IsCancelled) return;
        try
        {
            await _saleService.CancelAsync(sale.Id);
            FeedbackMessage = "Venta cancelada e inventario restablecido";
            await LoadSalesAsync();
            await LoadCashierPerformanceAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error cancelling sale");
        }
    }

    [RelayCommand]
    private void OpenReturnDialog(SaleDto sale)
    {
        if (sale != null && !sale.IsCancelled)
        {
            OpenReturnRequested?.Invoke(sale);
        }
    }

    [RelayCommand]
    private void PerformCashCutoff() => OpenCashupRequested?.Invoke();
}
