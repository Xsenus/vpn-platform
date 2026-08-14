using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedTariffIdsJson = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    VpnProtocol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ServerSelectionRule = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    InboundSelectionRule = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProvisioningMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OnPaymentSucceeded = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    OnPaymentFailed = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    OnRefund = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    OnSubscriptionExpired = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    OnRenewal = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    CabinetText = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    TelegramText = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    GenerateQrCode = table.Column<bool>(type: "boolean", nullable: false),
                    MaxDevices = table.Column<int>(type: "integer", nullable: false),
                    TrafficLimit = table.Column<long>(type: "bigint", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScenarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScenarios_IsActive_SortOrder",
                table: "WorkScenarios",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScenarios_Key",
                table: "WorkScenarios",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkScenarios");
        }
    }
}
