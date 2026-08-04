using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionLifecycleRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LifecycleAttemptCount",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LifecycleLastError",
                table: "Subscriptions",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LifecycleLeaseExpiresAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LifecycleNextAttemptAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LifecycleProcessingStartedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LifecycleAttemptCount",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LifecycleLastError",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LifecycleLeaseExpiresAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LifecycleNextAttemptAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LifecycleProcessingStartedAt",
                table: "Subscriptions");
        }
    }
}
