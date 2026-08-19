using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.Services.Implementations;

public sealed class GiftcardService : IGiftcardService
{
    private readonly AppDbContext _ctx;

    public GiftcardService(AppDbContext ctx) => _ctx = ctx;

    public async Task<List<GiftcardEntity>> GetAllAsync()
    {
        return await _ctx.Giftcards
            .AsNoTracking()
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<GiftcardEntity?> GetByCardNumberAsync(string cardNumber)
    {
        return await _ctx.Giftcards
            .FirstOrDefaultAsync(g => g.CardNumber == cardNumber && g.IsActive);
    }

    public async Task<bool> DeductBalanceAsync(string cardNumber, double amount)
    {
        var card = await _ctx.Giftcards.FirstOrDefaultAsync(g => g.CardNumber == cardNumber && g.IsActive);
        if (card == null || card.Balance < amount) return false;

        card.Balance -= amount;
        await _ctx.SaveChangesAsync();
        return true;
    }

    public async Task<(bool IsValid, decimal Balance, string Error)> ValidateCardAsync(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return (false, 0m, "Ingrese un número de tarjeta válido.");

        var card = await _ctx.Giftcards.FirstOrDefaultAsync(g => g.CardNumber == cardNumber && g.IsActive);
        if (card == null)
            return (false, 0m, "Tarjeta / Monedero no existente o inactivo.");

        if (card.Balance <= 0)
            return (false, 0m, "Monedero sin fondos disponibles ($0.00).");

        return (true, (decimal)card.Balance, string.Empty);
    }

    public async Task<bool> RedeemBalanceAsync(string cardNumber, decimal amountToRedeem, string saleId = "")
    {
        var card = await _ctx.Giftcards.FirstOrDefaultAsync(g => g.CardNumber == cardNumber && g.IsActive)
            ?? throw new InvalidOperationException("Monedero no válido.");

        double amount = (double)amountToRedeem;
        if (card.Balance < amount)
            throw new InvalidOperationException("Fondos insuficientes en el monedero.");

        card.Balance -= amount;
        _ctx.Giftcards.Update(card);
        await _ctx.SaveChangesAsync();
        return true;
    }

    public async Task CreateCardAsync(string cardNumber, double initialBalance, string? customerId = null)
    {
        var entity = new GiftcardEntity
        {
            Id = Guid.NewGuid().ToString(),
            CardNumber = cardNumber,
            Balance = initialBalance,
            CustomerId = customerId,
            IsActive = true,
            CreatedAt = DateTime.Now.ToString("g")
        };
        _ctx.Giftcards.Add(entity);
        await _ctx.SaveChangesAsync();
    }

    public async Task RechargeAsync(string cardId, decimal amount, NextVent.Core.Enums.PaymentMethod method, string activeShiftId)
    {
        var card = await _ctx.Giftcards.FindAsync(cardId);
        if (card == null) return;

        card.Balance += (double)amount;

        // GUARANTEE MONEY INFLOW RECORD
        if (method == NextVent.Core.Enums.PaymentMethod.Efectivo)
        {
            _ctx.ShiftMovements.Add(new ShiftMovementEntity
            {
                Id = Guid.NewGuid().ToString(),
                ShiftId = activeShiftId,
                MovementType = NextVent.Core.Enums.MovementType.AbonoCliente, // Treat as generic inflow
                Amount = (double)amount,
                IsOutflow = false,
                Description = $"Recarga Monedero {card.CardNumber}",
                Timestamp = DateTime.Now.ToString("s")
            });
        }

        await _ctx.SaveChangesAsync();
    }
}
