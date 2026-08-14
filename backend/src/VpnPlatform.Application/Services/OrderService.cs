using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class OrderService
{
    private const string RenewalSubscriptionIdKey = "renewalSubscriptionId";
    private const string PromoFreeDaysKey = "promoFreeDays";
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;

    public OrderService(IApplicationDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.Type))
        {
            return Result<OrderDto>.Failure("Order type is not supported.");
        }

        if (!Enum.IsDefined(command.Channel))
        {
            return Result<OrderDto>.Failure("Order channel is not supported.");
        }

        if (!Enum.IsDefined(command.PaymentProvider))
        {
            return Result<OrderDto>.Failure("Payment provider is not supported.");
        }

        var tariff = await _db.Tariffs.FirstOrDefaultAsync(x => x.Id == command.TariffId && x.IsActive, cancellationToken);
        if (tariff is null)
        {
            return Result<OrderDto>.Failure("Tariff not found or inactive.");
        }

        var renewalValidation = await ValidateRenewalAsync(command, cancellationToken);
        if (renewalValidation is not null)
        {
            return Result<OrderDto>.Failure(renewalValidation);
        }

        var promoResult = await ResolvePromoAsync(command.PromoCode, command.TariffId, command.Channel, cancellationToken);
        if (!promoResult.IsSuccess || promoResult.Value is null)
        {
            return Result<OrderDto>.Failure(promoResult.Error ?? "Promo code validation failed.");
        }

        var promo = promoResult.Value.Promo;
        if (promo is not null
            && _db is DbContext dbContext
            && dbContext.Database.IsRelational()
            && dbContext.Database.CurrentTransaction is null)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var transactionFinished = false;
            try
            {
                var result = await CreateOrderCoreAsync(command, tariff, promo, cancellationToken);
                if (result.IsSuccess)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                transactionFinished = true;
                return result;
            }
            catch
            {
                if (!transactionFinished)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }

                throw;
            }
        }

        return await CreateOrderCoreAsync(command, tariff, promo, cancellationToken);
    }

    public async Task<Result<OrderDto>> SelectPaymentProviderAsync(
        Guid orderId,
        Guid userId,
        PaymentProvider provider,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(provider))
        {
            return Result<OrderDto>.Failure("Payment provider is not supported.");
        }

        await using var processingGate = await PaymentProcessingGate.AcquireOrderAsync(orderId, cancellationToken);
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId, cancellationToken);
        if (order is null)
        {
            return Result<OrderDto>.Failure("Order not found.");
        }

        if (order.Channel != ChannelType.Telegram)
        {
            return Result<OrderDto>.Failure("Payment provider selection is available only for Telegram orders.");
        }

        if (order.Status != OrderStatus.PendingPayment || order.ExpiresAt <= _clock.UtcNow)
        {
            return Result<OrderDto>.Failure("Payment provider can be selected only for a live pending order.");
        }

        if (await _db.Payments.AnyAsync(x => x.OrderId == orderId, cancellationToken))
        {
            return order.PaymentProvider == provider
                ? Result<OrderDto>.Success(MapToDto(order))
                : Result<OrderDto>.Failure("Payment provider is locked after the first payment attempt.");
        }

        if (order.PaymentProvider != provider)
        {
            order.PaymentProvider = provider;
            order.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result<OrderDto>.Success(MapToDto(order));
    }

    public async Task<Result<bool>> ValidatePromoForCheckoutAsync(
        string? promoCode,
        Guid tariffId,
        ChannelType channel,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(channel))
        {
            return Result<bool>.Failure("Order channel is not supported.");
        }

        var promoResult = await ResolvePromoAsync(promoCode, tariffId, channel, cancellationToken);
        if (!promoResult.IsSuccess || promoResult.Value is null)
        {
            return Result<bool>.Failure(promoResult.Error ?? "Promo code validation failed.");
        }

        var promo = promoResult.Value.Promo;
        if (promo is null)
        {
            return Result<bool>.Success(true);
        }

        var limitError = await ValidatePromoLimitsAsync(promo, userId: null, cancellationToken);
        return limitError is null
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(limitError);
    }

    private async Task<Result<OrderDto>> CreateOrderCoreAsync(
        CreateOrderCommand command,
        Tariff tariff,
        PromoCode? promo,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var pendingOrders = await _db.Orders
            .AsNoTracking()
            .Where(x =>
                x.UserId == command.UserId &&
                x.TariffId == command.TariffId &&
                x.Type == command.Type &&
                x.Channel == command.Channel &&
                x.PromoCodeId == (promo == null ? null : promo.Id) &&
                x.Status == OrderStatus.PendingPayment)
            .ToListAsync(cancellationToken);
        pendingOrders = pendingOrders
            .Where(x => GetRenewalSubscriptionId(x) == command.RenewalSubscriptionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        foreach (var expiredOrder in pendingOrders.Where(x => x.ExpiresAt <= now))
        {
            if (!await ExpirePendingOrderAsync(expiredOrder.Id, expiredOrder.UpdatedAt, now, cancellationToken))
            {
                return Result<OrderDto>.Failure("Pending order changed while it was being expired. Try again.", isRetryable: true);
            }
        }

        var existingPending = pendingOrders.FirstOrDefault(x => x.Status == OrderStatus.PendingPayment && x.ExpiresAt > now);

        if (existingPending is not null)
        {
            return Result<OrderDto>.Success(MapToDto(existingPending));
        }

        if (promo is not null)
        {
            var claimResult = await ClaimPromoAsync(promo, cancellationToken);
            if (!claimResult.IsSuccess)
            {
                return Result<OrderDto>.Failure(claimResult.Error!, claimResult.IsRetryable);
            }

            var limitError = await ValidatePromoLimitsAsync(promo, command.UserId, cancellationToken);
            if (limitError is not null)
            {
                return Result<OrderDto>.Failure(limitError);
            }
        }

        var expiresAt = now.AddMinutes(15);
        var amount = tariff.Price;
        if (promo is not null)
        {
            amount = promo.DiscountType.Equals("percent", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(0, amount - amount * (promo.DiscountValue / 100m))
                : Math.Max(0, amount - promo.DiscountValue);
        }

        var isFirstPurchase = command.Type == OrderType.NewSubscription
            && !await _db.Orders.AsNoTracking().AnyAsync(
                x => x.UserId == command.UserId
                    && x.Type == OrderType.NewSubscription
                    && x.Status == OrderStatus.Completed,
                cancellationToken);

        var order = new Order
        {
            UserId = command.UserId,
            TariffId = command.TariffId,
            CheckoutSessionId = command.CheckoutSessionId,
            Type = command.Type,
            Channel = command.Channel,
            PaymentProvider = command.PaymentProvider,
            Status = OrderStatus.PendingPayment,
            Amount = amount,
            Currency = tariff.Currency,
            PromoCodeId = promo?.Id,
            ExpiresAt = expiresAt,
            IsFirstPurchase = isFirstPurchase,
            ReferralContext = BuildReferralContext(command.RenewalSubscriptionId, promo?.FreeDays),
            PendingIntentKey = BuildPendingIntentKey(command, promo?.Id),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Orders.Add(order);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsPendingIntentConflict(ex))
        {
            _db.Orders.Remove(order);
            var winner = (await _db.Orders
                .AsNoTracking()
                .Where(x =>
                    x.PendingIntentKey == order.PendingIntentKey &&
                    x.Status == OrderStatus.PendingPayment)
                .ToListAsync(cancellationToken))
                .FirstOrDefault(x => x.ExpiresAt > now);

            if (winner is not null)
            {
                return Result<OrderDto>.Success(MapToDto(winner));
            }

            throw;
        }

        return Result<OrderDto>.Success(MapToDto(order));
    }

    private async Task<Result<bool>> ClaimPromoAsync(PromoCode promo, CancellationToken cancellationToken)
    {
        if (_db is not DbContext dbContext || !dbContext.Database.IsRelational())
        {
            return Result<bool>.Success(true);
        }

        var nextVersion = NextVersion(promo.UpdatedAt, _clock.UtcNow);
        var claimed = await _db.PromoCodes
            .Where(x => x.Id == promo.Id && x.UpdatedAt == promo.UpdatedAt)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UpdatedAt, nextVersion), cancellationToken);
        return claimed == 1
            ? Result<bool>.Success(true)
            : Result<bool>.Failure("Promo code changed while the order was being created. Try again.", isRetryable: true);
    }

    private async Task<string?> ValidatePromoLimitsAsync(PromoCode promo, Guid? userId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await ExpireStalePromoOrdersAsync(promo.Id, now, cancellationToken);

        var countedOrders = _db.Orders
            .AsNoTracking()
            .Where(x =>
                x.PromoCodeId == promo.Id &&
                x.Status != OrderStatus.Failed &&
                x.Status != OrderStatus.Cancelled &&
                x.Status != OrderStatus.Expired);

        if (promo.MaxRedemptions.HasValue
            && await countedOrders.CountAsync(cancellationToken) >= promo.MaxRedemptions.Value)
        {
            return "Promo code redemption limit has been reached.";
        }

        if (userId.HasValue
            && promo.MaxPerUser.HasValue
            && await countedOrders.CountAsync(x => x.UserId == userId.Value, cancellationToken) >= promo.MaxPerUser.Value)
        {
            return "Promo code usage limit for this account has been reached.";
        }

        return null;
    }

    private async Task ExpireStalePromoOrdersAsync(
        Guid promoId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_db is DbContext dbContext && dbContext.Database.IsRelational())
        {
            if (string.Equals(
                    dbContext.Database.ProviderName,
                    "Microsoft.EntityFrameworkCore.Sqlite",
                    StringComparison.Ordinal))
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE "Orders"
                    SET "Status" = {(int)OrderStatus.Expired},
                        "UpdatedAt" = {now}
                    WHERE "PromoCodeId" = {promoId}
                      AND "Status" = {(int)OrderStatus.PendingPayment}
                      AND julianday("ExpiresAt") <= julianday({now})
                    """, cancellationToken);
                return;
            }

            await _db.Orders
                .Where(x =>
                    x.PromoCodeId == promoId &&
                    x.Status == OrderStatus.PendingPayment &&
                    x.ExpiresAt <= now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, OrderStatus.Expired)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            return;
        }

        var staleOrders = await _db.Orders
            .Where(x => x.PromoCodeId == promoId && x.Status == OrderStatus.PendingPayment)
            .ToListAsync(cancellationToken);
        foreach (var staleOrder in staleOrders.Where(x => x.ExpiresAt <= now))
        {
            StatusStateMachine.SetOrderStatus(staleOrder, OrderStatus.Expired, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result<PromoResolution>> ResolvePromoAsync(
        string? promoCode,
        Guid tariffId,
        ChannelType channel,
        CancellationToken cancellationToken)
    {
        var normalizedCode = NormalizePromoCode(promoCode);
        if (normalizedCode is null)
        {
            return Result<PromoResolution>.Success(new PromoResolution(null));
        }

        var promo = await _db.PromoCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code.ToUpper() == normalizedCode, cancellationToken);
        if (promo is null)
        {
            return Result<PromoResolution>.Failure("Promo code not found.");
        }

        var now = _clock.UtcNow;
        if (!promo.IsActive)
        {
            return Result<PromoResolution>.Failure("Promo code is inactive.");
        }

        if (promo.StartsAt.HasValue && promo.StartsAt.Value > now)
        {
            return Result<PromoResolution>.Failure("Promo code is not active yet.");
        }

        if (promo.EndsAt.HasValue && promo.EndsAt.Value <= now)
        {
            return Result<PromoResolution>.Failure("Promo code expired.");
        }

        if (!JsonArrayAllowsGuid(promo.AllowedTariffIdsJson, tariffId))
        {
            return Result<PromoResolution>.Failure("Promo code is not available for this tariff.");
        }

        if (!JsonArrayAllowsChannel(promo.AllowedChannelsJson, channel))
        {
            return Result<PromoResolution>.Failure("Promo code is not available for this channel.");
        }

        var discountType = promo.DiscountType.Trim().ToLowerInvariant();
        if (discountType is not ("percent" or "fixed")
            || promo.DiscountValue < 0
            || (discountType == "percent" && promo.DiscountValue > 100)
            || promo.FreeDays < 0
            || promo.MaxRedemptions is < 0
            || promo.MaxPerUser is < 0
            || (promo.DiscountValue == 0 && promo.FreeDays == 0))
        {
            return Result<PromoResolution>.Failure("Promo code configuration is invalid.");
        }

        promo.DiscountType = discountType;
        return Result<PromoResolution>.Success(new PromoResolution(promo));
    }

    private async Task<bool> ExpirePendingOrderAsync(
        Guid orderId,
        DateTimeOffset expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_db is DbContext dbContext && dbContext.Database.IsRelational())
        {
            var expired = await _db.Orders
                .Where(x => x.Id == orderId && x.Status == OrderStatus.PendingPayment && x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, OrderStatus.Expired)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            return expired == 1;
        }

        var order = await _db.Orders.FirstOrDefaultAsync(
            x => x.Id == orderId && x.Status == OrderStatus.PendingPayment && x.UpdatedAt == expectedVersion,
            cancellationToken);
        if (order is null)
        {
            return false;
        }

        StatusStateMachine.SetOrderStatus(order, OrderStatus.Expired, now);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<string?> ValidateRenewalAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Type != OrderType.Renewal)
        {
            return command.RenewalSubscriptionId.HasValue
                ? "Subscription is only supported for renewal orders."
                : null;
        }

        if (!command.RenewalSubscriptionId.HasValue)
        {
            return "Subscription is required for renewal orders.";
        }

        var subscription = await _db.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.RenewalSubscriptionId.Value && x.UserId == command.UserId, cancellationToken);
        if (subscription is null)
        {
            return "Subscription not found.";
        }

        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Blocked)
        {
            return "Subscription is not available for renewal.";
        }

        return subscription.TariffId != command.TariffId
            ? "Tariff does not match subscription."
            : null;
    }

    public async Task<int> ExpirePendingOrdersAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        if (_db is DbContext dbContext && dbContext.Database.IsRelational())
        {
            if (string.Equals(
                    dbContext.Database.ProviderName,
                    "Microsoft.EntityFrameworkCore.Sqlite",
                    StringComparison.Ordinal))
            {
                return await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE "Orders"
                    SET "Status" = {(int)OrderStatus.Expired},
                        "UpdatedAt" = {now}
                    WHERE "Status" = {(int)OrderStatus.PendingPayment}
                      AND julianday("ExpiresAt") <= julianday({now})
                    """, cancellationToken);
            }

            return await _db.Orders
                .Where(x => x.Status == OrderStatus.PendingPayment && x.ExpiresAt <= now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, OrderStatus.Expired)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
        }

        var orders = await _db.Orders
            .Where(x => x.Status == OrderStatus.PendingPayment)
            .ToListAsync(cancellationToken);

        var expired = 0;
        foreach (var order in orders.Where(x => x.Status == OrderStatus.PendingPayment && x.ExpiresAt <= now))
        {
            StatusStateMachine.SetOrderStatus(order, OrderStatus.Expired, now);
            expired += 1;
        }

        if (expired > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return expired;
    }

    public static Guid? GetRenewalSubscriptionId(Order order)
        => TryReadRenewalSubscriptionId(order.ReferralContext);

    public static int? GetPromoFreeDays(Order order)
        => TryReadPromoFreeDays(order.ReferralContext);

    public static OrderDto MapToDto(Order order)
        => new(order.Id, order.UserId, order.TariffId, order.Amount, order.Currency, order.Status.ToString(), order.ExpiresAt, order.PaymentProvider, GetRenewalSubscriptionId(order));

    private static string BuildReferralContext(Guid? renewalSubscriptionId, int? promoFreeDays)
    {
        if (!renewalSubscriptionId.HasValue && !promoFreeDays.HasValue)
        {
            return "{}";
        }

        var context = new Dictionary<string, object>();
        if (renewalSubscriptionId.HasValue)
        {
            context[RenewalSubscriptionIdKey] = renewalSubscriptionId.Value.ToString("D");
        }

        if (promoFreeDays.HasValue)
        {
            context[PromoFreeDaysKey] = promoFreeDays.Value;
        }

        return JsonSerializer.Serialize(context);
    }

    public static string? NormalizePromoCode(string? promoCode)
        => string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim().ToUpperInvariant();

    private static string BuildPendingIntentKey(CreateOrderCommand command, Guid? promoCodeId)
    {
        var source = string.Join(
            ':',
            command.UserId.ToString("N"),
            command.TariffId.ToString("N"),
            (int)command.Type,
            (int)command.Channel,
            command.RenewalSubscriptionId?.ToString("N") ?? string.Empty);
        if (promoCodeId.HasValue)
        {
            source += $":promo:{promoCodeId.Value:N}";
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private static bool JsonArrayAllowsGuid(string? json, Guid value)
    {
        if (!TryReadJsonArray(json, out var items) || items is null)
        {
            return false;
        }

        if (items.Value.GetArrayLength() == 0)
        {
            return true;
        }

        var allowed = false;
        foreach (var item in items.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out var parsed))
            {
                return false;
            }

            if (parsed == value)
            {
                allowed = true;
            }
        }

        return allowed;
    }

    private static bool JsonArrayAllowsChannel(string? json, ChannelType value)
    {
        if (!TryReadJsonArray(json, out var items) || items is null)
        {
            return false;
        }

        if (items.Value.GetArrayLength() == 0)
        {
            return true;
        }

        var allowed = false;
        foreach (var item in items.Value.EnumerateArray())
        {
            ChannelType parsed;
            if (item.ValueKind == JsonValueKind.String)
            {
                if (!Enum.TryParse(item.GetString(), ignoreCase: true, out parsed) || !Enum.IsDefined(parsed))
                {
                    return false;
                }
            }
            else if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var numeric) && Enum.IsDefined(typeof(ChannelType), numeric))
            {
                parsed = (ChannelType)numeric;
            }
            else
            {
                return false;
            }

            if (parsed == value)
            {
                allowed = true;
            }
        }

        return allowed;
    }

    private static bool TryReadJsonArray(string? json, out JsonElement? array)
    {
        array = null;
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            array = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static DateTimeOffset NextVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);

    private static bool IsPendingIntentConflict(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("IX_Orders_Pending_IntentKey", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("Orders.PendingIntentKey", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static Guid? TryReadRenewalSubscriptionId(string? referralContext)
    {
        if (string.IsNullOrWhiteSpace(referralContext))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(referralContext);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(RenewalSubscriptionIdKey, out var value)
                || value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return Guid.TryParse(value.GetString(), out var id) ? id : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int? TryReadPromoFreeDays(string? referralContext)
    {
        if (string.IsNullOrWhiteSpace(referralContext))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(referralContext);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(PromoFreeDaysKey, out var value)
                || value.ValueKind != JsonValueKind.Number
                || !value.TryGetInt32(out var freeDays))
            {
                return null;
            }

            return freeDays;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record PromoResolution(PromoCode? Promo);
}
