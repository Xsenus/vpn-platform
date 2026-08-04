using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class TelegramBotService
{
    private static readonly TimeSpan TelegramUpdateLease = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private static readonly PaymentProvider[] PreferredPaymentProviderOrder =
    {
        PaymentProvider.YooKassa,
        PaymentProvider.TBankAcquiring,
        PaymentProvider.RoboKassa,
        PaymentProvider.Prodamus,
        PaymentProvider.YooMoney,
        PaymentProvider.TelegramStars,
        PaymentProvider.CloudPayments,
        PaymentProvider.Stripe,
        PaymentProvider.PayPal
    };
    private static class BotStates
    {
        public const string Idle = "idle";
        public const string WaitingForRegistration = "waiting_for_registration";
        public const string WaitingForLink = "waiting_for_link";
        public const string BrowsingTariffs = "browsing_tariffs";
        public const string ConfirmingOrder = "confirming_order";
        public const string ChoosingPaymentProvider = "choosing_payment_provider";
        public const string WaitingForPayment = "waiting_for_payment";
        public const string SupportMode = "support";
        public const string OwnVpsAwaitingHost = "own_vps_awaiting_host";
        public const string OwnVpsAwaitingPort = "own_vps_awaiting_port";
        public const string OwnVpsAwaitingUsername = "own_vps_awaiting_username";
        public const string OwnVpsAwaitingAuthMethod = "own_vps_awaiting_auth_method";
        public const string OwnVpsAwaitingCredential = "own_vps_awaiting_credential";
        public const string OwnVpsAwaitingName = "own_vps_awaiting_name";
        public const string OwnVpsAwaitingConfirmation = "own_vps_awaiting_confirmation";
    }

    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly OrderService? _orderService;
    private readonly PaymentOrchestrator? _paymentOrchestrator;
    private readonly SubscriptionService? _subscriptionService;
    private readonly ITelegramInvoiceProvider? _invoiceProvider;
    private readonly ProvisioningService? _provisioningService;
    private readonly ISecretProtector? _secretProtector;

    public TelegramBotService(
        IApplicationDbContext db,
        IClock clock,
        OrderService? orderService = null,
        PaymentOrchestrator? paymentOrchestrator = null,
        SubscriptionService? subscriptionService = null,
        ITelegramInvoiceProvider? invoiceProvider = null,
        ProvisioningService? provisioningService = null,
        ISecretProtector? secretProtector = null)
    {
        _db = db;
        _clock = clock;
        _orderService = orderService;
        _paymentOrchestrator = paymentOrchestrator;
        _subscriptionService = subscriptionService;
        _invoiceProvider = invoiceProvider;
        _provisioningService = provisioningService;
        _secretProtector = secretProtector;
    }

    public async Task<Result<TelegramLinkTokenDto>> CreateLinkTokenAsync(Guid userId, string publicBotUsername, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result<TelegramLinkTokenDto>.Failure("Invalid user id.");
        }

        if (string.IsNullOrWhiteSpace(publicBotUsername))
        {
            return Result<TelegramLinkTokenDto>.Failure("TelegramBot:PublicBotUsername is required to create a deep link.");
        }

        var existingLinked = await _db.TelegramAccounts.AsNoTracking().AnyAsync(x => x.UserId == userId, cancellationToken);
        if (existingLinked)
        {
            return Result<TelegramLinkTokenDto>.Failure("User already has a linked Telegram account.");
        }

        var token = CreateToken();
        var expiresAt = _clock.UtcNow.AddMinutes(10);
        _db.TelegramBotDeepLinks.Add(new TelegramBotDeepLink
        {
            UserId = userId,
            TokenHash = HashToken(token),
            Purpose = "link_account",
            ExpiresAt = expiresAt,
            MetadataJson = "{}"
        });

        await _db.SaveChangesAsync(cancellationToken);

        var username = publicBotUsername.Trim().TrimStart('@');
        var url = $"https://t.me/{username}?start=link_{token}";
        return Result<TelegramLinkTokenDto>.Success(new TelegramLinkTokenDto(token, url, expiresAt));
    }

    public async Task<TelegramStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await _db.TelegramAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return account is null
            ? new TelegramStatusDto(false, null, null, null)
            : new TelegramStatusDto(true, account.TelegramUserId, account.Username, account.LinkedAt);
    }

    public async Task<Result<TelegramStatusDto>> UnlinkAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (account is null)
        {
            return Result<TelegramStatusDto>.Success(new TelegramStatusDto(false, null, null, null));
        }

        account.UserId = null;
        account.LinkedAt = null;
        account.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<TelegramStatusDto>.Success(new TelegramStatusDto(false, account.TelegramUserId, account.Username, null));
    }

    public async Task<Result<TelegramBotProcessResult>> ProcessUpdateAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        string? expectedSecretToken,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(expectedSecretToken))
        {
            if (!headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var actualSecret)
                || !FixedEquals(actualSecret, expectedSecretToken))
            {
                return Result<TelegramBotProcessResult>.Failure("Invalid Telegram webhook secret token.");
            }
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawBody);
        }
        catch (JsonException ex)
        {
            return Result<TelegramBotProcessResult>.Failure($"Invalid Telegram update JSON: {ex.Message}");
        }

        using var parsedDocument = doc;
        var root = parsedDocument.RootElement;
        if (!root.TryGetProperty("update_id", out var updateIdElement) || !updateIdElement.TryGetInt64(out var updateId))
        {
            return Result<TelegramBotProcessResult>.Failure("Telegram update_id is required.");
        }

        var parsed = ParseUpdate(root);
        var sanitizedRawBody = await RedactSensitiveTelegramRawBodyAsync(parsed.TelegramUserId, rawBody, cancellationToken);
        var now = _clock.UtcNow;
        await using var updateGate = await PaymentProcessingGate.AcquireTelegramUpdateAsync(updateId, cancellationToken);
        var claim = await ClaimTelegramUpdateAsync(updateId, parsed, sanitizedRawBody, rawBody, now, cancellationToken);
        if (claim.IsProcessed)
        {
            return Result<TelegramBotProcessResult>.Success(new TelegramBotProcessResult(false, string.Empty, UpdateId: updateId));
        }

        if (claim.IsInProgress || claim.Update is null)
        {
            return Result<TelegramBotProcessResult>.Failure("Telegram update processing is already in progress; retry shortly.", isRetryable: true);
        }

        var update = claim.Update;
        try
        {
            if (!parsed.TelegramUserId.HasValue)
            {
                var responseText = HelpText();
                var replyMarkupJson = LinkedMenuReplyMarkupJson();
                update.IsProcessed = true;
                update.ProcessedAt = now;
                StoreTelegramDelivery(update, responseText, parsed.ChatId, replyMarkupJson, null, null, null);
                update.UpdatedAt = NextTelegramUpdateVersion(update.UpdatedAt, now);
                await _db.SaveChangesAsync(cancellationToken);
                return Result<TelegramBotProcessResult>.Success(new TelegramBotProcessResult(true, responseText, parsed.ChatId, replyMarkupJson, UpdateId: updateId));
            }

            var account = await UpsertTelegramAccountAsync(parsed, cancellationToken);
            await StoreInboundMessageAsync(account, parsed, sanitizedRawBody, cancellationToken);
            var route = await RouteAsync(account, parsed, rawBody, cancellationToken);

            update.IsProcessed = true;
            update.ProcessedAt = now;
            update.ErrorText = string.Empty;
            var responseChatId = route.ChatId ?? parsed.ChatId ?? parsed.TelegramUserId;
            StoreTelegramDelivery(
                update,
                route.ResponseText,
                responseChatId,
                route.ReplyMarkupJson,
                route.PreCheckoutQueryId,
                route.PreCheckoutOk,
                route.PreCheckoutError);
            update.UpdatedAt = NextTelegramUpdateVersion(update.UpdatedAt, now);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<TelegramBotProcessResult>.Success(new TelegramBotProcessResult(
                true,
                route.ResponseText,
                responseChatId,
                route.ReplyMarkupJson,
                route.PreCheckoutQueryId,
                route.PreCheckoutOk,
                route.PreCheckoutError,
                updateId));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkTelegramUpdateFailedAsync(update, "Telegram update processing was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            await MarkTelegramUpdateFailedAsync(update, ex.Message);
            throw;
        }
    }

    private async Task<TelegramUpdateClaim> ClaimTelegramUpdateAsync(
        long updateId,
        ParsedTelegramUpdate parsed,
        string sanitizedRawBody,
        string rawBody,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var payloadSha256 = HashRawPayload(rawBody);
        var update = await _db.TelegramBotUpdates.FirstOrDefaultAsync(x => x.UpdateId == updateId, cancellationToken);
        if (update?.IsProcessed == true)
        {
            return TelegramUpdateClaim.Processed;
        }

        if (update is null)
        {
            update = new TelegramBotUpdate
            {
                UpdateId = updateId,
                TelegramUserId = parsed.TelegramUserId,
                UpdateType = parsed.UpdateType,
                RawPayload = sanitizedRawBody,
                PayloadSha256 = payloadSha256,
                IsProcessed = false,
                ErrorText = string.Empty,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.TelegramBotUpdates.Add(update);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return TelegramUpdateClaim.Claimed(update);
            }
            catch (DbUpdateException)
            {
                _db.TelegramBotUpdates.Remove(update);
                var concurrent = await _db.TelegramBotUpdates.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UpdateId == updateId, cancellationToken);
                if (concurrent is null)
                {
                    throw;
                }

                return concurrent.IsProcessed ? TelegramUpdateClaim.Processed : TelegramUpdateClaim.InProgress;
            }
        }

        if (string.IsNullOrWhiteSpace(update.ErrorText) && update.UpdatedAt > now.Subtract(TelegramUpdateLease))
        {
            return TelegramUpdateClaim.InProgress;
        }

        var expectedVersion = update.UpdatedAt;
        var claimVersion = NextTelegramUpdateVersion(expectedVersion, now);
        if (_db is DbContext dbContext
            && !string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            var claimed = await _db.TelegramBotUpdates
                .Where(x => x.Id == update.Id && !x.IsProcessed && x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.TelegramUserId, parsed.TelegramUserId)
                    .SetProperty(x => x.UpdateType, parsed.UpdateType)
                    .SetProperty(x => x.RawPayload, sanitizedRawBody)
                    .SetProperty(x => x.PayloadSha256, payloadSha256)
                    .SetProperty(x => x.ProcessedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.ErrorText, string.Empty)
                    .SetProperty(x => x.UpdatedAt, claimVersion), cancellationToken);
            if (claimed == 0)
            {
                return TelegramUpdateClaim.InProgress;
            }
        }
        else
        {
            ApplyTelegramUpdateClaim(update, parsed, sanitizedRawBody, payloadSha256, claimVersion);
            await _db.SaveChangesAsync(cancellationToken);
            return TelegramUpdateClaim.Claimed(update);
        }

        ApplyTelegramUpdateClaim(update, parsed, sanitizedRawBody, payloadSha256, claimVersion);
        return TelegramUpdateClaim.Claimed(update);
    }

    private static void ApplyTelegramUpdateClaim(
        TelegramBotUpdate update,
        ParsedTelegramUpdate parsed,
        string sanitizedRawBody,
        string payloadSha256,
        DateTimeOffset claimVersion)
    {
        update.TelegramUserId = parsed.TelegramUserId;
        update.UpdateType = parsed.UpdateType;
        update.RawPayload = sanitizedRawBody;
        update.PayloadSha256 = payloadSha256;
        update.IsProcessed = false;
        update.ProcessedAt = null;
        update.ErrorText = string.Empty;
        ClearTelegramDelivery(update);
        update.UpdatedAt = claimVersion;
    }

    private static void StoreTelegramDelivery(
        TelegramBotUpdate update,
        string responseText,
        long? responseChatId,
        string? replyMarkupJson,
        string? preCheckoutQueryId,
        bool? preCheckoutOk,
        string? preCheckoutError)
    {
        update.ResponseText = responseText;
        update.ResponseChatId = !string.IsNullOrWhiteSpace(responseText) ? responseChatId : null;
        update.ResponseReplyMarkupJson = replyMarkupJson ?? string.Empty;
        update.ResponseSentAt = null;
        update.PreCheckoutQueryId = preCheckoutQueryId ?? string.Empty;
        update.PreCheckoutOk = preCheckoutOk;
        update.PreCheckoutError = preCheckoutError ?? string.Empty;
        update.PreCheckoutAnsweredAt = null;
        update.DeliveryClaimedAt = null;
        update.DeliveryNextAttemptAt = null;
        update.DeliveryAttemptCount = 0;
        update.DeliveryErrorText = string.Empty;
    }

    private static void ClearTelegramDelivery(TelegramBotUpdate update)
        => StoreTelegramDelivery(update, string.Empty, null, null, null, null, null);

    private async Task MarkTelegramUpdateFailedAsync(TelegramBotUpdate update, string error)
    {
        update.IsProcessed = false;
        update.ProcessedAt = null;
        ClearTelegramDelivery(update);
        update.ErrorText = SensitiveDataRedactor.Redact(error, maxLength: 500);
        update.UpdatedAt = NextTelegramUpdateVersion(update.UpdatedAt, _clock.UtcNow);
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private static DateTimeOffset NextTelegramUpdateVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);

    public string MainMenuText() => "Главное меню VPN Platform:\n• Купить VPN — выбрать тариф и оплату\n• Мои подписки — срок и статус\n• Мои ключи — VPN URI и QR payload\n• Продлить доступ\n• Инструкция\n• Поддержка\n• Профиль\n• VPN на мой VPS — safe dry-run/precheck";

    public string MainMenuReplyMarkupJson() => LinkedMenuReplyMarkupJson();

    private string UnlinkedMenuText()
        => "Telegram аккаунт создан. Можно посмотреть тарифы, зарегистрироваться или привязать существующий аккаунт из личного кабинета. Секреты и платежные данные бот не запрашивает.";

    private string LinkedMenuText(TelegramAccount account)
        => $"Здравствуйте, {DisplayName(account)}. Telegram привязан к аккаунту.\n\n{MainMenuText()}";

    private string UnlinkedMenuReplyMarkupJson()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new[]
            {
                new[] { new { text = "Зарегистрироваться", callback_data = "register_tg" }, new { text = "Привязать аккаунт", callback_data = "link_account" } },
                new[] { new { text = "Купить VPN", callback_data = "tariffs" }, new { text = "Инструкция", callback_data = "instruction" } },
                new[] { new { text = "Поддержка", callback_data = "support" } }
            }
        }, JsonOptions);

    private string LinkedMenuReplyMarkupJson()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new[]
            {
                new[] { new { text = "Купить VPN", callback_data = "tariffs" }, new { text = "Мои подписки", callback_data = "subscriptions" } },
                new[] { new { text = "Мои ключи", callback_data = "keys" }, new { text = "Продлить доступ", callback_data = "renew" } },
                new[] { new { text = "Инструкция", callback_data = "instruction" }, new { text = "Поддержка", callback_data = "support" } },
                new[] { new { text = "Профиль", callback_data = "profile" }, new { text = "VPN на мой VPS", callback_data = "own_vps" } }
            }
        }, JsonOptions);

    private async Task<TelegramAccount> UpsertTelegramAccountAsync(ParsedTelegramUpdate parsed, CancellationToken cancellationToken)
    {
        var telegramUserId = parsed.TelegramUserId!.Value;
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId, cancellationToken);
        if (account is null)
        {
            account = new TelegramAccount
            {
                TelegramUserId = telegramUserId,
                CreatedAt = _clock.UtcNow
            };
            _db.TelegramAccounts.Add(account);
        }

        account.Username = parsed.Username ?? account.Username;
        account.FirstName = parsed.FirstName ?? account.FirstName;
        account.LastName = parsed.LastName ?? account.LastName;
        account.LanguageCode = parsed.LanguageCode ?? account.LanguageCode;
        account.LastSeenAt = _clock.UtcNow;
        account.UpdatedAt = _clock.UtcNow;
        return account;
    }

    private async Task<string> RedactSensitiveTelegramRawBodyAsync(long? telegramUserId, string rawBody, CancellationToken cancellationToken)
    {
        if (!telegramUserId.HasValue)
        {
            return Redact(rawBody);
        }

        var now = _clock.UtcNow;
        var session = await _db.TelegramBotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId.Value, cancellationToken);
        if (session is not null && session.ExpiresAt <= now)
        {
            session = null;
        }
        if (session is null || !IsSensitiveProvisioningInputState(session.CurrentState))
        {
            return Redact(rawBody);
        }

        return RedactProvisioningCredentialPayload(rawBody);
    }

    private static bool IsSensitiveProvisioningInputState(string? state)
        => string.Equals(state, BotStates.OwnVpsAwaitingCredential, StringComparison.OrdinalIgnoreCase);

    private static string RedactProvisioningCredentialPayload(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return string.Empty;
        }

        try
        {
            var node = JsonNode.Parse(rawBody);
            if (node is JsonObject obj)
            {
                RedactProvisioningCredentialNode(obj);
                return node?.ToJsonString(JsonOptions) ?? "{\"redacted\":true}";
            }
        }
        catch
        {
            return "[redacted provisioning credential]";
        }

        return "[redacted provisioning credential]";
    }

    private static void RedactProvisioningCredentialNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(x => x.Key).ToList())
            {
                if (string.Equals(key, "text", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "caption", StringComparison.OrdinalIgnoreCase))
                {
                    obj[key] = "[redacted provisioning credential]";
                }
                else
                {
                    RedactProvisioningCredentialNode(obj[key]);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                RedactProvisioningCredentialNode(item);
            }
        }
    }

    private async Task StoreInboundMessageAsync(TelegramAccount account, ParsedTelegramUpdate parsed, string rawBody, CancellationToken cancellationToken)
    {
        if (parsed.CallbackQueryId is not null)
        {
            var exists = await _db.TelegramBotCallbackQueries.AnyAsync(x => x.CallbackQueryId == parsed.CallbackQueryId, cancellationToken);
            if (!exists)
            {
                _db.TelegramBotCallbackQueries.Add(new TelegramBotCallbackQuery
                {
                    CallbackQueryId = parsed.CallbackQueryId,
                    TelegramUserId = account.TelegramUserId,
                    Data = parsed.CallbackData ?? string.Empty,
                    RawPayload = Redact(rawBody),
                    IsProcessed = true,
                    ProcessedAt = _clock.UtcNow
                });
            }
            return;
        }

        if (parsed.MessageId.HasValue || !string.IsNullOrWhiteSpace(parsed.Text))
        {
            var now = _clock.UtcNow;
            var session = await _db.TelegramBotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.TelegramUserId == account.TelegramUserId, cancellationToken);
            if (session is not null && session.ExpiresAt <= now)
            {
                session = null;
            }
            var isSensitiveInput = session is not null && IsSensitiveProvisioningInputState(session.CurrentState);
            _db.TelegramBotMessages.Add(new TelegramBotMessage
            {
                TelegramAccount = account,
                TelegramUserId = account.TelegramUserId,
                ChatId = parsed.ChatId ?? account.TelegramUserId,
                MessageId = parsed.MessageId,
                Direction = "inbound",
                Text = isSensitiveInput ? "[redacted provisioning credential]" : parsed.Text ?? parsed.SuccessfulPaymentPayload ?? string.Empty,
                RawPayload = isSensitiveInput ? rawBody : Redact(rawBody)
            });
        }
    }

    private async Task<RouteResult> RouteAsync(TelegramAccount account, ParsedTelegramUpdate parsed, string rawBody, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(parsed.PreCheckoutQueryId))
        {
            return await HandlePreCheckoutQueryAsync(account, parsed, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(parsed.SuccessfulPaymentTelegramChargeId))
        {
            return await HandleSuccessfulPaymentAsync(account, parsed, rawBody, cancellationToken);
        }

        var text = (parsed.CallbackData ?? parsed.Text ?? string.Empty).Trim();
        var normalized = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        var payload = text.Length > normalized.Length ? text[(normalized.Length + 1)..].Trim() : string.Empty;

        if (normalized.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "/start", payload, cancellationToken);
            if (payload.StartsWith("link_", StringComparison.OrdinalIgnoreCase))
            {
                return new RouteResult(await LinkAccountAsync(account, payload[5..], cancellationToken), parsed.ChatId, LinkedMenuReplyMarkupJson());
            }

            return account.UserId.HasValue
                ? new RouteResult(LinkedMenuText(account), parsed.ChatId, LinkedMenuReplyMarkupJson())
                : new RouteResult(UnlinkedMenuText(), parsed.ChatId, UnlinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("register_tg", StringComparison.OrdinalIgnoreCase))
        {
            var registration = await RegisterTelegramUserAsync(account, cancellationToken);
            if (!registration.IsSuccess)
            {
                return new RouteResult(registration.Error ?? "Не удалось зарегистрироваться через Telegram.", parsed.ChatId, UnlinkedMenuReplyMarkupJson());
            }

            var pending = await GetSessionPayloadAsync(account.TelegramUserId, BotStates.WaitingForRegistration, cancellationToken);
            if (pending.TryGetPropertyValue("tariffId", out var tariffNode) && Guid.TryParse(tariffNode?.GetValue<string>(), out var tariffId))
            {
                return await CreateTelegramOrderAsync(account, tariffId, parsed.ChatId, cancellationToken);
            }

            if (pending.TryGetPropertyValue("intent", out var intentNode) && string.Equals(intentNode?.GetValue<string>(), "own_vps", StringComparison.OrdinalIgnoreCase))
            {
                return await StartOwnVpsFlowAsync(account, parsed.ChatId, cancellationToken);
            }

            return new RouteResult($"Аккаунт создан: {registration.Value!.DisplayName}.\n\n{MainMenuText()}", parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("link_help", StringComparison.OrdinalIgnoreCase) || normalized.Equals("link_account", StringComparison.OrdinalIgnoreCase))
        {
            await SetSessionStateAsync(account.TelegramUserId, BotStates.WaitingForLink, "{}", cancellationToken);
            return new RouteResult("Чтобы привязать существующий аккаунт: войдите в личный кабинет, нажмите «Привязать Telegram» и откройте deep link в этом боте.", parsed.ChatId, UnlinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("/help", StringComparison.OrdinalIgnoreCase) || normalized.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "/help", payload, cancellationToken);
            return new RouteResult(HelpText(), parsed.ChatId, account.UserId.HasValue ? LinkedMenuReplyMarkupJson() : UnlinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("/menu", StringComparison.OrdinalIgnoreCase) || normalized.Equals("menu", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "/menu", payload, cancellationToken);
            return account.UserId.HasValue
                ? new RouteResult(LinkedMenuText(account), parsed.ChatId, LinkedMenuReplyMarkupJson())
                : new RouteResult(UnlinkedMenuText(), parsed.ChatId, UnlinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("/subscriptions", StringComparison.OrdinalIgnoreCase) || normalized.Equals("subscriptions", StringComparison.OrdinalIgnoreCase) || normalized.Equals("my_subscriptions", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "/subscriptions", payload, cancellationToken);
            return new RouteResult(await BuildSubscriptionsTextAsync(account, cancellationToken), parsed.ChatId, BuildSubscriptionsKeyboard());
        }

        if (normalized.Equals("/orders", StringComparison.OrdinalIgnoreCase) || normalized.Equals("orders", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "/orders", payload, cancellationToken);
            return new RouteResult(await BuildOrdersTextAsync(account, cancellationToken), parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("/access", StringComparison.OrdinalIgnoreCase) || normalized.Equals("access", StringComparison.OrdinalIgnoreCase) || normalized.Equals("/keys", StringComparison.OrdinalIgnoreCase) || normalized.Equals("keys", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "/keys", payload, cancellationToken);
            return new RouteResult(await BuildAccessTextAsync(account, cancellationToken), parsed.ChatId, BuildAccessKeyboard());
        }

        if (normalized.Equals("cancel", StringComparison.OrdinalIgnoreCase) || normalized.Equals("/cancel", StringComparison.OrdinalIgnoreCase) || normalized.Equals("own_vps_cancel", StringComparison.OrdinalIgnoreCase))
        {
            await ClearSessionAsync(account.TelegramUserId, cancellationToken);
            return new RouteResult("Действие отменено.", parsed.ChatId, account.UserId.HasValue ? LinkedMenuReplyMarkupJson() : UnlinkedMenuReplyMarkupJson());
        }

        var now = _clock.UtcNow;
        var activeSession = await _db.TelegramBotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.TelegramUserId == account.TelegramUserId, cancellationToken);
        if (activeSession is not null && activeSession.ExpiresAt <= now)
        {
            activeSession = null;
        }
        if (IsOwnVpsState(activeSession?.CurrentState) || normalized.StartsWith("own_vps_auth:", StringComparison.OrdinalIgnoreCase) || normalized.Equals("own_vps_confirm", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleOwnVpsSessionAsync(account, parsed, normalized, text, activeSession, cancellationToken);
        }

        if (normalized.Equals("renew", StringComparison.OrdinalIgnoreCase) || normalized.Equals("/renew", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "/renew", payload, cancellationToken);
            return new RouteResult(await BuildRenewalTextAsync(account, cancellationToken), parsed.ChatId, await BuildRenewalKeyboardAsync(account, cancellationToken));
        }

        if (normalized.StartsWith("renew:", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(normalized[6..], out var subscriptionId))
            {
                return new RouteResult("Некорректная подписка для продления.", parsed.ChatId, await BuildRenewalKeyboardAsync(account, cancellationToken));
            }

            return await CreateTelegramRenewalOrderAsync(account, subscriptionId, parsed.ChatId, cancellationToken);
        }

        if (normalized.Equals("instruction", StringComparison.OrdinalIgnoreCase) || normalized.Equals("instructions", StringComparison.OrdinalIgnoreCase) || normalized.Equals("/instruction", StringComparison.OrdinalIgnoreCase) || normalized.Equals("/help_connect", StringComparison.OrdinalIgnoreCase))
        {
            return new RouteResult(InstructionText(), parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("profile", StringComparison.OrdinalIgnoreCase) || normalized.Equals("/profile", StringComparison.OrdinalIgnoreCase) || normalized.Equals("cabinet", StringComparison.OrdinalIgnoreCase))
        {
            return new RouteResult(await BuildProfileTextAsync(account, cancellationToken), parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("own_vps", StringComparison.OrdinalIgnoreCase) || normalized.Equals("vps", StringComparison.OrdinalIgnoreCase))
        {
            return await StartOwnVpsFlowAsync(account, parsed.ChatId, cancellationToken);
        }

        if (normalized.Equals("/support", StringComparison.OrdinalIgnoreCase) || normalized.Equals("support", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "/support", payload, cancellationToken);
            await EnsureSupportConversationAsync(account, "Пользователь открыл поддержку", "[]", false, cancellationToken);
            await SetSessionStateAsync(account.TelegramUserId, BotStates.SupportMode, "{}", cancellationToken);
            return new RouteResult("Напишите сообщение для поддержки. Можно приложить фото или документ — вложения сохранятся как metadata.", parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        if (normalized.Equals("tariffs", StringComparison.OrdinalIgnoreCase) || normalized.Equals("buy", StringComparison.OrdinalIgnoreCase) || normalized.Equals("/plans", StringComparison.OrdinalIgnoreCase) || normalized.Equals("/tariffs", StringComparison.OrdinalIgnoreCase) || text.Contains("Купить", StringComparison.OrdinalIgnoreCase))
        {
            await LogCommandAsync(account.TelegramUserId, parsed.UpdateId, "tariffs", text, cancellationToken);
            await SetSessionStateAsync(account.TelegramUserId, BotStates.BrowsingTariffs, "{}", cancellationToken);
            return new RouteResult(await BuildTariffsTextAsync(cancellationToken), parsed.ChatId, await BuildTariffsKeyboardAsync(cancellationToken));
        }

        if (normalized.StartsWith("buy:", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(normalized[4..], out var tariffId))
            {
                return new RouteResult("Некорректный тариф.", parsed.ChatId, await BuildTariffsKeyboardAsync(cancellationToken));
            }

            var tariff = await _db.Tariffs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tariffId && x.IsActive, cancellationToken);
            if (tariff is null)
            {
                return new RouteResult("Этот тариф больше недоступен. Выберите актуальный тариф из списка.", parsed.ChatId, await BuildTariffsKeyboardAsync(cancellationToken));
            }

            if (!account.UserId.HasValue)
            {
                await SetSessionStateAsync(account.TelegramUserId, BotStates.WaitingForRegistration, JsonSerializer.SerializeToNode(new { tariffId })!.ToJsonString(JsonOptions), cancellationToken);
                return new RouteResult("Для покупки нужно зарегистрироваться через Telegram или привязать существующий аккаунт.", parsed.ChatId, UnlinkedMenuReplyMarkupJson());
            }

            await SetSessionStateAsync(account.TelegramUserId, BotStates.ConfirmingOrder, JsonSerializer.SerializeToNode(new { tariffId })!.ToJsonString(JsonOptions), cancellationToken);
            return new RouteResult(BuildTariffConfirmationText(tariff), parsed.ChatId, BuildConfirmOrderKeyboard(tariffId));
        }

        if (normalized.StartsWith("confirm_order:", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(normalized[14..], out var tariffId))
            {
                return new RouteResult("Некорректный тариф.", parsed.ChatId, await BuildTariffsKeyboardAsync(cancellationToken));
            }

            return await CreateTelegramOrderAsync(account, tariffId, parsed.ChatId, cancellationToken);
        }

        if (normalized.StartsWith("pay:", StringComparison.OrdinalIgnoreCase))
        {
            return await HandlePaymentChoiceAsync(account, normalized, parsed.ChatId, cancellationToken);
        }

        if (normalized.StartsWith("checkpay:", StringComparison.OrdinalIgnoreCase))
        {
            return await HandlePaymentCheckAsync(account, normalized, parsed.ChatId, cancellationToken);
        }

        var session = activeSession;
        if (session?.CurrentState == BotStates.SupportMode && (!string.IsNullOrWhiteSpace(text) || parsed.HasAttachment))
        {
            await EnsureSupportConversationAsync(account, text, parsed.AttachmentsJson, false, cancellationToken);
            return new RouteResult("Сообщение передано в поддержку.", parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        return new RouteResult(HelpText(), parsed.ChatId, account.UserId.HasValue ? LinkedMenuReplyMarkupJson() : UnlinkedMenuReplyMarkupJson());
    }

    private static bool IsOwnVpsState(string? state)
        => state is BotStates.OwnVpsAwaitingHost
            or BotStates.OwnVpsAwaitingPort
            or BotStates.OwnVpsAwaitingUsername
            or BotStates.OwnVpsAwaitingAuthMethod
            or BotStates.OwnVpsAwaitingCredential
            or BotStates.OwnVpsAwaitingName
            or BotStates.OwnVpsAwaitingConfirmation;

    private async Task<RouteResult> StartOwnVpsFlowAsync(TelegramAccount account, long? chatId, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            await SetSessionStateAsync(account.TelegramUserId, BotStates.WaitingForRegistration, JsonSerializer.SerializeToNode(new { intent = "own_vps" })!.ToJsonString(JsonOptions), cancellationToken);
            return new RouteResult("Для заявки VPN на своём VPS нужно зарегистрироваться через Telegram или привязать аккаунт.", chatId, UnlinkedMenuReplyMarkupJson());
        }

        await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingHost, "{}", cancellationToken);
        return new RouteResult(OwnVpsRequirementsText(), chatId, BuildOwnVpsCancelKeyboard());
    }

    private async Task<RouteResult> HandleOwnVpsSessionAsync(TelegramAccount account, ParsedTelegramUpdate parsed, string normalized, string text, TelegramBotSession? session, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            return new RouteResult("Сначала зарегистрируйтесь через Telegram или привяжите аккаунт.", parsed.ChatId, UnlinkedMenuReplyMarkupJson());
        }

        var state = session?.CurrentState ?? string.Empty;
        var payload = await GetSessionPayloadAsync(account.TelegramUserId, state, cancellationToken);

        if (normalized.StartsWith("own_vps_auth:", StringComparison.OrdinalIgnoreCase))
        {
            var authMethod = ProvisioningService.NormalizeAuthMethod(normalized[13..]);
            if (authMethod != "password" && authMethod != "ssh_key")
            {
                return new RouteResult("Неподдерживаемый способ доступа. Выберите password или SSH key.", parsed.ChatId, BuildOwnVpsAuthKeyboard());
            }

            payload["authMethod"] = authMethod;
            await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingCredential, payload.ToJsonString(JsonOptions), cancellationToken);
            return new RouteResult(authMethod == "password"
                ? "Введите SSH-пароль. Мы не будем показывать его повторно, не сохраним его в Telegram messages и защитим перед записью. В validation mode live SSH не выполняется."
                : "Вставьте private SSH key. Мы не будем показывать его повторно, не сохраним его в Telegram messages и защитим перед записью. В validation mode live SSH не выполняется.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
        }

        if (normalized.Equals("own_vps_confirm", StringComparison.OrdinalIgnoreCase))
        {
            return await ConfirmOwnVpsAsync(account, parsed.ChatId, payload, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new RouteResult("Введите текстовое значение или нажмите /cancel.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
        }

        switch (state)
        {
            case BotStates.OwnVpsAwaitingHost:
                {
                    var host = ProvisioningService.NormalizeHost(text);
                    if (!ProvisioningService.IsValidHost(host))
                    {
                        return new RouteResult("Некорректный host/IP. Введите IPv4/IPv6 или DNS hostname без схемы https:// и без пути.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
                    }

                    payload["host"] = host;
                    await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingPort, payload.ToJsonString(JsonOptions), cancellationToken);
                    return new RouteResult("Введите SSH port. Обычно 22.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
                }
            case BotStates.OwnVpsAwaitingPort:
                {
                    if (!int.TryParse(text.Trim(), out var port) || port <= 0 || port > 65535)
                    {
                        return new RouteResult("Некорректный SSH port. Введите число от 1 до 65535.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
                    }

                    payload["sshPort"] = port;
                    await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingUsername, payload.ToJsonString(JsonOptions), cancellationToken);
                    return new RouteResult("Введите SSH username. Нужен root или пользователь с sudo.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
                }
            case BotStates.OwnVpsAwaitingUsername:
                {
                    var username = text.Trim();
                    if (string.IsNullOrWhiteSpace(username) || username.Length > 64 || username.Contains(' '))
                    {
                        return new RouteResult("Некорректный username. Введите SSH login без пробелов.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
                    }

                    payload["username"] = username;
                    await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingAuthMethod, payload.ToJsonString(JsonOptions), cancellationToken);
                    return new RouteResult("Выберите способ доступа к VPS.", parsed.ChatId, BuildOwnVpsAuthKeyboard());
                }
            case BotStates.OwnVpsAwaitingAuthMethod:
                {
                    var authMethod = ProvisioningService.NormalizeAuthMethod(text);
                    if (authMethod != "password" && authMethod != "ssh_key")
                    {
                        return new RouteResult("Неподдерживаемый способ доступа. Нажмите кнопку password или SSH key.", parsed.ChatId, BuildOwnVpsAuthKeyboard());
                    }

                    payload["authMethod"] = authMethod;
                    await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingCredential, payload.ToJsonString(JsonOptions), cancellationToken);
                    return new RouteResult("Введите credential. Значение будет защищено и не будет возвращаться в API/админку.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
                }
            case BotStates.OwnVpsAwaitingCredential:
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return new RouteResult("Credential пустой. Введите пароль или private key, либо /cancel.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
                    }

                    payload["credentialProtected"] = ProtectProvisioningCredential(text);
                    payload["credentialsConfigured"] = true;
                    await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingName, payload.ToJsonString(JsonOptions), cancellationToken);
                    return new RouteResult("Credential принят и скрыт. Введите имя/локацию сервера, например `Amsterdam VPS`, или отправьте `-`, чтобы использовать host.", parsed.ChatId, BuildOwnVpsCancelKeyboard());
                }
            case BotStates.OwnVpsAwaitingName:
                {
                    var displayName = text.Trim();
                    if (displayName == "-" || displayName.Equals("skip", StringComparison.OrdinalIgnoreCase))
                    {
                        displayName = string.Empty;
                    }

                    payload["displayName"] = displayName;
                    await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingConfirmation, payload.ToJsonString(JsonOptions), cancellationToken);
                    return new RouteResult(BuildOwnVpsConfirmationText(payload), parsed.ChatId, BuildOwnVpsConfirmKeyboard());
                }
            case BotStates.OwnVpsAwaitingConfirmation:
                return new RouteResult("Подтвердите запуск precheck кнопкой ниже или нажмите /cancel.", parsed.ChatId, BuildOwnVpsConfirmKeyboard());
            default:
                await SetSessionStateAsync(account.TelegramUserId, BotStates.OwnVpsAwaitingHost, "{}", cancellationToken);
                return new RouteResult(OwnVpsRequirementsText(), parsed.ChatId, BuildOwnVpsCancelKeyboard());
        }
    }

    private async Task<RouteResult> ConfirmOwnVpsAsync(TelegramAccount account, long? chatId, JsonObject payload, CancellationToken cancellationToken)
    {
        if (_provisioningService is null)
        {
            await EnsureSupportConversationAsync(account, "Own VPS provisioning requested, but ProvisioningService is not available in this environment", "[]", false, cancellationToken);
            return new RouteResult("ProvisioningService недоступен в текущем окружении. Мы создали обращение в поддержку.", chatId, LinkedMenuReplyMarkupJson());
        }

        var host = payload.TryGetPropertyValue("host", out var hostNode) ? hostNode?.GetValue<string>() ?? string.Empty : string.Empty;
        var sshPort = payload.TryGetPropertyValue("sshPort", out var portNode) && portNode is not null ? portNode.GetValue<int>() : 22;
        var username = payload.TryGetPropertyValue("username", out var userNode) ? userNode?.GetValue<string>() ?? string.Empty : string.Empty;
        var authMethod = payload.TryGetPropertyValue("authMethod", out var authNode) ? authNode?.GetValue<string>() ?? string.Empty : string.Empty;
        var credentialProtected = payload.TryGetPropertyValue("credentialProtected", out var credentialNode) ? credentialNode?.GetValue<string>() ?? string.Empty : string.Empty;
        var displayName = payload.TryGetPropertyValue("displayName", out var nameNode) ? nameNode?.GetValue<string>() : null;

        var result = await _provisioningService.CreateOwnVpsRequestAsync(new OwnVpsProvisioningCommand(
            account.UserId,
            account.TelegramUserId,
            host,
            sshPort,
            username,
            authMethod,
            credentialProtected,
            displayName,
            displayName,
            "telegram",
            AutoDeployAfterPrecheck: true,
            ValidationMode: true), cancellationToken);

        await ClearSessionAsync(account.TelegramUserId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            await EnsureSupportConversationAsync(account, $"Own VPS provisioning request failed: {result.Error}", "[]", false, cancellationToken);
            return new RouteResult($"Не удалось создать заявку на precheck: {result.Error}\n\nМы создали обращение в поддержку. Администратор увидит ошибку без ваших секретов.", chatId, LinkedMenuReplyMarkupJson());
        }

        await QueueTelegramNotificationAsync(account.TelegramUserId, "own_vps_request_created", $"Заявка VPN на своём VPS создана. Run ID: {result.Value.Id}. Precheck поставлен в очередь в safe validation mode. Live SSH/deploy не запускается.", cancellationToken, BuildOwnVpsStatusKeyboard(result.Value.Id));
        await _db.SaveChangesAsync(cancellationToken);
        return new RouteResult($"Заявка создана ✅\nRun ID: {result.Value.Id}\nСтатус: precheck_queued\n\nValidation mode: live SSH/deploy не выполняется без explicit flag. Я сообщу о результате precheck/deploy отдельным уведомлением. Секреты не будут отправлены обратно в чат.", chatId, BuildOwnVpsStatusKeyboard(result.Value.Id));
    }

    private string ProtectProvisioningCredential(string credential)
    {
        if (_secretProtector is not null)
        {
            return _secretProtector.Protect(credential.Trim());
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(credential.Trim()));
        return "validation-placeholder:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string OwnVpsRequirementsText()
        => "VPN на мой VPS — safe MVP.\n\nПеред precheck проверьте:\n• чистый VPS на Ubuntu/Debian;\n• root или sudo-доступ;\n• открытые SSH и VPN-порты;\n• стабильный IP или hostname;\n• не отправляйте доступы от production-сервера без отдельного согласования.\n\nСейчас включён validation mode: live SSH/deploy не выполняется без явного server flag. Пароль/ключ не будет показан повторно.\n\nВведите IP или hostname VPS.";

    private static string BuildOwnVpsConfirmationText(JsonObject payload)
    {
        var host = payload.TryGetPropertyValue("host", out var hostNode) ? hostNode?.GetValue<string>() ?? "—" : "—";
        var port = payload.TryGetPropertyValue("sshPort", out var portNode) && portNode is not null ? portNode.GetValue<int>().ToString(CultureInfo.InvariantCulture) : "22";
        var username = payload.TryGetPropertyValue("username", out var userNode) ? userNode?.GetValue<string>() ?? "—" : "—";
        var authMethod = payload.TryGetPropertyValue("authMethod", out var authNode) ? authNode?.GetValue<string>() ?? "—" : "—";
        var displayName = payload.TryGetPropertyValue("displayName", out var nameNode) ? nameNode?.GetValue<string>() ?? string.Empty : string.Empty;
        return $"Проверьте заявку:\nHost: {host}\nSSH port: {port}\nUsername: {username}\nAuth method: {authMethod}\nCredentials configured: yes\nName/location: {(string.IsNullOrWhiteSpace(displayName) ? "host" : displayName)}\n\nНажмите кнопку, чтобы создать provisioning request и запустить precheck в safe validation mode.";
    }

    private static string BuildOwnVpsAuthKeyboard()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Password", callback_data = "own_vps_auth:password" }, new { text = "SSH key", callback_data = "own_vps_auth:ssh_key" } },
                new object[] { new { text = "Отмена", callback_data = "own_vps_cancel" } }
            }
        }, JsonOptions);

    private static string BuildOwnVpsConfirmKeyboard()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Запустить precheck", callback_data = "own_vps_confirm" } },
                new object[] { new { text = "Отмена", callback_data = "own_vps_cancel" } }
            }
        }, JsonOptions);

    private static string BuildOwnVpsCancelKeyboard()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Отмена", callback_data = "own_vps_cancel" } }
            }
        }, JsonOptions);

    private static string BuildOwnVpsStatusKeyboard(Guid runId)
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Мои ключи", callback_data = "keys" }, new { text = "Поддержка", callback_data = "support" } },
                new object[] { new { text = "Меню", callback_data = "menu" } }
            }
        }, JsonOptions);

    public async Task<Result<CreateTelegramUserResultDto>> RegisterTelegramUserAsync(TelegramAccount account, CancellationToken cancellationToken = default)
    {
        if (account.UserId.HasValue)
        {
            var existing = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == account.UserId.Value, cancellationToken);
            if (existing is not null)
            {
                return Result<CreateTelegramUserResultDto>.Success(new CreateTelegramUserResultDto(existing.Id, false, existing.Email ?? string.Empty, existing.DisplayName));
            }
        }

        var placeholderEmail = $"tg_{account.TelegramUserId}@telegram.local";
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == placeholderEmail, cancellationToken);
        var created = false;
        if (user is null)
        {
            user = new User
            {
                Email = placeholderEmail,
                DisplayName = DisplayName(account),
                PasswordHash = string.Empty,
                RolesCsv = UserRoles.User,
                Status = UserStatus.Active,
                PreferredLanguage = string.IsNullOrWhiteSpace(account.LanguageCode) ? "ru" : account.LanguageCode,
                ReferralCode = $"TG{account.TelegramUserId}",
                AuthSource = AuthSource.Telegram,
                TelegramRegistrationCompletedAt = _clock.UtcNow,
                EmailConfirmed = false,
                MetadataJson = JsonSerializer.Serialize(new { placeholderEmail = true, telegramUserId = account.TelegramUserId })
            };
            _db.Users.Add(user);
            created = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        account.UserId = user.Id;
        account.LinkedAt ??= _clock.UtcNow;
        account.RegistrationCompletedAt ??= _clock.UtcNow;
        account.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<CreateTelegramUserResultDto>.Success(new CreateTelegramUserResultDto(user.Id, created, user.Email ?? string.Empty, user.DisplayName));
    }

    private async Task<RouteResult> CreateTelegramOrderAsync(TelegramAccount account, Guid tariffId, long? chatId, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            return new RouteResult("Для покупки нужно зарегистрироваться через Telegram или привязать аккаунт.", chatId, UnlinkedMenuReplyMarkupJson());
        }

        if (_orderService is null)
        {
            return new RouteResult("OrderService недоступен в текущем окружении.", chatId, LinkedMenuReplyMarkupJson());
        }

        var tariff = await _db.Tariffs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tariffId && x.IsActive, cancellationToken);
        if (tariff is null)
        {
            return new RouteResult("Этот тариф больше недоступен. Выберите актуальный тариф из списка.", chatId, await BuildTariffsKeyboardAsync(cancellationToken));
        }

        var now = _clock.UtcNow;
        var existingOrders = await _db.Orders.AsNoTracking()
            .Where(x => x.UserId == account.UserId.Value && x.TariffId == tariffId && x.Channel == ChannelType.Telegram && x.Status == OrderStatus.PendingPayment)
            .ToListAsync(cancellationToken);
        var existing = existingOrders
            .Where(x => x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        var reusedExistingOrder = existing is not null;
        var order = reusedExistingOrder
            ? OrderService.MapToDto(existing!)
            : (await _orderService.CreateOrderAsync(new CreateOrderCommand(account.UserId.Value, tariffId, OrderType.NewSubscription, ChannelType.Telegram, PaymentProvider.YooKassa, null, false), cancellationToken)).Value;

        if (order is null)
        {
            return new RouteResult("Не удалось создать заказ.", chatId, LinkedMenuReplyMarkupJson());
        }

        await ClearSessionAsync(account.TelegramUserId, cancellationToken);
        await SetSessionStateAsync(account.TelegramUserId, BotStates.ChoosingPaymentProvider, JsonSerializer.SerializeToNode(new { orderId = order.Id })!.ToJsonString(JsonOptions), cancellationToken);
        if (!reusedExistingOrder)
        {
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "OrderTimelineEvent",
                CorrelationId = order.Id.ToString("N"),
                PayloadJson = JsonSerializer.Serialize(new { orderId = order.Id, eventType = "CreatedFromTelegramBot", telegramUserId = account.TelegramUserId })
            });
            await QueueTelegramNotificationAsync(account.TelegramUserId, "order_created", $"Заказ {order.Id} создан. Сумма: {order.Amount:0.00} {order.Currency}.", cancellationToken);
        }
        await _db.SaveChangesAsync(cancellationToken);

        var providers = await GetAvailableBotPaymentProvidersAsync(cancellationToken);
        var action = reusedExistingOrder ? "Найден существующий заказ" : "Заказ создан";
        var providerPrompt = providers.Count == 0
            ? "\n\nСейчас нет доступных способов оплаты. Заказ сохранён: повторите оплату позже или напишите в поддержку."
            : "\n\nВыберите способ оплаты.";
        return new RouteResult($"{action}: {order.Id}\nСумма: {order.Amount:0.00} {order.Currency}\nСтатус: {order.Status}{providerPrompt}", chatId, BuildPaymentProvidersKeyboard(order.Id, providers));
    }

    private async Task<RouteResult> CreateTelegramRenewalOrderAsync(TelegramAccount account, Guid subscriptionId, long? chatId, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            return new RouteResult("Для продления нужно зарегистрироваться или привязать аккаунт.", chatId, UnlinkedMenuReplyMarkupJson());
        }

        if (_orderService is null)
        {
            return new RouteResult("OrderService недоступен в текущем окружении.", chatId, LinkedMenuReplyMarkupJson());
        }

        var subscription = await _db.Subscriptions
            .Include(x => x.Tariff)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId && x.UserId == account.UserId.Value && x.Status != SubscriptionStatus.Cancelled && x.Status != SubscriptionStatus.Blocked, cancellationToken);
        if (subscription is null || subscription.Tariff is null || !subscription.Tariff.IsActive)
        {
            return new RouteResult("Подписка для продления не найдена или тариф больше недоступен.", chatId, await BuildRenewalKeyboardAsync(account, cancellationToken));
        }

        var now = _clock.UtcNow;
        var existingOrders = await _db.Orders.AsNoTracking()
            .Where(x => x.UserId == account.UserId.Value && x.TariffId == subscription.TariffId && x.Type == OrderType.Renewal && x.Channel == ChannelType.Telegram && x.Status == OrderStatus.PendingPayment)
            .ToListAsync(cancellationToken);
        var existing = existingOrders
            .Where(x => x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        var reusedExistingOrder = existing is not null;
        var order = reusedExistingOrder
            ? OrderService.MapToDto(existing!)
            : (await _orderService.CreateOrderAsync(new CreateOrderCommand(account.UserId.Value, subscription.TariffId, OrderType.Renewal, ChannelType.Telegram, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: subscription.Id), cancellationToken)).Value;

        if (order is null)
        {
            return new RouteResult("Не удалось создать заказ на продление.", chatId, LinkedMenuReplyMarkupJson());
        }

        await ClearSessionAsync(account.TelegramUserId, cancellationToken);
        await SetSessionStateAsync(account.TelegramUserId, BotStates.ChoosingPaymentProvider, JsonSerializer.SerializeToNode(new { orderId = order.Id, subscriptionId })!.ToJsonString(JsonOptions), cancellationToken);
        if (!reusedExistingOrder)
        {
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "OrderTimelineEvent",
                CorrelationId = order.Id.ToString("N"),
                PayloadJson = JsonSerializer.Serialize(new { orderId = order.Id, eventType = "RenewalCreatedFromTelegramBot", telegramUserId = account.TelegramUserId, subscriptionId })
            });
            await QueueTelegramNotificationAsync(account.TelegramUserId, "renewal_order_created", $"Заказ на продление {order.Id} создан. Сумма: {order.Amount:0.00} {order.Currency}.", cancellationToken);
        }
        await _db.SaveChangesAsync(cancellationToken);

        var providers = await GetAvailableBotPaymentProvidersAsync(cancellationToken);
        var action = reusedExistingOrder ? "Найден существующий заказ на продление" : "Заказ на продление создан";
        var providerPrompt = providers.Count == 0
            ? "\n\nСейчас нет доступных способов оплаты. Напишите в поддержку или повторите позже — отключённые провайдеры скрыты специально."
            : "\n\nВыберите способ оплаты.";
        return new RouteResult($"{action}: {order.Id}\nПодписка: {subscription.Id}\nТариф: {subscription.Tariff.Name}\nСумма: {order.Amount:0.00} {order.Currency}{providerPrompt}", chatId, BuildPaymentProvidersKeyboard(order.Id, providers));
    }

    private async Task<RouteResult> HandlePaymentChoiceAsync(TelegramAccount account, string callback, long? chatId, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            return new RouteResult("Для оплаты нужно зарегистрироваться или привязать аккаунт. Это защищает заказ и последующую выдачу VPN-доступа.", chatId, UnlinkedMenuReplyMarkupJson());
        }

        var parts = callback.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
        {
            return new RouteResult("Некорректный способ оплаты. Выберите вариант из списка.", chatId, LinkedMenuReplyMarkupJson());
        }

        PaymentProvider provider;
        Guid orderId;
        if (Guid.TryParse(parts[1], out orderId) && Enum.TryParse<PaymentProvider>(parts[2], true, out provider))
        {
            // canonical pay:<orderId>:<provider>
        }
        else if (Enum.TryParse<PaymentProvider>(parts[1], true, out provider) && Guid.TryParse(parts[2], out orderId))
        {
            // backwards-compatible pay:<provider>:<orderId>
        }
        else
        {
            return new RouteResult("Некорректный способ оплаты. Выберите вариант из списка.", chatId, LinkedMenuReplyMarkupJson());
        }

        var ownsOrder = await _db.Orders.AnyAsync(x => x.Id == orderId && x.UserId == account.UserId.Value, cancellationToken);
        if (!ownsOrder)
        {
            return new RouteResult("Заказ не найден.", chatId, LinkedMenuReplyMarkupJson());
        }

        if (!await IsPaymentProviderAvailableForBotAsync(provider, cancellationToken))
        {
            return new RouteResult("Этот способ оплаты сейчас отключен или не настроен. Я покажу только доступные варианты.", chatId, await BuildPaymentProvidersKeyboardAsync(orderId, cancellationToken));
        }

        if (provider == PaymentProvider.TelegramStars)
        {
            return await PrepareTelegramStarsPaymentAsync(account, orderId, chatId, cancellationToken);
        }

        if (_paymentOrchestrator is null)
        {
            return new RouteResult("PaymentOrchestrator недоступен в текущем окружении.", chatId, LinkedMenuReplyMarkupJson());
        }

        var init = await _paymentOrchestrator.InitPaymentAsync(new PaymentInitCommand(orderId, provider), cancellationToken);
        if (!init.IsSuccess || init.Value is null)
        {
            return new RouteResult(init.Error ?? "Не удалось создать платеж.", chatId, await BuildPaymentProvidersKeyboardAsync(orderId, cancellationToken));
        }

        var payment = await _db.Payments.AsNoTracking().Where(x => x.ProviderPaymentId == init.Value.PaymentId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        await SetSessionStateAsync(account.TelegramUserId, BotStates.WaitingForPayment, JsonSerializer.SerializeToNode(new { orderId, provider = provider.ToString(), paymentId = payment?.Id })!.ToJsonString(JsonOptions), cancellationToken);
        await QueueTelegramNotificationAsync(account.TelegramUserId, "payment_pending", $"Платеж ожидает оплаты через {provider}. Заказ {orderId}.", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new RouteResult($"Платеж создан через {provider}. Нажмите «Оплатить», завершите оплату и вернитесь сюда. Повторный webhook не продублирует подписку или VPN-доступ.", chatId, BuildPaymentLinkKeyboard(init.Value.RedirectUrl, payment?.Id ?? Guid.Empty, orderId));
    }

    private async Task<RouteResult> PrepareTelegramStarsPaymentAsync(TelegramAccount account, Guid orderId, long? chatId, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == account.UserId, cancellationToken);
        if (order is null)
        {
            return new RouteResult("Заказ не найден.", chatId, LinkedMenuReplyMarkupJson());
        }

        var existing = await _db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId && x.Provider == PaymentProvider.TelegramStars && (x.Status == PaymentStatus.New || x.Status == PaymentStatus.Pending), cancellationToken);
        if (existing is null)
        {
            existing = new PaymentAttempt
            {
                OrderId = order.Id,
                Provider = PaymentProvider.TelegramStars,
                ProviderMode = PaymentProviderMode.Production,
                ProviderPaymentId = $"tgstars_{Guid.NewGuid():N}",
                IdempotencyKey = HashRawPayload($"tgstars:{order.Id:N}"),
                Amount = order.Amount,
                Currency = "XTR",
                Status = PaymentStatus.Pending,
                RawRequest = JsonSerializer.Serialize(new { orderId = order.Id, amount = order.Amount, currency = "XTR" })
            };
            _db.Payments.Add(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var payload = $"tgstars:{existing.Id:N}";
        if (_invoiceProvider is null || !chatId.HasValue)
        {
            return new RouteResult($"Telegram Stars invoice не настроен. Платеж подготовлен без fake-success: payload {payload}. Настройте BotToken в админке или конфигурации API либо выберите внешний платежный провайдер.", chatId, await BuildPaymentProvidersKeyboardAsync(orderId, cancellationToken));
        }

        var amount = decimal.Round(existing.Amount, 0, MidpointRounding.AwayFromZero);
        if (amount != existing.Amount || amount <= 0 || amount > int.MaxValue)
        {
            return new RouteResult("Telegram Stars требует положительную целую сумму XTR. Настройте отдельный Stars tariff/price mapping.", chatId, await BuildPaymentProvidersKeyboardAsync(orderId, cancellationToken));
        }

        try
        {
            await _invoiceProvider.CreateInvoiceAsync(new TelegramInvoiceRequest(
                order.Id,
                existing.Id,
                chatId.Value,
                $"VPN {order.Id.ToString("N")[..8]}",
                $"VPN subscription order {order.Id}",
                payload,
                "XTR",
                (int)amount), cancellationToken);
            return new RouteResult("Telegram Stars invoice отправлен. После оплаты бот обработает successful_payment.", chatId, LinkedMenuReplyMarkupJson());
        }
        catch (Exception ex)
        {
            existing.StatusReason = ex.Message;
            existing.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return new RouteResult("Telegram Stars invoice не отправлен: " + ex.Message, chatId, await BuildPaymentProvidersKeyboardAsync(orderId, cancellationToken));
        }
    }

    private async Task<RouteResult> HandlePaymentCheckAsync(TelegramAccount account, string callback, long? chatId, CancellationToken cancellationToken)
    {
        var idText = callback.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1).FirstOrDefault();
        if (!Guid.TryParse(idText, out var paymentId))
        {
            return new RouteResult("Платеж еще не сохранен. Проверьте историю заказов через /orders.", chatId, LinkedMenuReplyMarkupJson());
        }

        var payment = await _db.Payments.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == paymentId && x.Order != null && x.Order.UserId == account.UserId, cancellationToken);
        if (payment is null)
        {
            return new RouteResult("Платеж не найден.", chatId, LinkedMenuReplyMarkupJson());
        }

        if (_paymentOrchestrator is not null && payment.Provider == PaymentProvider.YooKassa)
        {
            var recheck = await _paymentOrchestrator.RecheckPaymentAsync(payment.Id, cancellationToken);
            if (recheck.IsSuccess)
            {
                payment = await _db.Payments.Include(x => x.Order).FirstAsync(x => x.Id == paymentId, cancellationToken);
            }
        }

        return new RouteResult($"Платеж {payment.Provider}: {payment.Status}. Заказ: {payment.Order?.Status}.", chatId, LinkedMenuReplyMarkupJson());
    }

    private async Task<RouteResult> HandlePreCheckoutQueryAsync(TelegramAccount account, ParsedTelegramUpdate parsed, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parsed.SuccessfulPaymentPayload) && string.IsNullOrWhiteSpace(parsed.InvoicePayload))
        {
            return new RouteResult(string.Empty, parsed.ChatId, null, parsed.PreCheckoutQueryId, false, "Invoice payload is missing.");
        }

        var payload = parsed.InvoicePayload ?? parsed.SuccessfulPaymentPayload ?? string.Empty;
        if (!payload.StartsWith("tgstars:", StringComparison.OrdinalIgnoreCase) || !Guid.TryParse(payload[8..], out var paymentId))
        {
            return new RouteResult(string.Empty, parsed.ChatId, null, parsed.PreCheckoutQueryId, false, "Unsupported invoice payload.");
        }

        var payment = await _db.Payments.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == paymentId && x.Provider == PaymentProvider.TelegramStars, cancellationToken);
        if (payment is null || payment.Order is null || payment.Order.UserId != account.UserId)
        {
            return new RouteResult(string.Empty, parsed.ChatId, null, parsed.PreCheckoutQueryId, false, "Payment not found.");
        }

        var validationError = ValidateTelegramStarsPayload(payment, parsed.SuccessfulPaymentTotalAmount, parsed.SuccessfulPaymentCurrency);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return new RouteResult(string.Empty, parsed.ChatId, null, parsed.PreCheckoutQueryId, false, validationError);
        }

        return new RouteResult(string.Empty, parsed.ChatId, null, parsed.PreCheckoutQueryId, true, null);
    }

    private async Task<RouteResult> HandleSuccessfulPaymentAsync(TelegramAccount account, ParsedTelegramUpdate parsed, string rawBody, CancellationToken cancellationToken)
    {
        var chargeId = parsed.SuccessfulPaymentTelegramChargeId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            return new RouteResult("Получен successful_payment без telegram_payment_charge_id.", parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        var existingPaymentEvent = await _db.TelegramBotPayments.AsNoTracking().FirstOrDefaultAsync(x => x.TelegramPaymentChargeId == chargeId, cancellationToken);
        if (existingPaymentEvent is not null)
        {
            return new RouteResult("Платеж Telegram Stars уже обработан.", parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        var payload = parsed.SuccessfulPaymentPayload ?? string.Empty;
        if (!payload.StartsWith("tgstars:", StringComparison.OrdinalIgnoreCase) || !Guid.TryParse(payload[8..], out var paymentId))
        {
            return new RouteResult("Получен successful_payment с неподдерживаемым payload. Платеж сохранен для ручной проверки.", parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        var payment = await _db.Payments.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == paymentId && x.Provider == PaymentProvider.TelegramStars, cancellationToken);
        if (payment is null || payment.Order is null || payment.Order.UserId != account.UserId)
        {
            return new RouteResult("Платеж Telegram Stars не найден или не принадлежит пользователю.", parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        _db.TelegramBotPayments.Add(new TelegramBotPayment
        {
            PaymentAttemptId = payment.Id,
            TelegramUserId = account.TelegramUserId,
            ProviderPaymentChargeId = parsed.SuccessfulPaymentProviderChargeId ?? string.Empty,
            TelegramPaymentChargeId = chargeId,
            InvoicePayload = payload,
            TotalAmount = parsed.SuccessfulPaymentTotalAmount ?? 0,
            Currency = parsed.SuccessfulPaymentCurrency ?? "XTR",
            RawPayload = Redact(rawBody)
        });

        var validationError = ValidateTelegramStarsPayload(payment, parsed.SuccessfulPaymentTotalAmount, parsed.SuccessfulPaymentCurrency);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            payment.StatusReason = validationError;
            payment.WebhookPayload = Redact(rawBody);
            await _db.SaveChangesAsync(cancellationToken);
            return new RouteResult($"Платеж Telegram Stars отклонен: {validationError}", parsed.ChatId, LinkedMenuReplyMarkupJson());
        }

        var responseText = "Оплата Telegram Stars получена. Проверьте /subscriptions и /keys.";
        var responseKeyboard = LinkedMenuReplyMarkupJson();
        if (payment.Status != PaymentStatus.Succeeded)
        {
            var now = _clock.UtcNow;
            var paymentStatus = StatusStateMachine.TrySetPaymentStatus(payment, PaymentStatus.Succeeded, now);
            if (!paymentStatus.IsSuccess)
            {
                payment.StatusReason = paymentStatus.Error ?? string.Empty;
                payment.WebhookPayload = Redact(rawBody);
                await _db.SaveChangesAsync(cancellationToken);
                return new RouteResult(paymentStatus.Error ?? "Telegram Stars payment status transition is not allowed.", parsed.ChatId, LinkedMenuReplyMarkupJson());
            }

            payment.PaidAt = now;
            payment.SignatureValidated = true;
            payment.ExternalEventId = chargeId;
            payment.WebhookPayload = Redact(rawBody);
            var orderStatus = StatusStateMachine.TrySetOrderStatus(payment.Order, OrderStatus.PaymentReceived, now);
            if (!orderStatus.IsSuccess)
            {
                payment.StatusReason = orderStatus.Error ?? string.Empty;
                await _db.SaveChangesAsync(cancellationToken);
                return new RouteResult(orderStatus.Error ?? "Telegram Stars order status transition is not allowed.", parsed.ChatId, LinkedMenuReplyMarkupJson());
            }

            payment.Order.PaidAt = now;

            if (!payment.IsActivationProcessed && _subscriptionService is not null)
            {
                var activation = await _subscriptionService.ActivateOrRenewFromOrderAsync(payment.Order, payment, cancellationToken);
                if (activation.IsSuccess)
                {
                    payment.IsActivationProcessed = true;
                    payment.ActivationProcessedAt = now;
                    responseText = await BuildActivatedAccessTextAsync(payment.Order, activation.Value!, cancellationToken);
                    responseKeyboard = BuildPostPaymentReplyMarkupJson();
                    await QueueTelegramNotificationAsync(account.TelegramUserId, "subscription_activated", responseText, cancellationToken, responseKeyboard);
                }
                else
                {
                    payment.StatusReason = activation.Error ?? string.Empty;
                    StatusStateMachine.SetOrderStatus(payment.Order, OrderStatus.PartiallyProcessed, now);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new RouteResult(responseText, parsed.ChatId, responseKeyboard);
    }

    private static string? ValidateTelegramStarsPayload(PaymentAttempt payment, long? totalAmount, string? currency)
    {
        if (!string.Equals(payment.Currency, "XTR", StringComparison.OrdinalIgnoreCase))
        {
            return "PaymentAttempt currency is not configured for Telegram Stars.";
        }

        if (!string.Equals(currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return "Telegram Stars currency does not match the PaymentAttempt.";
        }

        if (!totalAmount.HasValue)
        {
            return "Telegram Stars total_amount is missing.";
        }

        var roundedAmount = decimal.Round(payment.Amount, 0, MidpointRounding.AwayFromZero);
        if (roundedAmount != payment.Amount || roundedAmount <= 0 || roundedAmount > long.MaxValue)
        {
            return "Telegram Stars amount must be a positive whole number of XTR.";
        }

        if (totalAmount.Value != (long)roundedAmount)
        {
            return "Telegram Stars amount does not match the PaymentAttempt.";
        }

        return null;
    }

    private async Task<string> BuildSubscriptionsTextAsync(TelegramAccount account, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            return "Сначала зарегистрируйтесь через Telegram или привяжите аккаунт.";
        }

        var subscriptions = await _db.Subscriptions.AsNoTracking()
            .Include(x => x.Tariff)
            .Where(x => x.UserId == account.UserId.Value)
            .OrderByDescending(x => x.EndAt)
            .Take(5)
            .ToListAsync(cancellationToken);
        if (subscriptions.Count == 0)
        {
            return "Активных подписок пока нет. Нажмите «Купить VPN», чтобы выбрать тариф.";
        }

        var lines = subscriptions.Select(x =>
        {
            var tariffName = x.Tariff?.Name ?? x.TariffId.ToString("N")[..8];
            var marker = x.Status == SubscriptionStatus.Active && x.EndAt >= _clock.UtcNow ? "✅" : x.Status == SubscriptionStatus.Expired ? "⌛" : "•";
            return $"{marker} {tariffName}\nПодписка: {x.Id}\nСтатус: {x.Status}\nДействует до: {x.EndAt:yyyy-MM-dd HH:mm} UTC";
        });
        return "Мои подписки:\n\n" + string.Join("\n\n", lines);
    }

    private async Task<string> BuildOrdersTextAsync(TelegramAccount account, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            return "Сначала зарегистрируйтесь через Telegram или привяжите аккаунт.";
        }

        var orders = await _db.Orders.AsNoTracking()
            .Where(x => x.UserId == account.UserId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);
        return orders.Count == 0
            ? "Заказов пока нет."
            : string.Join("\n", orders.Select(x => $"Заказ {x.Id}: {x.Status}, {x.Amount:0.00} {x.Currency}"));
    }

    private async Task<string> BuildAccessTextAsync(TelegramAccount account, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            return "Сначала зарегистрируйтесь через Telegram или привяжите аккаунт.";
        }

        var activeSubscriptions = await _db.Subscriptions.AsNoTracking()
            .Include(x => x.Tariff)
            .Where(x => x.UserId == account.UserId.Value && x.Status == SubscriptionStatus.Active)
            .OrderByDescending(x => x.EndAt)
            .Take(3)
            .ToListAsync(cancellationToken);
        if (activeSubscriptions.Count == 0)
        {
            return "Активных подписок пока нет. Нажмите «Купить VPN», чтобы выбрать тариф.";
        }

        var subscriptionIds = activeSubscriptions.Select(x => x.Id).ToList();
        var accesses = await _db.AccessCredentials.AsNoTracking()
            .Where(x => subscriptionIds.Contains(x.SubscriptionId) && x.Status == AccessCredentialStatus.Active)
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync(cancellationToken);
        var clients = await _db.VpnClients.AsNoTracking()
            .Where(x => subscriptionIds.Contains(x.SubscriptionId))
            .ToListAsync(cancellationToken);

        var lines = new List<string>();
        foreach (var subscription in activeSubscriptions)
        {
            var tariffName = subscription.Tariff?.Name ?? subscription.TariffId.ToString("N")[..8];
            var access = accesses.FirstOrDefault(x => x.SubscriptionId == subscription.Id);
            var client = clients.FirstOrDefault(x => x.SubscriptionId == subscription.Id);
            if (client?.SyncStatus == "RequiresAdminReview")
            {
                lines.Add($"{tariffName} / {subscription.Id}: доступ требует проверки администратором. Поддержка уведомлена.");
                continue;
            }

            if (access is null || string.IsNullOrWhiteSpace(access.AccessUri))
            {
                lines.Add($"{tariffName} / {subscription.Id}: доступ готовится. Мы отправим уведомление, когда config будет создан.");
                continue;
            }

            lines.Add($"{tariffName}\nПодписка: {subscription.Id}\nСтатус: {subscription.Status}\nДействует до: {subscription.EndAt:yyyy-MM-dd HH:mm} UTC\nVPN URI:\n{access.AccessUri}\nQR payload:\n{access.QrCodePath}\n\nИнструкция: импортируйте URI или QR payload в совместимый VLESS/Xray-клиент. Не пересылайте ключ третьим лицам.");
        }

        return "Мои ключи:\n\n" + string.Join("\n\n", lines);
    }

    private async Task<string> BuildTariffsTextAsync(CancellationToken cancellationToken)
    {
        var tariffs = await _db.Tariffs.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).Take(8).ToListAsync(cancellationToken);
        return tariffs.Count == 0
            ? "Активных тарифов пока нет. Администратор ещё не включил тарифы для Telegram витрины."
            : "Выберите тариф. Отключённые тарифы скрыты:\n" + string.Join("\n", tariffs.Select(x => $"• {x.Name}: {x.Price:0.00} {x.Currency}, {x.DurationDays} дней. {x.Description}"));
    }

    private async Task<string> BuildTariffsKeyboardAsync(CancellationToken cancellationToken)
    {
        var tariffs = await _db.Tariffs.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).Take(8).ToListAsync(cancellationToken);
        var rows = tariffs.Select(x => new[] { new { text = $"Купить {x.Name}", callback_data = $"buy:{x.Id}" } }).ToList();
        rows.Add(new[] { new { text = "Назад в меню", callback_data = "menu" } });
        return JsonSerializer.Serialize(new { inline_keyboard = rows }, JsonOptions);
    }

    private async Task<string> BuildPaymentProvidersKeyboardAsync(Guid orderId, CancellationToken cancellationToken)
        => BuildPaymentProvidersKeyboard(orderId, await GetAvailableBotPaymentProvidersAsync(cancellationToken));

    private static string BuildPaymentProvidersKeyboard(Guid orderId, IReadOnlyCollection<PaymentProvider> providers)
    {
        var rows = new List<object[]>();
        foreach (var provider in providers)
        {
            rows.Add(new object[]
            {
                new
                {
                    text = PaymentProviderDisplayName(provider),
                    callback_data = $"pay:{orderId}:{provider}"
                }
            });
        }

        if (rows.Count == 0)
        {
            rows.Add(new object[] { new { text = "Платежные методы временно недоступны", callback_data = "orders" } });
        }

        rows.Add(new object[] { new { text = "Мои заказы", callback_data = "orders" } });
        rows.Add(new object[] { new { text = "Отмена", callback_data = "cancel" } });
        return JsonSerializer.Serialize(new { inline_keyboard = rows }, JsonOptions);
    }

    private async Task<IReadOnlyCollection<PaymentProvider>> GetAvailableBotPaymentProvidersAsync(CancellationToken cancellationToken)
    {
        var accounts = await _db.PaymentProviderAccounts.AsNoTracking()
            .ToListAsync(cancellationToken);

        return accounts
            .Where(PaymentProviderConfigurationRules.IsBotCheckoutConfigured)
            .Select(x => x.Provider)
            .Distinct()
            .OrderBy(PaymentProviderSortOrder)
            .ThenBy(x => x.ToString())
            .ToList();
    }

    private async Task<bool> IsPaymentProviderAvailableForBotAsync(PaymentProvider provider, CancellationToken cancellationToken)
    {
        var accounts = await _db.PaymentProviderAccounts.AsNoTracking()
            .Where(x => x.Provider == provider)
            .ToListAsync(cancellationToken);

        return accounts.Any(PaymentProviderConfigurationRules.IsBotCheckoutConfigured);
    }

    private static int PaymentProviderSortOrder(PaymentProvider provider)
    {
        var index = Array.IndexOf(PreferredPaymentProviderOrder, provider);
        return index >= 0 ? index : PreferredPaymentProviderOrder.Length + (int)provider;
    }

    private static string PaymentProviderDisplayName(PaymentProvider provider)
        => provider == PaymentProvider.TelegramStars ? "Telegram Stars" : provider.ToString();

    private static string BuildTariffConfirmationText(Tariff tariff)
        => $"Подтвердите заказ:\n{tariff.Name}\n{tariff.Description}\nЦена: {tariff.Price:0.00} {tariff.Currency}\nПериод: {tariff.DurationDays} дней";

    private static string BuildSubscriptionsKeyboard()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Продлить доступ", callback_data = "renew" }, new { text = "Мои ключи", callback_data = "keys" } },
                new object[] { new { text = "Купить VPN", callback_data = "tariffs" }, new { text = "Меню", callback_data = "menu" } }
            }
        }, JsonOptions);

    private async Task<string> BuildRenewalTextAsync(TelegramAccount account, CancellationToken cancellationToken)
    {
        if (!account.UserId.HasValue)
        {
            return "Сначала зарегистрируйтесь через Telegram или привяжите аккаунт.";
        }

        var subscriptions = await GetRenewableSubscriptionsAsync(account.UserId.Value, cancellationToken);
        if (subscriptions.Count == 0)
        {
            return "Нет подписок, доступных для продления. Нажмите «Купить VPN», чтобы создать новую подписку.";
        }

        var lines = subscriptions.Select(x => $"• {x.Tariff?.Name ?? x.TariffId.ToString("N")[..8]} — {x.Status}, до {x.EndAt:yyyy-MM-dd HH:mm} UTC");
        return "Мои подписки:\nВыберите подписку для продления:\n" + string.Join("\n", lines);
    }

    private async Task<string> BuildRenewalKeyboardAsync(TelegramAccount account, CancellationToken cancellationToken)
    {
        var rows = new List<object[]>();
        if (account.UserId.HasValue)
        {
            var subscriptions = await GetRenewableSubscriptionsAsync(account.UserId.Value, cancellationToken);
            foreach (var subscription in subscriptions)
            {
                var tariffName = subscription.Tariff?.Name ?? subscription.TariffId.ToString("N")[..8];
                rows.Add(new object[] { new { text = $"Продлить {tariffName}", callback_data = $"renew:{subscription.Id}" } });
            }
        }

        if (rows.Count == 0)
        {
            rows.Add(new object[] { new { text = "Купить VPN", callback_data = "tariffs" } });
        }

        rows.Add(new object[] { new { text = "Мои подписки", callback_data = "subscriptions" }, new { text = "Меню", callback_data = "menu" } });
        return JsonSerializer.Serialize(new { inline_keyboard = rows }, JsonOptions);
    }

    private async Task<List<Subscription>> GetRenewableSubscriptionsAsync(Guid userId, CancellationToken cancellationToken)
        => await _db.Subscriptions.AsNoTracking()
            .Include(x => x.Tariff)
            .Where(x => x.UserId == userId
                && x.Status != SubscriptionStatus.Cancelled
                && x.Status != SubscriptionStatus.Blocked
                && x.Tariff != null
                && x.Tariff.IsActive)
            .OrderByDescending(x => x.EndAt)
            .Take(5)
            .ToListAsync(cancellationToken);

    private async Task<string> BuildProfileTextAsync(TelegramAccount account, CancellationToken cancellationToken)
    {
        var userText = account.UserId.HasValue
            ? await _db.Users.AsNoTracking()
                .Where(x => x.Id == account.UserId.Value)
                .Select(x => $"Аккаунт: {x.DisplayName} ({x.Email})")
                .FirstOrDefaultAsync(cancellationToken)
            : "Аккаунт не привязан.";

        return $"Профиль Telegram:\nID: {account.TelegramUserId}\nUsername: @{account.Username ?? "—"}\nСтатус: {(account.IsBlocked ? "blocked" : "active")}\n{userText ?? "Аккаунт не найден."}";
    }

    private static string InstructionText()
        => "Инструкция подключения:\n1. Откройте «Мои ключи».\n2. Скопируйте VPN URI или QR payload.\n3. Импортируйте его в совместимый VLESS/Xray-клиент.\n4. Подключитесь и проверьте доступ.\n\nНе публикуйте свой ключ. Если ссылка не импортируется — нажмите «Поддержка».";

    private static string BuildPostPaymentReplyMarkupJson()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Мои ключи", callback_data = "keys" }, new { text = "Мои подписки", callback_data = "subscriptions" } },
                new object[] { new { text = "Продлить", callback_data = "renew" }, new { text = "Поддержка", callback_data = "support" } }
            }
        }, JsonOptions);

    private async Task<string> BuildActivatedAccessTextAsync(Order order, ActivationResult activation, CancellationToken cancellationToken)
    {
        var subscription = await _db.Subscriptions.AsNoTracking()
            .Include(x => x.Tariff)
            .FirstOrDefaultAsync(x => x.Id == activation.SubscriptionId, cancellationToken);
        AccessCredential? access = null;
        if (activation.AccessId.HasValue)
        {
            access = await _db.AccessCredentials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == activation.AccessId.Value, cancellationToken);
        }

        access ??= await _db.AccessCredentials.AsNoTracking()
            .Where(x => x.SubscriptionId == activation.SubscriptionId)
            .OrderByDescending(x => x.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var tariffName = subscription?.Tariff?.Name ?? order.TariffId.ToString("N")[..8];
        var until = subscription is null ? "—" : subscription.EndAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
        var accessUri = access is null || string.IsNullOrWhiteSpace(access.AccessUri) ? "Доступ готовится. Мы отправим ключ отдельным сообщением." : access.AccessUri;
        var qrPayload = access is null || string.IsNullOrWhiteSpace(access.QrCodePath) ? "QR payload пока не создан." : access.QrCodePath;

        return $"Оплата получена ✅\nЗаказ: {order.Id}\nТариф: {tariffName}\nПодписка действует до: {until}\n\nВаш VPN URI:\n{accessUri}\n\nQR payload:\n{qrPayload}\n\nИнструкция: импортируйте URI или QR payload в совместимый VLESS/Xray-клиент. Не пересылайте ключ третьим лицам. Если возникнут проблемы — нажмите «Поддержка».";
    }

    private static string BuildConfirmOrderKeyboard(Guid tariffId)
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new[]
            {
                new[] { new { text = "Подтвердить заказ", callback_data = $"confirm_order:{tariffId}" } },
                new[] { new { text = "Назад к тарифам", callback_data = "tariffs" }, new { text = "Отмена", callback_data = "cancel" } }
            }
        }, JsonOptions);

    private static string BuildAccessKeyboard()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new[]
            {
                new[] { new { text = "Продлить", callback_data = "renew" }, new { text = "Поддержка", callback_data = "support" } },
                new[] { new { text = "Мои подписки", callback_data = "subscriptions" }, new { text = "Меню", callback_data = "menu" } }
            }
        }, JsonOptions);

    private static string BuildPaymentLinkKeyboard(string paymentUrl, Guid paymentId, Guid orderId)
    {
        var checkId = paymentId == Guid.Empty ? orderId : paymentId;
        return JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Оплатить", url = paymentUrl } },
                new object[] { new { text = "Проверить оплату", callback_data = $"checkpay:{checkId}" }, new { text = "Мои заказы", callback_data = "orders" } }
            }
        }, JsonOptions);
    }

    private async Task<string> LinkAccountAsync(TelegramAccount account, string token, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);
        var link = await _db.TelegramBotDeepLinks.FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.Purpose == "link_account", cancellationToken);
        if (link is null)
        {
            return "Код привязки не найден или уже недействителен.";
        }

        if (link.ExpiresAt <= _clock.UtcNow)
        {
            return "Код привязки истек. Создайте новый код в личном кабинете.";
        }

        if (link.UsedAt.HasValue)
        {
            return "Код привязки уже использован.";
        }

        if (!link.UserId.HasValue)
        {
            return "Код привязки поврежден: не указан пользователь.";
        }

        if (account.UserId.HasValue && account.UserId.Value != link.UserId.Value)
        {
            return "Этот Telegram уже привязан к другому аккаунту.";
        }

        if (account.UserId.HasValue && account.UserId.Value == link.UserId.Value)
        {
            link.UsedAt ??= _clock.UtcNow;
            link.UsedByTelegramUserId ??= account.TelegramUserId;
            link.UpdatedAt = _clock.UtcNow;
            return "Этот Telegram уже привязан к вашему аккаунту.";
        }

        var userAlreadyLinked = await _db.TelegramAccounts.AnyAsync(x => x.UserId == link.UserId.Value && x.TelegramUserId != account.TelegramUserId, cancellationToken);
        if (userAlreadyLinked)
        {
            return "У этого аккаунта уже есть привязанный Telegram.";
        }

        account.UserId = link.UserId.Value;
        account.LinkedAt = _clock.UtcNow;
        account.UpdatedAt = _clock.UtcNow;
        link.UsedAt = _clock.UtcNow;
        link.UsedByTelegramUserId = account.TelegramUserId;
        link.UpdatedAt = _clock.UtcNow;
        return "Telegram успешно привязан к аккаунту.\n\n" + MainMenuText();
    }

    private async Task EnsureSupportConversationAsync(TelegramAccount account, string text, string attachmentsJson, bool isInternalNote, CancellationToken cancellationToken)
    {
        var conversation = await _db.SupportConversations
            .Include(x => x.Messages)
            .Where(x => x.TelegramUserId == account.TelegramUserId && x.Status == "open")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            conversation = new SupportConversation
            {
                UserId = account.UserId,
                TelegramUserId = account.TelegramUserId,
                Channel = "telegram",
                Status = "open",
                Subject = "Telegram support"
            };
            _db.SupportConversations.Add(conversation);
        }

        if (!string.IsNullOrWhiteSpace(text) || attachmentsJson != "[]")
        {
            _db.SupportMessages.Add(new SupportMessage
            {
                SupportConversation = conversation,
                UserId = account.UserId,
                TelegramUserId = account.TelegramUserId,
                Direction = "inbound",
                Text = string.IsNullOrWhiteSpace(text) ? "[attachment]" : text,
                AttachmentsJson = attachmentsJson,
                IsInternalNote = isInternalNote
            });
            conversation.UpdatedAt = _clock.UtcNow;
        }
    }

    private async Task QueueTelegramNotificationAsync(long telegramUserId, string type, string text, CancellationToken cancellationToken, string? replyMarkupJson = null)
    {
        var isBlocked = await _db.TelegramAccounts.AsNoTracking().AnyAsync(x => x.TelegramUserId == telegramUserId && x.IsBlocked, cancellationToken);
        if (isBlocked)
        {
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new { text, replyMarkupJson }, JsonOptions);
        var alreadyQueued = await _db.TelegramBotNotifications.AsNoTracking()
            .AnyAsync(x => x.TelegramUserId == telegramUserId && x.Type == type && x.PayloadJson == payloadJson && x.Status != "failed" && x.Status != "cancelled", cancellationToken);
        if (alreadyQueued)
        {
            return;
        }

        _db.TelegramBotNotifications.Add(new TelegramBotNotification
        {
            TelegramUserId = telegramUserId,
            Type = type,
            PayloadJson = payloadJson,
            Status = "pending",
            NextAttemptAt = _clock.UtcNow
        });
    }

    private async Task<JsonObject> GetSessionPayloadAsync(long telegramUserId, string expectedState, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var session = await _db.TelegramBotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId && x.CurrentState == expectedState, cancellationToken);
        if (session is not null && session.ExpiresAt <= now)
        {
            session = null;
        }
        if (session is null)
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(session.PayloadJson) as JsonObject ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private async Task SetSessionStateAsync(long telegramUserId, string state, string payloadJson, CancellationToken cancellationToken)
    {
        var session = await _db.TelegramBotSessions.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId, cancellationToken);
        if (session is null)
        {
            session = new TelegramBotSession { TelegramUserId = telegramUserId };
            _db.TelegramBotSessions.Add(session);
        }

        session.CurrentState = state;
        session.PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson;
        session.ExpiresAt = _clock.UtcNow.AddHours(2);
        session.UpdatedAt = _clock.UtcNow;
    }

    private async Task ClearSessionAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        var session = await _db.TelegramBotSessions.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId, cancellationToken);
        if (session is not null)
        {
            session.CurrentState = "idle";
            session.PayloadJson = "{}";
            session.ExpiresAt = _clock.UtcNow;
            session.UpdatedAt = _clock.UtcNow;
        }
    }

    private Task LogCommandAsync(long telegramUserId, long updateId, string command, string payload, CancellationToken cancellationToken)
    {
        _db.TelegramBotCommandLogs.Add(new TelegramBotCommandLog
        {
            TelegramUserId = telegramUserId,
            UpdateId = updateId,
            Command = command,
            Payload = payload,
            ResultStatus = "routed"
        });
        return Task.CompletedTask;
    }

    private static ParsedTelegramUpdate ParseUpdate(JsonElement root)
    {
        var updateId = root.GetProperty("update_id").GetInt64();
        if (root.TryGetProperty("pre_checkout_query", out var preCheckout))
        {
            var from = preCheckout.GetProperty("from");
            return new ParsedTelegramUpdate(
                updateId,
                "pre_checkout_query",
                GetLong(from, "id"),
                GetString(from, "username"),
                GetString(from, "first_name"),
                GetString(from, "last_name"),
                GetString(from, "language_code"),
                GetLong(from, "id"),
                null,
                null,
                null,
                null,
                GetString(preCheckout, "id"),
                GetString(preCheckout, "invoice_payload"),
                null,
                null,
                null,
                GetLongOrNull(preCheckout, "total_amount"),
                GetString(preCheckout, "currency"),
                false,
                "[]");
        }

        if (root.TryGetProperty("callback_query", out var callback))
        {
            var from = callback.GetProperty("from");
            var message = callback.TryGetProperty("message", out var messageElement) ? messageElement : default;
            var chat = message.ValueKind == JsonValueKind.Object && message.TryGetProperty("chat", out var chatElement) ? chatElement : default;
            return new ParsedTelegramUpdate(
                updateId,
                "callback_query",
                GetLong(from, "id"),
                GetString(from, "username"),
                GetString(from, "first_name"),
                GetString(from, "last_name"),
                GetString(from, "language_code"),
                GetLongOrNull(chat, "id"),
                GetLongOrNull(message, "message_id"),
                null,
                GetString(callback, "id"),
                GetString(callback, "data"),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                "[]");
        }

        if (root.TryGetProperty("message", out var messageRoot))
        {
            var from = messageRoot.TryGetProperty("from", out var fromElement) ? fromElement : default;
            var chat = messageRoot.TryGetProperty("chat", out var chatElement) ? chatElement : default;
            var successful = messageRoot.TryGetProperty("successful_payment", out var successfulPayment) ? successfulPayment : default;
            var attachments = ExtractAttachmentsJson(messageRoot);
            return new ParsedTelegramUpdate(
                updateId,
                "message",
                GetLong(from, "id"),
                GetString(from, "username"),
                GetString(from, "first_name"),
                GetString(from, "last_name"),
                GetString(from, "language_code"),
                GetLongOrNull(chat, "id"),
                GetLongOrNull(messageRoot, "message_id"),
                GetString(messageRoot, "text") ?? GetString(messageRoot, "caption"),
                null,
                null,
                null,
                null,
                successful.ValueKind == JsonValueKind.Object ? GetString(successful, "invoice_payload") : null,
                successful.ValueKind == JsonValueKind.Object ? GetString(successful, "telegram_payment_charge_id") : null,
                successful.ValueKind == JsonValueKind.Object ? GetString(successful, "provider_payment_charge_id") : null,
                successful.ValueKind == JsonValueKind.Object ? GetLongOrNull(successful, "total_amount") : null,
                successful.ValueKind == JsonValueKind.Object ? GetString(successful, "currency") : null,
                attachments != "[]",
                attachments);
        }

        return new ParsedTelegramUpdate(updateId, "unknown", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, "[]");
    }

    private static string ExtractAttachmentsJson(JsonElement messageRoot)
    {
        var attachments = new JsonArray();
        if (messageRoot.TryGetProperty("photo", out var photo) && photo.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in photo.EnumerateArray())
            {
                attachments.Add(JsonNode.Parse(item.GetRawText()));
            }
        }

        if (messageRoot.TryGetProperty("document", out var document) && document.ValueKind == JsonValueKind.Object)
        {
            attachments.Add(JsonNode.Parse(document.GetRawText()));
        }

        return attachments.ToJsonString(JsonOptions);
    }

    private static long? GetLongOrNull(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var parsed) ? parsed : null;

    private static long? GetLong(JsonElement element, string propertyName)
        => GetLongOrNull(element, propertyName);

    private static string? GetString(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string HelpText()
        => "Доступные команды: /start, /menu, /plans, /subscriptions, /keys, /renew, /instruction, /support, /profile. В меню доступны тарифы, оплата, VPN-доступ и поддержка.";

    private static string DisplayName(TelegramAccount account)
        => !string.IsNullOrWhiteSpace(account.FirstName) ? account.FirstName : !string.IsNullOrWhiteSpace(account.Username) ? account.Username : account.TelegramUserId.ToString(CultureInfo.InvariantCulture);

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashRawPayload(string rawBody)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawBody ?? string.Empty));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool FixedEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left.Trim());
        var rightBytes = Encoding.UTF8.GetBytes(right.Trim());
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Redact(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        try
        {
            var node = JsonNode.Parse(raw);
            if (node is JsonObject obj)
            {
                RedactNode(obj);
                return node?.ToJsonString(JsonOptions) ?? raw;
            }
        }
        catch
        {
            return raw.Replace("bot_token", "bot_***", StringComparison.OrdinalIgnoreCase);
        }

        return raw;
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(x => x.Key).ToList())
            {
                if (key.Contains("token", StringComparison.OrdinalIgnoreCase)
                    || key.Contains("password", StringComparison.OrdinalIgnoreCase)
                    || key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                {
                    obj[key] = "***";
                }
                else
                {
                    RedactNode(obj[key]);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                RedactNode(item);
            }
        }
    }

    private sealed record RouteResult(
        string ResponseText,
        long? ChatId,
        string? ReplyMarkupJson,
        string? PreCheckoutQueryId = null,
        bool? PreCheckoutOk = null,
        string? PreCheckoutError = null);

    private sealed record TelegramUpdateClaim(TelegramBotUpdate? Update, bool IsProcessed, bool IsInProgress)
    {
        public static TelegramUpdateClaim Processed { get; } = new(null, true, false);
        public static TelegramUpdateClaim InProgress { get; } = new(null, false, true);
        public static TelegramUpdateClaim Claimed(TelegramBotUpdate update) => new(update, false, false);
    }

    private sealed record ParsedTelegramUpdate(
        long UpdateId,
        string UpdateType,
        long? TelegramUserId,
        string? Username,
        string? FirstName,
        string? LastName,
        string? LanguageCode,
        long? ChatId,
        long? MessageId,
        string? Text,
        string? CallbackQueryId,
        string? CallbackData,
        string? PreCheckoutQueryId,
        string? InvoicePayload,
        string? SuccessfulPaymentPayload,
        string? SuccessfulPaymentTelegramChargeId,
        string? SuccessfulPaymentProviderChargeId,
        long? SuccessfulPaymentTotalAmount,
        string? SuccessfulPaymentCurrency,
        bool HasAttachment,
        string AttachmentsJson);
}
