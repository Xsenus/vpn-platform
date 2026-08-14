using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendTariffContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tariffs",
                type: "text",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<string>(
                name: "AfterPaymentText",
                table: "Tariffs",
                type: "text",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Badge",
                table: "Tariffs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FeaturesJson",
                table: "Tariffs",
                type: "text",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullDescription",
                table: "Tariffs",
                type: "text",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProvisioningScenario",
                table: "Tariffs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "auto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfterPaymentText",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "Badge",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "FeaturesJson",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "FullDescription",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "ProvisioningScenario",
                table: "Tariffs");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tariffs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 4000);
        }
    }
}
