using System.Text.Json.Serialization;
using VpnPlatform.Domain.Common;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Domain.Entities;

public class User : AuditableEntity
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string RolesCsv { get; set; } = "User";
    public UserStatus Status { get; set; } = UserStatus.New;
    public bool IsBlocked { get; set; }
    public int SessionVersion { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public string PreferredLanguage { get; set; } = "ru";
    public string ReferralCode { get; set; } = string.Empty;
    public Guid? ReferredByUserId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public AuthSource AuthSource { get; set; } = AuthSource.Local;
    public DateTimeOffset? TelegramRegistrationCompletedAt { get; set; }
    public bool EmailConfirmed { get; set; }

    [JsonIgnore]
    public ICollection<ChannelProfile> ChannelProfiles { get; set; } = new List<ChannelProfile>();
    [JsonIgnore]
    public ICollection<TelegramAccount> TelegramAccounts { get; set; } = new List<TelegramAccount>();
    [JsonIgnore]
    public ICollection<SupportConversation> SupportConversations { get; set; } = new List<SupportConversation>();
    [JsonIgnore]
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    [JsonIgnore]
    public ICollection<VpnClient> VpnClients { get; set; } = new List<VpnClient>();
}

public class UserRefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public int SessionVersion { get; set; }
    public Guid? FamilyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string ReplacedByTokenHash { get; set; } = string.Empty;
    public DateTimeOffset? ReuseDetectedAt { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public string RevokedByIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string RevocationReason { get; set; } = string.Empty;

    [JsonIgnore]
    public User? User { get; set; }
}

public class PasswordResetToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public int Generation { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
    public string InvalidationReason { get; set; } = string.Empty;
    public int Revision { get; set; }
    public string RequestedByIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    public User? User { get; set; }
}

public class PasswordResetState : AuditableEntity
{
    public Guid UserId { get; set; }
    public int Generation { get; set; }
    public int Revision { get; set; }

    public User? User { get; set; }
}

public class ChannelProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public ChannelType ProviderType { get; set; }
    public string ExternalUserId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? ChatId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}

public class Tariff : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string FeaturesJson { get; set; } = "[]";
    public string Badge { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "RUB";
    public int MaxDevices { get; set; } = 3;
    public long? TrafficLimit { get; set; }
    public bool IsTrial { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset? VisibleFrom { get; set; }
    public DateTimeOffset? VisibleTo { get; set; }
    public TariffType TariffType { get; set; } = TariffType.Monthly;
    public string Category { get; set; } = "default";
    public string AllowedRegionsCsv { get; set; } = string.Empty;
    public string AllowedNodeGroupsCsv { get; set; } = string.Empty;
    public bool IsReferralEligible { get; set; } = true;
    public string ProvisioningScenario { get; set; } = "auto";
    public string AfterPaymentText { get; set; } = string.Empty;
}

public class PromoCode : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "percent";
    public decimal DiscountValue { get; set; }
    public int FreeDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxPerUser { get; set; }
    public string AllowedTariffIdsJson { get; set; } = "[]";
    public string AllowedChannelsJson { get; set; } = "[]";
    public bool AllowStackWithReferral { get; set; }
}

public class CheckoutSession : AuditableEntity
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid TariffId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrderId { get; set; }
    public OrderType Type { get; set; } = OrderType.NewSubscription;
    public ChannelType Channel { get; set; } = ChannelType.Web;
    public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.YooKassa;
    public string? PromoCode { get; set; }
    public bool IsFirstPurchase { get; set; }
    public string? EmailHint { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Status { get; set; } = "open";
    public string MetadataJson { get; set; } = "{}";

    public Tariff? Tariff { get; set; }
    public User? User { get; set; }
    public Order? Order { get; set; }
}

public class Order : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid TariffId { get; set; }
    public Guid? CheckoutSessionId { get; set; }
    public OrderType Type { get; set; } = OrderType.NewSubscription;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public ChannelType Channel { get; set; } = ChannelType.Web;
    public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.YooKassa;
    public Guid? PromoCodeId { get; set; }
    public string ReferralContext { get; set; } = "{}";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public bool IsFirstPurchase { get; set; }

    public User? User { get; set; }
    public Tariff? Tariff { get; set; }
    public CheckoutSession? CheckoutSession { get; set; }
    public ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();
}

