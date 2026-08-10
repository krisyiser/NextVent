using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using NextVent.Core.Services;
using NextVent.Data;
using NextVent.Data.Entities;
using Serilog;
using NextVent.Core.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels.Dialogs;

public record ForceLogoutMessage();

public partial class CashupDialogViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly NextVent.Services.Interfaces.IShiftService? _shiftService;
    private readonly ISessionManager? _sessionManager;
    private readonly NextVent.Services.Interfaces.IEscPosPrinterService? _printerService;
    private readonly NextVent.Services.Interfaces.IBackupService? _backupService;
    private readonly NextVent.Services.Interfaces.IAttendanceService? _attendanceService;

    [ObservableProperty] private double _openCashAmount = 1000.00;
    [ObservableProperty] private double _theoreticalCash = 4250.00;
    [ObservableProperty] private double _totalPhysicalCash;
    [ObservableProperty] private double _differenceAmount;
    [ObservableProperty] private bool _isBlindMode = false;
    [ObservableProperty] private bool _isFinalZCut = false;
    private NextVent.Data.Dtos.ShiftDto? _activeShift;

    // Denomination Counts
    [ObservableProperty] private int _count1000;
    [ObservableProperty] private int _count500;
    [ObservableProperty] private int _count200;
    [ObservableProperty] private int _count100;
    [ObservableProperty] private int _count50;
    [ObservableProperty] private int _count20;
    [ObservableProperty] private int _count10;
    [ObservableProperty] private int _count5;
    [ObservableProperty] private int _count2;
    [ObservableProperty] private int _count1;
    [ObservableProperty] private int _count050;

    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public event Action? RequestClose;

    public CashupDialogViewModel(
        AppDbContext db,
        NextVent.Services.Interfaces.IShiftService? shiftService = null,
        ISessionManager? sessionManager = null,
        NextVent.Services.Interfaces.IEscPosPrinterService? printerService = null,
        NextVent.Services.Interfaces.IBackupService? backupService = null,
        bool isFinalZCut = false,
        bool isBlindMode = false,
        NextVent.Services.Interfaces.IAttendanceService? attendanceService = null)
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

    partial void OnCount1000Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount500Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount200Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount100Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount50Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount20Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount10Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount5Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount2Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount1Changed(int value) => RecalculatePhysicalTotal();
    partial void OnCount050Changed(int value) => RecalculatePhysicalTotal();

    private void RecalculatePhysicalTotal()
    {
        TotalPhysicalCash =
            (Count1000 * 1000.0) +
            (Count500 * 500.0) +
            (Count200 * 200.0) +
            (Count100 * 100.0) +
            (Count50 * 50.0) +
            (Count20 * 20.0) +
            (Count10 * 10.0) +
            (Count5 * 5.0) +
            (Count2 * 2.0) +
            (Count1 * 1.0) +
            (Count050 * 0.50);

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
                Count1000 = Count1000,
                Count500 = Count500,
                Count200 = Count200,
                Count100 = Count100,
                Count50 = Count50,
                Count20 = Count20,
                Count10 = Count10,
                Count5 = Count5,
                Count2 = Count2,
                Count1 = Count1,
                Count050 = Count050,
                TheoreticalCash = TheoreticalCash,
                Difference = DifferenceAmount,
                Notes = Notes,
                Timestamp = DateTime.UtcNow.ToBusinessLocalTime().ToString("g")
            };

            _db.Cashups.Add(entity);
            await _db.SaveChangesAsync();

            if (_activeShift != null && IsFinalZCut && _shiftService != null)
            {
                await _shiftService.CloseAsync(_activeShift.Id, TotalPhysicalCash);

                if (_backupService != null)
                {
                    string refId = _activeShift.Id.Length >= 8 ? _activeShift.Id.Substring(0, 8) : _activeShift.Id;
                    await _backupService.CreateZCutBackupAsync(refId);
                }

                if (_sessionManager?.CurrentCashier != null && _attendanceService != null)
                {
                    await _attendanceService.ClockOutAsync(_sessionManager.CurrentCashier.Id.ToString());
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

                // Calculate theoretical cash balance in active shift
                var cashSales = await _db.Sales
                    .AsNoTracking()
                    .Where(s => s.PaymentMethod == "Cash"
                             && s.IsCancelled == 0
                             && string.Compare(s.Date, shift.StartTime) >= 0)
                    .SumAsync(s => s.Total);

                var customerAbonosCash = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == NextVent.Core.Enums.MovementType.AbonoCliente)
                    .SumAsync(m => m.Amount);

                var cashExpenses = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == NextVent.Core.Enums.MovementType.GastoOperativo)
                    .SumAsync(m => m.Amount);

                var cashPurchases = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == NextVent.Core.Enums.MovementType.CompraEfectivo)
                    .SumAsync(m => m.Amount);

                var cashReturns = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shift.Id && m.MovementType == NextVent.Core.Enums.MovementType.DevolucionCliente)
                    .SumAsync(m => m.Amount);

                TheoreticalCash = shift.OpeningBalance + cashSales + customerAbonosCash - cashExpenses - cashPurchases - cashReturns;
                RecalculatePhysicalTotal();
            });
        }
    }
}
