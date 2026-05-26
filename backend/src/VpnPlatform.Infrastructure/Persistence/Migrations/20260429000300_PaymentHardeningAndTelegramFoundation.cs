using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VpnPlatform.Infrastructure.Persistence;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260429000300_PaymentHardeningAndTelegramFoundation")]
public partial class PaymentHardeningAndTelegramFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE "PaymentWebhookEvents" ADD COLUMN IF NOT EXISTS "PaymentProviderAccountId" uuid NULL;
DROP INDEX IF EXISTS "IX_PaymentWebhookEvents_Provider_ExternalEventId_PayloadSha256";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PaymentWebhookEvents_Provider_ExternalEventId_ProviderPaymentId" ON "PaymentWebhookEvents" ("Provider", "ExternalEventId", "ProviderPaymentId");
CREATE INDEX IF NOT EXISTS "IX_PaymentWebhookEvents_PaymentProviderAccountId" ON "PaymentWebhookEvents" ("PaymentProviderAccountId");
ALTER TABLE "PaymentWebhookEvents" ADD CONSTRAINT "FK_PaymentWebhookEvents_PaymentProviderAccounts_PaymentProviderAccountId" FOREIGN KEY ("PaymentProviderAccountId") REFERENCES "PaymentProviderAccounts" ("Id") ON DELETE SET NULL;

