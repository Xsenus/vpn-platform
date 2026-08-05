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

        var promoValidation = await _orderService.ValidatePromoForCheckoutAsync(
            command.PromoCode,
            command.TariffId,
            command.Channel,
            cancellationToken);
        if (!promoValidation.IsSuccess)
        {
            return Result<CheckoutSessionDto>.Failure(promoValidation.Error ?? "Promo code validation failed.");
        }

        var token = CreateToken();
        var session = new CheckoutSession
        {
            TokenHash = HashToken(token),
            TariffId = command.TariffId,
            Type = command.Type,
            Channel = command.Channel,
            PaymentProvider = command.PaymentProvider,
            PromoCode = OrderService.NormalizePromoCode(command.PromoCode),
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
        var session = await _db.CheckoutSessions.AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (session is null)
        {
            return Result<OrderDto>.Failure("Checkout session not found.");
        }

        var resolved = await ResolveClaimStateAsync(session, command.UserId, cancellationToken);
        if (resolved is not null)
        {
            return resolved;
        }

        if (_db is DbContext dbContext && dbContext.Database.IsRelational())
        {
            return await ClaimRelationalAsync(dbContext, session, command.UserId, cancellationToken);
        }

        return await ClaimInMemoryAsync(session.Id, command.UserId, cancellationToken);
    }

    private async Task<Result<OrderDto>> ClaimRelationalAsync(
        DbContext dbContext,
        CheckoutSession snapshot,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var transactionFinished = false;
        try
        {
            var now = _clock.UtcNow;
            var reservationVersion = NextVersion(snapshot.UpdatedAt, now);
            var reserved = await _db.CheckoutSessions
                .Where(x =>
                    x.Id == snapshot.Id &&
                    x.TokenHash == snapshot.TokenHash &&
                    x.Status == "open" &&
                    x.UserId == null &&
                    x.OrderId == null &&
                    x.ExpiresAt == snapshot.ExpiresAt &&
                    x.UpdatedAt == snapshot.UpdatedAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UserId, userId)
                    .SetProperty(x => x.ClaimedAt, now)
                    .SetProperty(x => x.Status, "claiming")
                    .SetProperty(x => x.UpdatedAt, reservationVersion), cancellationToken);

            if (reserved != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                transactionFinished = true;
                dbContext.ChangeTracker.Clear();
                return await ResolveConcurrentClaimAsync(snapshot.Id, userId, cancellationToken);
            }

            var orderResult = await CreateOrderAsync(snapshot, userId, cancellationToken);
            if (!orderResult.IsSuccess || orderResult.Value is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                transactionFinished = true;
                dbContext.ChangeTracker.Clear();
                return orderResult;
            }

            var completedAt = _clock.UtcNow;
            var completedVersion = NextVersion(reservationVersion, completedAt);
            var completed = await _db.CheckoutSessions
                .Where(x =>
                    x.Id == snapshot.Id &&
                    x.UserId == userId &&
                    x.OrderId == null &&
                    x.Status == "claiming" &&
                    x.UpdatedAt == reservationVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.OrderId, orderResult.Value.Id)
                    .SetProperty(x => x.Status, "claimed")
                    .SetProperty(x => x.UpdatedAt, completedVersion), cancellationToken);

            if (completed != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                transactionFinished = true;
                dbContext.ChangeTracker.Clear();
                return await ResolveConcurrentClaimAsync(snapshot.Id, userId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            transactionFinished = true;
            return orderResult;
        }
        catch
        {
            if (!transactionFinished)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<Result<OrderDto>> ClaimInMemoryAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _db.CheckoutSessions.FirstAsync(x => x.Id == sessionId, cancellationToken);
        var resolved = await ResolveClaimStateAsync(session, userId, cancellationToken);
        if (resolved is not null)
        {
            return resolved;
        }

        var orderResult = await CreateOrderAsync(session, userId, cancellationToken);
        if (!orderResult.IsSuccess || orderResult.Value is null)
        {
            return orderResult;
        }

        var now = _clock.UtcNow;
        session.UserId = userId;
        session.OrderId = orderResult.Value.Id;
        session.ClaimedAt = now;
        session.Status = "claimed";
        session.UpdatedAt = NextVersion(session.UpdatedAt, now);
        await _db.SaveChangesAsync(cancellationToken);

        return orderResult;
    }

    private Task<Result<OrderDto>> CreateOrderAsync(CheckoutSession session, Guid userId, CancellationToken cancellationToken)
        => _orderService.CreateOrderAsync(
            new CreateOrderCommand(
                userId,
                session.TariffId,
                session.Type,
                session.Channel,
                session.PaymentProvider,
                session.PromoCode,
                session.IsFirstPurchase,
                session.Id),
            cancellationToken);

    private async Task<Result<OrderDto>> ResolveConcurrentClaimAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var current = await _db.CheckoutSessions.AsNoTracking().FirstAsync(x => x.Id == sessionId, cancellationToken);
        return await ResolveClaimStateAsync(current, userId, cancellationToken)
            ?? Result<OrderDto>.Failure("Checkout session state changed while it was being claimed. Try again.", isRetryable: true);
    }

    private async Task<Result<OrderDto>?> ResolveClaimStateAsync(
        CheckoutSession session,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (session.UserId.HasValue && session.UserId.Value != userId)
        {
            return Result<OrderDto>.Failure("Checkout session is already claimed by another user.");
        }

        if (session.OrderId.HasValue)
        {
            var existingOrder = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == session.OrderId.Value, cancellationToken);
            return existingOrder is not null && existingOrder.UserId == userId
                ? Result<OrderDto>.Success(OrderService.MapToDto(existingOrder))
                : Result<OrderDto>.Failure("Checkout session is already claimed.");
        }

        if (session.Status == "completed")
        {
            return Result<OrderDto>.Failure("Checkout session is already completed.");
        }

        var now = _clock.UtcNow;
        if (session.Status == "expired" || session.ExpiresAt <= now)
        {
            await ExpireOpenSessionAsync(session.Id, session.UpdatedAt, now, cancellationToken);
            return Result<OrderDto>.Failure("Checkout session expired.");
        }

        return session.Status == "open"
            ? null
            : Result<OrderDto>.Failure("Checkout session claim is already in progress. Try again.", isRetryable: true);
    }

    private async Task ExpireOpenSessionAsync(
        Guid sessionId,
        DateTimeOffset expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_db is DbContext dbContext && dbContext.Database.IsRelational())
        {
            var nextVersion = NextVersion(expectedVersion, now);
            await _db.CheckoutSessions
                .Where(x =>
                    x.Id == sessionId &&
                    x.Status == "open" &&
                    x.OrderId == null &&
                    x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "expired")
                    .SetProperty(x => x.UpdatedAt, nextVersion), cancellationToken);
            return;
        }

        var tracked = await _db.CheckoutSessions.FirstOrDefaultAsync(
            x => x.Id == sessionId && x.Status == "open" && x.OrderId == null && x.ExpiresAt <= now,
            cancellationToken);
        if (tracked is null)
        {
            return;
        }

        tracked.Status = "expired";
        tracked.UpdatedAt = NextVersion(tracked.UpdatedAt, now);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static DateTimeOffset NextVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);

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
