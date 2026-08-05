using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "UserRefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId_SessionVersion_FamilyId",
                table: "UserRefreshTokens",
                columns: new[] { "UserId", "SessionVersion", "FamilyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_UserId_SessionVersion_FamilyId",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "UserRefreshTokens");
        }
    }
}
