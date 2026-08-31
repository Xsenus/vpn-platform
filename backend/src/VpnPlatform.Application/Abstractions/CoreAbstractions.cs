using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IRuntimeEnvironment
{
    string EnvironmentName { get; }
}

public interface ITokenService
{
    string CreateAccessToken(User user, IEnumerable<string> roles);
    string CreateRefreshToken();
}

public interface IPasswordService
{
    string Hash(string input);
    bool Verify(string input, string hash);
}

public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
    string Mask(string? value, int visibleTail = 4);
}

public interface IPaymentProvider
{
    PaymentProvider Provider { get; }
    Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken);
    Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken);
    Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken);
    Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken);
}

public interface IPaymentApprovedOrderCaptureProvider
{
    bool RequiresCapture(PaymentWebhookParseResult webhook);
    Task<Result<PaymentStatusResult>> CaptureApprovedOrderAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken);
}

public interface IPaymentRefundStatusProvider
{
    Task<PaymentRefundResult> GetRefundStatusAsync(
        PaymentAttempt payment,
        PaymentProviderAccount account,
        Refund refund,
        CancellationToken cancellationToken);
}

public interface IPaymentProviderFactory
{
    IPaymentProvider Get(PaymentProvider provider);
}

public interface IPaymentWebhookVerifier
{
    PaymentProvider Provider { get; }
    Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken);
}

public interface IPaymentWebhookProcessor
{
    Task<Result<string>> ProcessAsync(PaymentProvider provider, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken);
}

public interface IPaymentStatusMapper
{
    PaymentProvider Provider { get; }
    PaymentStatus MapPaymentStatus(string providerStatus, bool paid);
    RefundStatus MapRefundStatus(string providerStatus);
}

public interface IVpnProvider
{
    string Name { get; }
    Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken);
    Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken);
    Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken);
    Task EnableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
    Task<VpnUsageSnapshot> SyncAccessAsync(string providerAccessId, CancellationToken cancellationToken) => GetUsageAsync(providerAccessId, cancellationToken);
    Task ResetTrafficAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
    Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken);
    Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken);
    Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken);
}

public interface IVpnProviderFactory
{
    IVpnProvider Get(string providerName);
}

public interface IQrCodeGenerator
{
    QrCodeGenerationResult GeneratePayload(string configUri, string purpose);
    QrCodeImageResult GenerateSvg(string configUri, string purpose);
}

public interface IProvisioningExecutor
{
    Task<ProvisioningExecutionResult> ExecuteAsync(VpnNode node, ProvisioningRun run, CancellationToken cancellationToken);
}

public interface IOutboxMessageSink
{
    Task DispatchAsync(Guid messageId, string type, string correlationId, string payloadJson, CancellationToken cancellationToken);
}

public sealed record EmailMessage(string ToAddress, string Subject, string Body);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}


public interface ITelegramInvoiceProvider
{
    Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken);
    Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken);
    Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken);
}

public interface IX3UiClient
{
    Task<X3UiSession> LoginAsync(VpnPanel panel, string password, CancellationToken cancellationToken);
    Task<X3UiHealthResult> CheckHealthAsync(VpnPanel panel, string password, CancellationToken cancellationToken);
    Task<X3UiPanelVersionResult> GetPanelVersionAsync(VpnPanel panel, string password, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<X3UiInboundDto>> GetInboundsAsync(VpnPanel panel, string password, CancellationToken cancellationToken);
    Task<X3UiInboundDto?> GetInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken);
    Task<X3UiInboundDto> CreateInboundAsync(VpnPanel panel, string password, X3UiCreateInboundRequest request, CancellationToken cancellationToken);
    Task DeleteInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken);
    Task<X3UiInboundDto> UpdateInboundAsync(VpnPanel panel, string password, X3UiUpdateInboundRequest request, CancellationToken cancellationToken);
    Task<X3UiClientDto> AddClientAsync(VpnPanel panel, string password, X3UiAddClientRequest request, CancellationToken cancellationToken);
    Task<X3UiClientDto> UpdateClientAsync(VpnPanel panel, string password, X3UiUpdateClientRequest request, CancellationToken cancellationToken);
    Task DeleteClientAsync(VpnPanel panel, string password, string inboundId, string clientId, string email, CancellationToken cancellationToken);
    Task EnableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, string email, CancellationToken cancellationToken);
    Task DisableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, string email, CancellationToken cancellationToken);
    Task ResetClientTrafficAsync(VpnPanel panel, string password, string inboundId, string email, CancellationToken cancellationToken);
    Task<X3UiTrafficSnapshot> GetClientTrafficAsync(VpnPanel panel, string password, string email, CancellationToken cancellationToken);
}
