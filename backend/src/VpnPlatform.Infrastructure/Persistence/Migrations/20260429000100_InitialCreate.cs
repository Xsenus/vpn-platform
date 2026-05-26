using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VpnPlatform.Infrastructure.Persistence;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260429000100_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Email" character varying(4000) NULL,
    "Phone" character varying(4000) NULL,
    "DisplayName" character varying(4000) NOT NULL,
    "PasswordHash" character varying(4000) NOT NULL,
    "RolesCsv" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "IsBlocked" boolean NOT NULL,
    "LastLoginAt" timestamp with time zone NULL,
    "PreferredLanguage" character varying(4000) NOT NULL,
    "ReferralCode" character varying(4000) NOT NULL,
    "ReferredByUserId" uuid NULL,
    "MetadataJson" character varying(4000) NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "Tariffs" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Name" character varying(4000) NOT NULL,
    "Slug" character varying(4000) NOT NULL,
    "Description" character varying(4000) NOT NULL,
    "DurationDays" integer NOT NULL,
    "Price" numeric NOT NULL,
    "Currency" character varying(4000) NOT NULL,
    "MaxDevices" integer NOT NULL,
    "TrafficLimit" bigint NULL,
    "IsTrial" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "SortOrder" integer NOT NULL,
    "VisibleFrom" timestamp with time zone NULL,
    "VisibleTo" timestamp with time zone NULL,
    "TariffType" integer NOT NULL,
    "Category" character varying(4000) NOT NULL,
    "AllowedRegionsCsv" character varying(4000) NOT NULL,
    "AllowedNodeGroupsCsv" character varying(4000) NOT NULL,
    "IsReferralEligible" boolean NOT NULL,
    CONSTRAINT "PK_Tariffs" PRIMARY KEY ("Id")
);

CREATE TABLE "PromoCodes" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Code" character varying(4000) NOT NULL,
    "DiscountType" character varying(4000) NOT NULL,
    "DiscountValue" numeric NOT NULL,
    "FreeDays" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "StartsAt" timestamp with time zone NULL,
    "EndsAt" timestamp with time zone NULL,
    "MaxRedemptions" integer NULL,
    "MaxPerUser" integer NULL,
    "AllowedTariffIdsJson" character varying(4000) NOT NULL,
    "AllowedChannelsJson" character varying(4000) NOT NULL,
    "AllowStackWithReferral" boolean NOT NULL,
    CONSTRAINT "PK_PromoCodes" PRIMARY KEY ("Id")
);

CREATE TABLE "NodeGroups" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Name" character varying(4000) NOT NULL,
    "Code" character varying(4000) NOT NULL,
    "Region" character varying(4000) NOT NULL,
    "IsActive" boolean NOT NULL,
    "AllocationStrategy" character varying(4000) NOT NULL,
    CONSTRAINT "PK_NodeGroups" PRIMARY KEY ("Id")
);

CREATE TABLE "VpnNodes" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Name" character varying(4000) NOT NULL,
    "Host" character varying(4000) NOT NULL,
    "IpAddress" character varying(4000) NOT NULL,
    "Provider" character varying(4000) NOT NULL,
    "Region" character varying(4000) NOT NULL,
    "Country" character varying(4000) NOT NULL,
    "Datacenter" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "Capacity" integer NOT NULL,
    "UsedCapacity" integer NOT NULL,
    "SupportedProtocolsCsv" character varying(4000) NOT NULL,
    "HealthStatus" integer NOT NULL,
    "LastHealthCheckAt" timestamp with time zone NULL,
    "ProvisioningStatus" integer NOT NULL,
    "InstalledVersion" character varying(4000) NOT NULL,
    "BackupStatus" character varying(4000) NOT NULL,
    "MonitoringStatus" character varying(4000) NOT NULL,
    "LoggingStatus" character varying(4000) NOT NULL,
    "TagsCsv" character varying(4000) NOT NULL,
    "Priority" integer NOT NULL,
    "IsAvailableForNewUsers" boolean NOT NULL,
    "SshPort" integer NOT NULL,
    "SshUser" character varying(4000) NOT NULL,
    "SshPrivateKeyPath" character varying(4000) NOT NULL,
    "SkipHostKeyChecking" boolean NOT NULL,
    "PanelBaseUrl" character varying(4000) NOT NULL,
    "PanelUsername" character varying(4000) NOT NULL,
    "PanelPassword" character varying(4000) NOT NULL,
    "PanelInboundId" integer NULL,
    "PublicHostname" character varying(4000) NOT NULL,
    "PublicPort" integer NOT NULL,
    "NodeGroupId" uuid NULL,
    CONSTRAINT "PK_VpnNodes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VpnNodes_NodeGroups_NodeGroupId" FOREIGN KEY ("NodeGroupId") REFERENCES "NodeGroups" ("Id")
);

