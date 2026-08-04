using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProvisioningWorkerRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "ProvisioningRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "ProvisioningRuns",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseExpiresAt",
                table: "ProvisioningRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedAt",
                table: "ProvisioningRuns",
                type: "timestamp with time zone",
                nullable: true);

            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.Sql("""
                    WITH ranked AS (
                        SELECT
                            "Id",
                            row_number() OVER (
                                PARTITION BY "NodeId"
                                ORDER BY
                                    CASE WHEN "Status" IN (1, 9, 13) THEN 0 ELSE 1 END,
                                    "CreatedAt",
                                    "Id") AS active_rank
                        FROM "ProvisioningRuns"
                        WHERE "Status" IN (0, 1, 8, 9, 12, 13, 15)
                    )
                    UPDATE "ProvisioningRuns" AS run
                    SET
                        "Status" = CASE WHEN run."DryRun" THEN 10 ELSE 3 END,
                        "ProcessingStartedAt" = NULL,
                        "LeaseExpiresAt" = NULL,
                        "FinishedAt" = CURRENT_TIMESTAMP,
                        "LastError" = 'Duplicate active provisioning run quarantined during migration.',
                        "ExecutionLog" = left(concat_ws(E'\n', nullif(run."ExecutionLog", ''), 'Duplicate active provisioning run quarantined during migration.'), 4000),
                        "UpdatedAt" = CURRENT_TIMESTAMP
                    FROM ranked
                    WHERE run."Id" = ranked."Id"
                        AND ranked.active_rank > 1;
                    """);
            }
            else
            {
                migrationBuilder.Sql("""
                    CREATE TEMP TABLE "__DuplicateActiveProvisioningRuns" AS
                    SELECT "Id" FROM (
                        SELECT
                            "Id",
                            row_number() OVER (
                                PARTITION BY "NodeId"
                                ORDER BY
                                    CASE WHEN "Status" IN (1, 9, 13) THEN 0 ELSE 1 END,
                                    "CreatedAt",
                                    "Id") AS active_rank
                        FROM "ProvisioningRuns"
                        WHERE "Status" IN (0, 1, 8, 9, 12, 13, 15)
                    ) AS ranked
                    WHERE active_rank > 1;

                    UPDATE "ProvisioningRuns"
                    SET
                        "Status" = CASE WHEN "DryRun" THEN 10 ELSE 3 END,
                        "ProcessingStartedAt" = NULL,
                        "LeaseExpiresAt" = NULL,
                        "FinishedAt" = CURRENT_TIMESTAMP,
                        "LastError" = 'Duplicate active provisioning run quarantined during migration.',
                        "ExecutionLog" = substr(CASE
                            WHEN "ExecutionLog" = '' THEN 'Duplicate active provisioning run quarantined during migration.'
                            ELSE "ExecutionLog" || char(10) || 'Duplicate active provisioning run quarantined during migration.'
                        END, 1, 4000),
                        "UpdatedAt" = CURRENT_TIMESTAMP
                    WHERE "Id" IN (SELECT "Id" FROM "__DuplicateActiveProvisioningRuns");

                    DROP TABLE "__DuplicateActiveProvisioningRuns";
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_ProvisioningRuns_Active_NodeId",
                table: "ProvisioningRuns",
                column: "NodeId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 8, 9, 12, 13, 15)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProvisioningRuns_Active_NodeId",
                table: "ProvisioningRuns");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "ProvisioningRuns");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "ProvisioningRuns");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "ProvisioningRuns");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "ProvisioningRuns");
        }
    }
}
