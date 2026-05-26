using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VpnPlatform.Infrastructure.Persistence;

#nullable disable

namespace VpnPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260429000200_Phase2Payments")]
public partial class Phase2Payments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE "PaymentProviderAccounts" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Provider" integer NOT NULL,
    "Mode" integer NOT NULL,
    "Name" character varying(4000) NOT NULL,
    "PublicName" character varying(4000) NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "IsDefault" boolean NOT NULL,
    "ShopId" character varying(4000) NOT NULL,
    "ApiBaseUrl" character varying(4000) NOT NULL,
    "ReturnUrl" character varying(4000) NOT NULL,
    "SecretKeyProtected" character varying(4000) NOT NULL,
    "WebhookSecretProtected" character varying(4000) NOT NULL,
    "UseWebhookIpAllowList" boolean NOT NULL,
    "AllowedWebhookIpRangesCsv" character varying(4000) NOT NULL,
    "ExtraSettingsJson" character varying(4000) NOT NULL,
    "LastHealthCheckAt" timestamp with time zone NULL,
    "HealthStatus" integer NOT NULL,
    CONSTRAINT "PK_PaymentProviderAccounts" PRIMARY KEY ("Id")
);

CREATE TABLE "PaymentProviderSettings" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "PaymentProviderAccountId" uuid NOT NULL,
    "Key" character varying(4000) NOT NULL,
    "Value" character varying(4000) NOT NULL,
    "IsSecret" boolean NOT NULL,
    "Description" character varying(4000) NOT NULL,
    CONSTRAINT "PK_PaymentProviderSettings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PaymentProviderSettings_PaymentProviderAccounts_PaymentProviderAccountId" FOREIGN KEY ("PaymentProviderAccountId") REFERENCES "PaymentProviderAccounts" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CheckoutSessions" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "TokenHash" character varying(4000) NOT NULL,
    "TariffId" uuid NOT NULL,
    "UserId" uuid NULL,
    "OrderId" uuid NULL,
    "Type" integer NOT NULL,
    "Channel" integer NOT NULL,
    "PaymentProvider" integer NOT NULL,
    "PromoCode" character varying(4000) NULL,
    "IsFirstPurchase" boolean NOT NULL,
    "EmailHint" character varying(4000) NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "ClaimedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    "Status" character varying(4000) NOT NULL,
    "MetadataJson" character varying(4000) NOT NULL,
    CONSTRAINT "PK_CheckoutSessions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CheckoutSessions_Tariffs_TariffId" FOREIGN KEY ("TariffId") REFERENCES "Tariffs" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CheckoutSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

ALTER TABLE "Orders" ADD COLUMN "CheckoutSessionId" uuid NULL;
ALTER TABLE "Payments" ADD COLUMN "PaymentProviderAccountId" uuid NULL;
ALTER TABLE "Payments" ADD COLUMN "ProviderMode" integer NOT NULL DEFAULT 0;
ALTER TABLE "Payments" ADD COLUMN "IdempotencyKey" character varying(4000) NOT NULL DEFAULT '';
ALTER TABLE "Payments" ALTER COLUMN "RawRequest" TYPE text;
ALTER TABLE "Payments" ALTER COLUMN "RawResponse" TYPE text;
ALTER TABLE "Payments" ALTER COLUMN "WebhookPayload" TYPE text;
ALTER TABLE "Payments" ADD COLUMN "ConfirmationUrl" character varying(4000) NOT NULL DEFAULT '';
ALTER TABLE "Payments" ADD COLUMN "ReturnUrl" character varying(4000) NOT NULL DEFAULT '';
ALTER TABLE "Payments" ADD COLUMN "IsActivationProcessed" boolean NOT NULL DEFAULT false;
ALTER TABLE "Payments" ADD COLUMN "ActivationProcessedAt" timestamp with time zone NULL;
ALTER TABLE "Payments" ADD COLUMN "PaidAt" timestamp with time zone NULL;
ALTER TABLE "Payments" ADD COLUMN "FailedAt" timestamp with time zone NULL;
ALTER TABLE "Payments" ADD COLUMN "RefundedAt" timestamp with time zone NULL;
ALTER TABLE "Payments" ADD COLUMN "RefundedAmount" numeric NOT NULL DEFAULT 0;
ALTER TABLE "Payments" ADD COLUMN "StatusReason" character varying(4000) NOT NULL DEFAULT '';
UPDATE "Payments" SET "IdempotencyKey" = 'legacy-' || "Id"::text WHERE "IdempotencyKey" = '';

