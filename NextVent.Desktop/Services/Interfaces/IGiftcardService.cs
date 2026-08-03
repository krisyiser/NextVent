using NextVent.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextVent.Services.Interfaces;

public interface IGiftcardService
{
    Task<List<GiftcardEntity>> GetAllAsync();
    Task<GiftcardEntity?> GetByCardNumberAsync(string cardNumber);
    Task<bool> DeductBalanceAsync(string cardNumber, double amount);
    Task CreateCardAsync(string cardNumber, double initialBalance, string? customerId = null);
}
