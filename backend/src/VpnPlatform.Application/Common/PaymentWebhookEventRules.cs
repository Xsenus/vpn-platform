using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Common;

public static class PaymentWebhookEventRules
{
    public static readonly TimeSpan ClaimTimeout = TimeSpan.FromMinutes(10);

    public static bool IsTerminal(PaymentWebhookEventStatus status)
        => status is PaymentWebhookEventStatus.Processed
            or PaymentWebhookEventStatus.Rejected
            or PaymentWebhookEventStatus.Duplicate;

    public static bool IsRetryable(PaymentWebhookEventStatus status, DateTimeOffset receivedAt, DateTimeOffset now)
        => status == PaymentWebhookEventStatus.Failed
            || (status is PaymentWebhookEventStatus.Received or PaymentWebhookEventStatus.Verified
                && receivedAt <= now - ClaimTimeout);

    public static bool RequiresAttention(PaymentWebhookEventStatus status, DateTimeOffset receivedAt, DateTimeOffset now)
        => status is PaymentWebhookEventStatus.Failed or PaymentWebhookEventStatus.Rejected
            || (status is PaymentWebhookEventStatus.Received or PaymentWebhookEventStatus.Verified
                && receivedAt <= now - ClaimTimeout);
}
