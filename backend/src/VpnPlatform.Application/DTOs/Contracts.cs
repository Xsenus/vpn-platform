using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.DTOs;

public sealed record TariffDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string FullDescription,
    IReadOnlyList<string> Features,
    string FeaturesJson,
    string Badge,
    int DurationDays,
    decimal Price,
    string Currency,
    int MaxDevices,
    long? TrafficLimit,
    bool IsTrial,
    bool IsActive,
    int SortOrder,
    DateTimeOffset? VisibleFrom,
    DateTimeOffset? VisibleTo,
    string TariffType,
    string Category,
    string AllowedRegionsCsv,
    string AllowedNodeGroupsCsv,
    bool IsReferralEligible,
    string ProvisioningScenario,
    string AfterPaymentText,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record FaqEntryDto(Guid Id, string Question, string Answer, string Category, bool IsActive, bool ShowOnHome, bool ShowOnFaqPage, int SortOrder, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record FaqEntryUpsertRequest(string Question, string Answer, string? Category, bool IsActive, bool ShowOnHome, bool ShowOnFaqPage, int SortOrder);

public sealed record SiteContentBlockDto(Guid Id, string Key, string Value, string Group, string Label, string Description, string InputType, bool IsActive, int SortOrder, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record SiteContentBlockUpsertRequest(string Key, string Value, string? Group, string? Label, string? Description, string? InputType, bool IsActive, int SortOrder);

public sealed record WorkScenarioDto(
    Guid Id,
    string Name,
    string Key,
    bool IsActive,
    string AllowedTariffIdsJson,
    string VpnProtocol,
    string ServerSelectionRule,
    string InboundSelectionRule,
    string ProvisioningMode,
    string OnPaymentSucceeded,
    string OnPaymentFailed,
    string OnRefund,
    string OnSubscriptionExpired,
    string OnRenewal,
    string CabinetText,
    string TelegramText,
    bool GenerateQrCode,
    int MaxDevices,
    long? TrafficLimit,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record WorkScenarioUpsertRequest(
    string Name,
    string Key,
    bool IsActive,
    string? AllowedTariffIdsJson,
    string? VpnProtocol,
    string? ServerSelectionRule,
    string? InboundSelectionRule,
    string? ProvisioningMode,
    string? OnPaymentSucceeded,
    string? OnPaymentFailed,
    string? OnRefund,
    string? OnSubscriptionExpired,
    string? OnRenewal,
    string? CabinetText,
    string? TelegramText,
    bool GenerateQrCode,
    int MaxDevices,
    long? TrafficLimit,
    int SortOrder);

public sealed record AppReleaseItemDto(Guid? Id, string Type, string Text, int SortOrder);
public sealed record AppReleaseDto(
    Guid Id,
    string ReleaseId,
    string Version,
    DateTimeOffset ReleasedAt,
    string Title,
    string Summary,
    bool IsActive,
    string Source,
    IReadOnlyList<AppReleaseItemDto> Items,
    Guid? CreatedByUserId,
    string CreatedByUserName,
    Guid? UpdatedByUserId,
    string UpdatedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AppVersionLatestResponse(string? CurrentVersion, AppReleaseDto? LatestRelease, bool SeenByCurrentUser);
public sealed record AppReleaseUpsertRequest(
    string ReleaseId,
    string Version,
    DateTimeOffset ReleasedAt,
    string Title,
    string Summary,
    bool IsActive,
    string? Source,
    IReadOnlyList<AppReleaseItemDto> Items);
public sealed record AppReleaseMarkSeenRequest(string ReleaseId);

public sealed record CreateCheckoutSessionCommand(Guid TariffId, OrderType Type, ChannelType Channel, PaymentProvider PaymentProvider, string? PromoCode, bool IsFirstPurchase, string? EmailHint, string? ReturnUrl);
public sealed record CheckoutSessionDto(Guid Id, string Token, Guid TariffId, Guid? UserId, Guid? OrderId, string Status, DateTimeOffset ExpiresAt, string? EmailHint);
public sealed record ClaimCheckoutSessionCommand(string Token, Guid UserId);

public sealed record CreateOrderCommand(Guid UserId, Guid TariffId, OrderType Type, ChannelType Channel, PaymentProvider PaymentProvider, string? PromoCode, bool IsFirstPurchase, Guid? CheckoutSessionId = null, Guid? RenewalSubscriptionId = null);
public sealed record OrderDto(Guid Id, Guid UserId, Guid TariffId, decimal Amount, string Currency, string Status, DateTimeOffset ExpiresAt, Guid? LinkedSubscriptionId = null);

public sealed record PaymentInitResult(string PaymentId, string RedirectUrl, string RawResponse);
public sealed record PaymentInitCommand(Guid OrderId, PaymentProvider Provider, string? ReturnUrl = null);
public sealed record PaymentCreateRequest(Order Order, PaymentAttempt Payment, PaymentProviderAccount Account, string ReturnUrl);
public sealed record PaymentWebhookParseResult(string ExternalEventId, string EventType, string PaymentId, PaymentStatus Status, string RawPayload, bool SignatureValidated, decimal? Amount = null, string? Currency = null, bool? Paid = null, string? ProviderAccountExternalId = null, string? InternalOrderId = null);
public sealed record PaymentWebhookVerificationResult(bool IsValid, string Method, string? Error);
public sealed record PaymentRefundResult(string RefundId, RefundStatus Status, string RawResponse);
public sealed record PaymentStatusResult(string PaymentId, PaymentStatus Status, string RawResponse, string? StatusReason = null);

public sealed record PaymentProviderAccountDto(
    Guid Id,
    PaymentProvider Provider,
    PaymentProviderMode Mode,
    string Name,
    string PublicName,
    bool IsEnabled,
    bool IsDefault,
    string ShopId,
    string ApiBaseUrl,
    string ReturnUrl,
    string WebhookUrl,
    bool HasSecretKey,
    bool HasWebhookSecret,
    bool UseWebhookIpAllowList,
    string AllowedWebhookIpRangesCsv,
    string ExtraSettingsJson,
    string HealthStatus,
    bool IsCheckoutConfigured,
    string? CheckoutConfigurationIssue,
    string CapabilitiesJson,
    IReadOnlyCollection<PaymentProviderCapabilityDto> Capabilities,
    IReadOnlyCollection<PaymentProviderRequiredFieldDto> RequiredFields,
    IReadOnlyCollection<string> ReadinessBlockers,
    bool IsPubliclyAvailable,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PaymentProviderCapabilityDto(
    string Key,
    string Label,
    bool Supported,
    string Status);

public sealed record PaymentProviderRequiredFieldDto(
    string Key,
    string Label,
    bool Required,
    bool Configured,
    string? Issue);

public sealed record UpsertPaymentProviderAccountCommand(
    PaymentProvider Provider,
    PaymentProviderMode Mode,
    string Name,
    string PublicName,
    bool IsEnabled,
    bool IsDefault,
    string ShopId,
    string ApiBaseUrl,
    string ReturnUrl,
    string WebhookUrl,
    string? SecretKey,
    string? WebhookSecret,
    bool UseWebhookIpAllowList,
    string AllowedWebhookIpRangesCsv,
    string ExtraSettingsJson);

public sealed record PaymentProviderAccountCheckResultDto(
    Guid AccountId,
    PaymentProvider Provider,
    PaymentProviderMode Mode,
    bool IsReady,
    string HealthStatus,
    string Message,
    IReadOnlyCollection<string> Details,
    DateTimeOffset CheckedAt,
    PaymentProviderAccountDto Account);


public sealed record AdminDashboardSummaryDto(
    int TotalUsers,
    int TelegramUsers,
    int ActiveSubscriptions,
    int ExpiringSubscriptions,
    int PaidOrders,
    int PendingOrders,
    int FailedPayments,
    int RecentPayments,
    int RecentOrders,
    int VpnAccessesCount,
    int VpnNodesCount,
    int HealthyVpnNodes,
    int VpnPanelsCount,
    int HealthyVpnPanels,
    int SupportConversationsCount,
    int OpenSupportConversations,
    int ProvisioningErrors,
    AdminProductionReadinessDto ProductionReadiness,
    DateTimeOffset GeneratedAt);

public sealed record AdminProductionReadinessDto(
    bool IsReady,
    string Status,
    IReadOnlyCollection<AdminProductionReadinessCheckDto> Checks);

public sealed record AdminProductionReadinessCheckDto(
    string Key,
    string Label,
    string Status,
    string Message,
    string Category,
    string Severity,
    string ActionLabel,
    string ActionHref);

public sealed record AdminTelegramBotSettingsDto(
    bool Enabled,
    string Mode,
    string PublicBotUsername,
    bool HasBotToken,
    string BotTokenMasked,
    string WebhookUrl,
    bool HasSecretToken,
    string AdminChatId,
    string WebAppUrl,
    string WelcomeText,
    string InstructionText,
    string SupportText,
    string AfterPaymentTextTemplate,
    string RenewalTextTemplate,
    string PaymentFailedTextTemplate,
    string SubscriptionExpiredTextTemplate,
    DateTimeOffset GeneratedAt);

public sealed record AdminTelegramBotConnectionCheckDto(
    bool IsReady,
    string Status,
    IReadOnlyCollection<string> RequiredActions,
    IReadOnlyCollection<string> Warnings,
    DateTimeOffset CheckedAt);

public sealed record UpdateTelegramBotSettingsCommand(
    bool? Enabled,
    string? Mode,
    string? PublicBotUsername,
    string? BotToken,
    string? WebhookUrl,
    string? SecretToken,
    string? AdminChatId,
    string? WebAppUrl,
    string? WelcomeText,
    string? InstructionText,
    string? SupportText,
    string? AfterPaymentTextTemplate,
    string? RenewalTextTemplate,
    string? PaymentFailedTextTemplate,
    string? SubscriptionExpiredTextTemplate);

public sealed record PaymentProviderSettingDto(Guid Id, Guid PaymentProviderAccountId, string Key, string? Value, bool IsSecret, string Description);
public sealed record PaymentWebhookEventDto(Guid Id, PaymentProvider Provider, Guid? PaymentAttemptId, Guid? PaymentProviderAccountId, string ProviderPaymentId, string ExternalEventId, string EventType, string Status, bool SignatureValidated, DateTimeOffset ReceivedAt, DateTimeOffset? ProcessedAt, string ErrorText);
public sealed record RefundDto(Guid Id, Guid PaymentAttemptId, PaymentProvider Provider, string ProviderRefundId, string Status, decimal Amount, string Currency, string Reason, DateTimeOffset CreatedAt, DateTimeOffset? RefundedAt);

public sealed record SubscriptionDto(
    Guid Id,
    Guid UserId,
    Guid TariffId,
    string Status,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? AccessUri,
    string? QrCodePath,
    string? ConfigPath,
    string? NodeName,
    string? TariffName = null,
    DateTimeOffset? GracePeriodEndAt = null,
    bool AutoRenewFlag = false,
    string? SourceChannel = null,
    Guid? CurrentServerId = null,
    Guid? CurrentAccessId = null,
    Guid? LastPaymentId = null,
    int RenewalCount = 0,
    string? BlockReason = null,
    DateTimeOffset? SuspendedAt = null,
    DateTimeOffset? CancelledAt = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null);
public sealed record ActivationResult(Guid SubscriptionId, Guid? AccessId, string ScenarioKey = "auto", string CabinetText = "", string TelegramText = "");

public sealed record VpnProvisionRequest(
    Guid SubscriptionId,
    Guid UserId,
    Guid TariffId,
    Guid NodeId,
    DateTimeOffset EndsAt,
    int MaxDevices,
    string Protocol = "vless",
    long? TrafficLimit = null,
    bool GenerateQrCode = true,
    string ScenarioKey = "auto",
    string InboundSelectionRule = "default",
    bool UseSandboxProvisioning = false);
public sealed record VpnProvisionResult(string ProviderAccessId, string AccessUri, string QrCodePath, string ConfigPath);
public sealed record VpnUsageSnapshot(string ProviderAccessId, long? UsedTrafficBytes, int? ActiveConnections, DateTimeOffset SyncedAt);
public sealed record AdminAccessActionResult(Guid Id, string Status, DateTimeOffset? DisabledAt, DateTimeOffset? LastSyncedAt, int Revision, long? UsedTrafficBytes = null, string? Message = null);
public sealed record AdminAccessCredentialHistoryDto(Guid Id, Guid AccessCredentialId, Guid SubscriptionId, string EventType, string OldValueJson, string NewValueJson, DateTimeOffset CreatedAt);
public sealed record QrCodeGenerationResult(string Payload, string? ImagePath, bool ImageGenerated, DateTimeOffset GeneratedAt);
public sealed record QrCodeImageResult(string Payload, string MediaType, string Content, DateTimeOffset GeneratedAt);


public sealed record TelegramStatusDto(bool IsLinked, long? TelegramUserId, string? Username, DateTimeOffset? LinkedAt);
public sealed record TelegramLinkTokenDto(string Token, string DeepLinkUrl, DateTimeOffset ExpiresAt);
public sealed record TelegramBotProcessResult(bool Processed, string ResponseText, long? ChatId = null, string? ReplyMarkupJson = null, string? PreCheckoutQueryId = null, bool? PreCheckoutOk = null, string? PreCheckoutError = null);
public sealed record CreateTelegramUserResultDto(Guid UserId, bool Created, string Email, string DisplayName);
public sealed record TelegramInvoiceRequest(Guid OrderId, Guid PaymentAttemptId, long TelegramUserId, string Title, string Description, string Payload, string Currency, int TotalAmountMinor);
public sealed record TelegramInvoiceResult(string Payload, string RawResponse);
public sealed record SupportConversationDto(Guid Id, Guid? UserId, long? TelegramUserId, string Channel, string Status, string Subject, Guid? AssignedToUserId, string InternalNote, DateTimeOffset? ClosedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record SupportMessageDto(Guid Id, Guid SupportConversationId, Guid? UserId, long? TelegramUserId, string Direction, string Text, string AttachmentsJson, bool IsInternalNote, DateTimeOffset CreatedAt);


public sealed record CreateVpnPanelCommand(
    string Name,
    string BaseUrl,
    string Login,
    string Password,
    string Region,
    int Capacity,
    string SslVerificationMode,
    string ApiVariant,
    bool AutoCreateInbound,
    string DefaultInboundTemplateJson);

public sealed record UpdateVpnPanelCommand(
    string? Name,
    string? BaseUrl,
    string? Login,
    string? Password,
    string? Region,
    int? Capacity,
    string? SslVerificationMode,
    string? ApiVariant,
    bool? AutoCreateInbound,
    string? DefaultInboundTemplateJson,
    string? Status);

public sealed record VpnPanelDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string Region,
    string Status,
    string HealthStatus,
    string Login,
    string SslVerificationMode,
    string ApiVariant,
    int Capacity,
    int UsedCapacity,
    bool AutoCreateInbound,
    string DefaultInboundTemplateJson,
    DateTimeOffset? LastHealthCheckAt,
    DateTimeOffset? LastSyncAt,
    string Version,
    string LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record VpnInboundDto(
    Guid Id,
    Guid VpnPanelId,
    string ExternalInboundId,
    string Name,
    string Protocol,
    int Port,
    string Listen,
    string SettingsJson,
    string StreamSettingsJson,
    string SniffingJson,
    bool IsDefault,
    bool IsActive,
    int Capacity,
    int UsedCapacity);

public sealed record CreateVpnInboundCommand(
    string Name,
    string Protocol,
    int Port,
    string Listen,
    string SettingsJson,
    string StreamSettingsJson,
    string SniffingJson,
    bool IsDefault,
    int Capacity);

public sealed record VpnClientDto(
    Guid Id,
    Guid UserId,
    Guid SubscriptionId,
    Guid VpnPanelId,
    Guid VpnInboundId,
    string ExternalClientId,
    string Email,
    string Uuid,
    string Flow,
    long? TotalGb,
    DateTimeOffset ExpiryTime,
    bool Enable,
    string ConfigUri,
    string QrCodePayload,
    string SyncStatus,
    DateTimeOffset? LastSyncedAt);

public sealed record PanelHealthCheckDto(Guid Id, Guid VpnPanelId, string Status, long? LatencyMs, string Version, string ErrorMessage, DateTimeOffset CheckedAt);
public sealed record PanelSyncRunDto(Guid Id, Guid VpnPanelId, string Status, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt, string SummaryJson, string ErrorMessage);
public sealed record PanelSyncEventDto(Guid Id, Guid PanelSyncRunId, string EventType, string EntityType, Guid? EntityId, string ExternalId, string Message, string PayloadJson);
public sealed record DeleteVpnPanelResultDto(Guid Id, bool Deleted, bool Archived, int LinkedInbounds, int LinkedClients, int LinkedSyncRuns, int LinkedHealthChecks);

public sealed record X3UiSession(string SessionCookie, DateTimeOffset CreatedAt);
public sealed record X3UiLoginRequest(string BaseUrl, string Username, string Password, VpnSslVerificationMode SslVerificationMode, X3UiApiVariant ApiVariant);
public sealed record X3UiHealthResult(bool IsHealthy, string Version, long LatencyMs, string? ErrorMessage = null);
public sealed record X3UiPanelVersionResult(string Version, string RawPayload);
public sealed record X3UiInboundDto(string Id, string Remark, string Protocol, int Port, string Listen, string SettingsJson, string StreamSettingsJson, string SniffingJson, bool Enable);
public sealed record X3UiClientDto(string Id, string Email, string Uuid, string Flow, int LimitIp, long? TotalGb, DateTimeOffset ExpiryTime, bool Enable, long? Up, long? Down);
public sealed record X3UiCreateInboundRequest(string Remark, string Protocol, int Port, string Listen, string SettingsJson, string StreamSettingsJson, string SniffingJson, bool Enable);
public sealed record X3UiUpdateInboundRequest(string Id, string Remark, string Protocol, int Port, string Listen, string SettingsJson, string StreamSettingsJson, string SniffingJson, bool Enable);
public sealed record X3UiAddClientRequest(string InboundId, string Email, string Uuid, string Flow, int LimitIp, long? TotalGb, DateTimeOffset ExpiryTime, bool Enable);
public sealed record X3UiUpdateClientRequest(string InboundId, string ClientId, string Email, string Uuid, string Flow, int LimitIp, long? TotalGb, DateTimeOffset ExpiryTime, bool Enable);
public sealed record X3UiTrafficSnapshot(string ClientId, long Up, long Down, DateTimeOffset SyncedAt);
