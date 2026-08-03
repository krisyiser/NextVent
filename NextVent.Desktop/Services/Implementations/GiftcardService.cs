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
}