CREATE TABLE "Orders" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NOT NULL,
    "TariffId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "Status" integer NOT NULL,
    "Amount" numeric NOT NULL,
    "Currency" character varying(4000) NOT NULL,
    "Channel" integer NOT NULL,
    "PaymentProvider" integer NOT NULL,
    "PromoCodeId" uuid NULL,
    "ReferralContext" character varying(4000) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "PaidAt" timestamp with time zone NULL,
    "IsFirstPurchase" boolean NOT NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Orders_Tariffs_TariffId" FOREIGN KEY ("TariffId") REFERENCES "Tariffs" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Orders_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Payments" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "OrderId" uuid NOT NULL,
    "Provider" integer NOT NULL,
    "ProviderPaymentId" character varying(4000) NOT NULL,
    "ExternalEventId" character varying(4000) NOT NULL,
    "Amount" numeric NOT NULL,
    "Currency" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "RawRequest" character varying(4000) NOT NULL,
    "RawResponse" character varying(4000) NOT NULL,
    "WebhookPayload" character varying(4000) NOT NULL,
    "SignatureValidated" boolean NOT NULL,
    CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Payments_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Subscriptions" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NOT NULL,
    "TariffId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "StartAt" timestamp with time zone NOT NULL,
    "EndAt" timestamp with time zone NOT NULL,
    "GracePeriodEndAt" timestamp with time zone NULL,
    "AutoRenewFlag" boolean NOT NULL,
    "SourceChannel" integer NOT NULL,
    "CurrentServerId" uuid NULL,
    "CurrentAccessId" uuid NULL,
    "LastPaymentId" uuid NULL,
    "RenewalCount" integer NOT NULL,
    "BlockReason" character varying(4000) NULL,
    "SuspendedAt" timestamp with time zone NULL,
    "CancelledAt" timestamp with time zone NULL,
    CONSTRAINT "PK_Subscriptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Subscriptions_Tariffs_TariffId" FOREIGN KEY ("TariffId") REFERENCES "Tariffs" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Subscriptions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Subscriptions_VpnNodes_CurrentServerId" FOREIGN KEY ("CurrentServerId") REFERENCES "VpnNodes" ("Id") ON DELETE SET NULL
);

CREATE TABLE "AccessCredentials" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "SubscriptionId" uuid NOT NULL,
    "ProviderType" character varying(4000) NOT NULL,
    "ProviderAccessId" character varying(4000) NOT NULL,
    "ServerId" uuid NOT NULL,
    "AccessUri" character varying(4000) NOT NULL,
    "QrCodePath" character varying(4000) NOT NULL,
    "ConfigPath" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "IssuedAt" timestamp with time zone NOT NULL,
    "DisabledAt" timestamp with time zone NULL,
    "LastSyncedAt" timestamp with time zone NULL,
    "Revision" integer NOT NULL,
    CONSTRAINT "PK_AccessCredentials" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccessCredentials_Subscriptions_SubscriptionId" FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AccessCredentials_VpnNodes_ServerId" FOREIGN KEY ("ServerId") REFERENCES "VpnNodes" ("Id") ON DELETE CASCADE
);

ALTER TABLE "Subscriptions" ADD CONSTRAINT "FK_Subscriptions_AccessCredentials_CurrentAccessId" FOREIGN KEY ("CurrentAccessId") REFERENCES "AccessCredentials" ("Id") ON DELETE SET NULL;

CREATE TABLE "ChannelProfiles" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NOT NULL,
    "ProviderType" integer NOT NULL,
    "ExternalUserId" character varying(4000) NOT NULL,
    "Username" character varying(4000) NULL,
    "ChatId" character varying(4000) NULL,
    "MetadataJson" character varying(4000) NOT NULL,
    "LinkedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ChannelProfiles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ChannelProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ReferralPrograms" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Name" character varying(4000) NOT NULL,
    "Status" character varying(4000) NOT NULL,
    "StartAt" timestamp with time zone NULL,
    "EndAt" timestamp with time zone NULL,
    "RuleDefinition" character varying(4000) NOT NULL,
    "RewardDefinition" character varying(4000) NOT NULL,
    "AntiFraudSettings" character varying(4000) NOT NULL,
    CONSTRAINT "PK_ReferralPrograms" PRIMARY KEY ("Id")
);

