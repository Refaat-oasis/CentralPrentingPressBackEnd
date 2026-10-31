using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DebitHeaderTextAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditBacklogs",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EmployeeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    JobTitleName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RecordId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditBacklogs", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "CostingFormulaConfigs",
                columns: table => new
                {
                    FormulaConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FieldLabelAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DefaultPercentage = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    AllowedOptionsCsv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    WorkersFundBaseMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FormulaExpressionDescriptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostingFormulaConfigs", x => x.FormulaConfigId);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                columns: table => new
                {
                    JobTitleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleNameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.JobTitleId);
                });

            migrationBuilder.CreateTable(
                name: "Machineries",
                columns: table => new
                {
                    MachineryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineryNameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    WorkerHourlyFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MachineOperatingHourlyFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machineries", x => x.MachineryId);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    MaterialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitNameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.MaterialId);
                });

            migrationBuilder.CreateTable(
                name: "SystemModules",
                columns: table => new
                {
                    ModuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModuleNameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemModules", x => x.ModuleId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerNameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_Customers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PasswordResetCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PasswordResetCodeExpiryUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "JobTitleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RolePermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTitleId = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    CanAccess = table.Column<bool>(type: "bit", nullable: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: false),
                    CanUpdate = table.Column<bool>(type: "bit", nullable: false),
                    CanToggleActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.RolePermissionId);
                    table.ForeignKey(
                        name: "FK_RolePermissions_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "JobTitleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_SystemModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "SystemModules",
                        principalColumn: "ModuleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Moqaysat",
                columns: table => new
                {
                    MoqaysaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobOrderNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    RequiredQuantityText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SpecificationsText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RawMaterials = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaterialProcessingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FilmsProcessingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMaterials = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWorkerWages = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMachineCosts = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApplyAdminExpenses = table.Column<bool>(type: "bit", nullable: false),
                    AdminExpenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExpenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApplyMarkup = table.Column<bool>(type: "bit", nullable: false),
                    SelectedMarkupPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarkupAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAfterMarkup = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinistryOfFinanceFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkersSyndicateFund = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApplyOther = table.Column<bool>(type: "bit", nullable: false),
                    OtherAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal3 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApplyVat = table.Column<bool>(type: "bit", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalGrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsIncludedInTasweya = table.Column<bool>(type: "bit", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moqaysat", x => x.MoqaysaId);
                    table.ForeignKey(
                        name: "FK_Moqaysat_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentOrders",
                columns: table => new
                {
                    PaymentOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentOrderNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    TotalPaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmountInWordsAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOrders", x => x.PaymentOrderId);
                    table.ForeignKey(
                        name: "FK_PaymentOrders_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tasweyat",
                columns: table => new
                {
                    TasweyaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettlementNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettlementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    JobOrderNumbersText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClaimNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalMaterialsSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWorkerWagesSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMachineCostsSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdminExpensesSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarkupAmountSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinistryOfFinanceSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkersFundSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherAmountSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RevenuesTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SettlementGrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmountInWordsAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasweyat", x => x.TasweyaId);
                    table.ForeignKey(
                        name: "FK_Tasweyat_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasweyat_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissionOverrides",
                columns: table => new
                {
                    OverrideId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    CanAccess = table.Column<bool>(type: "bit", nullable: true),
                    CanCreate = table.Column<bool>(type: "bit", nullable: true),
                    CanUpdate = table.Column<bool>(type: "bit", nullable: true),
                    CanToggleActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissionOverrides", x => x.OverrideId);
                    table.ForeignKey(
                        name: "FK_UserPermissionOverrides_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPermissionOverrides_SystemModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "SystemModules",
                        principalColumn: "ModuleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MoqaysaMachineryItems",
                columns: table => new
                {
                    OperationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MoqaysaId = table.Column<int>(type: "int", nullable: false),
                    MachineryId = table.Column<int>(type: "int", nullable: false),
                    MachineryNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkerHourlyRateSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWorkerWages = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MachineHourlyRateSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMachineOperatingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoqaysaMachineryItems", x => x.OperationId);
                    table.ForeignKey(
                        name: "FK_MoqaysaMachineryItems_Machineries_MachineryId",
                        column: x => x.MachineryId,
                        principalTable: "Machineries",
                        principalColumn: "MachineryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MoqaysaMachineryItems_Moqaysat_MoqaysaId",
                        column: x => x.MoqaysaId,
                        principalTable: "Moqaysat",
                        principalColumn: "MoqaysaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MoqaysaMaterialItems",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MoqaysaId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    MaterialNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoqaysaMaterialItems", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_MoqaysaMaterialItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MoqaysaMaterialItems_Moqaysat_MoqaysaId",
                        column: x => x.MoqaysaId,
                        principalTable: "Moqaysat",
                        principalColumn: "MoqaysaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentOrderTasweyaLinks",
                columns: table => new
                {
                    PaymentLinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentOrderId = table.Column<int>(type: "int", nullable: false),
                    TasweyaId = table.Column<int>(type: "int", nullable: false),
                    SnapshotTasweyaAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOrderTasweyaLinks", x => x.PaymentLinkId);
                    table.ForeignKey(
                        name: "FK_PaymentOrderTasweyaLinks_PaymentOrders_PaymentOrderId",
                        column: x => x.PaymentOrderId,
                        principalTable: "PaymentOrders",
                        principalColumn: "PaymentOrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentOrderTasweyaLinks_Tasweyat_TasweyaId",
                        column: x => x.TasweyaId,
                        principalTable: "Tasweyat",
                        principalColumn: "TasweyaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TasweyaMoqaysaLinks",
                columns: table => new
                {
                    LinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasweyaId = table.Column<int>(type: "int", nullable: false),
                    MoqaysaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasweyaMoqaysaLinks", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_TasweyaMoqaysaLinks_Moqaysat_MoqaysaId",
                        column: x => x.MoqaysaId,
                        principalTable: "Moqaysat",
                        principalColumn: "MoqaysaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasweyaMoqaysaLinks_Tasweyat_TasweyaId",
                        column: x => x.TasweyaId,
                        principalTable: "Tasweyat",
                        principalColumn: "TasweyaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmployeeCode",
                table: "AspNetUsers",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_JobTitleId",
                table: "AspNetUsers",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AccountId",
                table: "Customers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MoqaysaMachineryItems_MachineryId",
                table: "MoqaysaMachineryItems",
                column: "MachineryId");

            migrationBuilder.CreateIndex(
                name: "IX_MoqaysaMachineryItems_MoqaysaId",
                table: "MoqaysaMachineryItems",
                column: "MoqaysaId");

            migrationBuilder.CreateIndex(
                name: "IX_MoqaysaMaterialItems_MaterialId",
                table: "MoqaysaMaterialItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MoqaysaMaterialItems_MoqaysaId",
                table: "MoqaysaMaterialItems",
                column: "MoqaysaId");

            migrationBuilder.CreateIndex(
                name: "IX_Moqaysat_CustomerId",
                table: "Moqaysat",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_AccountId",
                table: "PaymentOrders",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_CustomerId",
                table: "PaymentOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_PaymentOrderNumber",
                table: "PaymentOrders",
                column: "PaymentOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrderTasweyaLinks_PaymentOrderId",
                table: "PaymentOrderTasweyaLinks",
                column: "PaymentOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrderTasweyaLinks_TasweyaId",
                table: "PaymentOrderTasweyaLinks",
                column: "TasweyaId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_JobTitleId",
                table: "RolePermissions",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_ModuleId",
                table: "RolePermissions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TasweyaMoqaysaLinks_MoqaysaId",
                table: "TasweyaMoqaysaLinks",
                column: "MoqaysaId");

            migrationBuilder.CreateIndex(
                name: "IX_TasweyaMoqaysaLinks_TasweyaId",
                table: "TasweyaMoqaysaLinks",
                column: "TasweyaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasweyat_AccountId",
                table: "Tasweyat",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasweyat_CustomerId",
                table: "Tasweyat",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_ModuleId",
                table: "UserPermissionOverrides",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_UserId",
                table: "UserPermissionOverrides",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditBacklogs");

            migrationBuilder.DropTable(
                name: "CostingFormulaConfigs");

            migrationBuilder.DropTable(
                name: "MoqaysaMachineryItems");

            migrationBuilder.DropTable(
                name: "MoqaysaMaterialItems");

            migrationBuilder.DropTable(
                name: "PaymentOrderTasweyaLinks");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "TasweyaMoqaysaLinks");

            migrationBuilder.DropTable(
                name: "UserPermissionOverrides");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Machineries");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "PaymentOrders");

            migrationBuilder.DropTable(
                name: "Moqaysat");

            migrationBuilder.DropTable(
                name: "Tasweyat");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "SystemModules");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "JobTitles");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
