using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramNotificationDeduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "TelegramBotNotifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.Sql("""
                    WITH ranked AS (
                        SELECT
                            "Id",
                            encode(sha256(convert_to(
                                "TelegramUserId"::text || E'\n' || btrim("Type") || E'\n' || "PayloadJson",
                                'UTF8')),
                                'hex') AS base_key,
                            row_number() OVER (
                                PARTITION BY "TelegramUserId", btrim("Type"), "PayloadJson"
                                ORDER BY
                                    CASE
                                        WHEN "Status" = 'sent' THEN 0
                                        WHEN "Status" IN ('pending', 'sending') THEN 1
                                        ELSE 2
                                    END,
                                    "CreatedAt",
                                    "Id") AS duplicate_rank
                        FROM "TelegramBotNotifications"
                    )
                    UPDATE "TelegramBotNotifications" AS notification
                    SET
                        "DeduplicationKey" = CASE
                            WHEN ranked.duplicate_rank = 1 THEN ranked.base_key
                            ELSE 'legacy:' || replace(notification."Id"::text, '-', '')
                        END,
                        "Status" = CASE
                            WHEN ranked.duplicate_rank > 1 AND notification."Status" IN ('pending', 'sending') THEN 'cancelled'
                            ELSE notification."Status"
                        END,
                        "NextAttemptAt" = CASE
                            WHEN ranked.duplicate_rank > 1 AND notification."Status" IN ('pending', 'sending') THEN NULL
                            ELSE notification."NextAttemptAt"
                        END,
                        "ErrorText" = CASE
                            WHEN ranked.duplicate_rank > 1 AND notification."Status" IN ('pending', 'sending')
                                THEN 'Duplicate Telegram notification cancelled during migration.'
                            ELSE notification."ErrorText"
                        END
                    FROM ranked
                    WHERE notification."Id" = ranked."Id";
                    """);
            }
            else
            {
                migrationBuilder.Sql("""
                    UPDATE "TelegramBotNotifications"
                    SET "DeduplicationKey" = 'legacy:' || replace(CAST("Id" AS TEXT), '-', '');
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBotNotifications_DeduplicationKey",
                table: "TelegramBotNotifications",
                column: "DeduplicationKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelegramBotNotifications_DeduplicationKey",
                table: "TelegramBotNotifications");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "TelegramBotNotifications");
        }
    }
}
