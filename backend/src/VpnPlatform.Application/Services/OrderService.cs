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
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;

    public OrderService(IApplicationDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
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

        var now = _clock.UtcNow;
        var pendingOrders = await _db.Orders
            .AsNoTracking()
            .Where(x =>
                x.UserId == command.UserId &&
                x.TariffId == command.TariffId &&
                x.Type == command.Type &&
                x.Channel == command.Channel &&
                x.Status == OrderStatus.PendingPayment)
            .ToListAsync(cancellationToken);
        pendingOrders = pendingOrders
            .Where(x => GetRenewalSubscriptionId(x) == command.RenewalSubscriptionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        foreach (var expiredOrder in pendingOrders.Where(x => x.ExpiresAt <= now))
        {
            await ExpirePendingOrderAsync(expiredOrder.Id, now, cancellationToken);
        }

        var existingPending = pendingOrders.FirstOrDefault(x => x.Status == OrderStatus.PendingPayment && x.ExpiresAt > now);

        if (existingPending is not null)
        {
            return Result<OrderDto>.Success(MapToDto(existingPending));
        }

        var expiresAt = now.AddMinutes(15);
        var amount = tariff.Price;

        if (!string.IsNullOrWhiteSpace(command.PromoCode))
        {
            var promo = await _db.PromoCodes.FirstOrDefaultAsync(x => x.Code == command.PromoCode && x.IsActive, cancellationToken);
            if (promo is not null)
            {
                amount = promo.DiscountType == "percent"
                    ? Math.Max(0, amount - amount * (promo.DiscountValue / 100m))
                    : Math.Max(0, amount - promo.DiscountValue);
            }
        }

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
            ExpiresAt = expiresAt,
            IsFirstPurchase = command.IsFirstPurchase,
            ReferralContext = BuildReferralContext(command.RenewalSubscriptionId),
            PendingIntentKey = BuildPendingIntentKey(command)
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

    private async Task ExpirePendingOrderAsync(Guid orderId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (_db is DbContext dbContext && dbContext.Database.IsRelational())
        {
            await _db.Orders
                .Where(x => x.Id == orderId && x.Status == OrderStatus.PendingPayment)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, OrderStatus.Expired)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            return;
        }

        var order = await _db.Orders.FirstOrDefaultAsync(
            x => x.Id == orderId && x.Status == OrderStatus.PendingPayment,
            cancellationToken);
        if (order is null)
        {
            return;
        }

        StatusStateMachine.SetOrderStatus(order, OrderStatus.Expired, now);
        await _db.SaveChangesAsync(cancellationToken);
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

    public async Task<Result<OrderDto>> GetOrderStatusAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        return order is null
            ? Result<OrderDto>.Failure("Order not found.")
            : Result<OrderDto>.Success(MapToDto(order));
    }

    public async Task<int> ExpirePendingOrdersAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var orders = await _db.Orders.ToListAsync(cancellationToken);

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

    public static OrderDto MapToDto(Order order)
        => new(order.Id, order.UserId, order.TariffId, order.Amount, order.Currency, order.Status.ToString(), order.ExpiresAt, GetRenewalSubscriptionId(order));

    private static string BuildReferralContext(Guid? renewalSubscriptionId)
        => renewalSubscriptionId.HasValue
            ? JsonSerializer.Serialize(new Dictionary<string, string> { [RenewalSubscriptionIdKey] = renewalSubscriptionId.Value.ToString("D") })
            : "{}";

    private static string BuildPendingIntentKey(CreateOrderCommand command)
    {
        var source = string.Join(
            ':',
            command.UserId.ToString("N"),
            command.TariffId.ToString("N"),
            (int)command.Type,
            (int)command.Channel,
            command.RenewalSubscriptionId?.ToString("N") ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

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
}
