using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxDispatchRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Type_CorrelationId",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.Sql("""
                    WITH ranked AS (
                        SELECT
                            "Id",
                            row_number() OVER (
                                PARTITION BY "Type", "CorrelationId"
                                ORDER BY
                                    CASE WHEN "ProcessedAt" IS NOT NULL THEN 0 ELSE 1 END,
                                    "CreatedAt",
                                    "Id") AS duplicate_rank
                        FROM "OutboxMessages"
                    )
                    UPDATE "OutboxMessages" AS message
                    SET
                        "CorrelationId" = 'legacy:' || replace(message."Id"::text, '-', ''),
                        "FailedAt" = CASE
                            WHEN message."ProcessedAt" IS NULL
                                AND message."Type" NOT IN ('password_reset_requested', 'PaymentStatusChanged')
                                THEN CURRENT_TIMESTAMP
                            ELSE message."FailedAt"
                        END,
                        "LastError" = CASE
                            WHEN message."ProcessedAt" IS NULL
                                AND message."Type" NOT IN ('password_reset_requested', 'PaymentStatusChanged')
                                THEN 'Duplicate outbox message cancelled during migration.'
                            ELSE message."LastError"
                        END
                    FROM ranked
                    WHERE message."Id" = ranked."Id"
                        AND ranked.duplicate_rank > 1;
                    """);
            }
            else
            {
                migrationBuilder.Sql("""
                    CREATE TEMP TABLE "__OutboxDuplicateIds" AS
                    SELECT "Id", "Type" FROM (
                        SELECT
                            "Id",
                            "Type",
                            row_number() OVER (
                                PARTITION BY "Type", "CorrelationId"
                                ORDER BY
                                    CASE WHEN "ProcessedAt" IS NOT NULL THEN 0 ELSE 1 END,
                                    "CreatedAt",
                                    "Id") AS duplicate_rank
                        FROM "OutboxMessages"
                    ) AS ranked
                    WHERE duplicate_rank > 1;

                    UPDATE "OutboxMessages"
                    SET
                        "CorrelationId" = 'legacy:' || replace(CAST("Id" AS TEXT), '-', ''),
                        "FailedAt" = CASE
                            WHEN "ProcessedAt" IS NULL
                                AND "Type" NOT IN ('password_reset_requested', 'PaymentStatusChanged')
                                THEN CURRENT_TIMESTAMP
                            ELSE "FailedAt"
                        END,
                        "LastError" = CASE
                            WHEN "ProcessedAt" IS NULL
                                AND "Type" NOT IN ('password_reset_requested', 'PaymentStatusChanged')
                                THEN 'Duplicate outbox message cancelled during migration.'
                            ELSE "LastError"
                        END
                    WHERE "Id" IN (SELECT "Id" FROM "__OutboxDuplicateIds");

                    DROP TABLE "__OutboxDuplicateIds";
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Type_CorrelationId",
                table: "OutboxMessages",
                columns: new[] { "Type", "CorrelationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Type_CorrelationId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Type_CorrelationId",
                table: "OutboxMessages",
                columns: new[] { "Type", "CorrelationId" });
        }
    }
}
