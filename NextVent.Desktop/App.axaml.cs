using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NextVent.Data;
using NextVent.Data.Seed;
using NextVent.Views;
using NextVent.ViewModels;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;

namespace NextVent;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Log.Information("NextVent POS v3.0 — Avalonia Native Desktop starting");

            try
            {
                var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;Journal Mode=WAL;")
                    .Options;

                using var context = new AppDbContext(options);
                await context.Database.MigrateAsync();
                await DatabaseSeeder.SeedAsync(context);
                Log.Information($"Database initialized and seeded successfully at {dbPath}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialization or seeding failed");
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
