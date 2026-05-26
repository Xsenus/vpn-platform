using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VpnPlatform.Infrastructure.Persistence;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260520000100_SecurityHardeningMvp")]
public partial class SecurityHardeningMvp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE "VpnNodes" ADD COLUMN IF NOT EXISTS "ProtectedSshCredential" text NOT NULL DEFAULT '';
ALTER TABLE "VpnNodes" ADD COLUMN IF NOT EXISTS "SshCredentialRef" character varying(4000) NOT NULL DEFAULT '';
ALTER TABLE "VpnNodes" ADD COLUMN IF NOT EXISTS "ProtectedPanelPassword" text NOT NULL DEFAULT '';
ALTER TABLE "VpnNodes" ADD COLUMN IF NOT EXISTS "PanelSecretRef" character varying(4000) NOT NULL DEFAULT '';

CREATE TABLE IF NOT EXISTS "UserRefreshTokens" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NOT NULL,
    "TokenHash" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone NULL,
    "ReplacedByTokenHash" text NOT NULL,
    "ReuseDetectedAt" timestamp with time zone NULL,
    "CreatedByIp" character varying(4000) NOT NULL,
    "RevokedByIp" character varying(4000) NOT NULL,
    "UserAgent" character varying(4000) NOT NULL,
    "RevocationReason" character varying(4000) NOT NULL,
    CONSTRAINT "PK_UserRefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserRefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserRefreshTokens_TokenHash" ON "UserRefreshTokens" ("TokenHash");
CREATE INDEX IF NOT EXISTS "IX_UserRefreshTokens_UserId_ExpiresAt" ON "UserRefreshTokens" ("UserId", "ExpiresAt");

CREATE TABLE IF NOT EXISTS "PasswordResetTokens" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NOT NULL,
    "TokenHash" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "UsedAt" timestamp with time zone NULL,
    "RequestedByIp" character varying(4000) NOT NULL,
    "UserAgent" character varying(4000) NOT NULL,
    CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PasswordResetTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PasswordResetTokens_TokenHash" ON "PasswordResetTokens" ("TokenHash");
CREATE INDEX IF NOT EXISTS "IX_PasswordResetTokens_UserId_ExpiresAt" ON "PasswordResetTokens" ("UserId", "ExpiresAt");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS "PasswordResetTokens";
DROP TABLE IF EXISTS "UserRefreshTokens";
ALTER TABLE "VpnNodes" DROP COLUMN IF EXISTS "PanelSecretRef";
ALTER TABLE "VpnNodes" DROP COLUMN IF EXISTS "ProtectedPanelPassword";
ALTER TABLE "VpnNodes" DROP COLUMN IF EXISTS "SshCredentialRef";
ALTER TABLE "VpnNodes" DROP COLUMN IF EXISTS "ProtectedSshCredential";
""");
    }
}
