using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class PaymentProviderAccountService
{
    private readonly IApplicationDbContext _db;
    private readonly ISecretProtector _secretProtector;
    private readonly IClock _clock;

    public PaymentProviderAccountService(IApplicationDbContext db, ISecretProtector secretProtector, IClock clock)
    {
        _db = db;
        _secretProtector = secretProtector;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<PaymentProviderAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _db.PaymentProviderAccounts
            .AsNoTracking()
            .OrderBy(x => x.Provider)
            .ThenByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return accounts.Select(MapToDto).ToList();
    }

    public async Task<Result<PaymentProviderAccountDto>> GetAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _db.PaymentProviderAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return account is null
            ? Result<PaymentProviderAccountDto>.Failure("Payment provider account not found.")
            : Result<PaymentProviderAccountDto>.Success(MapToDto(account));
    }

    public async Task<Result<PaymentProviderAccount>> GetEnabledAccountEntityAsync(PaymentProvider provider, CancellationToken cancellationToken = default)
    {
        var account = await _db.PaymentProviderAccounts
            .Where(x => x.Provider == provider && x.IsEnabled && x.Mode != PaymentProviderMode.Disabled)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return account is null
            ? Result<PaymentProviderAccount>.Failure($"Payment provider {provider} is not configured or disabled.")
            : Result<PaymentProviderAccount>.Success(account);
    }

    public async Task<Result<PaymentProviderAccountDto>> UpsertAsync(Guid? id, UpsertPaymentProviderAccountCommand command, CancellationToken cancellationToken = default)
    {
        var shopId = Normalize(command.ShopId);
        var apiBaseUrl = Normalize(command.ApiBaseUrl);
        var returnUrl = Normalize(command.ReturnUrl);
        var allowedWebhookIpRangesCsv = Normalize(command.AllowedWebhookIpRangesCsv);
        var replaceExtraSettings = !id.HasValue || !string.IsNullOrWhiteSpace(command.ExtraSettingsJson);
        var extraSettingsJson = string.IsNullOrWhiteSpace(command.ExtraSettingsJson) ? "{}" : command.ExtraSettingsJson.Trim();

        if (command.Mode == PaymentProviderMode.Production && string.IsNullOrWhiteSpace(shopId))
        {
            return Result<PaymentProviderAccountDto>.Failure("ShopId is required for production provider account.");
        }

        PaymentProviderAccount account;
        if (id.HasValue)
        {
            var existing = await _db.PaymentProviderAccounts.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
            if (existing is null)
            {
                return Result<PaymentProviderAccountDto>.Failure("Payment provider account not found.");
            }

            account = existing;
        }
        else
        {
            account = new PaymentProviderAccount { Provider = command.Provider, CreatedAt = _clock.UtcNow };
            _db.PaymentProviderAccounts.Add(account);

            if (command.Mode == PaymentProviderMode.Production && string.IsNullOrWhiteSpace(command.SecretKey))
            {
                return Result<PaymentProviderAccountDto>.Failure("SecretKey is required when creating a production provider account.");
            }
        }

        account.Provider = command.Provider;
        account.Mode = command.Mode;
        account.Name = string.IsNullOrWhiteSpace(command.Name) ? command.Provider.ToString() : command.Name.Trim();
        account.PublicName = string.IsNullOrWhiteSpace(command.PublicName) ? account.Name : command.PublicName.Trim();
        account.IsEnabled = command.IsEnabled;
        account.IsDefault = command.IsDefault;
        account.ShopId = shopId;
        account.ApiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? DefaultApiBaseUrl(command.Provider) : apiBaseUrl.TrimEnd('/');
        account.ReturnUrl = returnUrl;
        account.UseWebhookIpAllowList = command.UseWebhookIpAllowList;
        account.AllowedWebhookIpRangesCsv = allowedWebhookIpRangesCsv;
        if (replaceExtraSettings)
        {
            var extraSettingsValidationError = ValidateExtraSettingsJson(extraSettingsJson);
            if (!string.IsNullOrWhiteSpace(extraSettingsValidationError))
            {
                return Result<PaymentProviderAccountDto>.Failure(extraSettingsValidationError);
            }

            account.ExtraSettingsJson = extraSettingsJson;
        }
        account.UpdatedAt = _clock.UtcNow;

        if (!string.IsNullOrWhiteSpace(command.SecretKey))
        {
            account.SecretKeyProtected = _secretProtector.Protect(command.SecretKey.Trim());
        }

        if (!string.IsNullOrWhiteSpace(command.WebhookSecret))
        {
            account.WebhookSecretProtected = _secretProtector.Protect(command.WebhookSecret.Trim());
        }

        var credentialValidationError = ValidateProviderCredentials(account);
        if (!string.IsNullOrWhiteSpace(credentialValidationError))
        {
            return Result<PaymentProviderAccountDto>.Failure(credentialValidationError);
        }

        if (account.IsDefault)
        {
            var others = await _db.PaymentProviderAccounts
                .Where(x => x.Provider == account.Provider && x.Id != account.Id)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
            {
                other.IsDefault = false;
                other.UpdatedAt = _clock.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<PaymentProviderAccountDto>.Success(MapToDto(account));
    }

    public async Task<Result<PaymentProviderAccountDto>> SetEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken = default)
    {
        var account = await _db.PaymentProviderAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account is null)
        {
            return Result<PaymentProviderAccountDto>.Failure("Payment provider account not found.");
        }

        account.IsEnabled = enabled;
        account.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PaymentProviderAccountDto>.Success(MapToDto(account));
    }

    public string GetSecretKey(PaymentProviderAccount account)
        => _secretProtector.Unprotect(account.SecretKeyProtected);

    public string GetWebhookSecret(PaymentProviderAccount account)
        => string.IsNullOrWhiteSpace(account.WebhookSecretProtected) ? string.Empty : _secretProtector.Unprotect(account.WebhookSecretProtected);

    public static PaymentProviderAccountDto MapToDto(PaymentProviderAccount account)
        => new(
            account.Id,
            account.Provider,
            account.Mode,
            account.Name,
            account.PublicName,
            account.IsEnabled,
            account.IsDefault,
            account.ShopId,
            account.ApiBaseUrl,
            account.ReturnUrl,
            !string.IsNullOrWhiteSpace(account.SecretKeyProtected),
            !string.IsNullOrWhiteSpace(account.WebhookSecretProtected),
            account.UseWebhookIpAllowList,
            account.AllowedWebhookIpRangesCsv,
            MaskExtraSettings(account.ExtraSettingsJson),
            account.HealthStatus.ToString(),
            PaymentProviderConfigurationRules.IsCheckoutConfigured(account),
            PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account),
            PaymentProviderConfigurationRules.GetCapabilitiesJson(account.Provider),
            account.CreatedAt,
            account.UpdatedAt);


    private static string MaskExtraSettings(string extraSettingsJson)
    {
        if (string.IsNullOrWhiteSpace(extraSettingsJson) || extraSettingsJson.Trim() == "{}")
        {
            return "{}";
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(extraSettingsJson);
            var result = new Dictionary<string, object?>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var key = property.Name;
                var lower = key.ToLowerInvariant();
                if (lower.Contains("secret", StringComparison.Ordinal) || lower.Contains("password", StringComparison.Ordinal) || lower.Contains("token", StringComparison.Ordinal) || lower.Contains("key", StringComparison.Ordinal))
                {
                    result[key] = "***";
                    continue;
                }

                result[key] = property.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => property.Value.GetString(),
                    System.Text.Json.JsonValueKind.Number => property.Value.GetRawText(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => property.Value.GetRawText()
                };
            }

            return System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch
        {
            return "{}";
        }
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static string? ValidateExtraSettingsJson(string extraSettingsJson)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(extraSettingsJson);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                ? null
                : "ExtraSettingsJson must be a JSON object.";
        }
        catch (System.Text.Json.JsonException)
        {
            return "ExtraSettingsJson must be a valid JSON object.";
        }
    }

    private static string? ValidateProviderCredentials(PaymentProviderAccount account)
    {
        if (account.Mode is PaymentProviderMode.Disabled || !account.IsEnabled)
        {
            return null;
        }

        if (account.Mode == PaymentProviderMode.Production && string.IsNullOrWhiteSpace(account.ShopId))
        {
            return "Production provider account requires ShopId/MerchantLogin/Receiver.";
        }

        if (account.Mode == PaymentProviderMode.Production && string.IsNullOrWhiteSpace(account.SecretKeyProtected))
        {
            return $"Production {account.Provider} provider account requires an encrypted SecretKey.";
        }

        if (account.Provider is PaymentProvider.RoboKassa or PaymentProvider.YooMoney or PaymentProvider.Stripe or PaymentProvider.PayPal or PaymentProvider.Prodamus
            && account.Mode == PaymentProviderMode.Production
            && string.IsNullOrWhiteSpace(account.WebhookSecretProtected))
        {
            return $"Production {account.Provider} provider account requires an encrypted WebhookSecret for notification verification.";
        }

        return null;
    }

    private static string DefaultApiBaseUrl(PaymentProvider provider)
        => provider switch
        {
            PaymentProvider.YooKassa => "https://api.yookassa.ru/v3",
            PaymentProvider.RoboKassa => "https://auth.robokassa.ru/Merchant/Index.aspx",
            PaymentProvider.YooMoney => "https://yoomoney.ru/quickpay/confirm",
            PaymentProvider.TBankAcquiring => "https://securepay.tinkoff.ru",
            PaymentProvider.Stripe => "https://api.stripe.com",
            PaymentProvider.PayPal => "https://api-m.paypal.com",
            PaymentProvider.Prodamus => "https://demo.payform.ru",
            _ => string.Empty
        };
}
