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

    public CustomerService(AppDbContext ctx) => _ctx = ctx;

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
        if (payment.Amount <= 0)
        {
            throw new ArgumentException("El monto del abono debe ser mayor a 0.");
        }

        var customer = await _ctx.Customers.FindAsync(payment.CustomerId);
        if (customer is null) return;

        var amount = Math.Round(payment.Amount, 2);

        _ctx.CustomerPayments.Add(new CustomerPaymentEntity
        {
            Id = string.IsNullOrEmpty(payment.Id) ? IdGenerator.NewPaymentId() : payment.Id,
            CustomerId = payment.CustomerId,
            Date = string.IsNullOrEmpty(payment.Date) ? DateTimeOffset.UtcNow.ToString("o") : payment.Date,
            Amount = amount
        });

        customer.Debt = Math.Max(0.0, Math.Round(customer.Debt - amount, 2));
        await _ctx.SaveChangesAsync();
    }

    public async Task<List<CustomerPaymentDto>> GetPaymentsAsync(string customerId)
    {
        var list = await _ctx.CustomerPayments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        return list.Select(p => new CustomerPaymentDto(p.Id, p.CustomerId, p.Date, p.Amount, "Efectivo", "")).ToList();
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
