using Microsoft.EntityFrameworkCore;
using NextVent.Data.Entities;

namespace NextVent.Data;

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
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<AttendanceEntity> Attendances => Set<AttendanceEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
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
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;Journal Mode=WAL;");
        }
    }
}

public class AppDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=app.db;Cache=Shared;Mode=ReadWriteCreate;Journal Mode=WAL;");
        return new AppDbContext(optionsBuilder.Options);
    }
}
