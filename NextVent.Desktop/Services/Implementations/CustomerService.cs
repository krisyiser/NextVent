using Microsoft.EntityFrameworkCore;
using NextVent.Core.Helpers;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.Services.Implementations;

public sealed class CustomerService : ICustomerService
{
    private readonly AppDbContext _ctx;
    private readonly IEscPosPrinterService? _printerService;

    public CustomerService(AppDbContext ctx, IEscPosPrinterService? printerService = null)
    {
        _ctx = ctx;
        _printerService = printerService;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var list = await _ctx.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return list.Select(c => new CustomerDto(
            c.Id, c.Name, c.Phone ?? "", c.Email ?? "", c.Rfc ?? "", c.CreditLimit, c.Debt, c.PuntosSaldo, 0.0, c.CustomerCode ?? ""
        )).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(string id)
    {
        var c = await _ctx.Customers.FindAsync(id);
        return c is null ? null : new CustomerDto(
            c.Id, c.Name, c.Phone ?? "", c.Email ?? "", c.Rfc ?? "", c.CreditLimit, c.Debt, c.PuntosSaldo, 0.0, c.CustomerCode ?? ""
        );
    }

    public async Task AddAsync(CustomerDto customer)
    {
        _ctx.Customers.Add(new CustomerEntity
        {
            Id = string.IsNullOrEmpty(customer.Id) ? IdGenerator.NewCustomerId() : customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            Email = customer.Email,
            Rfc = customer.Rfc,
            CreditLimit = customer.CreditLimit,
            Debt = customer.Debt,
            PuntosSaldo = customer.PuntosSaldo,
            CustomerCode = string.IsNullOrWhiteSpace(customer.CustomerCode) ? $"CLI-{Random.Shared.Next(1000, 9999)}" : customer.CustomerCode
        });
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerDto customer)
    {
        var c = await _ctx.Customers.FindAsync(customer.Id);
        if (c is not null)
        {
            c.Name = customer.Name;
            c.Phone = customer.Phone;
            c.Email = customer.Email;
            c.Rfc = customer.Rfc;
            c.CreditLimit = customer.CreditLimit;
            c.Debt = customer.Debt;
            c.CustomerCode = customer.CustomerCode;
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task UpdateDebtAsync(string customerId, double newDebt)
    {
        var c = await _ctx.Customers.FindAsync(customerId);
        if (c is not null)
        {
            c.Debt = Math.Max(0.0, Math.Round(newDebt, 2));
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task AddPaymentAsync(CustomerPaymentDto payment)
    {
        await RegisterCustomerPaymentAsync(payment.CustomerId, payment.Amount, payment.Method ?? "Efectivo", payment.Notes ?? "");
    }

    public async Task<bool> RegisterCustomerPaymentAsync(string customerId, double amount, string method, string notes)
    {
        if (amount <= 0)
            throw new InvalidOperationException("El abono debe ser mayor a $0.00.");

        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var customer = await _ctx.Customers.FindAsync(customerId)
                ?? throw new InvalidOperationException("Cliente no encontrado.");

            var roundedAmount = Math.Round(amount, 2);

            // 1. DEDUCT CUSTOMER DEBT
            customer.Debt = Math.Max(0.0, Math.Round(customer.Debt - roundedAmount, 2));
            _ctx.Customers.Update(customer);

            // 2. IDENTIFY ACTIVE CASHIER SHIFT
            var activeShift = await _ctx.Shifts.FirstOrDefaultAsync(s => s.IsOpen == 1);

            // 3. CREATE PAYMENT RECORD WITH SHIFT BINDING
            var paymentRecord = new CustomerPaymentEntity
            {
                Id = IdGenerator.NewPaymentId(),
                CustomerId = customerId,
                ShiftId = activeShift?.Id,
                Date = DateTimeOffset.UtcNow.ToString("o"),
                Amount = roundedAmount,
                Method = method,
                Notes = notes
            };
            _ctx.CustomerPayments.Add(paymentRecord);

            // 4. INJECT PHYSICAL MONEY INTO SHIFT CASH LEDGER IF PAID IN CASH
            bool isCash = method.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) || method.Equals("Cash", StringComparison.OrdinalIgnoreCase);

            if (activeShift != null && isCash)
            {
                var cashMovement = new ShiftMovementEntity
                {
                    ShiftId = activeShift.Id,
                    MovementType = NextVent.Core.Enums.MovementType.AbonoCliente,
                    Amount = roundedAmount,
                    Description = $"Abono a deuda - Cliente: {customer.Name}",
                    Timestamp = DateTimeOffset.UtcNow.ToString("o")
                };
                _ctx.ShiftMovements.Add(cashMovement);
            }

            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            // 5. TRIGGER PHYSICAL DRAWER KICK AND AUDIT SLIP
            if (isCash && _printerService != null)
            {
                _ = _printerService.OpenCashDrawerAsync("COM1");
                _ = _printerService.PrintNonSaleCashMovementSlipAsync(new NextVent.Core.Models.ShiftMovementSlipModel
                {
                    Folio = paymentRecord.Id.Substring(0, Math.Min(8, paymentRecord.Id.Length)).ToUpper(),
                    MovementTypeLabel = "ABONO DE CLIENTE - CUENTA CORRIENTE",
                    Amount = roundedAmount,
                    Description = $"Abono a cuenta corriente de {customer.Name}. Notas: {notes}",
                    CashierName = "CAJERO EN TURNO",
                    Timestamp = DateTime.Now
                });
            }

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<CustomerPaymentDto>> GetPaymentsAsync(string customerId)
    {
        var list = await _ctx.CustomerPayments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        return list.Select(p => new CustomerPaymentDto(p.Id, p.CustomerId, p.Date, p.Amount, p.Method ?? "Efectivo", p.Notes ?? "")).ToList();
    }

    public async Task DeleteAsync(string id)
    {
        var c = await _ctx.Customers.FindAsync(id);
        if (c is not null)
        {
            _ctx.Customers.Remove(c);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task UpdatePointsAsync(string customerId, double points)
    {
        var c = await _ctx.Customers.FindAsync(customerId);
        if (c is not null)
        {
            c.PuntosSaldo = points;
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task<FiscalClientDto?> GetFiscalDataAsync(string customerId)
    {
        var f = await _ctx.FiscalClients.FindAsync(customerId);
        return f is null ? null : new FiscalClientDto(
            f.Id, f.Id, f.Rfc, f.RazonSocial, f.RegimenFiscal, f.CodigoPostal, f.UsoCfdi);
    }

    public async Task SaveFiscalDataAsync(FiscalClientDto data)
    {
        var existing = await _ctx.FiscalClients.FindAsync(data.Id);
        if (existing is not null)
        {
            existing.Rfc = data.Rfc;
            existing.RazonSocial = data.RazonSocial;
            existing.CodigoPostal = data.CodigoPostal;
            existing.RegimenFiscal = data.RegimenFiscal;
            existing.UsoCfdi = data.UsoCfdi;
        }
        else
        {
            _ctx.FiscalClients.Add(new FiscalClientEntity
            {
                Id = data.Id,
                Rfc = data.Rfc,
                RazonSocial = data.RazonSocial,
                CodigoPostal = data.CodigoPostal,
                RegimenFiscal = data.RegimenFiscal,
                UsoCfdi = data.UsoCfdi
            });
        }
        await _ctx.SaveChangesAsync();
    }
}
