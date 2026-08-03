using NextVent.Data.Dtos;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NextVent.Services.Hardware;

public class EscPosService
{
    public async Task PrintReceiptAsync(SaleDto sale)
    {
        Log.Information("Printed receipt for sale {Id}", sale.Id);
        await Task.CompletedTask;
    }
}
