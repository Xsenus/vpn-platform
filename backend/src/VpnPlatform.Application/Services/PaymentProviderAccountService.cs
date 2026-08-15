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
    public const string AccountChangedError = "Аккаунт платёжного провайдера уже изменён. Обновите список и повторите действие.";
    public const string RevisionRequiredError = "Версия аккаунта платёжного провайдера обязательна для обновления.";
    public const string ChangesNotDetectedError = "Изменения аккаунта платёжного провайдера не обнаружены.";

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
            ? Result<PaymentProviderAccountDto>.Failure("Аккаунт платёжного провайдера не найден.")
            : Result<PaymentProviderAccountDto>.Success(MapToDto(account));
    }

    public async Task<Result<PaymentProviderAccount>> GetWebCheckoutAccountEntityAsync(PaymentProvider provider, CancellationToken cancellationToken = default)
    {
        var candidates = await _db.PaymentProviderAccounts
            .Where(x => x.Provider == provider && x.IsEnabled && x.Mode != PaymentProviderMode.Disabled)
            .ToListAsync(cancellationToken);

        var safeCandidates = candidates
            .Where(x => ValidateAccountUrls(x) is null)
            .ToList();
        var account = PaymentProviderConfigurationRules.SelectWebCheckoutAccount(safeCandidates, provider);

        if (account is null)
        {
            var unsafeCandidateError = candidates
                .Select(ValidateAccountUrls)
                .FirstOrDefault(x => x is not null);
            return Result<PaymentProviderAccount>.Failure(
                unsafeCandidateError ?? $"Провайдер {provider} не настроен для оплаты на сайте или выключен.");
        }

        return Result<PaymentProviderAccount>.Success(account);
    }

    public async Task<Result<PaymentProviderAccountDto>> UpsertAsync(Guid? id, UpsertPaymentProviderAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.Provider))
        {
            return Result<PaymentProviderAccountDto>.Failure("Платёжный провайдер не поддерживается.");
        }

        if (!Enum.IsDefined(command.Mode))
        {
            return Result<PaymentProviderAccountDto>.Failure("Режим платёжного провайдера не поддерживается.");
        }

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
            return Result<PaymentProviderAccountDto>.Failure("Для рабочего аккаунта провайдера нужен ShopId.");
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
                return Result<PaymentProviderAccountDto>.Failure("Аккаунт платёжного провайдера не найден.");
            }
            if (command.Revision is null or < 0)
            {
                return Result<PaymentProviderAccountDto>.Failure(RevisionRequiredError);
            }
            if (command.Revision.Value != existing.Revision)
            {
                return Result<PaymentProviderAccountDto>.Failure(AccountChangedError);
            }

        }
        else if (command.Provider != PaymentProvider.TelegramStars
                 && command.Mode == PaymentProviderMode.Production
                 && string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return Result<PaymentProviderAccountDto>.Failure("Для нового рабочего аккаунта провайдера нужен SecretKey.");
        }

        var name = string.IsNullOrWhiteSpace(command.Name) ? command.Provider.ToString() : command.Name.Trim();
        if (await _db.PaymentProviderAccounts.AnyAsync(
                x => x.Id != (id ?? Guid.Empty)
                     && x.Provider == command.Provider
                     && x.Mode == command.Mode
                     && x.Name == name,
                cancellationToken))
        {
            return Result<PaymentProviderAccountDto>.Failure("Аккаунт с таким провайдером, режимом и названием уже существует.");
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
            Revision = existing is null ? 0 : checked(existing.Revision + 1),
            CreatedAt = existing?.CreatedAt ?? _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };

        var urlValidationError = ValidateAccountUrls(proposed);
        if (urlValidationError is not null)
        {
            return Result<PaymentProviderAccountDto>.Failure(urlValidationError);
        }

        var credentialValidationError = ValidateProviderCredentials(proposed);
        if (!string.IsNullOrWhiteSpace(credentialValidationError))
        {
            return Result<PaymentProviderAccountDto>.Failure(credentialValidationError);
        }

        if (existing is not null
            && string.IsNullOrWhiteSpace(command.SecretKey)
            && string.IsNullOrWhiteSpace(command.WebhookSecret)
            && AccountConfigurationEquals(existing, proposed))
        {
            return Result<PaymentProviderAccountDto>.Failure(ChangesNotDetectedError);
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
                other.Revision = checked(other.Revision + 1);
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
                "Аккаунт конфликтует с существующей записью или основным аккаунтом. Обновите список и повторите.");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            return Result<PaymentProviderAccountDto>.Failure(AccountChangedError);
        }
    }

    private static bool AccountConfigurationEquals(PaymentProviderAccount current, PaymentProviderAccount proposed)
        => current.Provider == proposed.Provider
           && current.Mode == proposed.Mode
           && current.Name == proposed.Name
           && current.PublicName == proposed.PublicName
           && current.IsEnabled == proposed.IsEnabled
           && current.IsDefault == proposed.IsDefault
           && current.ShopId == proposed.ShopId
           && current.ApiBaseUrl == proposed.ApiBaseUrl
           && current.ReturnUrl == proposed.ReturnUrl
           && current.WebhookUrl == proposed.WebhookUrl
           && current.UseWebhookIpAllowList == proposed.UseWebhookIpAllowList
           && current.AllowedWebhookIpRangesCsv == proposed.AllowedWebhookIpRangesCsv
           && current.ExtraSettingsJson == proposed.ExtraSettingsJson;

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
        account.Revision = proposed.Revision;
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
            return Result<PaymentProviderAccountCheckResultDto>.Failure("Аккаунт платёжного провайдера не найден.");
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
            "Базовый URL API",
            required: requiredFields.Any(x => x.Key == "apiBaseUrl" && x.Required));
        AddUrlCheck(details, blockingIssues, account.ReturnUrl, "URL возврата", required: false);
        AddUrlCheck(details, blockingIssues, account.WebhookUrl, "URL webhook", required: false);

        var hostedCheckoutUrl = PaymentProviderConfigurationRules.ReadExtraSetting(account.ExtraSettingsJson, "hostedCheckoutUrl");
        if (account.Provider == PaymentProvider.CloudPayments)
        {
            AddUrlCheck(details, blockingIssues, hostedCheckoutUrl ?? string.Empty, "URL виджета CloudPayments", required: false);
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
            account.Revision = checked(account.Revision + 1);
        }
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PaymentProviderAccountCheckResultDto>.Failure(AccountChangedError);
        }

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

    public async Task<Result<PaymentProviderAccountDto>> SetEnabledAsync(Guid id, bool enabled, int? revision = null, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquirePaymentProviderAccountAsync(id, cancellationToken);
        var account = await _db.PaymentProviderAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account is null)
        {
            return Result<PaymentProviderAccountDto>.Failure("Аккаунт платёжного провайдера не найден.");
        }
        if (revision is null or < 0)
        {
            return Result<PaymentProviderAccountDto>.Failure(RevisionRequiredError);
        }
        if (revision.Value != account.Revision)
        {
            return Result<PaymentProviderAccountDto>.Failure(AccountChangedError);
        }
        if (account.IsEnabled == enabled)
        {
            return Result<PaymentProviderAccountDto>.Failure(ChangesNotDetectedError);
        }

        if (enabled)
        {
            var urlValidationError = ValidateAccountUrls(account);
            if (urlValidationError is not null)
            {
                return Result<PaymentProviderAccountDto>.Failure(urlValidationError);
            }
        }

        account.IsEnabled = enabled;
        account.UpdatedAt = _clock.UtcNow;
        account.Revision = checked(account.Revision + 1);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PaymentProviderAccountDto>.Failure(AccountChangedError);
        }
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
            account.Revision,
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
            Field("mode", "Проверочный или рабочий режим", true, account.Mode != PaymentProviderMode.Disabled, "Выберите проверочный или рабочий режим."),
            Field("shopId", telegramStars ? "Username Telegram-бота" : "ShopId / ID мерчанта", webCheckout || telegramStars, !string.IsNullOrWhiteSpace(account.ShopId), telegramStars ? "Укажите username Telegram-бота для оплаты Stars." : "Укажите идентификатор магазина или мерчанта."),
            Field("secretKey", "Секретный ключ", production && webCheckout, !string.IsNullOrWhiteSpace(account.SecretKeyProtected), "Для рабочего режима нужен защищённый секретный ключ."),
            Field("webhookSecret", "Секрет webhook", webhookSecretRequired, !string.IsNullOrWhiteSpace(account.WebhookSecretProtected), "Для рабочих уведомлений нужен секрет webhook."),
            Field("apiBaseUrl", "Базовый URL API", webCheckout && account.Provider != PaymentProvider.CloudPayments, !string.IsNullOrWhiteSpace(account.ApiBaseUrl), "Укажите URL API провайдера."),
            Field("hostedCheckoutUrl", "URL виджета CloudPayments", account.Provider == PaymentProvider.CloudPayments, !string.IsNullOrWhiteSpace(hostedCheckoutUrl), "В ExtraSettingsJson нужен hostedCheckoutUrl со страницей виджета."),
            Field("telegramBotFlow", "Сценарий оплаты Telegram", account.Provider == PaymentProvider.TelegramStars, PaymentProviderConfigurationRules.IsBotCheckoutConfigured(account), "Настройте Telegram-бота и сценарий выставления счёта.")
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

        var validationError = ValidateOptionalSafeHttpUrl(value, fieldName);
        if (validationError is not null)
        {
            details.Add(validationError);
            blockingIssues.Add(validationError);
            return;
        }

        details.Add($"URL корректен: {fieldName}.");
    }

    private static string? ValidateOptionalSafeHttpUrl(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (SafeHttpUrl.ContainsCredentials(value))
        {
            return $"{fieldName} не должен содержать логин или пароль.";
        }

        return SafeHttpUrl.TryNormalize(value, out _)
            ? null
            : $"{fieldName} указан неверно. Используйте абсолютный http/https URL.";
    }

    private static string? ValidateAccountUrls(PaymentProviderAccount account)
    {
        foreach (var (value, fieldName) in new[]
                 {
                     (account.ApiBaseUrl, "Базовый URL API"),
                     (account.ReturnUrl, "URL возврата"),
                     (account.WebhookUrl, "URL webhook"),
                     (PaymentProviderConfigurationRules.ReadExtraSetting(account.ExtraSettingsJson, "hostedCheckoutUrl"), "URL виджета CloudPayments")
                 })
        {
            var validationError = ValidateOptionalSafeHttpUrl(value, fieldName);
            if (validationError is not null)
            {
                return validationError;
            }
        }

        return null;
    }

    private static IReadOnlyCollection<string> GetProviderCheckGuidance(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.YooKassa => new[]
            {
                "YooKassa: для проверочного режима достаточно ShopId и URL API; в рабочем режиме проверьте секретный ключ и webhook в кабинете YooKassa."
            },
            PaymentProvider.RoboKassa => new[]
            {
                "RoboKassa: проверьте MerchantLogin, пароль № 1 для создания платежа, пароль № 2 для уведомлений и ResultURL."
            },
            PaymentProvider.YooMoney => new[]
            {
                "YooMoney: проверьте receiver/shopId, секрет уведомлений для рабочего режима и URL уведомлений."
            },
            PaymentProvider.CloudPayments => new[]
            {
                "CloudPayments: оплата на сайте использует страницу виджета магазина из ExtraSettingsJson.hostedCheckoutUrl."
            },
            PaymentProvider.TBankAcquiring => new[]
            {
                "TBank: проверьте TerminalKey, пароль или токен API и рабочий URL API терминала."
            },
            PaymentProvider.Prodamus => new[]
            {
                "Prodamus: проверьте URL формы оплаты, секретный ключ подписи и URL webhook для уведомлений."
            },
            PaymentProvider.Stripe => new[]
            {
                "Stripe: проверьте публичный и секретный ключи, секрет webhook для рабочего режима и событие checkout.session.completed."
            },
            PaymentProvider.PayPal => new[]
            {
                "PayPal: проверьте ID клиента, секрет клиента, ID webhook и соответствие проверочного или рабочего окружения."
            },
            PaymentProvider.TelegramStars => new[]
            {
                "Telegram Stars: оплата на сайте скрыта; счёт выставляется внутри Telegram-бота."
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
                : "ExtraSettingsJson должен содержать объект JSON.";
        }
        catch (System.Text.Json.JsonException)
        {
            return "ExtraSettingsJson должен содержать корректный объект JSON.";
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
            return "Для рабочего аккаунта провайдера нужен ShopId, MerchantLogin или Receiver.";
        }

        if (account.Provider != PaymentProvider.TelegramStars
            && account.Mode == PaymentProviderMode.Production
            && string.IsNullOrWhiteSpace(account.SecretKeyProtected))
        {
            return $"Для рабочего аккаунта {account.Provider} нужен защищённый SecretKey.";
        }

        if (account.Provider is PaymentProvider.RoboKassa or PaymentProvider.YooMoney or PaymentProvider.Stripe or PaymentProvider.PayPal or PaymentProvider.Prodamus
            && account.Mode == PaymentProviderMode.Production
            && string.IsNullOrWhiteSpace(account.WebhookSecretProtected))
        {
            return $"Для рабочего аккаунта {account.Provider} нужен защищённый WebhookSecret для проверки уведомлений.";
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
