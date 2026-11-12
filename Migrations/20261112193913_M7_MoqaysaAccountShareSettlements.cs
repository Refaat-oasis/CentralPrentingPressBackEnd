using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class M7_MoqaysaAccountShareSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIncludedInTasweya",
                table: "MoqaysaAccountShares",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "MoqaysaAccountShares",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "MoqaysaAccountShares",
                type: "datetime2",
                nullable: true);

            // Backfill per-share settlement & payment status from active linked Tasweyat matching the share AccountId
            migrationBuilder.Sql(@"
                UPDATE s
                SET s.IsIncludedInTasweya = 1,
                    s.IsPaid = t.IsPaid,
                    s.PaidAtUtc = t.PaidAtUtc
                FROM MoqaysaAccountShares s
                INNER JOIN TasweyaMoqaysaLinks l ON l.MoqaysaId = s.MoqaysaId
                INNER JOIN Tasweyat t ON t.TasweyaId = l.TasweyaId
                WHERE t.IsActive = 1 AND t.AccountId = s.AccountId;

                UPDATE s
                SET s.IsIncludedInTasweya = m.IsIncludedInTasweya,
                    s.IsPaid = m.IsPaid,
                    s.PaidAtUtc = m.PaidAtUtc
                FROM MoqaysaAccountShares s
                INNER JOIN Moqaysat m ON m.MoqaysaId = s.MoqaysaId
                WHERE s.IsIncludedInTasweya = 0
                  AND m.IsIncludedInTasweya = 1
                  AND s.SharePercentage = 100.00
                  AND NOT EXISTS (
                      SELECT 1
                      FROM TasweyaMoqaysaLinks l
                      INNER JOIN Tasweyat t ON t.TasweyaId = l.TasweyaId
                      WHERE l.MoqaysaId = m.MoqaysaId AND t.IsActive = 1
                  );

                UPDATE m
                SET m.IsIncludedInTasweya = CASE
                        WHEN EXISTS (SELECT 1 FROM MoqaysaAccountShares s WHERE s.MoqaysaId = m.MoqaysaId AND s.IsIncludedInTasweya = 0)
                        THEN 0 ELSE m.IsIncludedInTasweya END,
                    m.IsPaid = CASE
                        WHEN EXISTS (SELECT 1 FROM MoqaysaAccountShares s WHERE s.MoqaysaId = m.MoqaysaId AND s.IsPaid = 0)
                        THEN 0 ELSE m.IsPaid END
                FROM Moqaysat m
                WHERE EXISTS (SELECT 1 FROM MoqaysaAccountShares s WHERE s.MoqaysaId = m.MoqaysaId);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIncludedInTasweya",
                table: "MoqaysaAccountShares");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "MoqaysaAccountShares");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "MoqaysaAccountShares");
        }
    }
}
