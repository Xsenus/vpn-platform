using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SiteContentBlockConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "SiteContentBlocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Revision",
                table: "SiteContentBlocks");
        }
    }
}
