using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Core.Models;
using NextVent.Services.Interfaces;
using Serilog;

namespace NextVent.ViewModels;

public partial class CashierPerformanceViewModel : ObservableObject
{
    private readonly IPerformanceAnalyticsService _performanceService;
    private readonly IAttendanceService? _attendanceService;

    public ObservableCollection<CashierProductivityReportModel> CashierProductivityReports { get; } = [];

    [ObservableProperty] private DateTimeOffset _startDate = DateTimeOffset.Now.AddDays(-7);
    [ObservableProperty] private DateTimeOffset _endDate = DateTimeOffset.Now;
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public CashierPerformanceViewModel(IPerformanceAnalyticsService performanceService, IAttendanceService? attendanceService = null)
    {
        _performanceService = performanceService;
        _attendanceService = attendanceService;
        _ = LoadReportsAsync();
    }

    [RelayCommand]
    public async Task LoadReportsAsync()
    {
        IsLoading = true;
        try
        {
            if (_attendanceService != null)
            {
                await _attendanceService.AutoCloseForgottenAttendancesAsync();
            }

            var reports = await _performanceService.CalculateTrueCashierProductivityAsync(StartDate.DateTime, EndDate.DateTime);
            CashierProductivityReports.Clear();
            foreach (var r in reports)
            {
                CashierProductivityReports.Add(r);
            }
            FeedbackMessage = $"Reporte cargado: {CashierProductivityReports.Count} cajeros analizados";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading Cashier Productivity Reports");
            FeedbackMessage = "Error al calcular la productividad de cajeros";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
