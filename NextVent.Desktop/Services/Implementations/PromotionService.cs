using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.Services.Implementations;

public class PromotionService : IPromotionService
{
    private readonly AppDbContext _ctx;

    public PromotionService(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<List<PromotionDto>> GetAllAsync()
    {
        var list = await _ctx.Promotions.AsNoTracking().ToListAsync();
        return list.Select(p => new PromotionDto(p.Id, p.Name, p.DiscountValue, p.IsActive == 1)).ToList();
    }

    public async Task<List<PromotionDto>> GetActiveAsync()
    {
        var list = await _ctx.Promotions.AsNoTracking().Where(p => p.IsActive == 1).ToListAsync();
        return list.Select(p => new PromotionDto(p.Id, p.Name, p.DiscountValue, p.IsActive == 1)).ToList();
    }

    public async Task SaveAsync(PromotionDto promotion)
    {
        var entity = await _ctx.Promotions.FindAsync(promotion.Id);
        if (entity != null)
        {
            entity.Name = promotion.Name;
            entity.DiscountValue = promotion.DiscountValue;
            entity.IsActive = promotion.IsActive ? 1 : 0;
        }
        else
        {
            _ctx.Promotions.Add(new PromotionEntity
            {
                Id = string.IsNullOrEmpty(promotion.Id) ? Guid.NewGuid().ToString() : promotion.Id,
                Name = promotion.Name,
                DiscountValue = promotion.DiscountValue,
                IsActive = promotion.IsActive ? 1 : 0
            });
        }
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _ctx.Promotions.FindAsync(id);
        if (entity != null)
        {
            _ctx.Promotions.Remove(entity);
            await _ctx.SaveChangesAsync();
        }
    }
}
