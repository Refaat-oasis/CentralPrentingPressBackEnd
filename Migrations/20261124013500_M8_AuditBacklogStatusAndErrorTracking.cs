using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralPress.API.Migrations
{
    /// <inheritdoc />
    public partial class M8_AuditBacklogStatusAndErrorTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "AuditBacklogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccess",
                table: "AuditBacklogs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "AuditBacklogs",
                type: "int",
                nullable: false,
                defaultValue: 200);

            migrationBuilder.CreateIndex(
                name: "IX_AuditBacklogs_ActionType_Timestamp",
                table: "AuditBacklogs",
                columns: new[] { "ActionType", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditBacklogs_IsSuccess_Timestamp",
                table: "AuditBacklogs",
                columns: new[] { "IsSuccess", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditBacklogs_ActionType_Timestamp",
                table: "AuditBacklogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditBacklogs_IsSuccess_Timestamp",
                table: "AuditBacklogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "AuditBacklogs");

            migrationBuilder.DropColumn(
                name: "IsSuccess",
                table: "AuditBacklogs");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "AuditBacklogs");
        }
    }
}
