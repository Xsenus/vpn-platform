using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TariffConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Tariffs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Tariffs");
        }
    }
}
