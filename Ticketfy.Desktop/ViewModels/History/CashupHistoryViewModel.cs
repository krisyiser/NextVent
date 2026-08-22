using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Ticketfy.Data;
using Ticketfy.Data.Entities;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Ticketfy.ViewModels.History;

/// <summary>
/// Manages Cashup logs (Cortes de caja/turno) display and physical audit history.
/// Extracted from HistoryViewModel.
/// </summary>
public partial class CashupHistoryViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    public ObservableCollection<CashupEntity> Cashups { get; } = [];

    [ObservableProperty] private bool _isLoading = false;

    public CashupHistoryViewModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task LoadCashupsAsync(DateTime start, DateTime end)
    {
        IsLoading = true;
        try
        {
            var allCashups = await _db.Cashups.ToListAsync();
            var filtered = allCashups
                .Where(c =>
                {
                    if (DateTime.TryParse(c.Timestamp, out var dt))
                    {
                        return dt >= start && dt <= end;
                    }
                    return false;
                })
                .OrderByDescending(c =>
                {
                    DateTime.TryParse(c.Timestamp, out var dt);
                    return dt;
                })
                .ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Cashups.Clear();
                foreach (var c in filtered) Cashups.Add(c);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CashupHistoryViewModel: error loading cashups");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
