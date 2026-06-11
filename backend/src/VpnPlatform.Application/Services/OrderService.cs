using Microsoft.EntityFrameworkCore;
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

        var now = _clock.UtcNow;
        var pendingOrders = await _db.Orders
            .AsNoTracking()
            .Where(x =>
                x.UserId == command.UserId &&
                x.TariffId == command.TariffId)
            .ToListAsync(cancellationToken);
        pendingOrders = pendingOrders
            .Where(x =>
                x.Type == command.Type &&
                x.Channel == command.Channel &&
                x.Status == OrderStatus.PendingPayment &&
                x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var existingPending = command.Type == OrderType.Renewal && command.RenewalSubscriptionId.HasValue
            ? pendingOrders.FirstOrDefault(x => GetRenewalSubscriptionId(x) == command.RenewalSubscriptionId.Value)
            : pendingOrders.FirstOrDefault(x => GetRenewalSubscriptionId(x) is null);

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
            ReferralContext = BuildReferralContext(command.RenewalSubscriptionId)
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<OrderDto>.Success(MapToDto(order));
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
