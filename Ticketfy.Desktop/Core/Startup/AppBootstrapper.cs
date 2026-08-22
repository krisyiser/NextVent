using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ticketfy.Data;
using Ticketfy.Services.Implementations;
using Ticketfy.Services.Interfaces;
using Ticketfy.Services.Security;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ticketfy.Core.Startup;

/// <summary>
/// Orchestrates the startup sequence: DB initialization, DI container setup,
/// business profile resolution, and license validation.
/// </summary>
public static class AppBootstrapper
{
    public static async Task<IServiceProvider> BootstrapServicesAsync()
    {
        await DatabaseInitializer.InitializeAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IFacturamaService>(sp => new FacturamaService(new System.Net.Http.HttpClient()));

        return services.BuildServiceProvider();
    }

    public static async Task<(string businessName, string contactEmail)> GetBusinessProfileAsync()
    {
        string businessName = "TICKETFY!";
        string contactEmail = "admin@ticketfy.com";
        try
        {
            string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ticketfy", "Database");
            string securePassword = SecurityManager.GetMasterKey();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={Path.Combine(dbFolder, "ticketfy.db")};Password={securePassword};")
                .Options;

            using var tempCtx = new AppDbContext(options);
            var bName = await tempCtx.Settings.FirstOrDefaultAsync(s => s.Key == "EmpresaNombreComercial");
            if (bName != null && !string.IsNullOrWhiteSpace(bName.Value)) businessName = bName.Value;

            var bEmail = await tempCtx.Settings.FirstOrDefaultAsync(s => s.Key == "ContactEmail");
            if (bEmail != null && !string.IsNullOrWhiteSpace(bEmail.Value)) contactEmail = bEmail.Value;
        }
        catch { }

        return (businessName, contactEmail);
    }
}
