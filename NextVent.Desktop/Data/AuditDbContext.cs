using Microsoft.EntityFrameworkCore;
using NextVent.Data.Entities;
using System;
using System.IO;

namespace NextVent.Data;

public class AuditDbContext : DbContext
{
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AuditLogEntity>().HasKey(a => a.Id);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (!optionsBuilder.IsConfigured)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataFolder, "NextVent", "Database");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            string dbPath = Path.Combine(appFolder, "audit_logs.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath};");
        }
    }
}
