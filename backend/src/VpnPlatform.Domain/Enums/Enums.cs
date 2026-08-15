namespace VpnPlatform.Domain.Enums;

public enum UserStatus { New = 0, Active = 1, Suspended = 2, Deleted = 3 }
public enum AuthSource { Local = 0, Telegram = 1, Imported = 2 }
public enum ChannelType { Web = 0, Telegram = 1, Discord = 2, Vk = 3, WhatsApp = 4, Email = 5 }
public enum TariffType { Weekly = 0, Monthly = 1, Quarterly = 2, SemiAnnual = 3, Annual = 4, Trial = 5, Promo = 6, Personal = 7 }
public enum OrderType { NewSubscription = 0, Renewal = 1, Upgrade = 2, Compensation = 3 }
public enum OrderStatus { Draft = 0, PendingPayment = 1, PaymentReceived = 2, FulfillmentInProgress = 3, Completed = 4, Failed = 5, Cancelled = 6, Expired = 7, Refunded = 8, PartiallyProcessed = 9 }

public enum PaymentProvider
{
    YooMoney = 0,
    YooKassa = 1,
    RoboKassa = 2,
    TelegramStars = 3,
    CloudPayments = 4,
    TBankAcquiring = 5,
    Prodamus = 6,
    Stripe = 7,
    PayPal = 8
}

public enum PaymentProviderMode { Disabled = 0, Sandbox = 1, Production = 2 }
public enum PaymentStatus { New = 0, Pending = 1, WaitingConfirmation = 2, Succeeded = 3, Failed = 4, Cancelled = 5, Refunded = 6, PartiallyRefunded = 7, Unknown = 8 }
public enum RefundStatus { New = 0, Pending = 1, Succeeded = 2, Failed = 3, Cancelled = 4, Unknown = 5 }
public enum PaymentWebhookEventStatus { Received = 0, Duplicate = 1, Verified = 2, Processed = 3, Rejected = 4, Failed = 5 }
public enum PaymentReceiptStatus { Pending = 0, Succeeded = 1, Failed = 2, Unknown = 3 }

public enum SubscriptionStatus { PendingActivation = 0, Active = 1, GracePeriod = 2, Expired = 3, Suspended = 4, Cancelled = 5, Blocked = 6 }
public enum AccessCredentialStatus { Provisioning = 0, Active = 1, Rotating = 2, Disabled = 3, Revoked = 4, Error = 5, SyncRequired = 6 }
public enum NodeStatus { New = 0, Provisioning = 1, Ready = 2, Degraded = 3, Full = 4, Draining = 5, Maintenance = 6, Disabled = 7, Error = 8, Archived = 9 }
public enum HealthStatus { Unknown = 0, Healthy = 1, Degraded = 2, Unhealthy = 3 }
public enum NotificationChannelType { Email = 0, Telegram = 1, Bot = 2, Web = 3, Push = 4 }
public enum NotificationDeliveryStatus { Pending = 0, Sent = 1, Failed = 2, Cancelled = 3 }
public enum ProvisioningRunStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4,
    Requested = 5,
    AwaitingCredentials = 6,
    AwaitingConfirmation = 7,
    PrecheckQueued = 8,
    Prechecking = 9,
    PrecheckFailed = 10,
    ReadyToDeploy = 11,
    DeployQueued = 12,
    Deploying = 13,
    Deployed = 14,
    Retrying = 15
}
public enum MigrationJobStatus { Planned = 0, Running = 1, Completed = 2, Failed = 3, Cancelled = 4 }
public enum RewardStatus { Pending = 0, Approved = 1, Cancelled = 2, Reverted = 3 }

public enum VpnPanelStatus { New = 0, Active = 1, Disabled = 2, Maintenance = 3, Error = 4, Archived = 5 }
public enum VpnSslVerificationMode { Strict = 0, AllowSelfSigned = 1, Disabled = 2 }
public enum X3UiApiVariant { X3UiOfficial = 0, ThreeXUi = 1, LegacyXUi = 2, Custom = 3 }
public enum PanelSyncRunStatus { Pending = 0, Running = 1, Succeeded = 2, Failed = 3 }
