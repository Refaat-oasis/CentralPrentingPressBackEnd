using System;
using System.Collections.Generic;
using System.Linq;
using CentralPress.API.Entities;
using Microsoft.AspNetCore.Identity;
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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Enforce Arabic Collation
        builder.UseCollation("Arabic_100_CS_AI");

        // 2. Global Default Decimal Precision (18, 2)
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

        // 3. High Precision Columns (18, 4 and 18, 3) - HIGH-24 & LOW-09
        builder.Entity<MoqaysaMaterialItem>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 4);
        });

        builder.Entity<MoqaysaMachineryItem>(entity =>
        {
            entity.Property(x => x.WorkerHourlyRateSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.MachineHourlyRateSnapshot).HasPrecision(18, 4);
        });

        builder.Entity<Machinery>(entity =>
        {
            entity.Property(x => x.WorkerHourlyFee).HasPrecision(18, 4);
            entity.Property(x => x.MachineOperatingHourlyFee).HasPrecision(18, 4);
            entity.Property(x => x.DepreciationHourlyRate).HasPrecision(18, 4);
            entity.Property(x => x.MaintenanceHourlyRate).HasPrecision(18, 4);
        });

        builder.Entity<CostingFormulaConfig>()
            .Property(x => x.DefaultPercentage)
            .HasPrecision(8, 4);

        // 4. DeleteBehavior Configuration - HIGH-30
        // Exclude ASP.NET Core Identity internal entities so cascade delete on user/role deletion operates correctly
        var identityClrTypes = new HashSet<Type>
        {
            typeof(IdentityUserRole<string>),
            typeof(IdentityUserClaim<string>),
            typeof(IdentityUserLogin<string>),
            typeof(IdentityUserToken<string>),
            typeof(IdentityRoleClaim<string>)
        };

        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (identityClrTypes.Contains(entity.ClrType))
            {
                continue;
            }

            foreach (var relationship in entity.GetForeignKeys())
            {
                if (identityClrTypes.Contains(relationship.PrincipalEntityType.ClrType))
                {
                    continue;
                }

                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        // 5. Unique Indexes - HIGH-23
        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("IX_AspNetUsers_EmployeeCode_Unique");

        builder.Entity<PaymentOrder>()
            .HasIndex(p => p.PaymentOrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_PaymentOrders_PaymentOrderNumber_Unique");

        builder.Entity<Moqaysa>()
            .HasIndex(m => m.JobOrderNumber)
            .HasDatabaseName("IX_Moqaysat_JobOrderNumber");

        builder.Entity<Tasweya>()
            .HasIndex(t => t.SettlementNumber)
            .IsUnique()
            .HasDatabaseName("IX_Tasweyat_SettlementNumber_Unique");

        builder.Entity<TasweyaMoqaysaLink>()
            .HasIndex(l => new { l.TasweyaId, l.MoqaysaId })
            .IsUnique()
            .HasDatabaseName("IX_TasweyaMoqaysaLinks_Tasweya_Moqaysa_Unique");

        builder.Entity<PaymentOrderTasweyaLink>()
            .HasIndex(l => new { l.PaymentOrderId, l.TasweyaId })
            .IsUnique()
            .HasDatabaseName("IX_PaymentOrderTasweyaLinks_Order_Tasweya_Unique");

        builder.Entity<PaymentOrderTasweyaLink>()
            .HasIndex(l => l.TasweyaId)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_PaymentOrderTasweyaLinks_TasweyaId_Active_Unique");

        builder.Entity<PaymentOrder>()
            .Property(p => p.RowVersion)
            .IsRowVersion();

        builder.Entity<RolePermission>()
            .HasIndex(r => new { r.JobTitleId, r.ModuleId })
            .IsUnique()
            .HasDatabaseName("IX_RolePermissions_JobTitle_Module_Unique");

        builder.Entity<UserPermissionOverride>()
            .HasIndex(o => new { o.UserId, o.ModuleId })
            .IsUnique()
            .HasDatabaseName("IX_UserPermissionOverrides_User_Module_Unique");

        builder.Entity<AccountEntity>()
            .HasIndex(a => a.AccountCode)
            .IsUnique()
            .HasDatabaseName("IX_Accounts_AccountCode_Unique");

        builder.Entity<RefreshToken>()
            .HasIndex(r => r.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token_Unique");

        builder.Entity<RefreshToken>()
            .HasIndex(r => r.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        // 6. Navigation Alignment to Prevent Shadow Properties - MED-18
        builder.Entity<TasweyaMoqaysaLink>(entity =>
        {
            entity.HasKey(x => x.LinkId);

            entity.HasOne(x => x.Tasweya)
                .WithMany(t => t.MoqaysaLinks)
                .HasForeignKey(x => x.TasweyaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Moqaysa)
                .WithMany(m => m.TasweyaLinks)
                .HasForeignKey(x => x.MoqaysaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentOrderTasweyaLink>(entity =>
        {
            entity.HasKey(x => x.PaymentLinkId);

            entity.HasOne(x => x.PaymentOrder)
                .WithMany(p => p.TasweyaLinks)
                .HasForeignKey(x => x.PaymentOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Tasweya)
                .WithMany(t => t.PaymentOrderLinks)
                .HasForeignKey(x => x.TasweyaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SystemModule>(entity =>
        {
            entity.HasMany(m => m.RolePermissions)
                .WithOne(p => p.Module)
                .HasForeignKey(p => p.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(m => m.UserOverrides)
                .WithOne(o => o.Module)
                .HasForeignKey(o => o.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasMany(u => u.PermissionOverrides)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.JobTitle)
                .WithMany(j => j.Users)
                .HasForeignKey(u => u.JobTitleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.RefreshTokens)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. Missing Audit & Performance Indexes - MED-16
        builder.Entity<AuditBacklog>(entity =>
        {
            entity.HasIndex(a => a.TimestampUtc).HasDatabaseName("IX_AuditBacklogs_TimestampUtc");
            entity.HasIndex(a => a.UserId).HasDatabaseName("IX_AuditBacklogs_UserId");
            entity.HasIndex(a => a.ModuleName).HasDatabaseName("IX_AuditBacklogs_ModuleName");
            entity.HasIndex(a => new { a.ModuleName, a.TimestampUtc }).HasDatabaseName("IX_AuditBacklogs_Module_Timestamp");
        });

        builder.Entity<Moqaysa>()
            .HasIndex(m => m.CreatedByUserId)
            .HasDatabaseName("IX_Moqaysat_CreatedByUserId");

        builder.Entity<Tasweya>()
            .HasIndex(t => t.CreatedByUserId)
            .HasDatabaseName("IX_Tasweyat_CreatedByUserId");

        builder.Entity<PaymentOrder>()
            .HasIndex(p => p.ConfirmedByUserId)
            .HasDatabaseName("IX_PaymentOrders_ConfirmedByUserId");

        // 8. String Length Constraints - MED-17
        builder.Entity<UserPermissionOverride>()
            .Property(o => o.UserId)
            .HasMaxLength(450)
            .IsRequired();

        // 9. Soft-Delete Global Query Filters - MED-15
        builder.Entity<AccountEntity>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Customer>().HasQueryFilter(e => e.IsActive);
        builder.Entity<JobTitle>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Machinery>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Material>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Moqaysa>().HasQueryFilter(e => e.IsActive);
        builder.Entity<PaymentOrder>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Tasweya>().HasQueryFilter(e => e.IsActive);
        builder.Entity<CostingFormulaConfig>().HasQueryFilter(e => e.IsActive);
        builder.Entity<SystemModule>().HasQueryFilter(e => e.IsActive);

        // Junction and dependent entity filter consistency to prevent EF Core warning 10622
        builder.Entity<TasweyaMoqaysaLink>().HasQueryFilter(l => l.Tasweya!.IsActive && l.Moqaysa!.IsActive);
        builder.Entity<PaymentOrderTasweyaLink>().HasQueryFilter(l => l.PaymentOrder!.IsActive && l.Tasweya!.IsActive);
        builder.Entity<RolePermission>().HasQueryFilter(p => p.JobTitle!.IsActive && p.Module!.IsActive);
        builder.Entity<UserPermissionOverride>().HasQueryFilter(o => o.Module!.IsActive);
    }
}
