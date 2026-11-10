using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class M1_DatabaseConstraintsAndRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissionOverrides_UserId",
                table: "UserPermissionOverrides");

            migrationBuilder.DropIndex(
                name: "IX_TasweyaMoqaysaLinks_TasweyaId",
                table: "TasweyaMoqaysaLinks");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_JobTitleId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentOrderTasweyaLinks_PaymentOrderId",
                table: "PaymentOrderTasweyaLinks");

            migrationBuilder.DropColumn(
                name: "PasswordResetCode",
                table: "AspNetUsers");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentOrders_PaymentOrderNumber",
                table: "PaymentOrders",
                newName: "IX_PaymentOrders_PaymentOrderNumber_Unique");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_EmployeeCode",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_EmployeeCode_Unique");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "MoqaysaMaterialItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "WorkerHourlyRateSnapshot",
                table: "MoqaysaMachineryItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MachineHourlyRateSnapshot",
                table: "MoqaysaMachineryItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "WorkerHourlyFee",
                table: "Machineries",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MachineOperatingHourlyFee",
                table: "Machineries",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCodeHash",
                table: "AspNetUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PasswordResetFailedAttempts",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_User_Module_Unique",
                table: "UserPermissionOverrides",
                columns: new[] { "UserId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasweyat_CreatedByUserId",
                table: "Tasweyat",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasweyat_SettlementNumber_Unique",
                table: "Tasweyat",
                column: "SettlementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TasweyaMoqaysaLinks_Tasweya_Moqaysa_Unique",
                table: "TasweyaMoqaysaLinks",
                columns: new[] { "TasweyaId", "MoqaysaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_JobTitle_Module_Unique",
                table: "RolePermissions",
                columns: new[] { "JobTitleId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrderTasweyaLinks_Order_Tasweya_Unique",
                table: "PaymentOrderTasweyaLinks",
                columns: new[] { "PaymentOrderId", "TasweyaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_ConfirmedByUserId",
                table: "PaymentOrders",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Moqaysat_CreatedByUserId",
                table: "Moqaysat",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Moqaysat_JobOrderNumber_Unique",
                table: "Moqaysat",
                column: "JobOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditBacklogs_Module_Timestamp",
                table: "AuditBacklogs",
                columns: new[] { "ModuleName", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditBacklogs_ModuleName",
                table: "AuditBacklogs",
                column: "ModuleName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditBacklogs_TimestampUtc",
                table: "AuditBacklogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditBacklogs_UserId",
                table: "AuditBacklogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountCode_Unique",
                table: "Accounts",
                column: "AccountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token_Unique",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissionOverrides_User_Module_Unique",
                table: "UserPermissionOverrides");

            migrationBuilder.DropIndex(
                name: "IX_Tasweyat_CreatedByUserId",
                table: "Tasweyat");

            migrationBuilder.DropIndex(
                name: "IX_Tasweyat_SettlementNumber_Unique",
                table: "Tasweyat");

            migrationBuilder.DropIndex(
                name: "IX_TasweyaMoqaysaLinks_Tasweya_Moqaysa_Unique",
                table: "TasweyaMoqaysaLinks");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_JobTitle_Module_Unique",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentOrderTasweyaLinks_Order_Tasweya_Unique",
                table: "PaymentOrderTasweyaLinks");

            migrationBuilder.DropIndex(
                name: "IX_PaymentOrders_ConfirmedByUserId",
                table: "PaymentOrders");

            migrationBuilder.DropIndex(
                name: "IX_Moqaysat_CreatedByUserId",
                table: "Moqaysat");

            migrationBuilder.DropIndex(
                name: "IX_Moqaysat_JobOrderNumber_Unique",
                table: "Moqaysat");

            migrationBuilder.DropIndex(
                name: "IX_AuditBacklogs_Module_Timestamp",
                table: "AuditBacklogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditBacklogs_ModuleName",
                table: "AuditBacklogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditBacklogs_TimestampUtc",
                table: "AuditBacklogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditBacklogs_UserId",
                table: "AuditBacklogs");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AccountCode_Unique",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetFailedAttempts",
                table: "AspNetUsers");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentOrders_PaymentOrderNumber_Unique",
                table: "PaymentOrders",
                newName: "IX_PaymentOrders_PaymentOrderNumber");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_EmployeeCode_Unique",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_EmployeeCode");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "MoqaysaMaterialItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "WorkerHourlyRateSnapshot",
                table: "MoqaysaMachineryItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "MachineHourlyRateSnapshot",
                table: "MoqaysaMachineryItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "WorkerHourlyFee",
                table: "Machineries",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "MachineOperatingHourlyFee",
                table: "Machineries",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCode",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_UserId",
                table: "UserPermissionOverrides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TasweyaMoqaysaLinks_TasweyaId",
                table: "TasweyaMoqaysaLinks",
                column: "TasweyaId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_JobTitleId",
                table: "RolePermissions",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrderTasweyaLinks_PaymentOrderId",
                table: "PaymentOrderTasweyaLinks",
                column: "PaymentOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
