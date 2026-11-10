using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class M4_MoqaysatJobOrderNumberNonUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Moqaysat_JobOrderNumber_Unique",
                table: "Moqaysat");

            migrationBuilder.CreateIndex(
                name: "IX_Moqaysat_JobOrderNumber",
                table: "Moqaysat",
                column: "JobOrderNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Moqaysat_JobOrderNumber",
                table: "Moqaysat");

            migrationBuilder.CreateIndex(
                name: "IX_Moqaysat_JobOrderNumber_Unique",
                table: "Moqaysat",
                column: "JobOrderNumber",
                unique: true);
        }
    }
}
