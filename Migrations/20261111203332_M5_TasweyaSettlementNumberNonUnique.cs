using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class M5_TasweyaSettlementNumberNonUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasweyat_SettlementNumber_Unique",
                table: "Tasweyat");

            migrationBuilder.CreateIndex(
                name: "IX_Tasweyat_SettlementNumber",
                table: "Tasweyat",
                column: "SettlementNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasweyat_SettlementNumber",
                table: "Tasweyat");

            migrationBuilder.CreateIndex(
                name: "IX_Tasweyat_SettlementNumber_Unique",
                table: "Tasweyat",
                column: "SettlementNumber",
                unique: true);
        }
    }
}