CREATE TABLE "ReferralRelationships" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "ReferrerUserId" uuid NOT NULL,
    "ReferredUserId" uuid NOT NULL,
    "SourceChannel" integer NOT NULL,
    "IsSuspicious" boolean NOT NULL,
    CONSTRAINT "PK_ReferralRelationships" PRIMARY KEY ("Id")
);

CREATE TABLE "RewardLedgers" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NOT NULL,
    "SourceUserId" uuid NULL,
    "ReferralProgramId" uuid NULL,
    "Type" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "Value" numeric NOT NULL,
    "CurrencyOrUnit" character varying(4000) NOT NULL,
    "ProcessedAt" timestamp with time zone NULL,
    "MetadataJson" character varying(4000) NOT NULL,
    CONSTRAINT "PK_RewardLedgers" PRIMARY KEY ("Id")
);

CREATE TABLE "NotificationTemplates" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Key" character varying(4000) NOT NULL,
    "Channel" integer NOT NULL,
    "Language" character varying(4000) NOT NULL,
    "Subject" character varying(4000) NOT NULL,
    "Body" character varying(4000) NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "PK_NotificationTemplates" PRIMARY KEY ("Id")
);

CREATE TABLE "NotificationDeliveries" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NULL,
    "TemplateKey" character varying(4000) NOT NULL,
    "Channel" integer NOT NULL,
    "ToAddress" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "PayloadJson" character varying(4000) NOT NULL,
    "Attempts" integer NOT NULL,
    "SentAt" timestamp with time zone NULL,
    "ErrorText" character varying(4000) NULL,
    CONSTRAINT "PK_NotificationDeliveries" PRIMARY KEY ("Id")
);

CREATE TABLE "OutboxMessages" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Type" character varying(4000) NOT NULL,
    "PayloadJson" character varying(4000) NOT NULL,
    "CorrelationId" character varying(4000) NOT NULL,
    "Attempts" integer NOT NULL,
    "ProcessedAt" timestamp with time zone NULL,
    "LastError" character varying(4000) NULL,
    CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
);

CREATE TABLE "InboxMessages" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Source" character varying(4000) NOT NULL,
    "ExternalKey" character varying(4000) NOT NULL,
    "PayloadJson" character varying(4000) NOT NULL,
    "Status" character varying(4000) NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_InboxMessages" PRIMARY KEY ("Id")
);

CREATE TABLE "AuditLogs" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "ActorType" character varying(4000) NOT NULL,
    "ActorId" character varying(4000) NOT NULL,
    "Action" character varying(4000) NOT NULL,
    "EntityType" character varying(4000) NOT NULL,
    "EntityId" character varying(4000) NOT NULL,
    "BeforeJson" character varying(4000) NOT NULL,
    "AfterJson" character varying(4000) NOT NULL,
    "Ip" character varying(4000) NOT NULL,
    "UserAgent" character varying(4000) NOT NULL,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

CREATE TABLE "ProvisioningRuns" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "NodeId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "RequestedByUserId" uuid NULL,
    "DryRun" boolean NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "FinishedAt" timestamp with time zone NULL,
    "ExecutionLog" character varying(4000) NOT NULL,
    CONSTRAINT "PK_ProvisioningRuns" PRIMARY KEY ("Id")
);

CREATE TABLE "ProvisioningStepRuns" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "ProvisioningRunId" uuid NOT NULL,
    "StepName" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "FinishedAt" timestamp with time zone NULL,
    "Output" character varying(4000) NOT NULL,
    "ErrorText" character varying(4000) NOT NULL,
    CONSTRAINT "PK_ProvisioningStepRuns" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProvisioningStepRuns_ProvisioningRuns_ProvisioningRunId" FOREIGN KEY ("ProvisioningRunId") REFERENCES "ProvisioningRuns" ("Id") ON DELETE CASCADE
);

CREATE TABLE "MigrationJobs" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "SourceNodeId" uuid NOT NULL,
    "TargetNodeId" uuid NULL,
    "Status" integer NOT NULL,
    "Type" character varying(4000) NOT NULL,
    "RequestedByUserId" uuid NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "StartedAt" timestamp with time zone NULL,
    "FinishedAt" timestamp with time zone NULL,
    "Notes" character varying(4000) NOT NULL,
    CONSTRAINT "PK_MigrationJobs" PRIMARY KEY ("Id")
);

CREATE TABLE "MigrationItems" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "MigrationJobId" uuid NOT NULL,
    "SubscriptionId" uuid NOT NULL,
    "OldAccessId" uuid NULL,
    "NewAccessId" uuid NULL,
    "Status" integer NOT NULL,
    "ErrorText" character varying(4000) NOT NULL,
    CONSTRAINT "PK_MigrationItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MigrationItems_MigrationJobs_MigrationJobId" FOREIGN KEY ("MigrationJobId") REFERENCES "MigrationJobs" ("Id") ON DELETE CASCADE
);