public class PaymentProviderAccount : AuditableEntity
{
    public PaymentProvider Provider { get; set; }
    public PaymentProviderMode Mode { get; set; } = PaymentProviderMode.Disabled;
    public string Name { get; set; } = string.Empty;
    public string PublicName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsDefault { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string SecretKeyProtected { get; set; } = string.Empty;
    public string WebhookSecretProtected { get; set; } = string.Empty;
    public bool UseWebhookIpAllowList { get; set; } = true;
    public string AllowedWebhookIpRangesCsv { get; set; } = string.Empty;
    public string ExtraSettingsJson { get; set; } = "{}";
    public DateTimeOffset? LastHealthCheckAt { get; set; }
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Unknown;

    public ICollection<PaymentProviderSetting> Settings { get; set; } = new List<PaymentProviderSetting>();
}

public class PaymentProviderSetting : AuditableEntity
{
    public Guid PaymentProviderAccountId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public string Description { get; set; } = string.Empty;

    public PaymentProviderAccount? PaymentProviderAccount { get; set; }
}

public class PaymentAttempt : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Guid? PaymentProviderAccountId { get; set; }
    public PaymentProvider Provider { get; set; }
    public PaymentProviderMode ProviderMode { get; set; } = PaymentProviderMode.Disabled;
    public string ProviderPaymentId { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public PaymentStatus Status { get; set; } = PaymentStatus.New;
    public string ConfirmationUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string RawRequest { get; set; } = "{}";
    public string RawResponse { get; set; } = "{}";
    public string WebhookPayload { get; set; } = "{}";
    public bool SignatureValidated { get; set; }
    public bool IsActivationProcessed { get; set; }
    public DateTimeOffset? ActivationProcessedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
    public decimal RefundedAmount { get; set; }
    public string StatusReason { get; set; } = string.Empty;

    public Order? Order { get; set; }
    public PaymentProviderAccount? PaymentProviderAccount { get; set; }
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public ICollection<PaymentReceipt> Receipts { get; set; } = new List<PaymentReceipt>();
}

public class PaymentWebhookEvent : AuditableEntity
{
    public PaymentProvider Provider { get; set; }
    public Guid? PaymentAttemptId { get; set; }
    public Guid? PaymentProviderAccountId { get; set; }
    public string ProviderPaymentId { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadSha256 { get; set; } = string.Empty;
    public string RawPayload { get; set; } = "{}";
    public string HeadersJson { get; set; } = "{}";
    public bool SignatureValidated { get; set; }
    public PaymentWebhookEventStatus Status { get; set; } = PaymentWebhookEventStatus.Received;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string ErrorText { get; set; } = string.Empty;

    public PaymentAttempt? PaymentAttempt { get; set; }
    public PaymentProviderAccount? PaymentProviderAccount { get; set; }
}

public class Refund : AuditableEntity
{
    public Guid PaymentAttemptId { get; set; }
    public PaymentProvider Provider { get; set; }
    public string ProviderRefundId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.New;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string Reason { get; set; } = string.Empty;
    public string RawRequest { get; set; } = "{}";
    public string RawResponse { get; set; } = "{}";
    public DateTimeOffset? RefundedAt { get; set; }

    public PaymentAttempt? PaymentAttempt { get; set; }
}

public class PaymentReceipt : AuditableEntity
{
    public Guid PaymentAttemptId { get; set; }
    public PaymentProvider Provider { get; set; }
    public string ProviderReceiptId { get; set; } = string.Empty;
    public string Type { get; set; } = "payment";
    public PaymentReceiptStatus Status { get; set; } = PaymentReceiptStatus.Pending;
    public string FiscalDocumentNumber { get; set; } = string.Empty;
    public string RawPayload { get; set; } = "{}";

    public PaymentAttempt? PaymentAttempt { get; set; }
}



public class TelegramAccount : AuditableEntity
{
    public Guid? UserId { get; set; }
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public DateTimeOffset? LinkedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset? RegistrationCompletedAt { get; set; }

