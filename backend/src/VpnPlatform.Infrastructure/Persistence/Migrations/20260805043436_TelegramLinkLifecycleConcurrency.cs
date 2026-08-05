using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TelegramLinkLifecycleConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelegramAccounts_UserId",
                table: "TelegramAccounts");

            migrationBuilder.AddColumn<int>(
                name: "Generation",
                table: "TelegramBotDeepLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvalidatedAt",
                table: "TelegramBotDeepLinks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvalidationReason",
                table: "TelegramBotDeepLinks",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "TelegramBotDeepLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TelegramLinkStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Generation = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramLinkStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramLinkStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE "TelegramBotDeepLinks"
                SET
                    "InvalidatedAt" = CURRENT_TIMESTAMP,
                    "InvalidationReason" = 'telegram_link_lifecycle_migration',
                    "Revision" = 1,
                    "UpdatedAt" = CURRENT_TIMESTAMP
                WHERE "Purpose" = 'link_account' AND "UsedAt" IS NULL;

                INSERT INTO "TelegramLinkStates"
                    ("Id", "UserId", "Generation", "Revision", "CreatedAt", "UpdatedAt")
                SELECT
                    "UserId", "UserId", 1, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM "TelegramBotDeepLinks"
                WHERE "UserId" IS NOT NULL
                GROUP BY "UserId";

                WITH ranked AS (
                    SELECT
                        "Id",
                        row_number() OVER (
                            PARTITION BY "UserId"
                            ORDER BY ("LinkedAt" IS NULL), "LinkedAt" DESC, "UpdatedAt" DESC, "CreatedAt" DESC, "Id") AS link_rank
                    FROM "TelegramAccounts"
                    WHERE "UserId" IS NOT NULL
                )
                UPDATE "TelegramAccounts"
                SET
                    "UserId" = NULL,
                    "LinkedAt" = NULL,
                    "UpdatedAt" = CURRENT_TIMESTAMP
                WHERE "Id" IN (SELECT "Id" FROM ranked WHERE link_rank > 1);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAccounts_UserId",
                table: "TelegramAccounts",
                column: "UserId",
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLinkStates_UserId",
                table: "TelegramLinkStates",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramLinkStates");

            migrationBuilder.DropIndex(
                name: "IX_TelegramAccounts_UserId",
                table: "TelegramAccounts");

            migrationBuilder.DropColumn(
                name: "Generation",
                table: "TelegramBotDeepLinks");

            migrationBuilder.DropColumn(
                name: "InvalidatedAt",
                table: "TelegramBotDeepLinks");

            migrationBuilder.DropColumn(
                name: "InvalidationReason",
                table: "TelegramBotDeepLinks");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "TelegramBotDeepLinks");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAccounts_UserId",
                table: "TelegramAccounts",
                column: "UserId");
        }
    }
}
