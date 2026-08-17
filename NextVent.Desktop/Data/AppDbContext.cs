using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NextVent.Data.Entities;
using System;

namespace NextVent.Data;

public class SqliteDecimalConverter : ValueConverter<decimal, long>
{
    public SqliteDecimalConverter() : base(
        v => Convert.ToInt64(Math.Round(v, 2, MidpointRounding.ToEven) * 100m),
        v => Convert.ToDecimal(v) / 100m)
    { }
}

public class AppDbContext : DbContext
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<PromotionEntity> Promotions => Set<PromotionEntity>();
    public DbSet<SettingEntity> Settings => Set<SettingEntity>();
    public DbSet<SaleEntity> Sales => Set<SaleEntity>();
    public DbSet<FiscalClientEntity> FiscalClients => Set<FiscalClientEntity>();
    public DbSet<ShiftEntity> Shifts => Set<ShiftEntity>();
    public DbSet<CustomerPaymentEntity> CustomerPayments => Set<CustomerPaymentEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();
    public DbSet<PurchaseEntity> Purchases => Set<PurchaseEntity>();
    public DbSet<PurchaseItemEntity> PurchaseItems => Set<PurchaseItemEntity>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<ParkedOrderEntity> ParkedOrders => Set<ParkedOrderEntity>();
    public DbSet<ItemKitEntity> ItemKits => Set<ItemKitEntity>();
    public DbSet<ItemKitItemEntity> ItemKitItems => Set<ItemKitItemEntity>();
    public DbSet<GiftcardEntity> Giftcards => Set<GiftcardEntity>();
    public DbSet<CashupEntity> Cashups => Set<CashupEntity>();
    public DbSet<ProductAttributeEntity> ProductAttributes => Set<ProductAttributeEntity>();
    public DbSet<ShiftNoteEntity> ShiftNotes => Set<ShiftNoteEntity>();
    public DbSet<ShiftMovementEntity> ShiftMovements => Set<ShiftMovementEntity>();
    public DbSet<SystemAlertEntity> SystemAlerts => Set<SystemAlertEntity>();
    public DbSet<ReturnEntity> Returns => Set<ReturnEntity>();
    public DbSet<AttendanceEntity> Attendances => Set<AttendanceEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<FolioSequenceEntity> FolioSequences => Set<FolioSequenceEntity>();
    public DbSet<CoOccurrenceEntity> CoOccurrences => Set<CoOccurrenceEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<CategoryEntity>().HasKey(c => c.Id);
        modelBuilder.Entity<ProductEntity>().HasKey(p => p.Id);
        modelBuilder.Entity<CustomerEntity>().HasKey(c => c.Id);
        modelBuilder.Entity<PromotionEntity>().HasKey(p => p.Id);
        modelBuilder.Entity<SettingEntity>().HasKey(s => s.Key);
        modelBuilder.Entity<SaleEntity>().HasKey(s => s.Id);
        modelBuilder.Entity<FiscalClientEntity>().HasKey(f => f.Id);
        modelBuilder.Entity<ShiftEntity>().HasKey(s => s.Id);
        modelBuilder.Entity<CustomerPaymentEntity>().HasKey(cp => cp.Id);
        modelBuilder.Entity<UserEntity>().HasKey(u => u.Id);
        modelBuilder.Entity<SupplierEntity>().HasKey(s => s.Id);
        modelBuilder.Entity<PurchaseEntity>().HasKey(p => p.Id);
        modelBuilder.Entity<PurchaseItemEntity>().HasKey(pi => pi.Id);
        modelBuilder.Entity<ExpenseEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<ParkedOrderEntity>().HasKey(po => po.Id);
        modelBuilder.Entity<ItemKitEntity>().HasKey(ik => ik.Id);
        modelBuilder.Entity<ItemKitItemEntity>().HasKey(iki => iki.Id);
        modelBuilder.Entity<GiftcardEntity>().HasKey(g => g.Id);
        modelBuilder.Entity<CashupEntity>().HasKey(c => c.Id);
        modelBuilder.Entity<ProductAttributeEntity>().HasKey(pa => pa.Id);
        modelBuilder.Entity<ShiftNoteEntity>().HasKey(sn => sn.Id);
        modelBuilder.Entity<AttendanceEntity>().HasKey(a => a.Id);
        modelBuilder.Entity<FolioSequenceEntity>().HasKey(f => f.DatePrefix);
        modelBuilder.Entity<CoOccurrenceEntity>().HasKey(c => new { c.ProductoA, c.ProductoB });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            configurationBuilder
                .Properties<decimal>()
                .HaveConversion<SqliteDecimalConverter>();
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        if (!optionsBuilder.IsConfigured)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = System.IO.Path.Combine(appDataFolder, "NextVent", "Database");
            if (!System.IO.Directory.Exists(appFolder))
            {
                System.IO.Directory.CreateDirectory(appFolder);
            }
            string dbPath = System.IO.Path.Combine(appFolder, "nextvent.db");
            string securePassword = NextVent.Services.Security.SecurityManager.GetMasterKey();
            optionsBuilder.UseSqlite($"Data Source={dbPath};Password={securePassword};Cache=Shared;Mode=ReadWriteCreate;");
        }
    }
}

public class AppDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = System.IO.Path.Combine(appDataFolder, "NextVent", "Database");
        if (!System.IO.Directory.Exists(appFolder))
        {
            System.IO.Directory.CreateDirectory(appFolder);
        }
        string dbPath = System.IO.Path.Combine(appFolder, "nextvent.db");
        string securePassword = NextVent.Services.Security.SecurityManager.GetMasterKey();
        optionsBuilder.UseSqlite($"Data Source={dbPath};Password={securePassword};Cache=Shared;Mode=ReadWriteCreate;");
        return new AppDbContext(optionsBuilder.Options);
    }
}
