using System.Data.Common;
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

    public async Task<Result<PaymentProviderAccount>> GetWebCheckoutAccountEntityAsync(PaymentProvider provider, CancellationToken cancellationToken = default)
    {
        var candidates = await _db.PaymentProviderAccounts
            .Where(x => x.Provider == provider && x.IsEnabled && x.Mode != PaymentProviderMode.Disabled)
            .ToListAsync(cancellationToken);

        var account = PaymentProviderConfigurationRules.SelectWebCheckoutAccount(candidates, provider);

        return account is null
            ? Result<PaymentProviderAccount>.Failure($"Payment provider {provider} is not configured for web checkout or disabled.")
            : Result<PaymentProviderAccount>.Success(account);
    }

    public async Task<Result<PaymentProviderAccountDto>> UpsertAsync(Guid? id, UpsertPaymentProviderAccountCommand command, CancellationToken cancellationToken = default)
    {
        await using var accountGate = id.HasValue
            ? await PaymentProcessingGate.AcquirePaymentProviderAccountAsync(id.Value, cancellationToken)
            : null;
        await using var providerGate = await PaymentProcessingGate.AcquirePaymentProviderConfigurationAsync(command.Provider, cancellationToken);
        var shopId = Normalize(command.ShopId);
        var apiBaseUrl = Normalize(command.ApiBaseUrl);
        var returnUrl = Normalize(command.ReturnUrl);
        var webhookUrl = Normalize(command.WebhookUrl);
        var allowedWebhookIpRangesCsv = Normalize(command.AllowedWebhookIpRangesCsv);
        var replaceExtraSettings = !id.HasValue || !string.IsNullOrWhiteSpace(command.ExtraSettingsJson);
        var extraSettingsJson = string.IsNullOrWhiteSpace(command.ExtraSettingsJson) ? "{}" : command.ExtraSettingsJson.Trim();

        if (command.Mode == PaymentProviderMode.Production && string.IsNullOrWhiteSpace(shopId))
        {
            return Result<PaymentProviderAccountDto>.Failure("ShopId is required for production provider account.");
        }

        if (replaceExtraSettings)
        {
            var extraSettingsValidationError = ValidateExtraSettingsJson(extraSettingsJson);
            if (!string.IsNullOrWhiteSpace(extraSettingsValidationError))
            {
                return Result<PaymentProviderAccountDto>.Failure(extraSettingsValidationError);
            }
        }

        PaymentProviderAccount? existing = null;
        if (id.HasValue)
        {
            existing = await _db.PaymentProviderAccounts.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
            if (existing is null)
            {
                return Result<PaymentProviderAccountDto>.Failure("Payment provider account not found.");
            }

        }
        else if (command.Provider != PaymentProvider.TelegramStars
                 && command.Mode == PaymentProviderMode.Production
                 && string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return Result<PaymentProviderAccountDto>.Failure("SecretKey is required when creating a production provider account.");
        }

        var name = string.IsNullOrWhiteSpace(command.Name) ? command.Provider.ToString() : command.Name.Trim();
        if (await _db.PaymentProviderAccounts.AnyAsync(
                x => x.Id != (id ?? Guid.Empty)
                     && x.Provider == command.Provider
                     && x.Mode == command.Mode
                     && x.Name == name,
                cancellationToken))
        {
            return Result<PaymentProviderAccountDto>.Failure("Payment provider account conflicts with an existing provider/mode/name.");
        }

        var proposed = new PaymentProviderAccount
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            Provider = command.Provider,
            Mode = command.Mode,
            Name = name,
            PublicName = string.IsNullOrWhiteSpace(command.PublicName) ? name : command.PublicName.Trim(),
            IsEnabled = command.IsEnabled,
            IsDefault = command.IsDefault,
            ShopId = shopId,
            ApiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? DefaultApiBaseUrl(command.Provider) : apiBaseUrl.TrimEnd('/'),
            ReturnUrl = returnUrl,
            WebhookUrl = webhookUrl,
            UseWebhookIpAllowList = command.UseWebhookIpAllowList,
            AllowedWebhookIpRangesCsv = allowedWebhookIpRangesCsv,
            ExtraSettingsJson = replaceExtraSettings ? extraSettingsJson : existing?.ExtraSettingsJson ?? "{}",
            SecretKeyProtected = string.IsNullOrWhiteSpace(command.SecretKey)
                ? existing?.SecretKeyProtected ?? string.Empty
                : _secretProtector.Protect(command.SecretKey.Trim()),
            WebhookSecretProtected = string.IsNullOrWhiteSpace(command.WebhookSecret)
                ? existing?.WebhookSecretProtected ?? string.Empty
                : _secretProtector.Protect(command.WebhookSecret.Trim()),
            LastHealthCheckAt = existing?.LastHealthCheckAt,
            HealthStatus = existing?.HealthStatus ?? HealthStatus.Unknown,
            CreatedAt = existing?.CreatedAt ?? _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };

        var credentialValidationError = ValidateProviderCredentials(proposed);
        if (!string.IsNullOrWhiteSpace(credentialValidationError))
        {
            return Result<PaymentProviderAccountDto>.Failure(credentialValidationError);
        }

        var oldDefaults = proposed.IsDefault
            ? await _db.PaymentProviderAccounts
                .Where(x => x.Provider == proposed.Provider && x.Id != proposed.Id && x.IsDefault)
                .ToListAsync(cancellationToken)
            : [];
        var dbContext = _db as DbContext;
        await using var transaction = dbContext is not null && dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            foreach (var other in oldDefaults)
            {
                other.IsDefault = false;
                other.UpdatedAt = _clock.UtcNow;
            }

            if (transaction is not null && oldDefaults.Count > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            var account = existing ?? new PaymentProviderAccount();
            ApplyProposedAccount(account, proposed);
            if (existing is null)
            {
                _db.PaymentProviderAccounts.Add(account);
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return Result<PaymentProviderAccountDto>.Success(MapToDto(account));
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            return Result<PaymentProviderAccountDto>.Failure(
                "Payment provider account conflicts with an existing provider/mode/name or default account. Reload and retry.");
        }
    }

    private static void ApplyProposedAccount(PaymentProviderAccount account, PaymentProviderAccount proposed)
    {
        account.Id = proposed.Id;
        account.Provider = proposed.Provider;
        account.Mode = proposed.Mode;
        account.Name = proposed.Name;
        account.PublicName = proposed.PublicName;
        account.IsEnabled = proposed.IsEnabled;
        account.IsDefault = proposed.IsDefault;
        account.ShopId = proposed.ShopId;
        account.ApiBaseUrl = proposed.ApiBaseUrl;
        account.ReturnUrl = proposed.ReturnUrl;
        account.WebhookUrl = proposed.WebhookUrl;
        account.SecretKeyProtected = proposed.SecretKeyProtected;
        account.WebhookSecretProtected = proposed.WebhookSecretProtected;
        account.UseWebhookIpAllowList = proposed.UseWebhookIpAllowList;
        account.AllowedWebhookIpRangesCsv = proposed.AllowedWebhookIpRangesCsv;
        account.ExtraSettingsJson = proposed.ExtraSettingsJson;
        account.LastHealthCheckAt = proposed.LastHealthCheckAt;
        account.HealthStatus = proposed.HealthStatus;
        account.CreatedAt = proposed.CreatedAt;
        account.UpdatedAt = proposed.UpdatedAt;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException databaseException
                && string.Equals(databaseException.SqlState, "23505", StringComparison.Ordinal))
            {
                return true;
            }

            if (current is DbException sqliteException
                && sqliteException.ErrorCode == 19
                && sqliteException.GetType().GetProperty("SqliteExtendedErrorCode")?.GetValue(sqliteException) is int extendedCode
                && extendedCode is 1555 or 2067)
            {
                return true;
            }

            if (current.Message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<Result<PaymentProviderAccountCheckResultDto>> CheckAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquirePaymentProviderAccountAsync(id, cancellationToken);
        var account = await _db.PaymentProviderAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account is null)
        {
            return Result<PaymentProviderAccountCheckResultDto>.Failure("Payment provider account not found.");
        }

        var details = new List<string>();
        var blockingIssues = new List<string>();
        var readinessIssue = account.Provider == PaymentProvider.TelegramStars
            ? PaymentProviderConfigurationRules.GetBotCheckoutConfigurationIssue(account)
            : PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account);
        if (readinessIssue is null)
        {
            details.Add("Готово: базовая конфигурация checkout заполнена.");
        }
        else
        {
            details.Add($"Проблема: {readinessIssue}");
            blockingIssues.Add(readinessIssue);
        }

        var requiredFields = BuildRequiredFields(account);
        foreach (var field in requiredFields.Where(x => x.Required))
        {
            if (field.Configured)
            {
                details.Add($"Заполнено: {field.Label}.");
                continue;
            }

            var issue = string.IsNullOrWhiteSpace(field.Issue)
                ? $"Поле {field.Label} обязательно для этого провайдера."
                : field.Issue;
            details.Add($"Не заполнено: {field.Label}. {issue}");
            blockingIssues.Add(issue);
        }

        AddUrlCheck(
            details,
            blockingIssues,
            account.ApiBaseUrl,
            "API base URL",
            required: requiredFields.Any(x => x.Key == "apiBaseUrl" && x.Required));
        AddUrlCheck(details, blockingIssues, account.ReturnUrl, "Return URL", required: false);
        AddUrlCheck(details, blockingIssues, account.WebhookUrl, "Webhook URL", required: false);

        var hostedCheckoutUrl = PaymentProviderConfigurationRules.ReadExtraSetting(account.ExtraSettingsJson, "hostedCheckoutUrl");
        if (account.Provider == PaymentProvider.CloudPayments)
        {
            AddUrlCheck(details, blockingIssues, hostedCheckoutUrl ?? string.Empty, "CloudPayments hosted checkout URL", required: false);
        }

        if (account.UseWebhookIpAllowList && string.IsNullOrWhiteSpace(account.AllowedWebhookIpRangesCsv))
        {
            const string issue = "Webhook IP allow list включен, но список разрешенных IP пуст.";
            details.Add(issue);
            blockingIssues.Add(issue);
        }

        var extraSettingsIssue = ValidateExtraSettingsJson(string.IsNullOrWhiteSpace(account.ExtraSettingsJson) ? "{}" : account.ExtraSettingsJson);
        if (extraSettingsIssue is not null)
        {
            details.Add($"ExtraSettingsJson: {extraSettingsIssue}");
            blockingIssues.Add(extraSettingsIssue);
        }

        details.AddRange(GetProviderCheckGuidance(account.Provider));

        var distinctDetails = details.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var hasBlockingIssue = blockingIssues.Distinct(StringComparer.OrdinalIgnoreCase).Any();

        var checkedAt = _clock.UtcNow;
        if (account.HealthStatus != HealthStatus.Unknown || account.LastHealthCheckAt.HasValue)
        {
            account.HealthStatus = HealthStatus.Unknown;
            account.LastHealthCheckAt = null;
            account.UpdatedAt = checkedAt;
        }
        await _db.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(account);
        return Result<PaymentProviderAccountCheckResultDto>.Success(new(
            account.Id,
            account.Provider,
            account.Mode,
            !hasBlockingIssue,
            "ConfigurationOnly",
            hasBlockingIssue ? "NeedsConfiguration" : "Ready",
            HealthStatus.Unknown.ToString(),
            hasBlockingIssue
                ? "Проверка конфигурации нашла проблемы. Внешний кабинет провайдера не запрашивался."
                : "Конфигурация готова. Внешний кабинет провайдера не запрашивался.",
            distinctDetails,
            checkedAt,
            dto));
    }

    public async Task<Result<PaymentProviderAccountDto>> SetEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquirePaymentProviderAccountAsync(id, cancellationToken);
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
            account.WebhookUrl,
            !string.IsNullOrWhiteSpace(account.SecretKeyProtected),
            !string.IsNullOrWhiteSpace(account.WebhookSecretProtected),
            account.UseWebhookIpAllowList,
            account.AllowedWebhookIpRangesCsv,
            MaskExtraSettings(account.ExtraSettingsJson),
            HealthStatus.Unknown.ToString(),
            PaymentProviderConfigurationRules.IsCheckoutConfigured(account),
            PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account),
            PaymentProviderConfigurationRules.GetCapabilitiesJson(account.Provider),
            BuildCapabilities(account.Provider),
            BuildRequiredFields(account),
            BuildReadinessBlockers(account),
            PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account),
            account.CreatedAt,
            account.UpdatedAt);

    private static IReadOnlyCollection<PaymentProviderCapabilityDto> BuildCapabilities(PaymentProvider provider)
        => PaymentProviderConfigurationRules.GetCapabilityRules(provider)
            .Select(x => new PaymentProviderCapabilityDto(x.Key, x.Label, x.Supported, x.Status))
            .ToArray();

    private static IReadOnlyCollection<PaymentProviderRequiredFieldDto> BuildRequiredFields(PaymentProviderAccount account)
    {
        var production = account.Mode == PaymentProviderMode.Production;
        var webCheckout = PaymentProviderConfigurationRules.SupportsWebCheckout(account.Provider);
        var telegramStars = account.Provider == PaymentProvider.TelegramStars;
        var webhookSecretRequired = production && account.Provider is PaymentProvider.RoboKassa or PaymentProvider.YooMoney or PaymentProvider.Stripe or PaymentProvider.PayPal or PaymentProvider.Prodamus;
        var hostedCheckoutUrl = PaymentProviderConfigurationRules.ReadExtraSetting(account.ExtraSettingsJson, "hostedCheckoutUrl");

        var fields = new List<PaymentProviderRequiredFieldDto>
        {
            Field("enabled", "Аккаунт включен", true, account.IsEnabled, "Включите аккаунт, иначе пользователи не увидят способ оплаты."),
            Field("mode", "Режим Sandbox или Production", true, account.Mode != PaymentProviderMode.Disabled, "Выберите Sandbox или Production."),
            Field("shopId", telegramStars ? "Bot username" : "ShopId / merchant id", webCheckout || telegramStars, !string.IsNullOrWhiteSpace(account.ShopId), telegramStars ? "Укажите username Telegram-бота для Stars invoice flow." : "Укажите идентификатор магазина или мерчанта."),
            Field("secretKey", "Secret key", production && webCheckout, !string.IsNullOrWhiteSpace(account.SecretKeyProtected), "Для production нужен защищенный secret key."),
            Field("webhookSecret", "Webhook secret", webhookSecretRequired, !string.IsNullOrWhiteSpace(account.WebhookSecretProtected), "Для production-уведомлений нужен webhook secret."),
            Field("apiBaseUrl", "API base URL", webCheckout && account.Provider != PaymentProvider.CloudPayments, !string.IsNullOrWhiteSpace(account.ApiBaseUrl), "Укажите API URL провайдера."),
            Field("hostedCheckoutUrl", "CloudPayments hostedCheckoutUrl", account.Provider == PaymentProvider.CloudPayments, !string.IsNullOrWhiteSpace(hostedCheckoutUrl), "В ExtraSettingsJson нужен hostedCheckoutUrl со страницей виджета."),
            Field("telegramBotFlow", "Telegram invoice flow", account.Provider == PaymentProvider.TelegramStars, PaymentProviderConfigurationRules.IsBotCheckoutConfigured(account), "Настройте Telegram-бота и invoice flow.")
        };

        return fields.Where(x => x.Required || x.Configured).ToArray();
    }

    private static PaymentProviderRequiredFieldDto Field(string key, string label, bool required, bool configured, string issue)
        => new(key, label, required, configured, required && !configured ? issue : null);

    private static IReadOnlyCollection<string> BuildReadinessBlockers(PaymentProviderAccount account)
    {
        var blockers = new List<string>();
        var checkoutIssue = account.Provider == PaymentProvider.TelegramStars
            ? PaymentProviderConfigurationRules.GetBotCheckoutConfigurationIssue(account)
            : PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account);
        if (!string.IsNullOrWhiteSpace(checkoutIssue))
        {
            blockers.Add(checkoutIssue);
        }

        blockers.AddRange(BuildRequiredFields(account)
            .Where(x => x.Required && !x.Configured && !string.IsNullOrWhiteSpace(x.Issue))
            .Select(x => x.Issue!));

        return blockers.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }


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

    private static void AddUrlCheck(List<string> details, List<string> blockingIssues, string value, string fieldName, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                var issue = $"{fieldName} обязателен для этого провайдера.";
                details.Add(issue);
                blockingIssues.Add(issue);
            }

            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            var issue = $"{fieldName} указан неверно. Используйте абсолютный http/https URL.";
            details.Add(issue);
            blockingIssues.Add(issue);
            return;
        }

        details.Add($"URL корректен: {fieldName}.");
    }

    private static IReadOnlyCollection<string> GetProviderCheckGuidance(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.YooKassa => new[]
            {
                "YooKassa: для sandbox достаточно ShopId и API URL; в production проверьте secret key и webhook в кабинете YooKassa."
            },
            PaymentProvider.RoboKassa => new[]
            {
                "RoboKassa: проверьте MerchantLogin, Password #1 для создания платежа, Password #2 для уведомлений и ResultURL."
            },
            PaymentProvider.YooMoney => new[]
            {
                "YooMoney: проверьте receiver/shopId, notification secret для production и URL уведомлений."
            },
            PaymentProvider.CloudPayments => new[]
            {
                "CloudPayments: публичный сценарий использует merchant-hosted widget page из ExtraSettingsJson.hostedCheckoutUrl."
            },
            PaymentProvider.TBankAcquiring => new[]
            {
                "TBank: проверьте TerminalKey, password/API token и рабочий API URL терминала."
            },
            PaymentProvider.Prodamus => new[]
            {
                "Prodamus: проверьте payform URL, secret key подписи и webhook URL для уведомлений."
            },
            PaymentProvider.Stripe => new[]
            {
                "Stripe: проверьте publishable/secret данные, webhook endpoint secret для production и события checkout.session.completed."
            },
            PaymentProvider.PayPal => new[]
            {
                "PayPal: проверьте client id, client secret, webhook id и соответствие sandbox/production окружения."
            },
            PaymentProvider.TelegramStars => new[]
            {
                "Telegram Stars: web checkout скрыт; оплата должна идти через Telegram invoice flow внутри бота."
            },
            _ => Array.Empty<string>()
        };
    }

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

        if (account.Provider != PaymentProvider.TelegramStars
            && account.Mode == PaymentProviderMode.Production
            && string.IsNullOrWhiteSpace(account.SecretKeyProtected))
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
