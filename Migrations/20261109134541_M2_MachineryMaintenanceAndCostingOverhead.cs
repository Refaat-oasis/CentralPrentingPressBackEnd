using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class M2_MachineryMaintenanceAndCostingOverhead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepreciationHourlyRate",
                table: "Machineries",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaintenanceHourlyRate",
                table: "Machineries",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceNotes",
                table: "Machineries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceStatus",
                table: "Machineries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepreciationHourlyRate",
                table: "Machineries");

            migrationBuilder.DropColumn(
                name: "MaintenanceHourlyRate",
                table: "Machineries");

            migrationBuilder.DropColumn(
                name: "MaintenanceNotes",
                table: "Machineries");

            migrationBuilder.DropColumn(
                name: "MaintenanceStatus",
                table: "Machineries");
        }
    }
}
