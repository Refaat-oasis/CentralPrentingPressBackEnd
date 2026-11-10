using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAndMoqaysaUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AreMachineryHoursLinked",
                table: "Moqaysat",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedAdminExpensesPercent",
                table: "Moqaysat",
                type: "decimal(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedMaterialProcessingPercent",
                table: "Moqaysat",
                type: "decimal(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedMinistryOfFinancePercent",
                table: "Moqaysat",
                type: "decimal(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedOtherPercent",
                table: "Moqaysat",
                type: "decimal(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedVatPercent",
                table: "Moqaysat",
                type: "decimal(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedWorkersFundPercent",
                table: "Moqaysat",
                type: "decimal(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MachineHours",
                table: "MoqaysaMachineryItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CurrentSessionId",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStartedUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreMachineryHoursLinked",
                table: "Moqaysat");

            migrationBuilder.DropColumn(
                name: "SelectedAdminExpensesPercent",
                table: "Moqaysat");

            migrationBuilder.DropColumn(
                name: "SelectedMaterialProcessingPercent",
                table: "Moqaysat");

            migrationBuilder.DropColumn(
                name: "SelectedMinistryOfFinancePercent",
                table: "Moqaysat");

            migrationBuilder.DropColumn(
                name: "SelectedOtherPercent",
                table: "Moqaysat");

            migrationBuilder.DropColumn(
                name: "SelectedVatPercent",
                table: "Moqaysat");

            migrationBuilder.DropColumn(
                name: "SelectedWorkersFundPercent",
                table: "Moqaysat");

            migrationBuilder.DropColumn(
                name: "MachineHours",
                table: "MoqaysaMachineryItems");

            migrationBuilder.DropColumn(
                name: "CurrentSessionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastActivityUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SessionStartedUtc",
                table: "AspNetUsers");
        }
    }
}
