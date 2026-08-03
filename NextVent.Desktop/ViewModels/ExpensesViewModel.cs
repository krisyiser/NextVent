using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public partial class ExpensesViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;

    public ObservableCollection<ExpenseDto> Expenses { get; } = [];
    public ObservableCollection<string> Categories { get; } = [
        "Renta", "Servicios (Luz/Agua/Internet)", "Nómina / Salarios", "Mantenimiento", "Transporte", "Varios"
    ];

    [ObservableProperty] private string _selectedCategory = "Renta";
    [ObservableProperty] private double _expenseAmount;
    [ObservableProperty] private string _expenseDescription = string.Empty;
    [ObservableProperty] private string _paymentMethod = "Efectivo";

    // Financial Metrics
    [ObservableProperty] private double _totalRevenue;
    [ObservableProperty] private double _totalCostOfGoods;
    [ObservableProperty] private double _grossProfit;
    [ObservableProperty] private double _totalExpenses;
    [ObservableProperty] private double _netProfit;

    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public ExpensesViewModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
        _ = LoadExpensesAsync();
    }

    public async Task LoadExpensesAsync()
    {
        try
        {
            var list = await _expenseService.GetAllAsync();
            var summary = await _expenseService.GetFinancialSummaryAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Expenses.Clear();
                foreach (var e in list) Expenses.Add(e);

                TotalRevenue = summary.TotalRevenue;
                TotalCostOfGoods = summary.TotalCostOfGoodsSold;
                GrossProfit = summary.GrossProfit;
                TotalExpenses = summary.TotalExpenses;
                NetProfit = summary.NetProfit;
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
