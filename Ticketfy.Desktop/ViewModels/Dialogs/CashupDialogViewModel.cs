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
using Avalonia.Threading;

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

    [ObservableProperty] private double _openCashAmount = 0.0;
    [ObservableProperty] private double _totalIngresosShift = 0.0;
    [ObservableProperty] private double _totalEgresosShift = 0.0;
    [ObservableProperty] private double _theoreticalCash = 0.0;
    [ObservableProperty] private double _totalPhysicalCash;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DifferenceAmountColor))]
    [NotifyPropertyChangedFor(nameof(DifferenceStatusLabel))]
    private double _differenceAmount;

    public string DifferenceAmountColor => Math.Abs(DifferenceAmount) < 0.001 ? "#10B981" : (DifferenceAmount > 0 ? "#3B82F6" : "#EF4444");
    public string DifferenceStatusLabel => Math.Abs(DifferenceAmount) < 0.001 ? "Cuadre Exacto" : (DifferenceAmount > 0 ? "Sobrante" : "Faltante");

    [ObservableProperty] private double _grossProfit;
    [ObservableProperty] private double _netProfit;
    [ObservableProperty] private bool _isBlindMode = false;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    [NotifyPropertyChangedFor(nameof(IsPartialMode))]
    private bool _isFinalZCut = false;

    public bool IsPartialMode
    {
        get => !IsFinalZCut;
        set => IsFinalZCut = !value;
    }

    public string SaveButtonText => IsFinalZCut 
        ? "GUARDAR CORTE FINAL Y CERRAR TURNO" 
        : "GUARDAR CORTE PARCIAL";

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
        bool dbSavedSuccessfully = false;
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
                CashierName = _sessionManager?.CurrentCashier?.FullName ?? "Cajero en turno",
                CashierRole = _sessionManager?.CurrentCashier?.DisplayRole ?? "CAJERO",
                Timestamp = DateTime.Now.ToString("g")
            };

            _db.Cashups.Add(entity);
            await _db.SaveChangesAsync();
            dbSavedSuccessfully = true;

            IsFeedbackError = false;
            FeedbackMessage = IsFinalZCut 
                ? "¡Cierre Z de Turno guardado correctamente!" 
                : "¡Arqueo Parcial de Caja guardado correctamente!";

            // Print Thermal Audit Slip
            if (_printerService != null)
            {
                try
                {
                    await _printerService.PrintNonSaleCashMovementSlipAsync(new Ticketfy.Core.Models.ShiftMovementSlipModel
                    {
                        Folio = entity.Id.Length >= 8 ? entity.Id.Substring(0, 8).ToUpper() : entity.Id.ToUpper(),
                        MovementTypeLabel = IsFinalZCut ? "CORTE FINAL Z" : "ARQUEO PARCIAL DE CAJA",
                        Amount = entity.ClosedCashAmount,
                        Description = $"Físico: ${entity.ClosedCashAmount:N2} | Teórico: ${entity.TheoreticalCash:N2} | Dif: ${entity.Difference:N2}",
                        CashierName = _sessionManager?.CurrentCashier?.FullName ?? "CAJERO EN TURNO",
                        Timestamp = DateTime.Now
                    });
                }
                catch (Exception exPrint)
                {
                    Log.Warning(exPrint, "Aviso imprimiendo ticket de arqueo de caja");
                }
            }

            if (IsFinalZCut)
            {
                var activeShift = _activeShift ?? (_shiftService != null ? await _shiftService.GetActiveAsync() : null);
                string? shiftId = activeShift?.Id;

                if (string.IsNullOrEmpty(shiftId))
                {
                    var openEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.IsOpen == 1);
                    shiftId = openEntity?.Id;
                }

                if (_shiftService != null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(shiftId))
                        {
                            await _shiftService.CloseAsync(shiftId, TotalPhysicalCash);
                            Log.Information("ShiftService: Closed shift {ShiftId} with physical cash ${Amount:N2}", shiftId, TotalPhysicalCash);
                        }
                        else
                        {
                            var newClosedShift = await _shiftService.OpenAsync(OpenCashAmount);
                            await _shiftService.CloseAsync(newClosedShift.Id, TotalPhysicalCash);
                            shiftId = newClosedShift.Id;
                            Log.Information("ShiftService: Created and closed placeholder shift {ShiftId}", newClosedShift.Id);
                        }
                    }
                    catch (Exception exShift)
                    {
                        Log.Warning(exShift, "Aviso cerrando el turno en ShiftService");
                    }
                }

                try
                {
                    if (_backupService != null)
                    {
                        string refId = (!string.IsNullOrEmpty(shiftId) && shiftId.Length >= 8) ? shiftId.Substring(0, 8) : (shiftId ?? "ZCUT");
                        await _backupService.CreateZCutBackupAsync(refId);
                    }
                }
                catch (Exception exBackup)
                {
                    Log.Warning(exBackup, "Aviso generando respaldo ZCutBackup");
                }

                try
                {
                    if (_sessionManager?.CurrentCashier != null && _attendanceService != null)
                    {
                        await _attendanceService.ClockOutAsync(_sessionManager.CurrentCashier.Id.ToString());
                    }
                }
                catch (Exception exClock)
                {
                    Log.Warning(exClock, "Aviso registrando salida de cajero");
                }

                try
                {
                    _sessionManager?.ClearSession();
                }
                catch (Exception exSession)
                {
                    Log.Warning(exSession, "Aviso limpiando sesión");
                }

                // Close the modal dialog first, then post ForceLogoutMessage to seamlessly switch shell to login screen!
                RequestClose?.Invoke();

                Dispatcher.UIThread.Post(() =>
                {
                    WeakReferenceMessenger.Default.Send(new ForceLogoutMessage());
                });

                return;
            }

            await Task.Delay(400);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving cashup audit entity");
            if (dbSavedSuccessfully)
            {
                // Entity was saved to DB successfully, close dialog cleanly!
                RequestClose?.Invoke();
            }
            else
            {
                IsFeedbackError = true;
                FeedbackMessage = "Error al guardar el registro del arqueo en la base de datos.";
            }
        }
    }

    private async Task LoadActiveShiftDetailsAsync()
    {
        try
        {
            var shift = _shiftService != null ? await _shiftService.GetActiveAsync() : null;
            DateTime? shiftStart = null;
            double openingBalance = 0.0;
            string? shiftId = null;

            if (shift != null)
            {
                _activeShift = shift;
                openingBalance = shift.OpeningBalance;
                shiftId = shift.Id;
                if (DateTime.TryParse(shift.StartTime, out var parsedStart))
                {
                    shiftStart = parsedStart;
                }
            }
            else
            {
                // Fallback to start of current calendar day if no active shift entity found
                shiftStart = DateTime.Today;
            }

            var allSales = await _db.Sales
                .AsNoTracking()
                .Where(s => s.Status == Ticketfy.Core.Enums.SaleStatus.Completed && s.IsCancelled == 0)
                .ToListAsync();

            var shiftSales = allSales
                .Where(s => s.Date.IsInDateRange(shiftStart, null))
                .ToList();

            var cashSales = shiftSales
                .Where(s => s.PaymentMethod != null && (s.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase) || s.PaymentMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase)))
                .Sum(s => s.Total);

            double customerAbonosCash = 0.0;
            double cashExpenses = 0.0;
            double cashReturns = 0.0;

            if (!string.IsNullOrEmpty(shiftId))
            {
                customerAbonosCash = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shiftId && m.MovementType == Ticketfy.Core.Enums.MovementType.AbonoCliente)
                    .SumAsync(m => m.Amount);

                cashExpenses = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shiftId && m.MovementType == Ticketfy.Core.Enums.MovementType.GastoOperativo)
                    .SumAsync(m => m.Amount);

                cashReturns = await _db.ShiftMovements
                    .AsNoTracking()
                    .Where(m => m.ShiftId == shiftId && m.MovementType == Ticketfy.Core.Enums.MovementType.DevolucionCliente)
                    .SumAsync(m => m.Amount);
            }

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                OpenCashAmount = openingBalance;
                TotalIngresosShift = cashSales + customerAbonosCash;
                TotalEgresosShift = cashExpenses + cashReturns;
                TheoreticalCash = OpenCashAmount + TotalIngresosShift - TotalEgresosShift;
                
                double allSalesTotal = shiftSales.Sum(s => s.Total);
                double allSalesCogs = shiftSales.Sum(s => s.TotalCost);
                double allExpensesTotal = cashExpenses;

                GrossProfit = allSalesTotal;
                NetProfit = GrossProfit - allExpensesTotal - allSalesCogs;
                
                RecalculatePhysicalTotal();
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading active shift details for cashup dialog");
        }
    }
}
