using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentProviderDefaultUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH ranked_defaults AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "Provider"
                               ORDER BY "UpdatedAt" DESC, "CreatedAt" DESC, "Id"
                           ) AS row_number
                    FROM "PaymentProviderAccounts"
                    WHERE "IsDefault" = true
                )
                UPDATE "PaymentProviderAccounts"
                SET "IsDefault" = false
                WHERE "Id" IN (
                    SELECT "Id"
                    FROM ranked_defaults
                    WHERE row_number > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderAccounts_Provider",
                table: "PaymentProviderAccounts",
                column: "Provider",
                unique: true,
                filter: "\"IsDefault\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentProviderAccounts_Provider",
                table: "PaymentProviderAccounts");
        }
    }
}
