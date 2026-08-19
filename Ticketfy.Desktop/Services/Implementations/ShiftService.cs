using Microsoft.EntityFrameworkCore;
using Ticketfy.Core.Helpers;
using Ticketfy.Data;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.Services.Implementations;

public sealed class ShiftService : IShiftService
{
    private readonly AppDbContext _ctx;

    public ShiftService(AppDbContext ctx) => _ctx = ctx;

    public async Task<ShiftDto?> GetActiveAsync()
    {
        var entity = await _ctx.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IsOpen == 1);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<ShiftDto> OpenAsync(double openingBalance)
    {
        var entity = new ShiftEntity
        {
            Id = IdGenerator.NewShiftId(),
            StartTime = DateTimeOffset.Now.ToString("o"),
            OpeningBalance = openingBalance,
            IsOpen = 1
        };

        _ctx.Shifts.Add(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ShiftDto> CloseAsync(string shiftId, double actualBalance)
    {
        var entity = await _ctx.Shifts.FindAsync(shiftId)
            ?? throw new InvalidOperationException($"Shift {shiftId} not found");

        var cashSales = await _ctx.Sales
            .AsNoTracking()
            .Where(s => s.PaymentMethod == "Cash"
                     && s.IsCancelled == 0
                     && string.Compare(s.Date, entity.StartTime) >= 0)
            .SumAsync(s => s.Total);

        var creditSales = await _ctx.Sales
            .AsNoTracking()
            .Where(s => s.PaymentMethod == "Credit"
                     && s.IsCancelled == 0
                     && string.Compare(s.Date, entity.StartTime) >= 0)
            .SumAsync(s => s.Total);

        var customerAbonosCash = await _ctx.ShiftMovements
            .AsNoTracking()
            .Where(m => m.ShiftId == entity.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.AbonoCliente)
            .SumAsync(m => m.Amount);

        var cashExpenses = await _ctx.ShiftMovements
            .AsNoTracking()
            .Where(m => m.ShiftId == entity.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.GastoOperativo)
            .SumAsync(m => m.Amount);

        var cashPurchases = await _ctx.ShiftMovements
            .AsNoTracking()
            .Where(m => m.ShiftId == entity.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.CompraEfectivo)
            .SumAsync(m => m.Amount);

        var cashReturns = await _ctx.ShiftMovements
            .AsNoTracking()
            .Where(m => m.ShiftId == entity.Id && m.MovementType == Ticketfy.Core.Enums.MovementType.DevolucionCliente)
            .SumAsync(m => m.Amount);

        entity.EndTime = DateTimeOffset.Now.ToString("o");
        entity.TotalCashSales = cashSales;
        entity.TotalCreditSales = creditSales;
        entity.ExpectedBalance = entity.OpeningBalance + cashSales + customerAbonosCash - cashExpenses - cashPurchases - cashReturns;
        entity.ActualBalance = actualBalance;
        double diff = actualBalance - entity.ExpectedBalance;
        entity.Diff = diff;
        entity.IsOpen = 0;

        if (Math.Abs(diff) > 0.01)
        {
            _ctx.ShiftMovements.Add(new ShiftMovementEntity
            {
                Id = Guid.NewGuid().ToString(),
                ShiftId = entity.Id,
                MovementType = diff < 0 ? Ticketfy.Core.Enums.MovementType.GastoOperativo : Ticketfy.Core.Enums.MovementType.AperturaCaja,
                Amount = Math.Abs(diff),
                IsOutflow = diff < 0,
                Description = diff < 0 ? "Faltante de Caja en Corte final" : "Sobrante de Caja en Corte final",
                Timestamp = DateTime.Now.ToString("s")
            });
        }

        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<List<ShiftDto>> GetAllAsync()
    {
        var list = await _ctx.Shifts
            .AsNoTracking()
            .OrderByDescending(s => s.StartTime)
            .Take(100)
            .ToListAsync();

        return list.Select(s => MapToDto(s)).ToList();
    }

    private static ShiftDto MapToDto(ShiftEntity e) => new(
        e.Id, e.StartTime, e.EndTime, e.OpeningBalance,
        e.TotalCashSales, e.TotalCreditSales, e.ExpectedBalance,
        e.ActualBalance, e.Diff, e.IsOpen);
}
