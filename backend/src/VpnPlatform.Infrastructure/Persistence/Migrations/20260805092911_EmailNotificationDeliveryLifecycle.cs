using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmailNotificationDeliveryLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "NotificationDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedAt",
                table: "NotificationDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceOutboxMessageId",
                table: "NotificationDeliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_SourceOutboxMessageId",
                table: "NotificationDeliveries",
                column: "SourceOutboxMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_Status_NextAttemptAt",
                table: "NotificationDeliveries",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_SourceOutboxMessageId",
                table: "NotificationDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_Status_NextAttemptAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "SourceOutboxMessageId",
                table: "NotificationDeliveries");
        }
    }
}
