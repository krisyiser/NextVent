using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Core.Services;
using Ticketfy.Data;
using Ticketfy.Data.Entities;
using Serilog;
using Ticketfy.Core.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Dialogs;

public record ForceLogoutMessage();

public partial class CashupDialogViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly Ticketfy.Services.Interfaces.IShiftService? _shiftService;
    private readonly ISessionManager? _sessionManager;
    private readonly Ticketfy.Services.Interfaces.IEscPosPrinterService? _printerService;
    private readonly Ticketfy.Services.Interfaces.IBackupService? _backupService;
    private readonly Ticketfy.Services.Interfaces.IAttendanceService? _attendanceService;

    [ObservableProperty] private double _openCashAmount = 1000.00;
    [ObservableProperty] private double _theoreticalCash = 4250.00;
    [ObservableProperty] private double _totalPhysicalCash;
    [ObservableProperty] private double _differenceAmount;
    [ObservableProperty] private double _grossProfit;
    [ObservableProperty] private double _netProfit;
    [ObservableProperty] private bool _isBlindMode = false;
    [ObservableProperty] private bool _isFinalZCut = false;
    [ObservableProperty] private bool _isFeedbackError = false;
    private Ticketfy.Data.Dtos.ShiftDto? _activeShift;

    // Denomination Counts
    [ObservableProperty] private string _count1000 = "0";
    [ObservableProperty] private string _count500 = "0";
    [ObservableProperty] private string _count200 = "0";
    [ObservableProperty] private string _count100 = "0";
    [ObservableProperty] private string _count50 = "0";
    [ObservableProperty] private string _count20 = "0";
    [ObservableProperty] private string _count10 = "0";
    [ObservableProperty] private string _count5 = "0";
    [ObservableProperty] private string _count2 = "0";
    [ObservableProperty] private string _count1 = "0";
    [ObservableProperty] private string _count050 = "0";

    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public event Action? RequestClose;

    public CashupDialogViewModel(
        AppDbContext db,
        Ticketfy.Services.Interfaces.IShiftService? shiftService = null,
        ISessionManager? sessionManager = null,
        Ticketfy.Services.Interfaces.IEscPosPrinterService? printerService = null,
        Ticketfy.Services.Interfaces.IBackupService? backupService = null,
        bool isFinalZCut = false,
        bool isBlindMode = false,
        Ticketfy.Services.Interfaces.IAttendanceService? attendanceService = null)
    {
        _db = db;
        _shiftService = shiftService;
        _sessionManager = sessionManager;
        _printerService = printerService;
        _backupService = backupService;
        _attendanceService = attendanceService;
        IsFinalZCut = isFinalZCut;
        IsBlindMode = isBlindMode;

        _ = LoadActiveShiftDetailsAsync();
        RecalculatePhysicalTotal();
    }

    partial void OnCount1000Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount500Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount200Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount100Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount50Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount20Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount10Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount5Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount2Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount1Changed(string value) => RecalculatePhysicalTotal();
    partial void OnCount050Changed(string value) => RecalculatePhysicalTotal();

    private void RecalculatePhysicalTotal()
    {
        double.TryParse(Count1000, out double c1000);
        double.TryParse(Count500, out double c500);
        double.TryParse(Count200, out double c200);
        double.TryParse(Count100, out double c100);
        double.TryParse(Count50, out double c50);
        double.TryParse(Count20, out double c20);
        double.TryParse(Count10, out double c10);
        double.TryParse(Count5, out double c5);
        double.TryParse(Count2, out double c2);
        double.TryParse(Count1, out double c1);
        double.TryParse(Count050, out double c050);

        TotalPhysicalCash =
            (c1000 * 1000.0) +
            (c500 * 500.0) +
            (c200 * 200.0) +
            (c100 * 100.0) +
            (c50 * 50.0) +
            (c20 * 20.0) +
            (c10 * 10.0) +
            (c5 * 5.0) +
            (c2 * 2.0) +
            (c1 * 1.0) +
            (c050 * 0.50);

        DifferenceAmount = TotalPhysicalCash - TheoreticalCash;
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    [RelayCommand]
    private async Task OnViewLoadedAsync()
    {
        if (_printerService != null)
        {
            await _printerService.OpenCashDrawerAsync();
        }
    }

    [RelayCommand]
    private async Task SaveCashupAsync()
    {
        try
        {
            var entity = new CashupEntity
            {
                Id = Guid.NewGuid().ToString(),
                OpenCashAmount = OpenCashAmount,
                ClosedCashAmount = TotalPhysicalCash,
                Count1000 = int.TryParse(Count1000, out int i1000) ? i1000 : 0,
                Count500 = int.TryParse(Count500, out int i500) ? i500 : 0,
                Count200 = int.TryParse(Count200, out int i200) ? i200 : 0,
                Count100 = int.TryParse(Count100, out int i100) ? i100 : 0,
                Count50 = int.TryParse(Count50, out int i50) ? i50 : 0,
                Count20 = int.TryParse(Count20, out int i20) ? i20 : 0,
                Count10 = int.TryParse(Count10, out int i10) ? i10 : 0,
                Count5 = int.TryParse(Count5, out int i5) ? i5 : 0,
                Count2 = int.TryParse(Count2, out int i2) ? i2 : 0,
                Count1 = int.TryParse(Count1, out int i1) ? i1 : 0,
                Count050 = int.TryParse(Count050, out int i050) ? i050 : 0,
                TheoreticalCash = TheoreticalCash,
                Difference = DifferenceAmount,
                Notes = Notes,
                Type = IsFinalZCut ? "Final" : "Parcial",
                Timestamp = DateTime.Now.ToBusinessLocalTime().ToString("g")
            };

            _db.Cashups.Add(entity);
            await _db.SaveChangesAsync();

            if (_activeShift != null && _shiftService != null)
            {
                await _shiftService.CloseAsync(_activeShift.Id, TotalPhysicalCash);

                if (IsFinalZCut)
                {
                    if (_backupService != null)
                    {
                        string refId = _activeShift.Id.Length >= 8 ? _activeShift.Id.Substring(0, 8) : _activeShift.Id;
                        await _backupService.CreateZCutBackupAsync(refId);
                    }

                    if (_sessionManager?.CurrentCashier != null && _attendanceService != null)
                    {
                        await _attendanceService.ClockOutAsync(_sessionManager.CurrentCashier.Id.ToString());
                    }
                }

                _sessionManager?.ClearSession();
                WeakReferenceMessenger.Default.Send(new ForceLogoutMessage());
            }

            FeedbackMessage = "¡Corte y Arqueo de Caja guardado correctamente!";
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving cashup audit");
            FeedbackMessage = "Error al guardar arqueo";
        }
    }

    private async Task LoadActiveShiftDetailsAsync()
    {
        if (_shiftService == null) return;

        var shift = await _shiftService.GetActiveAsync();
        if (shift != null)
        {
            _activeShift = shift;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                OpenCashAmount = shift.OpeningBalance;

                // Calculate theoretical cash balance in active shift (Cash only)
                var cashSales = await _db.Sales
                    .AsNoTracking()
                    .Where(s => (s.PaymentMethod == "Cash" || s.PaymentMethod == "Efectivo")
                             && s.Status == Ticketfy.Core.Enums.SaleStatus.Completed
                             && string.Compare(s.Date, shift.StartTime) >= 0)
                    .SumAsync(s => s.Total);

                var customerAbonosCash = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.AbonoCliente && m.Description.Contains("Efectivo"))
                    .SumAsync(m => m.Amount);

                var cashExpenses = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.GastoOperativo && m.Description.Contains("Efectivo"))
                    .SumAsync(m => m.Amount);

                var cashPurchases = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.CompraEfectivo)
                    .SumAsync(m => m.Amount);

                var cashReturns = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.DevolucionCliente && m.Description.Contains("Efectivo"))
                    .SumAsync(m => m.Amount);

                TheoreticalCash = shift.OpeningBalance + cashSales + customerAbonosCash - cashExpenses - cashPurchases - cashReturns;
                
                // Calculate Profit (All sales and expenses, regardless of payment method)
                var allSalesTotal = await _db.Sales
                    .AsNoTracking()
                    .Where(s => s.Status == Ticketfy.Core.Enums.SaleStatus.Completed
                             && string.Compare(s.Date, shift.StartTime) >= 0)
                    .SumAsync(s => s.Total);
                    
                var allSalesCogs = await _db.Sales
                    .AsNoTracking()
                    .Where(s => s.Status == Ticketfy.Core.Enums.SaleStatus.Completed
                             && string.Compare(s.Date, shift.StartTime) >= 0)
                    .SumAsync(s => s.TotalCost);

                var allExpensesTotal = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.GastoOperativo)
                    .SumAsync(m => m.Amount);

                GrossProfit = allSalesTotal;
                NetProfit = GrossProfit - allExpensesTotal - allSalesCogs;
                
                RecalculatePhysicalTotal();
            });
        }
    }
}
