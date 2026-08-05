using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PendingOrderIntentConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingIntentKey",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Pending_IntentKey",
                table: "Orders",
                column: "PendingIntentKey",
                unique: true,
                filter: "\"Status\" = 1 AND \"PendingIntentKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_Pending_IntentKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PendingIntentKey",
                table: "Orders");
        }
    }
}
