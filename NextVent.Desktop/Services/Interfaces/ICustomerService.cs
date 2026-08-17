using System.Collections.Generic;
using System.Threading.Tasks;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

/// <summary>
/// Customer CRUD with debt tracking, payment history, active shift cash ledger injection, and loyalty points.
/// </summary>
public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(string id);
    Task AddAsync(CustomerDto customer);
    Task UpdateAsync(CustomerDto customer);
    Task UpdateDebtAsync(string customerId, double newDebt);
    Task AddPaymentAsync(CustomerPaymentDto payment);
    Task<bool> RegisterCustomerPaymentAsync(string customerId, double amount, string method, string notes);
    Task<List<CustomerPaymentDto>> GetPaymentsAsync(string customerId);
    Task DeleteAsync(string id);
    Task UpdatePointsAsync(string customerId, double points);
    Task<FiscalClientDto?> GetFiscalDataAsync(string customerId);
    Task SaveFiscalDataAsync(FiscalClientDto data);
}
