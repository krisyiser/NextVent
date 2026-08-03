using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Entities;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NextVent.ViewModels.Dialogs;

public partial class CashupDialogViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    [ObservableProperty] private double _openCashAmount = 1000.00;
    [ObservableProperty] private double _theoreticalCash = 4250.00;
    [ObservableProperty] private double _totalPhysicalCash;
    [ObservableProperty] private double _differenceAmount;

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

    public CashupDialogViewModel(AppDbContext db)
    {
        _db = db;
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
                Timestamp = DateTime.Now.ToString("g")
            };

            _db.Cashups.Add(entity);
            await _db.SaveChangesAsync();

            FeedbackMessage = "¡Corte y Arqueo de Caja guardado correctamente!";
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving cashup audit");
            FeedbackMessage = "Error al guardar arqueo";
        }
    }
}
