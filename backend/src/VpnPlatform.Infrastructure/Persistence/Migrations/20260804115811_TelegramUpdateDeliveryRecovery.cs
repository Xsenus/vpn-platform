using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TelegramUpdateDeliveryRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttemptCount",
                table: "TelegramBotUpdates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveryClaimedAt",
                table: "TelegramBotUpdates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryErrorText",
                table: "TelegramBotUpdates",
                type: "text",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveryNextAttemptAt",
                table: "TelegramBotUpdates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PreCheckoutAnsweredAt",
                table: "TelegramBotUpdates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreCheckoutError",
                table: "TelegramBotUpdates",
                type: "text",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PreCheckoutOk",
                table: "TelegramBotUpdates",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreCheckoutQueryId",
                table: "TelegramBotUpdates",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ResponseChatId",
                table: "TelegramBotUpdates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseReplyMarkupJson",
                table: "TelegramBotUpdates",
                type: "text",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResponseSentAt",
                table: "TelegramBotUpdates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseText",
                table: "TelegramBotUpdates",
                type: "text",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAttemptCount",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "DeliveryClaimedAt",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "DeliveryErrorText",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "DeliveryNextAttemptAt",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "PreCheckoutAnsweredAt",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "PreCheckoutError",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "PreCheckoutOk",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "PreCheckoutQueryId",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "ResponseChatId",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "ResponseReplyMarkupJson",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "ResponseSentAt",
                table: "TelegramBotUpdates");

            migrationBuilder.DropColumn(
                name: "ResponseText",
                table: "TelegramBotUpdates");
        }
    }
}