    public User? User { get; set; }
    [JsonIgnore]
    public ICollection<TelegramBotMessage> Messages { get; set; } = new List<TelegramBotMessage>();
}

public class TelegramBotUpdate : AuditableEntity
{
    public long UpdateId { get; set; }
    public long? TelegramUserId { get; set; }
    public string UpdateType { get; set; } = string.Empty;
    public string RawPayload { get; set; } = "{}";
    public string PayloadSha256 { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string ErrorText { get; set; } = string.Empty;
    public long? ResponseChatId { get; set; }
    public string ResponseText { get; set; } = string.Empty;
    public string ResponseReplyMarkupJson { get; set; } = string.Empty;
    public DateTimeOffset? ResponseSentAt { get; set; }
    public string PreCheckoutQueryId { get; set; } = string.Empty;
    public bool? PreCheckoutOk { get; set; }
    public string PreCheckoutError { get; set; } = string.Empty;
    public DateTimeOffset? PreCheckoutAnsweredAt { get; set; }
    public DateTimeOffset? DeliveryClaimedAt { get; set; }
    public DateTimeOffset? DeliveryNextAttemptAt { get; set; }
    public int DeliveryAttemptCount { get; set; }
    public string DeliveryErrorText { get; set; } = string.Empty;
}

public class TelegramBotSession : AuditableEntity
{
    public long TelegramUserId { get; set; }
    public string CurrentState { get; set; } = "idle";
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset ExpiresAt { get; set; }
}

public class TelegramBotCommandLog : AuditableEntity
{
    public long TelegramUserId { get; set; }
    public long? UpdateId { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string ResultStatus { get; set; } = string.Empty;
}

public class TelegramBotMessage : AuditableEntity
{
    public Guid? TelegramAccountId { get; set; }
    public long TelegramUserId { get; set; }
    public long ChatId { get; set; }
    public long? MessageId { get; set; }
    public string Direction { get; set; } = "inbound";
    public string Text { get; set; } = string.Empty;
    public string RawPayload { get; set; } = "{}";

