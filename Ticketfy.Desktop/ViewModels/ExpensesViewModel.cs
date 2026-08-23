using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels;

public partial class ExpensesViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;
    private readonly IShiftService _shiftService;

    public ObservableCollection<ExpenseDto> Expenses { get; } = [];
    public ObservableCollection<string> Categories { get; } = [
        "Renta", "Servicios (Luz/Agua/Internet)", "Nómina / Salarios", "Mantenimiento", "Transporte", "Varios"
    ];

    [ObservableProperty] private string _selectedCategory = "Renta";
    [ObservableProperty] private double _expenseAmount;
    [ObservableProperty] private string _expenseDescription = string.Empty;
    [ObservableProperty] private string _paymentMethod = "Efectivo";

    // Financial Metrics (Double representation kept for compatibility)
    [ObservableProperty] private double _totalRevenue;
    [ObservableProperty] private double _totalCostOfGoods;
    [ObservableProperty] private double _grossProfit;
    [ObservableProperty] private double _totalExpenses;
    [ObservableProperty] private double _netProfit;

    // Cash-Flow Metrics (Decimal representation)
    [ObservableProperty] private decimal _fondoInicial;
    [ObservableProperty] private decimal _ingresos;
    [ObservableProperty] private decimal _pagosConTarjeta;
    [ObservableProperty] private decimal _costoDeVentas;
    [ObservableProperty] private decimal _egresos;
    [ObservableProperty] private decimal _totalEnCaja;
    [ObservableProperty] private decimal _reinversion;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UtilidadNetaColor))]
    private decimal _utilidadNeta;

    public string UtilidadNetaColor => UtilidadNeta >= 0 ? "#059669" : "#EF4444";

    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public ExpensesViewModel(IExpenseService expenseService, IShiftService shiftService)
    {
        _expenseService = expenseService;
        _shiftService = shiftService;
        _ = LoadExpensesAsync();
    }

    public async Task LoadExpensesAsync()
    {
        try
        {
            var list = await _expenseService.GetAllAsync();
            var activeShift = await _shiftService.GetActiveAsync();

            DateTime? shiftStart = null;
            if (activeShift != null && !string.IsNullOrWhiteSpace(activeShift.StartTime))
            {
                if (DateTime.TryParse(activeShift.StartTime, out var parsedStart))
                {
                    shiftStart = parsedStart;
                }
            }

            var summary = await _expenseService.GetFinancialSummaryAsync(shiftStart, DateTime.Now);
            decimal openingBalance = activeShift != null ? (decimal)activeShift.OpeningBalance : 0m;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Expenses.Clear();
                foreach (var e in list) Expenses.Add(e);

                TotalRevenue = summary.TotalRevenue;
                TotalCostOfGoods = summary.TotalCostOfGoodsSold;
                GrossProfit = summary.GrossProfit;
                TotalExpenses = summary.TotalExpenses;
                NetProfit = summary.NetProfit;

                FondoInicial = openingBalance;
                Ingresos = (decimal)summary.TotalRevenue;
                PagosConTarjeta = (decimal)summary.CardRevenue;
                CostoDeVentas = (decimal)summary.TotalCostOfGoodsSold;
                Egresos = (decimal)summary.TotalExpenses;
                
                // Total en Caja (Fondo Inicial + Ventas en Efectivo - Gastos en Efectivo)
                decimal cashRev = (decimal)summary.CashRevenue;
                decimal cashExp = (decimal)summary.CashExpenses;
                TotalEnCaja = FondoInicial + cashRev - cashExp;

                Reinversion = CostoDeVentas;
                // Utilidad Neta Real = Ventas - Costo de Ventas (COGS) - Gastos Operativos
                UtilidadNeta = Ingresos - CostoDeVentas - Egresos;
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading expenses data");
        }
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (ExpenseAmount <= 0)
        {
            FeedbackMessage = "Ingrese un monto mayor a cero";
            return;
        }

        try
        {
            var dto = new ExpenseDto(
                Guid.NewGuid().ToString(),
                SelectedCategory,
                ExpenseAmount,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ExpenseDescription,
                PaymentMethod,
                "admin"
            );

            var created = await _expenseService.CreateAsync(dto);
            Expenses.Insert(0, created);

            ExpenseAmount = 0;
            ExpenseDescription = string.Empty;
            FeedbackMessage = "Gasto registrado correctamente";

            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error adding expense");
            FeedbackMessage = "Error al registrar gasto";
        }
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(ExpenseDto expense)
    {
        try
        {
            await _expenseService.DeleteAsync(expense.Id);
            Expenses.Remove(expense);
            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting expense");
        }
    }
}
