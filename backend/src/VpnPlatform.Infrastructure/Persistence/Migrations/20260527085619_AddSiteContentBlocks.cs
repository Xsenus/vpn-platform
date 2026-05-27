using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteContentBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteContentBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Value = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    Group = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    InputType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteContentBlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteContentBlocks_Group_IsActive_SortOrder",
                table: "SiteContentBlocks",
                columns: new[] { "Group", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteContentBlocks_Key",
                table: "SiteContentBlocks",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteContentBlocks");
        }
    }
}
