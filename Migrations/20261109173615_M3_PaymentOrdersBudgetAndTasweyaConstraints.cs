using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class M3_PaymentOrdersBudgetAndTasweyaConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentOrderTasweyaLinks_TasweyaId",
                table: "PaymentOrderTasweyaLinks");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PaymentOrderTasweyaLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "PaymentOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "PaymentOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserId",
                table: "PaymentOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "PaymentOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "PaymentOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidByUserId",
                table: "PaymentOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAtUtc",
                table: "PaymentOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedByUserId",
                table: "PaymentOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "PaymentOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "PaymentOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "PaymentOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PaymentOrders",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PaymentOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetCeiling",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetCeiling",
                table: "Accounts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrderTasweyaLinks_TasweyaId_Active_Unique",
                table: "PaymentOrderTasweyaLinks",
                column: "TasweyaId",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentOrderTasweyaLinks_TasweyaId_Active_Unique",
                table: "PaymentOrderTasweyaLinks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PaymentOrderTasweyaLinks");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "RejectedAtUtc",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "BudgetCeiling",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BudgetCeiling",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrderTasweyaLinks_TasweyaId",
                table: "PaymentOrderTasweyaLinks",
                column: "TasweyaId");
        }
    }
}
