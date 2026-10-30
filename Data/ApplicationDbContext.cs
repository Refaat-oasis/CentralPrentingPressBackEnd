using System.Linq;
using CentralPress.API.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<SystemModule> SystemModules => Set<SystemModule>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
    public DbSet<AuditBacklog> AuditBacklogs => Set<AuditBacklog>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Machinery> Machineries => Set<Machinery>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<CostingFormulaConfig> CostingFormulaConfigs => Set<CostingFormulaConfig>();
    public DbSet<Moqaysa> Moqaysat => Set<Moqaysa>();
    public DbSet<MoqaysaMaterialItem> MoqaysaMaterialItems => Set<MoqaysaMaterialItem>();
    public DbSet<MoqaysaMachineryItem> MoqaysaMachineryItems => Set<MoqaysaMachineryItem>();
    public DbSet<Tasweya> Tasweyat => Set<Tasweya>();
    public DbSet<TasweyaMoqaysaLink> TasweyaMoqaysaLinks => Set<TasweyaMoqaysaLink>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<PaymentOrderTasweyaLink> PaymentOrderTasweyaLinks => Set<PaymentOrderTasweyaLink>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Enforce Arabic Collation
        builder.UseCollation("Arabic_100_CS_AI");

        // Decimal Precision Configuration (18, 2)
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (property.GetColumnType() == null)
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }

        // Specific scale for material item quantity (18, 3)
        builder.Entity<MoqaysaMaterialItem>()
            .Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.Entity<CostingFormulaConfig>()
            .Property(x => x.DefaultPercentage)
            .HasPrecision(8, 4);

        // Enforce Restrict Delete Behavior on all foreign keys to eliminate SQL Server cascade cycles
        // and strictly support the zero-hard-delete requirement
        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // ApplicationUser EmployeeCode Unique Index
        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.EmployeeCode)
            .IsUnique();

        // PaymentOrder PaymentOrderNumber Unique Index
        builder.Entity<PaymentOrder>()
            .HasIndex(p => p.PaymentOrderNumber)
            .IsUnique();
    }
}
