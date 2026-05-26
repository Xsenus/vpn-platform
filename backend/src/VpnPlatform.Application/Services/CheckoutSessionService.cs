using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class CheckoutSessionService
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly OrderService _orderService;

    public CheckoutSessionService(IApplicationDbContext db, IClock clock, OrderService orderService)
    {
        _db = db;
        _clock = clock;
        _orderService = orderService;
    }

    public async Task<Result<CheckoutSessionDto>> CreateAsync(CreateCheckoutSessionCommand command, CancellationToken cancellationToken = default)
    {
        var tariffExists = await _db.Tariffs.AnyAsync(x => x.Id == command.TariffId && x.IsActive, cancellationToken);
        if (!tariffExists)
        {
            return Result<CheckoutSessionDto>.Failure("Tariff not found or inactive.");
        }

        var token = CreateToken();
        var session = new CheckoutSession
        {
            TokenHash = HashToken(token),
            TariffId = command.TariffId,
            Type = command.Type,
            Channel = command.Channel,
            PaymentProvider = command.PaymentProvider,
            PromoCode = command.PromoCode,
            EmailHint = NormalizeEmail(command.EmailHint),
            IsFirstPurchase = command.IsFirstPurchase,
            ExpiresAt = _clock.UtcNow.AddMinutes(30),
            Status = "open",
            MetadataJson = string.IsNullOrWhiteSpace(command.ReturnUrl) ? "{}" : $$"""
            { "returnUrl": "{{JsonEscape(command.ReturnUrl)}}" }
            """
        };

        _db.CheckoutSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<CheckoutSessionDto>.Success(Map(session, token));
    }

    public async Task<Result<OrderDto>> ClaimAsync(ClaimCheckoutSessionCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(command.Token);
        var session = await _db.CheckoutSessions.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (session is null)
        {
            return Result<OrderDto>.Failure("Checkout session not found.");
        }

        if (session.ExpiresAt <= _clock.UtcNow || session.Status is "expired" or "completed")
        {
            session.Status = "expired";
            await _db.SaveChangesAsync(cancellationToken);
            return Result<OrderDto>.Failure("Checkout session expired.");
        }

        if (session.UserId.HasValue && session.UserId.Value != command.UserId)
        {
            return Result<OrderDto>.Failure("Checkout session is already claimed by another user.");
        }

        if (session.OrderId.HasValue)
        {
            var existingOrder = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == session.OrderId.Value, cancellationToken);
            if (existingOrder is not null && existingOrder.UserId == command.UserId)
            {
                return Result<OrderDto>.Success(OrderService.MapToDto(existingOrder));
            }

            return Result<OrderDto>.Failure("Checkout session is already claimed.");
        }

        var orderResult = await _orderService.CreateOrderAsync(
            new CreateOrderCommand(
                command.UserId,
                session.TariffId,
                session.Type,
                session.Channel,
                session.PaymentProvider,
                session.PromoCode,
                session.IsFirstPurchase,
                session.Id),
            cancellationToken);

        if (!orderResult.IsSuccess || orderResult.Value is null)
        {
            return orderResult;
        }

        session.UserId = command.UserId;
        session.OrderId = orderResult.Value.Id;
        session.ClaimedAt = _clock.UtcNow;
        session.Status = "claimed";
        session.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return orderResult;
    }

    public async Task<Result<CheckoutSessionDto>> GetAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        var session = await _db.CheckoutSessions.AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (session is null)
        {
            return Result<CheckoutSessionDto>.Failure("Checkout session not found.");
        }

        return Result<CheckoutSessionDto>.Success(Map(session, token));
    }

    private static CheckoutSessionDto Map(CheckoutSession session, string token)
        => new(session.Id, token, session.TariffId, session.UserId, session.OrderId, session.Status, session.ExpiresAt, session.EmailHint);

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static string JsonEscape(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
