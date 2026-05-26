using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VpnPlatform.Infrastructure.Persistence;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260430000100_TelegramBotE2EAndX3UiPanels")]
public partial class TelegramBotE2EAndX3UiPanels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AuthSource" integer NOT NULL DEFAULT 0;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "TelegramRegistrationCompletedAt" timestamp with time zone NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "EmailConfirmed" boolean NOT NULL DEFAULT false;
ALTER TABLE "TelegramAccounts" ADD COLUMN IF NOT EXISTS "RegistrationCompletedAt" timestamp with time zone NULL;
ALTER TABLE "TelegramBotNotifications" ADD COLUMN IF NOT EXISTS "AttemptCount" integer NOT NULL DEFAULT 0;
ALTER TABLE "TelegramBotNotifications" ADD COLUMN IF NOT EXISTS "NextAttemptAt" timestamp with time zone NULL;
ALTER TABLE "SupportConversations" ADD COLUMN IF NOT EXISTS "InternalNote" text NOT NULL DEFAULT '';
ALTER TABLE "SupportMessages" ADD COLUMN IF NOT EXISTS "AttachmentsJson" text NOT NULL DEFAULT '[]';
ALTER TABLE "SupportMessages" ADD COLUMN IF NOT EXISTS "IsInternalNote" boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS "VpnPanels" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Name" character varying(4000) NOT NULL,
    "BaseUrl" character varying(4000) NOT NULL,
    "Region" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "HealthStatus" integer NOT NULL,
    "Login" character varying(4000) NOT NULL,
    "EncryptedPassword" text NOT NULL,
    "SslVerificationMode" integer NOT NULL,
    "ApiVariant" integer NOT NULL,
    "Capacity" integer NOT NULL,
    "UsedCapacity" integer NOT NULL,
    "AutoCreateInbound" boolean NOT NULL,
    "DefaultInboundTemplateJson" text NOT NULL,
    "LastHealthCheckAt" timestamp with time zone NULL,
    "LastSyncAt" timestamp with time zone NULL,
    "LastError" character varying(4000) NOT NULL,
    "Version" character varying(4000) NOT NULL,
    CONSTRAINT "PK_VpnPanels" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "VpnInbounds" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "VpnPanelId" uuid NOT NULL,
    "ExternalInboundId" character varying(4000) NOT NULL,
    "Name" character varying(4000) NOT NULL,
    "Protocol" character varying(4000) NOT NULL,
    "Port" integer NOT NULL,
    "Listen" character varying(4000) NOT NULL,
    "SettingsJson" text NOT NULL,
    "StreamSettingsJson" text NOT NULL,
    "SniffingJson" text NOT NULL,
    "IsDefault" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "Capacity" integer NOT NULL,
    "UsedCapacity" integer NOT NULL,
    CONSTRAINT "PK_VpnInbounds" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VpnInbounds_VpnPanels_VpnPanelId" FOREIGN KEY ("VpnPanelId") REFERENCES "VpnPanels" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "VpnClients" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NOT NULL,
    "SubscriptionId" uuid NOT NULL,
    "VpnPanelId" uuid NOT NULL,
    "VpnInboundId" uuid NOT NULL,
    "ExternalClientId" character varying(4000) NOT NULL,
    "Email" character varying(4000) NOT NULL,
    "Uuid" character varying(4000) NOT NULL,
    "Flow" character varying(4000) NOT NULL,
    "LimitIp" integer NOT NULL,
    "TotalGb" bigint NULL,
    "ExpiryTime" timestamp with time zone NOT NULL,
    "Enable" boolean NOT NULL,
    "ConfigUri" text NOT NULL,
    "QrCodePayload" text NOT NULL,
    "LastSyncedAt" timestamp with time zone NULL,
    "SyncStatus" character varying(4000) NOT NULL,
    CONSTRAINT "PK_VpnClients" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VpnClients_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_VpnClients_Subscriptions_SubscriptionId" FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_VpnClients_VpnPanels_VpnPanelId" FOREIGN KEY ("VpnPanelId") REFERENCES "VpnPanels" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_VpnClients_VpnInbounds_VpnInboundId" FOREIGN KEY ("VpnInboundId") REFERENCES "VpnInbounds" ("Id") ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS "PanelSyncRuns" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "VpnPanelId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "FinishedAt" timestamp with time zone NULL,
    "SummaryJson" text NOT NULL,
    "ErrorMessage" text NOT NULL,
    CONSTRAINT "PK_PanelSyncRuns" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PanelSyncRuns_VpnPanels_VpnPanelId" FOREIGN KEY ("VpnPanelId") REFERENCES "VpnPanels" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "PanelSyncEvents" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "PanelSyncRunId" uuid NOT NULL,
    "EventType" character varying(4000) NOT NULL,
    "EntityType" character varying(4000) NOT NULL,
    "EntityId" uuid NULL,
    "ExternalId" character varying(4000) NOT NULL,
    "Message" text NOT NULL,
    "PayloadJson" text NOT NULL,
    CONSTRAINT "PK_PanelSyncEvents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PanelSyncEvents_PanelSyncRuns_PanelSyncRunId" FOREIGN KEY ("PanelSyncRunId") REFERENCES "PanelSyncRuns" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "PanelHealthChecks" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "VpnPanelId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "LatencyMs" bigint NULL,
    "Version" character varying(4000) NOT NULL,
    "ErrorMessage" text NOT NULL,
    "CheckedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PanelHealthChecks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PanelHealthChecks_VpnPanels_VpnPanelId" FOREIGN KEY ("VpnPanelId") REFERENCES "VpnPanels" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AccessCredentialHistories" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "AccessCredentialId" uuid NOT NULL,
    "SubscriptionId" uuid NOT NULL,
    "EventType" character varying(4000) NOT NULL,
    "OldValueJson" text NOT NULL,
    "NewValueJson" text NOT NULL,
    CONSTRAINT "PK_AccessCredentialHistories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccessCredentialHistories_AccessCredentials_AccessCredentialId" FOREIGN KEY ("AccessCredentialId") REFERENCES "AccessCredentials" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AccessCredentialHistories_Subscriptions_SubscriptionId" FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_VpnPanels_Name" ON "VpnPanels" ("Name");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_VpnPanels_BaseUrl" ON "VpnPanels" ("BaseUrl");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_VpnInbounds_VpnPanelId_ExternalInboundId" ON "VpnInbounds" ("VpnPanelId", "ExternalInboundId");