CREATE TABLE "PaymentWebhookEvents" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Provider" integer NOT NULL,
    "PaymentAttemptId" uuid NULL,
    "ProviderPaymentId" character varying(4000) NOT NULL,
    "ExternalEventId" character varying(4000) NOT NULL,
    "EventType" character varying(4000) NOT NULL,
    "PayloadSha256" character varying(4000) NOT NULL,
    "RawPayload" text NOT NULL,
    "HeadersJson" text NOT NULL,
    "SignatureValidated" boolean NOT NULL,
    "Status" integer NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone NULL,
    "ErrorText" text NOT NULL,
    CONSTRAINT "PK_PaymentWebhookEvents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PaymentWebhookEvents_Payments_PaymentAttemptId" FOREIGN KEY ("PaymentAttemptId") REFERENCES "Payments" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Refunds" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "PaymentAttemptId" uuid NOT NULL,
    "Provider" integer NOT NULL,
    "ProviderRefundId" character varying(4000) NOT NULL,
    "IdempotencyKey" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "Amount" numeric NOT NULL,
    "Currency" character varying(4000) NOT NULL,
    "Reason" character varying(4000) NOT NULL,
    "RawRequest" text NOT NULL,
    "RawResponse" text NOT NULL,
    "RefundedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_Refunds" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Refunds_Payments_PaymentAttemptId" FOREIGN KEY ("PaymentAttemptId") REFERENCES "Payments" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PaymentReceipts" (
    "Id" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "PaymentAttemptId" uuid NOT NULL,
    "Provider" integer NOT NULL,
    "ProviderReceiptId" character varying(4000) NOT NULL,
    "Type" character varying(4000) NOT NULL,
    "Status" integer NOT NULL,
    "FiscalDocumentNumber" character varying(4000) NOT NULL,
    "RawPayload" text NOT NULL,
    CONSTRAINT "PK_PaymentReceipts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PaymentReceipts_Payments_PaymentAttemptId" FOREIGN KEY ("PaymentAttemptId") REFERENCES "Payments" ("Id") ON DELETE CASCADE
);

ALTER TABLE "Orders" ADD CONSTRAINT "FK_Orders_CheckoutSessions_CheckoutSessionId" FOREIGN KEY ("CheckoutSessionId") REFERENCES "CheckoutSessions" ("Id") ON DELETE SET NULL;
ALTER TABLE "CheckoutSessions" ADD CONSTRAINT "FK_CheckoutSessions_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE SET NULL;
ALTER TABLE "Payments" ADD CONSTRAINT "FK_Payments_PaymentProviderAccounts_PaymentProviderAccountId" FOREIGN KEY ("PaymentProviderAccountId") REFERENCES "PaymentProviderAccounts" ("Id") ON DELETE SET NULL;

CREATE UNIQUE INDEX "IX_CheckoutSessions_TokenHash" ON "CheckoutSessions" ("TokenHash");
CREATE INDEX "IX_CheckoutSessions_TariffId" ON "CheckoutSessions" ("TariffId");
CREATE INDEX "IX_CheckoutSessions_UserId" ON "CheckoutSessions" ("UserId");
CREATE INDEX "IX_CheckoutSessions_OrderId" ON "CheckoutSessions" ("OrderId");
CREATE UNIQUE INDEX "IX_PaymentProviderAccounts_Provider_Mode_Name" ON "PaymentProviderAccounts" ("Provider", "Mode", "Name");
CREATE UNIQUE INDEX "IX_PaymentProviderSettings_PaymentProviderAccountId_Key" ON "PaymentProviderSettings" ("PaymentProviderAccountId", "Key");
CREATE INDEX "IX_Orders_CheckoutSessionId" ON "Orders" ("CheckoutSessionId");
CREATE INDEX "IX_Payments_PaymentProviderAccountId" ON "Payments" ("PaymentProviderAccountId");
CREATE UNIQUE INDEX "IX_Payments_IdempotencyKey" ON "Payments" ("IdempotencyKey");
CREATE UNIQUE INDEX "IX_PaymentWebhookEvents_Provider_ExternalEventId_PayloadSha256" ON "PaymentWebhookEvents" ("Provider", "ExternalEventId", "PayloadSha256");
CREATE INDEX "IX_PaymentWebhookEvents_PayloadSha256" ON "PaymentWebhookEvents" ("PayloadSha256");
CREATE INDEX "IX_PaymentWebhookEvents_PaymentAttemptId" ON "PaymentWebhookEvents" ("PaymentAttemptId");
CREATE UNIQUE INDEX "IX_Refunds_Provider_ProviderRefundId" ON "Refunds" ("Provider", "ProviderRefundId");
CREATE UNIQUE INDEX "IX_Refunds_IdempotencyKey" ON "Refunds" ("IdempotencyKey");
CREATE INDEX "IX_Refunds_PaymentAttemptId" ON "Refunds" ("PaymentAttemptId");
CREATE UNIQUE INDEX "IX_PaymentReceipts_Provider_ProviderReceiptId" ON "PaymentReceipts" ("Provider", "ProviderReceiptId");
CREATE INDEX "IX_PaymentReceipts_PaymentAttemptId" ON "PaymentReceipts" ("PaymentAttemptId");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS "PaymentReceipts" CASCADE;
DROP TABLE IF EXISTS "Refunds" CASCADE;
DROP TABLE IF EXISTS "PaymentWebhookEvents" CASCADE;
DROP TABLE IF EXISTS "PaymentProviderSettings" CASCADE;
ALTER TABLE "Payments" DROP CONSTRAINT IF EXISTS "FK_Payments_PaymentProviderAccounts_PaymentProviderAccountId";
ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_CheckoutSessions_CheckoutSessionId";
ALTER TABLE "CheckoutSessions" DROP CONSTRAINT IF EXISTS "FK_CheckoutSessions_Orders_OrderId";
DROP TABLE IF EXISTS "CheckoutSessions" CASCADE;
DROP TABLE IF EXISTS "PaymentProviderAccounts" CASCADE;
DROP INDEX IF EXISTS "IX_Payments_IdempotencyKey";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PaymentProviderAccountId";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "ProviderMode";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "IdempotencyKey";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "ConfirmationUrl";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "ReturnUrl";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "IsActivationProcessed";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "ActivationProcessedAt";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PaidAt";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "FailedAt";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "RefundedAt";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "RefundedAmount";
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "StatusReason";
ALTER TABLE "Orders" DROP COLUMN IF EXISTS "CheckoutSessionId";
""");
    }
}