CREATE TABLE "NodeHealthChecks" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "NodeId" uuid NOT NULL,
    "CheckedAt" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL,
    "LatencyMs" bigint NOT NULL,
    "MetadataJson" character varying(4000) NOT NULL,
    "ErrorText" character varying(4000) NOT NULL,
    CONSTRAINT "PK_NodeHealthChecks" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
CREATE UNIQUE INDEX "IX_Users_ReferralCode" ON "Users" ("ReferralCode");
CREATE INDEX "IX_ChannelProfiles_UserId" ON "ChannelProfiles" ("UserId");
CREATE UNIQUE INDEX "IX_ChannelProfiles_ProviderType_ExternalUserId" ON "ChannelProfiles" ("ProviderType", "ExternalUserId");
CREATE UNIQUE INDEX "IX_Tariffs_Slug" ON "Tariffs" ("Slug");
CREATE UNIQUE INDEX "IX_PromoCodes_Code" ON "PromoCodes" ("Code");
CREATE INDEX "IX_Orders_UserId" ON "Orders" ("UserId");
CREATE INDEX "IX_Orders_TariffId" ON "Orders" ("TariffId");
CREATE INDEX "IX_Payments_OrderId" ON "Payments" ("OrderId");
CREATE UNIQUE INDEX "IX_Payments_Provider_ProviderPaymentId" ON "Payments" ("Provider", "ProviderPaymentId");
CREATE INDEX "IX_Subscriptions_UserId" ON "Subscriptions" ("UserId");
CREATE INDEX "IX_Subscriptions_TariffId" ON "Subscriptions" ("TariffId");
CREATE UNIQUE INDEX "IX_Subscriptions_CurrentAccessId" ON "Subscriptions" ("CurrentAccessId");
CREATE INDEX "IX_Subscriptions_CurrentServerId" ON "Subscriptions" ("CurrentServerId");
CREATE INDEX "IX_AccessCredentials_SubscriptionId" ON "AccessCredentials" ("SubscriptionId");
CREATE INDEX "IX_AccessCredentials_ServerId" ON "AccessCredentials" ("ServerId");
CREATE INDEX "IX_VpnNodes_NodeGroupId" ON "VpnNodes" ("NodeGroupId");
CREATE UNIQUE INDEX "IX_InboxMessages_Source_ExternalKey" ON "InboxMessages" ("Source", "ExternalKey");
CREATE INDEX "IX_OutboxMessages_Type_CorrelationId" ON "OutboxMessages" ("Type", "CorrelationId");
CREATE INDEX "IX_ProvisioningStepRuns_ProvisioningRunId" ON "ProvisioningStepRuns" ("ProvisioningRunId");
CREATE INDEX "IX_MigrationItems_MigrationJobId" ON "MigrationItems" ("MigrationJobId");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS "MigrationItems" CASCADE;
DROP TABLE IF EXISTS "ProvisioningStepRuns" CASCADE;
DROP TABLE IF EXISTS "AccessCredentials" CASCADE;
DROP TABLE IF EXISTS "Payments" CASCADE;
DROP TABLE IF EXISTS "Subscriptions" CASCADE;
DROP TABLE IF EXISTS "Orders" CASCADE;
DROP TABLE IF EXISTS "VpnNodes" CASCADE;
DROP TABLE IF EXISTS "ChannelProfiles" CASCADE;
DROP TABLE IF EXISTS "RewardLedgers" CASCADE;
DROP TABLE IF EXISTS "ReferralRelationships" CASCADE;
DROP TABLE IF EXISTS "NodeHealthChecks" CASCADE;
DROP TABLE IF EXISTS "MigrationJobs" CASCADE;
DROP TABLE IF EXISTS "ProvisioningRuns" CASCADE;
DROP TABLE IF EXISTS "AuditLogs" CASCADE;
DROP TABLE IF EXISTS "InboxMessages" CASCADE;
DROP TABLE IF EXISTS "OutboxMessages" CASCADE;
DROP TABLE IF EXISTS "NotificationDeliveries" CASCADE;
DROP TABLE IF EXISTS "NotificationTemplates" CASCADE;
DROP TABLE IF EXISTS "ReferralPrograms" CASCADE;
DROP TABLE IF EXISTS "PromoCodes" CASCADE;
DROP TABLE IF EXISTS "NodeGroups" CASCADE;
DROP TABLE IF EXISTS "Tariffs" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;
""");
    }
}