CREATE TABLE IF NOT EXISTS "TelegramAccounts" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NULL,
    "TelegramUserId" bigint NOT NULL,
    "Username" character varying(4000) NOT NULL,
    "FirstName" character varying(4000) NOT NULL,
    "LastName" character varying(4000) NOT NULL,
    "LanguageCode" character varying(4000) NOT NULL,
    "IsBlocked" boolean NOT NULL,
    "LinkedAt" timestamp with time zone NULL,
    "LastSeenAt" timestamp with time zone NULL,
    "MetadataJson" character varying(4000) NOT NULL,
    CONSTRAINT "PK_TelegramAccounts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TelegramAccounts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS "TelegramBotUpdates" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UpdateId" bigint NOT NULL,
    "TelegramUserId" bigint NULL,
    "UpdateType" character varying(4000) NOT NULL,
    "RawPayload" text NOT NULL,
    "PayloadSha256" character varying(4000) NOT NULL,
    "IsProcessed" boolean NOT NULL,
    "ProcessedAt" timestamp with time zone NULL,
    "ErrorText" text NOT NULL,
    CONSTRAINT "PK_TelegramBotUpdates" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TelegramBotSessions" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "TelegramUserId" bigint NOT NULL,
    "CurrentState" character varying(4000) NOT NULL,
    "PayloadJson" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_TelegramBotSessions" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TelegramBotCommandLogs" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "TelegramUserId" bigint NOT NULL,
    "UpdateId" bigint NULL,
    "Command" character varying(4000) NOT NULL,
    "Payload" text NOT NULL,
    "ResultStatus" character varying(4000) NOT NULL,
    CONSTRAINT "PK_TelegramBotCommandLogs" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TelegramBotMessages" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "TelegramAccountId" uuid NULL,
    "TelegramUserId" bigint NOT NULL,
    "ChatId" bigint NOT NULL,
    "MessageId" bigint NULL,
    "Direction" character varying(4000) NOT NULL,
    "Text" text NOT NULL,
    "RawPayload" text NOT NULL,
    CONSTRAINT "PK_TelegramBotMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TelegramBotMessages_TelegramAccounts_TelegramAccountId" FOREIGN KEY ("TelegramAccountId") REFERENCES "TelegramAccounts" ("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS "TelegramBotCallbackQueries" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CallbackQueryId" character varying(4000) NOT NULL,
    "TelegramUserId" bigint NOT NULL,
    "Data" text NOT NULL,
    "RawPayload" text NOT NULL,
    "IsProcessed" boolean NOT NULL,
    "ProcessedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_TelegramBotCallbackQueries" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TelegramBotPayments" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "PaymentAttemptId" uuid NULL,
    "TelegramUserId" bigint NOT NULL,
    "ProviderPaymentChargeId" character varying(4000) NOT NULL,
    "TelegramPaymentChargeId" character varying(4000) NOT NULL,
    "InvoicePayload" text NOT NULL,
    "TotalAmount" bigint NOT NULL,
    "Currency" character varying(4000) NOT NULL,
    "RawPayload" text NOT NULL,
    CONSTRAINT "PK_TelegramBotPayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TelegramBotPayments_Payments_PaymentAttemptId" FOREIGN KEY ("PaymentAttemptId") REFERENCES "Payments" ("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS "TelegramBotDeepLinks" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NULL,
    "TokenHash" character varying(4000) NOT NULL,
    "Purpose" character varying(4000) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "UsedAt" timestamp with time zone NULL,
    "UsedByTelegramUserId" bigint NULL,
    "MetadataJson" text NOT NULL,
    CONSTRAINT "PK_TelegramBotDeepLinks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TelegramBotDeepLinks_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "TelegramBotNotifications" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "TelegramUserId" bigint NOT NULL,
    "Type" character varying(4000) NOT NULL,
    "PayloadJson" text NOT NULL,
    "Status" character varying(4000) NOT NULL,
    "SentAt" timestamp with time zone NULL,
    "ErrorText" text NOT NULL,
    CONSTRAINT "PK_TelegramBotNotifications" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "SupportConversations" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NULL,
    "TelegramUserId" bigint NULL,
    "Channel" character varying(4000) NOT NULL,
    "Status" character varying(4000) NOT NULL,
    "AssignedToUserId" uuid NULL,
    "Subject" character varying(4000) NOT NULL,
    "ClosedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_SupportConversations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SupportConversations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS "SupportMessages" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "SupportConversationId" uuid NOT NULL,
    "UserId" uuid NULL,
    "TelegramUserId" bigint NULL,
    "Direction" character varying(4000) NOT NULL,
    "Text" text NOT NULL,
    "RawPayload" text NOT NULL,
    "DeliveredAt" timestamp with time zone NULL,
    CONSTRAINT "PK_SupportMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SupportMessages_SupportConversations_SupportConversationId" FOREIGN KEY ("SupportConversationId") REFERENCES "SupportConversations" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_TelegramAccounts_TelegramUserId" ON "TelegramAccounts" ("TelegramUserId");
CREATE INDEX IF NOT EXISTS "IX_TelegramAccounts_UserId" ON "TelegramAccounts" ("UserId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TelegramBotUpdates_UpdateId" ON "TelegramBotUpdates" ("UpdateId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TelegramBotSessions_TelegramUserId" ON "TelegramBotSessions" ("TelegramUserId");
CREATE INDEX IF NOT EXISTS "IX_TelegramBotMessages_TelegramAccountId" ON "TelegramBotMessages" ("TelegramAccountId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TelegramBotCallbackQueries_CallbackQueryId" ON "TelegramBotCallbackQueries" ("CallbackQueryId");
CREATE INDEX IF NOT EXISTS "IX_TelegramBotPayments_PaymentAttemptId" ON "TelegramBotPayments" ("PaymentAttemptId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TelegramBotPayments_TelegramPaymentChargeId" ON "TelegramBotPayments" ("TelegramPaymentChargeId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TelegramBotDeepLinks_TokenHash" ON "TelegramBotDeepLinks" ("TokenHash");
CREATE INDEX IF NOT EXISTS "IX_TelegramBotDeepLinks_UserId" ON "TelegramBotDeepLinks" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_SupportConversations_UserId" ON "SupportConversations" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_SupportConversations_TelegramUserId_Status" ON "SupportConversations" ("TelegramUserId", "Status");
CREATE INDEX IF NOT EXISTS "IX_SupportMessages_SupportConversationId" ON "SupportMessages" ("SupportConversationId");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS "SupportMessages" CASCADE;
DROP TABLE IF EXISTS "SupportConversations" CASCADE;
DROP TABLE IF EXISTS "TelegramBotNotifications" CASCADE;
DROP TABLE IF EXISTS "TelegramBotDeepLinks" CASCADE;
DROP TABLE IF EXISTS "TelegramBotPayments" CASCADE;
DROP TABLE IF EXISTS "TelegramBotCallbackQueries" CASCADE;
DROP TABLE IF EXISTS "TelegramBotMessages" CASCADE;
DROP TABLE IF EXISTS "TelegramBotCommandLogs" CASCADE;
DROP TABLE IF EXISTS "TelegramBotSessions" CASCADE;
DROP TABLE IF EXISTS "TelegramBotUpdates" CASCADE;
DROP TABLE IF EXISTS "TelegramAccounts" CASCADE;
ALTER TABLE "PaymentWebhookEvents" DROP CONSTRAINT IF EXISTS "FK_PaymentWebhookEvents_PaymentProviderAccounts_PaymentProviderAccountId";
DROP INDEX IF EXISTS "IX_PaymentWebhookEvents_PaymentProviderAccountId";
DROP INDEX IF EXISTS "IX_PaymentWebhookEvents_Provider_ExternalEventId_ProviderPaymentId";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PaymentWebhookEvents_Provider_ExternalEventId_PayloadSha256" ON "PaymentWebhookEvents" ("Provider", "ExternalEventId", "PayloadSha256");
ALTER TABLE "PaymentWebhookEvents" DROP COLUMN IF EXISTS "PaymentProviderAccountId";
""");
    }
}
