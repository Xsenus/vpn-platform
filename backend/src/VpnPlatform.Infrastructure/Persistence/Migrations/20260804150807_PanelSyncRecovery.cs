using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PanelSyncRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "VpnPanels"
                SET "LastError" = 'Historical panel error redacted during panel sync recovery migration.'
                WHERE "LastError" <> '';

                UPDATE "PanelHealthChecks"
                SET "ErrorMessage" = 'Historical panel health error redacted during panel sync recovery migration.'
                WHERE "ErrorMessage" <> '';

                UPDATE "PanelSyncRuns"
                SET "ErrorMessage" = 'Historical panel sync error redacted during panel sync recovery migration.'
                WHERE "ErrorMessage" <> '';

                UPDATE "PanelSyncRuns"
                SET
                    "Status" = 3,
                    "FinishedAt" = CURRENT_TIMESTAMP,
                    "ErrorMessage" = 'Duplicate running panel sync quarantined during migration.',
                    "UpdatedAt" = CURRENT_TIMESTAMP
                WHERE "Id" IN (
                    SELECT "Id" FROM (
                        SELECT
                            "Id",
                            row_number() OVER (
                                PARTITION BY "VpnPanelId"
                                ORDER BY "StartedAt", "CreatedAt", "Id") AS running_rank
                        FROM "PanelSyncRuns"
                        WHERE "Status" = 1
                    ) AS ranked
                    WHERE running_rank > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PanelSyncRuns_Running_VpnPanelId",
                table: "PanelSyncRuns",
                column: "VpnPanelId",
                unique: true,
                filter: "\"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PanelSyncRuns_Running_VpnPanelId",
                table: "PanelSyncRuns");
        }
    }
}
