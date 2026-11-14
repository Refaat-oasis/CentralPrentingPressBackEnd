using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class M6_MoqaysaAccountShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MoqaysaAccountShares",
                columns: table => new
                {
                    ShareId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MoqaysaId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    SharePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoqaysaAccountShares", x => x.ShareId);
                    table.ForeignKey(
                        name: "FK_MoqaysaAccountShares_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MoqaysaAccountShares_Moqaysat_MoqaysaId",
                        column: x => x.MoqaysaId,
                        principalTable: "Moqaysat",
                        principalColumn: "MoqaysaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MoqaysaAccountShares_AccountId",
                table: "MoqaysaAccountShares",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MoqaysaAccountShares_MoqaysaId_AccountId",
                table: "MoqaysaAccountShares",
                columns: new[] { "MoqaysaId", "AccountId" },
                unique: true);

            // Backfill existing moqaysat with 100% share to customer's default account
            migrationBuilder.Sql(@"
                INSERT INTO MoqaysaAccountShares (MoqaysaId, AccountId, SharePercentage, AllocatedAmount)
                SELECT m.MoqaysaId, c.AccountId, 100.00, m.FinalGrandTotal
                FROM Moqaysat m
                INNER JOIN Customers c ON m.CustomerId = c.CustomerId
                WHERE NOT EXISTS (SELECT 1 FROM MoqaysaAccountShares s WHERE s.MoqaysaId = m.MoqaysaId);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MoqaysaAccountShares");
        }
    }
}