CREATE INDEX IF NOT EXISTS "IX_VpnInbounds_VpnPanelId_IsDefault" ON "VpnInbounds" ("VpnPanelId", "IsDefault");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_VpnClients_SubscriptionId" ON "VpnClients" ("SubscriptionId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_VpnClients_VpnPanelId_VpnInboundId_Uuid" ON "VpnClients" ("VpnPanelId", "VpnInboundId", "Uuid");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_VpnClients_VpnPanelId_ExternalClientId" ON "VpnClients" ("VpnPanelId", "ExternalClientId");
CREATE INDEX IF NOT EXISTS "IX_VpnClients_UserId" ON "VpnClients" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_VpnClients_VpnInboundId" ON "VpnClients" ("VpnInboundId");
CREATE INDEX IF NOT EXISTS "IX_PanelSyncRuns_VpnPanelId_StartedAt" ON "PanelSyncRuns" ("VpnPanelId", "StartedAt");
CREATE INDEX IF NOT EXISTS "IX_PanelSyncEvents_PanelSyncRunId_EventType" ON "PanelSyncEvents" ("PanelSyncRunId", "EventType");
CREATE INDEX IF NOT EXISTS "IX_PanelHealthChecks_VpnPanelId_CheckedAt" ON "PanelHealthChecks" ("VpnPanelId", "CheckedAt");
CREATE INDEX IF NOT EXISTS "IX_AccessCredentialHistories_AccessCredentialId_CreatedAt" ON "AccessCredentialHistories" ("AccessCredentialId", "CreatedAt");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS "AccessCredentialHistories";
DROP TABLE IF EXISTS "PanelHealthChecks";
DROP TABLE IF EXISTS "PanelSyncEvents";
DROP TABLE IF EXISTS "PanelSyncRuns";
DROP TABLE IF EXISTS "VpnClients";
DROP TABLE IF EXISTS "VpnInbounds";
DROP TABLE IF EXISTS "VpnPanels";
ALTER TABLE "SupportMessages" DROP COLUMN IF EXISTS "IsInternalNote";
ALTER TABLE "SupportMessages" DROP COLUMN IF EXISTS "AttachmentsJson";
ALTER TABLE "SupportConversations" DROP COLUMN IF EXISTS "InternalNote";
ALTER TABLE "TelegramBotNotifications" DROP COLUMN IF EXISTS "NextAttemptAt";
ALTER TABLE "TelegramBotNotifications" DROP COLUMN IF EXISTS "AttemptCount";
ALTER TABLE "TelegramAccounts" DROP COLUMN IF EXISTS "RegistrationCompletedAt";
ALTER TABLE "Users" DROP COLUMN IF EXISTS "EmailConfirmed";
ALTER TABLE "Users" DROP COLUMN IF EXISTS "TelegramRegistrationCompletedAt";
ALTER TABLE "Users" DROP COLUMN IF EXISTS "AuthSource";
""");
    }
}