    [JsonIgnore]
    public TelegramAccount? TelegramAccount { get; set; }
}

public class TelegramBotCallbackQuery : AuditableEntity
{
    public string CallbackQueryId { get; set; } = string.Empty;
    public long TelegramUserId { get; set; }
    public string Data { get; set; } = string.Empty;
    public string RawPayload { get; set; } = "{}";
    public bool IsProcessed { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public class TelegramBotPayment : AuditableEntity
{
    public Guid? PaymentAttemptId { get; set; }
    public long TelegramUserId { get; set; }
    public string ProviderPaymentChargeId { get; set; } = string.Empty;
    public string TelegramPaymentChargeId { get; set; } = string.Empty;
    public string InvoicePayload { get; set; } = string.Empty;
    public long TotalAmount { get; set; }
    public string Currency { get; set; } = "XTR";
    public string RawPayload { get; set; } = "{}";

    public PaymentAttempt? PaymentAttempt { get; set; }
}

public class TelegramBotDeepLink : AuditableEntity
{
    public Guid? UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = "link_account";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public long? UsedByTelegramUserId { get; set; }
    public string MetadataJson { get; set; } = "{}";

    public User? User { get; set; }
}

public class TelegramBotNotification : AuditableEntity
{
    public long TelegramUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string DeduplicationKey { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string ErrorText { get; set; } = string.Empty;
}

public class SupportConversation : AuditableEntity
{
    public Guid? UserId { get; set; }
    public long? TelegramUserId { get; set; }
    public string Channel { get; set; } = "telegram";
    public string Status { get; set; } = "open";
    public Guid? AssignedToUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string InternalNote { get; set; } = string.Empty;
    public DateTimeOffset? ClosedAt { get; set; }

    public User? User { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
}

public class SupportMessage : AuditableEntity
{
    public Guid SupportConversationId { get; set; }
    public Guid? UserId { get; set; }
    public long? TelegramUserId { get; set; }
    public string Direction { get; set; } = "inbound";
    public string Text { get; set; } = string.Empty;
    public string RawPayload { get; set; } = "{}";
    public string AttachmentsJson { get; set; } = "[]";
    public bool IsInternalNote { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    public SupportConversation? SupportConversation { get; set; }
}


public class VpnPanel : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public VpnPanelStatus Status { get; set; } = VpnPanelStatus.New;
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Unknown;
    public string Login { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public VpnSslVerificationMode SslVerificationMode { get; set; } = VpnSslVerificationMode.Strict;
    public X3UiApiVariant ApiVariant { get; set; } = X3UiApiVariant.X3UiOfficial;
    public int Capacity { get; set; } = 5000;
    public int UsedCapacity { get; set; }
    public bool AutoCreateInbound { get; set; }
    public string DefaultInboundTemplateJson { get; set; } = "{}";
    public DateTimeOffset? LastHealthCheckAt { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public string LastError { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public ICollection<VpnInbound> Inbounds { get; set; } = new List<VpnInbound>();
    public ICollection<VpnClient> Clients { get; set; } = new List<VpnClient>();
    public ICollection<PanelSyncRun> SyncRuns { get; set; } = new List<PanelSyncRun>();
    public ICollection<PanelHealthCheck> HealthChecks { get; set; } = new List<PanelHealthCheck>();
}

public class VpnInbound : AuditableEntity
{
    public Guid VpnPanelId { get; set; }
    public string ExternalInboundId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Protocol { get; set; } = "vless";
    public int Port { get; set; }
    public string Listen { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public string StreamSettingsJson { get; set; } = "{}";
    public string SniffingJson { get; set; } = "{}";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int Capacity { get; set; } = 5000;
    public int UsedCapacity { get; set; }

    public VpnPanel? VpnPanel { get; set; }
    public ICollection<VpnClient> Clients { get; set; } = new List<VpnClient>();
}

public class VpnClient : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid VpnPanelId { get; set; }
    public Guid VpnInboundId { get; set; }
    public string ExternalClientId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public string Flow { get; set; } = string.Empty;
    public int LimitIp { get; set; }
    public long? TotalGb { get; set; }
    public DateTimeOffset ExpiryTime { get; set; }
    public bool Enable { get; set; } = true;
    public string ConfigUri { get; set; } = string.Empty;
    public string QrCodePayload { get; set; } = string.Empty;
    public DateTimeOffset? LastSyncedAt { get; set; }
    public string SyncStatus { get; set; } = "synced";

    [JsonIgnore]
    public User? User { get; set; }
    [JsonIgnore]
    public Subscription? Subscription { get; set; }
    [JsonIgnore]
    public VpnPanel? VpnPanel { get; set; }
    [JsonIgnore]
    public VpnInbound? VpnInbound { get; set; }
}

public class PanelSyncRun : AuditableEntity
{
    public Guid VpnPanelId { get; set; }
    public PanelSyncRunStatus Status { get; set; } = PanelSyncRunStatus.Pending;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string SummaryJson { get; set; } = "{}";
    public string ErrorMessage { get; set; } = string.Empty;

    public VpnPanel? VpnPanel { get; set; }
    public ICollection<PanelSyncEvent> Events { get; set; } = new List<PanelSyncEvent>();
}

public class PanelSyncEvent : AuditableEntity
{
    public Guid PanelSyncRunId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";

    public PanelSyncRun? PanelSyncRun { get; set; }
}

public class PanelHealthCheck : AuditableEntity
{
    public Guid VpnPanelId { get; set; }
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;
    public long? LatencyMs { get; set; }
    public string Version { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTimeOffset CheckedAt { get; set; }

    public VpnPanel? VpnPanel { get; set; }
}

public class AccessCredentialHistory : AuditableEntity
{
    public Guid AccessCredentialId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string OldValueJson { get; set; } = "{}";
    public string NewValueJson { get; set; } = "{}";

    [JsonIgnore]
    public AccessCredential? AccessCredential { get; set; }
    [JsonIgnore]
    public Subscription? Subscription { get; set; }
}

public class Subscription : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid TariffId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.PendingActivation;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public DateTimeOffset? GracePeriodEndAt { get; set; }
    public bool AutoRenewFlag { get; set; }
    public ChannelType SourceChannel { get; set; } = ChannelType.Web;
    public Guid? CurrentServerId { get; set; }
    public Guid? CurrentAccessId { get; set; }
    public Guid? LastPaymentId { get; set; }
    public int RenewalCount { get; set; }
    public string? BlockReason { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public int LifecycleAttemptCount { get; set; }
    public DateTimeOffset? LifecycleProcessingStartedAt { get; set; }
    public DateTimeOffset? LifecycleLeaseExpiresAt { get; set; }
    public DateTimeOffset? LifecycleNextAttemptAt { get; set; }
    public string? LifecycleLastError { get; set; }

    [JsonIgnore]
    public User? User { get; set; }
    public Tariff? Tariff { get; set; }
    [JsonIgnore]
    public AccessCredential? CurrentAccess { get; set; }
    [JsonIgnore]
    public VpnNode? CurrentServer { get; set; }
    [JsonIgnore]
    public VpnClient? VpnClient { get; set; }
}

public class AccessCredential : AuditableEntity
{
    public Guid SubscriptionId { get; set; }
    public string ProviderType { get; set; } = "x3ui";
    public string ProviderAccessId { get; set; } = string.Empty;
    public Guid ServerId { get; set; }
    public string AccessUri { get; set; } = string.Empty;
    public string QrCodePath { get; set; } = string.Empty;
    public string ConfigPath { get; set; } = string.Empty;
    public AccessCredentialStatus Status { get; set; } = AccessCredentialStatus.Provisioning;
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DisabledAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public int Revision { get; set; } = 1;

    [JsonIgnore]
    public Subscription? Subscription { get; set; }
    [JsonIgnore]
    public VpnNode? Server { get; set; }
    [JsonIgnore]
    public ICollection<AccessCredentialHistory> History { get; set; } = new List<AccessCredentialHistory>();
}

public class NodeGroup : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string AllocationStrategy { get; set; } = "least-loaded";

    public ICollection<VpnNode> Nodes { get; set; } = new List<VpnNode>();
}

public class VpnNode : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Datacenter { get; set; } = string.Empty;
    public NodeStatus Status { get; set; } = NodeStatus.New;
    public int Capacity { get; set; } = 5000;
    public int UsedCapacity { get; set; }
    public string SupportedProtocolsCsv { get; set; } = "vless,vmess,trojan";
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Unknown;
    public DateTimeOffset? LastHealthCheckAt { get; set; }
    public ProvisioningRunStatus ProvisioningStatus { get; set; } = ProvisioningRunStatus.Pending;
    public string InstalledVersion { get; set; } = string.Empty;
    public string BackupStatus { get; set; } = "unknown";
    public string MonitoringStatus { get; set; } = "unknown";
    public string LoggingStatus { get; set; } = "unknown";
    public string TagsCsv { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public bool IsAvailableForNewUsers { get; set; } = true;
    public int SshPort { get; set; } = 22;
    public string SshUser { get; set; } = "root";
    // Legacy path field. New credentials must use ProtectedSshCredential; do not expose this via API.
    public string SshPrivateKeyPath { get; set; } = string.Empty;
    public string ProtectedSshCredential { get; set; } = string.Empty;
    public string SshCredentialRef { get; set; } = string.Empty;
    public bool SkipHostKeyChecking { get; set; } = true;
    public string PanelBaseUrl { get; set; } = string.Empty;
    public string PanelUsername { get; set; } = "admin";
    // Legacy plaintext field retained for backward compatibility only. New secrets must use ProtectedPanelPassword.
    public string PanelPassword { get; set; } = string.Empty;
    public string ProtectedPanelPassword { get; set; } = string.Empty;
    public string PanelSecretRef { get; set; } = string.Empty;
    public int? PanelInboundId { get; set; }
    public string PublicHostname { get; set; } = string.Empty;
    public int PublicPort { get; set; } = 443;
    public Guid? NodeGroupId { get; set; }

    public NodeGroup? NodeGroup { get; set; }
}
