using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
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
        WeakReferenceMessenger.Default.Register<Ticketfy.Core.Messages.CashupSavedMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(() => _ = LoadCashupsAsync(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1)));
        });
    }

    private static bool TryParseCashupDate(string? timestampStr, out DateTime dt)
    {
        dt = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(timestampStr)) return false;

        if (DateTime.TryParse(timestampStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
            return true;
        if (DateTime.TryParse(timestampStr, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dt))
            return true;
        if (DateTime.TryParse(timestampStr, out dt))
            return true;

        return false;
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
                    if (TryParseCashupDate(c.Timestamp, out var dt))
                    {
                        return dt >= start && dt <= end;
                    }
                    return true;
                })
                .OrderByDescending(c =>
                {
                    TryParseCashupDate(c.Timestamp, out var dt);
                    return dt;
                })
                .ToList();

            if (filtered.Count == 0 && start.Date == DateTime.Today)
            {
                filtered = allCashups
                    .OrderByDescending(c =>
                    {
                        TryParseCashupDate(c.Timestamp, out var dt);
                        return dt;
                    })
                    .Take(500)
                    .ToList();
            }

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
