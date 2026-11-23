using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CentralPress.API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CentralPress.API.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration? configuration = null,
        IHostEnvironment? environment = null,
        ILogger? logger = null)
    {
        // HIGH-25: Use MigrateAsync for relational database, EnsureCreatedAsync for in-memory test databases
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        // 1. Seed Accounts (3 Fixed Accounts)
        if (!await context.Accounts.AnyAsync())
        {
            var accounts = new List<AccountEntity>
            {
                new()
                {
                    AccountId = 1,
                    AccountCode = "ADMIN_GEN",
                    AccountNameAr = "الإدارة العامة",
                    DebitHeaderTextAr = "من حـ / الإدارة العامة",
                    IsActive = true
                },
                new()
                {
                    AccountId = 2,
                    AccountCode = "CLIENT_CITY",
                    AccountNameAr = "مدينة العملاء",
                    DebitHeaderTextAr = "من حـ / مدينة العملاء",
                    IsActive = true
                },
                new()
                {
                    AccountId = 3,
                    AccountCode = "CREDIT_UNDER_SETTLE",
                    AccountNameAr = "مبالغ دائنة تحت التسوية",
                    DebitHeaderTextAr = "من حـ / مبالغ دائنة تحت التسوية",
                    IsActive = true
                }
            };

            await context.Accounts.AddRangeAsync(accounts);
            await context.SaveChangesAsync();
        }

        // 2. Seed Machineries (11 Fixed Machineries in exact order)
        if (!await context.Machineries.AnyAsync())
        {
            var machineries = new List<Machinery>
            {
                new() { MachineryNameAr = "زنك", DisplayOrder = 1, WorkerHourlyFee = 45.00m, MachineOperatingHourlyFee = 65.00m, IsActive = true },
                new() { MachineryNameAr = "ماستر", DisplayOrder = 2, WorkerHourlyFee = 35.00m, MachineOperatingHourlyFee = 50.00m, IsActive = true },
                new() { MachineryNameAr = "طبع", DisplayOrder = 3, WorkerHourlyFee = 55.00m, MachineOperatingHourlyFee = 90.00m, IsActive = true },
                new() { MachineryNameAr = "كوبي برنتر", DisplayOrder = 4, WorkerHourlyFee = 40.00m, MachineOperatingHourlyFee = 60.00m, IsActive = true },
                new() { MachineryNameAr = "طبع حروف", DisplayOrder = 5, WorkerHourlyFee = 50.00m, MachineOperatingHourlyFee = 75.00m, IsActive = true },
                new() { MachineryNameAr = "تسطير", DisplayOrder = 6, WorkerHourlyFee = 30.00m, MachineOperatingHourlyFee = 40.00m, IsActive = true },
                new() { MachineryNameAr = "تجليد", DisplayOrder = 7, WorkerHourlyFee = 40.00m, MachineOperatingHourlyFee = 55.00m, IsActive = true },
                new() { MachineryNameAr = "دبوس", DisplayOrder = 8, WorkerHourlyFee = 25.00m, MachineOperatingHourlyFee = 35.00m, IsActive = true },
                new() { MachineryNameAr = "قص", DisplayOrder = 9, WorkerHourlyFee = 35.00m, MachineOperatingHourlyFee = 50.00m, IsActive = true },
                new() { MachineryNameAr = "لصق", DisplayOrder = 10, WorkerHourlyFee = 30.00m, MachineOperatingHourlyFee = 45.00m, IsActive = true },
                new() { MachineryNameAr = "سلفنة", DisplayOrder = 11, WorkerHourlyFee = 40.00m, MachineOperatingHourlyFee = 60.00m, IsActive = true },
            };

            await context.Machineries.AddRangeAsync(machineries);
            await context.SaveChangesAsync();
        }

        // 3. Seed System Modules (10 Modules)
        if (!await context.SystemModules.AnyAsync())
        {
            var modules = new List<SystemModule>
            {
                new() { ModuleKey = "EMPLOYEES", ModuleNameAr = "إدارة الموظفين", IsActive = true },
                new() { ModuleKey = "JOB_TITLES", ModuleNameAr = "إدارة المسميات الوظيفية والصلاحيات", IsActive = true },
                new() { ModuleKey = "MACHINERY", ModuleNameAr = "دليل الآلات وأجور الساعات", IsActive = true },
                new() { ModuleKey = "MATERIALS", ModuleNameAr = "دليل الخامات والأسعار", IsActive = true },
                new() { ModuleKey = "CUSTOMERS", ModuleNameAr = "الحسابات والجهات الطالبة", IsActive = true },
                new() { ModuleKey = "MOQAYSA", ModuleNameAr = "المقايسات وأذون التشغيل", IsActive = true },
                new() { ModuleKey = "FORMULAS", ModuleNameAr = "إعدادات نسب ومعادلات المقايسة", IsActive = true },
                new() { ModuleKey = "TASWEYA", ModuleNameAr = "كشوف التسويات المالية (المطالبات)", IsActive = true },
                new() { ModuleKey = "PAYMENT_ORDERS", ModuleNameAr = "أوامر الدفع وتأكيد السداد", IsActive = true },
                new() { ModuleKey = "REPORTS", ModuleNameAr = "التقارير المالية ومتابعة المسدد وغير المسدد", IsActive = true },
            };

            await context.SystemModules.AddRangeAsync(modules);
            await context.SaveChangesAsync();
        }

        // 4. Seed Formula Configs
        if (!await context.CostingFormulaConfigs.AnyAsync())
        {
            var configs = new List<CostingFormulaConfig>
            {
                new()
                {
                    StepOrder = 1,
                    FieldKey = "MATERIAL_PROCESSING",
                    FieldLabelAr = "م. تشغيل الخامات",
                    DefaultPercentage = 5.5m,
                    IsOptional = false,
                    FormulaExpressionDescriptionAr = "5.5% * الخامات",
                    IsActive = true
                },
                new()
                {
                    StepOrder = 2,
                    FieldKey = "ADMIN_EXPENSES",
                    FieldLabelAr = "مصروفات إدارية",
                    DefaultPercentage = 8.5m,
                    IsOptional = true,
                    FormulaExpressionDescriptionAr = "8.5% * الإجمالي الأول (جملة الخامات + م. الآلات + أجور العمال)",
                    IsActive = true
                },
                new()
                {
                    StepOrder = 3,
                    FieldKey = "MARKUP",
                    FieldLabelAr = "النسبة",
                    DefaultPercentage = 8.5m,
                    AllowedOptionsCsv = "8.5,15,30",
                    IsOptional = true,
                    FormulaExpressionDescriptionAr = "النسبة المختارة (8.5% أو 15% أو 30%) * إجمالي المصروفات",
                    IsActive = true
                },
                new()
                {
                    StepOrder = 4,
                    FieldKey = "MINISTRY_OF_FINANCE",
                    FieldLabelAr = "وزارة المالية",
                    DefaultPercentage = 15.0m,
                    IsOptional = false,
                    FormulaExpressionDescriptionAr = "15% * (الجملة - جملة الخامات)",
                    IsActive = true
                },
                new()
                {
                    StepOrder = 5,
                    FieldKey = "WORKERS_FUND",
                    FieldLabelAr = "ص. ت. العاملين",
                    DefaultPercentage = 10.0m,
                    WorkersFundBaseMode = "DiffTotalMinusMaterials",
                    IsOptional = false,
                    FormulaExpressionDescriptionAr = "10% * (الجملة - جملة الخامات)",
                    IsActive = true
                },
                new()
                {
                    StepOrder = 6,
                    FieldKey = "OTHER",
                    FieldLabelAr = "أخرى",
                    DefaultPercentage = 10.0m,
                    IsOptional = true,
                    FormulaExpressionDescriptionAr = "10% * الإجمالي الثاني",
                    IsActive = true
                },
                new()
                {
                    StepOrder = 7,
                    FieldKey = "VAT",
                    FieldLabelAr = "ض. القيمة المضافة",
                    DefaultPercentage = 14.0m,
                    IsOptional = true,
                    FormulaExpressionDescriptionAr = "14% * الإجمالي الثالث",
                    IsActive = true
                }
            };

            await context.CostingFormulaConfigs.AddRangeAsync(configs);
            await context.SaveChangesAsync();
        }

        // 5. Seed Job Titles & Permissions
        JobTitle? adminRole = await context.JobTitles.FirstOrDefaultAsync(j => j.TitleNameAr == "مدير عام إدارة المطبعة");
        if (adminRole == null)
        {
            adminRole = new JobTitle
            {
                TitleNameAr = "مدير عام إدارة المطبعة",
                Description = "مدير عام إدارة المطبعة المركزية - كافة الصلاحيات",
                IsActive = true
            };
            await context.JobTitles.AddAsync(adminRole);
            await context.SaveChangesAsync();

            var otherTitles = new List<JobTitle>
            {
                new() { TitleNameAr = "رئيس وحدة التكاليف", Description = "مسؤول حسابات المقايسات والتسعير الصناعي", IsActive = true },
                new() { TitleNameAr = "محاسب تسويات ومطالبات", Description = "مسؤول إصدار كشوف التسوية المالية", IsActive = true },
                new() { TitleNameAr = "مسؤول أوامر الدفع والخزينة", Description = "مسؤول إنشاء واعتماد أوامر الدفع", IsActive = true },
                new() { TitleNameAr = "مراجع مالي ومراقب حسابات", Description = "مراجعة واعتماد القيود المحاسبية", IsActive = true }
            };
            await context.JobTitles.AddRangeAsync(otherTitles);
            await context.SaveChangesAsync();

            // Grant full permissions for adminRole to all modules
            var allModules = await context.SystemModules.ToListAsync();
            foreach (var mod in allModules)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    JobTitleId = adminRole.JobTitleId,
                    ModuleId = mod.ModuleId,
                    CanAccess = true,
                    CanCreate = true,
                    CanUpdate = true,
                    CanToggleActive = true
                });
            }
            await context.SaveChangesAsync();
        }

        // 6. Seed Native Identity Roles & Claims (HIGH-06)
        string[] standardRoles = { "Developer", "Admin", "Accountant", "Manager", "Employee" };
        foreach (var rName in standardRoles)
        {
            if (!await roleManager.RoleExistsAsync(rName))
            {
                await roleManager.CreateAsync(new IdentityRole(rName));
            }
        }

        string[] moduleKeys = { "employees", "job_titles", "machinery", "materials", "customers", "moqaysa", "formulas", "tasweya", "payment_orders", "reports" };
        string[] moduleActions = { "access", "create", "update", "toggleactive" };

        foreach (var privilegedRoleName in new[] { "Developer", "Admin" })
        {
            var privilegedRole = await roleManager.FindByNameAsync(privilegedRoleName);
            if (privilegedRole != null)
            {
                var existingClaims = await roleManager.GetClaimsAsync(privilegedRole);
                foreach (var m in moduleKeys)
                {
                    foreach (var a in moduleActions)
                    {
                        var claimVal = $"{m}:{a}";
                        if (!existingClaims.Any(c => c.Type == "permission" && c.Value == claimVal))
                        {
                            await roleManager.AddClaimAsync(privilegedRole, new Claim("permission", claimVal));
                        }
                    }
                }
            }
        }

        var accountantRole = await roleManager.FindByNameAsync("Accountant");
        if (accountantRole != null)
        {
            var existingClaims = await roleManager.GetClaimsAsync(accountantRole);
            string[] accountantPerms =
            {
                "tasweya:access", "tasweya:create", "tasweya:update",
                "payment_orders:access", "payment_orders:create", "payment_orders:update",
                "reports:access", "customers:access"
            };
            foreach (var p in accountantPerms)
            {
                if (!existingClaims.Any(c => c.Type == "permission" && c.Value == p))
                {
                    await roleManager.AddClaimAsync(accountantRole, new Claim("permission", p));
                }
            }
        }

        // 7. Seed Admin User (CRIT-02: Zero Auto-Reset Backdoor & No Hardcoded Production Default!)
        var adminEmail = "admin@aup.edu.eg";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var configuredSeedPassword = configuration?["Seed:AdminInitialPassword"]?.Trim();
            if (!string.IsNullOrWhiteSpace(configuredSeedPassword) &&
                configuredSeedPassword.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogWarning("Seed:AdminInitialPassword contains a template placeholder value; ignoring placeholder.");
                configuredSeedPassword = null;
            }

            string? initialPassword = !string.IsNullOrWhiteSpace(configuredSeedPassword)
                ? configuredSeedPassword
                : (environment == null || environment.IsDevelopment() ? "Admin@12345" : null);

            if (string.IsNullOrWhiteSpace(initialPassword))
            {
                logger?.LogWarning("Seed:AdminInitialPassword is not set in Production; skipping default admin account seeding.");
            }
            else
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "مدير النظام المركزي",
                    EmployeeCode = "EMP001",
                    JobTitleId = adminRole.JobTitleId,
                    MustChangePassword = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, initialPassword);
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }

                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else
        {
            // Ensure admin is in Admin role if missing, without altering password or active status
            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                await userManager.AddToRoleAsync(existingAdmin, "Admin");
            }
        }

        // 7b. Seed & Sync Developer User from appsettings.json ("Developer:Email", "Developer:Password")
        var rawDevEmail = configuration?["Developer:Email"]?.Trim();
        var rawDevPassword = configuration?["Developer:Password"]?.Trim();
        var rawDevFullName = configuration?["Developer:FullName"]?.Trim();
        if (!string.IsNullOrWhiteSpace(rawDevEmail) &&
            !string.IsNullOrWhiteSpace(rawDevPassword) &&
            !rawDevEmail.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase) &&
            !rawDevEmail.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) &&
            !rawDevPassword.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase) &&
            !rawDevPassword.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            var devFullName = string.IsNullOrWhiteSpace(rawDevFullName) ? "مطور النظام" : rawDevFullName;
            var existingDev = await context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.EmployeeCode == "DEV001" || u.NormalizedEmail == rawDevEmail.ToUpperInvariant());

            if (existingDev == null)
            {
                var devUser = new ApplicationUser
                {
                    UserName = rawDevEmail,
                    Email = rawDevEmail,
                    EmailConfirmed = true,
                    FullName = devFullName,
                    EmployeeCode = "DEV001",
                    JobTitleId = null,
                    MustChangePassword = false,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var devCreateResult = await userManager.CreateAsync(devUser, rawDevPassword);
                if (!devCreateResult.Succeeded)
                {
                    logger?.LogWarning("Failed to seed Developer user: {Errors}", string.Join(", ", devCreateResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    await userManager.AddToRoleAsync(devUser, "Developer");
                }
            }
            else
            {
                bool needsUpdate = false;
                if (!string.Equals(existingDev.Email, rawDevEmail, StringComparison.OrdinalIgnoreCase))
                {
                    existingDev.Email = rawDevEmail;
                    existingDev.UserName = rawDevEmail;
                    existingDev.EmailConfirmed = true;
                    needsUpdate = true;
                }
                if (!string.Equals(existingDev.FullName, devFullName, StringComparison.Ordinal))
                {
                    existingDev.FullName = devFullName;
                    needsUpdate = true;
                }
                if (!existingDev.IsActive)
                {
                    existingDev.IsActive = true;
                    needsUpdate = true;
                }
                if (existingDev.MustChangePassword)
                {
                    existingDev.MustChangePassword = false;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await userManager.UpdateAsync(existingDev);
                }

                if (!await userManager.IsInRoleAsync(existingDev, "Developer"))
                {
                    await userManager.AddToRoleAsync(existingDev, "Developer");
                }

                if (!await userManager.CheckPasswordAsync(existingDev, rawDevPassword))
                {
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingDev);
                    var resetRes = await userManager.ResetPasswordAsync(existingDev, resetToken, rawDevPassword);
                    if (!resetRes.Succeeded)
                    {
                        logger?.LogWarning("Failed to sync Developer password: {Errors}", string.Join(", ", resetRes.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        // 8. Seed Sample Materials & Customers if empty
        if (!await context.Materials.AnyAsync())
        {
            var materials = new List<Material>
            {
                new() { MaterialNameAr = "ورق طبع 70 جم أبيض كونتراست", UnitNameAr = "فرخ", IsActive = true },
                new() { MaterialNameAr = "ورق كوشيه 150 جم مط فاخر", UnitNameAr = "فرخ", IsActive = true },
                new() { MaterialNameAr = "ورق كوشيه 300 جم لامع مستورد", UnitNameAr = "فرخ", IsActive = true },
                new() { MaterialNameAr = "زنكات أوفست 4 ألوان ديجيتال CTP", UnitNameAr = "زنكة", IsActive = true },
                new() { MaterialNameAr = "أحبار طباعة أوفست أوروبية فاخرة", UnitNameAr = "كجم", IsActive = true },
                new() { MaterialNameAr = "سلوفان حراري لامع 25 ميكرون", UnitNameAr = "متر", IsActive = true },
                new() { MaterialNameAr = "غراء تجليد حراري ألماني", UnitNameAr = "كجم", IsActive = true },
                new() { MaterialNameAr = "كرتون رمادي مقوى 2 ملم", UnitNameAr = "لوح", IsActive = true },
            };
            await context.Materials.AddRangeAsync(materials);
            await context.SaveChangesAsync();
        }

        if (!await context.Customers.AnyAsync())
        {
            var customers = new List<Customer>
            {
                new() { CustomerNameAr = "كلية الهندسة - جامعة أسيوط", AccountId = 1, Phone = "0882411000", Address = "الحرم الجامعي القديم", IsActive = true },
                new() { CustomerNameAr = "المستشفى الجامعي الرئيسي بأسيوط", AccountId = 1, Phone = "0882412000", Address = "مجمع المستشفيات الجامعية", IsActive = true },
                new() { CustomerNameAr = "كلية التجارة - جامعة أسيوط", AccountId = 1, Phone = "0882413000", Address = "مجمع الكليات النظرية", IsActive = true },
                new() { CustomerNameAr = "شركة مصر للتأمين - فرع أسيوط", AccountId = 2, Phone = "0882334455", Address = "شارع الجمهورية - أسيوط", IsActive = true },
                new() { CustomerNameAr = "نقابة المهندسين الفرعية بأسيوط", AccountId = 3, Phone = "0882321122", Address = "ميدان المحطة - أسيوط", IsActive = true },
            };
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }
    }
}
