using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public sealed class ReferralRewardService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "draft",
        "active",
        "paused",
        "archived"
    };

    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;

    public ReferralRewardService(IApplicationDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<int>> MaterializeForOrderAsync(
        Guid orderId,
        Guid sourceMessageId,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(x => x.Tariff)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.Completed)
        {
            return Result<int>.Failure("Referral reward requires a completed order.");
        }

        if (order.Type != OrderType.NewSubscription || order.Tariff?.IsReferralEligible != true)
        {
            return Result<int>.Success(0);
        }

        if (order.PromoCodeId.HasValue
            && !await _db.PromoCodes.AsNoTracking().AnyAsync(
                x => x.Id == order.PromoCodeId.Value && x.AllowStackWithReferral,
                cancellationToken))
        {
            return Result<int>.Success(0);
        }

        var relationship = (await _db.ReferralRelationships.AsNoTracking()
                .Where(x => x.ReferredUserId == order.UserId)
                .ToListAsync(cancellationToken))
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .FirstOrDefault();
        if (relationship is null || relationship.IsSuspicious || relationship.ReferrerUserId == relationship.ReferredUserId)
        {
            return Result<int>.Success(0);
        }

        var now = _clock.UtcNow;
        var activePrograms = (await _db.ReferralPrograms.AsNoTracking()
                .Where(x => x.Status.ToLower() == "active")
                .ToListAsync(cancellationToken))
            .Where(x => (!x.StartAt.HasValue || x.StartAt <= now) && (!x.EndAt.HasValue || x.EndAt > now))
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToList();
        if (activePrograms.Count == 0)
        {
            return Result<int>.Success(0);
        }

        var previousCompletedPurchases = await _db.Orders.AsNoTracking().CountAsync(
            x => x.Id != order.Id
                && x.UserId == order.UserId
                && x.Type == OrderType.NewSubscription
                && x.Status == OrderStatus.Completed,
            cancellationToken);

        var created = 0;
        foreach (var program in activePrograms)
        {
            if (!TryParseRules(program.RuleDefinition, out var rules, out var rulesError))
            {
                return Result<int>.Failure($"Referral program '{program.Name}' rules are invalid: {rulesError}");
            }

            if ((rules.FirstPurchaseOnly && previousCompletedPurchases > 0)
                || order.Amount < rules.MinimumOrderAmount
                || (rules.AllowedChannels.Count > 0 && !rules.AllowedChannels.Contains(order.Channel)))
            {
                continue;
            }

            if (!TryParseRewards(program.RewardDefinition, out var rewards, out var rewardsError))
            {
                return Result<int>.Failure($"Referral program '{program.Name}' rewards are invalid: {rewardsError}");
            }

            foreach (var reward in rewards)
            {
                var recipientId = reward.Role == "referrer" ? relationship.ReferrerUserId : relationship.ReferredUserId;
                var sourceUserId = reward.Role == "referrer" ? relationship.ReferredUserId : relationship.ReferrerUserId;
                var ledgerId = DeterministicLedgerId(sourceMessageId, program.Id, reward.Role);
                if (await _db.RewardLedgers.AsNoTracking().AnyAsync(x => x.Id == ledgerId, cancellationToken))
                {
                    continue;
                }

                _db.RewardLedgers.Add(new RewardLedger
                {
                    Id = ledgerId,
                    UserId = recipientId,
                    SourceUserId = sourceUserId,
                    ReferralProgramId = program.Id,
                    Type = reward.Type,
                    Status = reward.AutoApprove ? RewardStatus.Approved : RewardStatus.Pending,
                    Value = reward.Value,
                    CurrencyOrUnit = reward.Unit,
                    ProcessedAt = reward.AutoApprove ? now : null,
                    MetadataJson = JsonSerializer.Serialize(new
                    {
                        sourceOrderId = order.Id,
                        sourceOutboxMessageId = sourceMessageId,
                        role = reward.Role,
                        sourceChannel = relationship.SourceChannel.ToString()
                    })
                });
                created++;
            }
        }

        return Result<int>.Success(created);
    }

    public static Result<bool> ValidateProgramConfiguration(
        string? name,
        string? status,
        DateTimeOffset? startAt,
        DateTimeOffset? endAt,
        string? ruleDefinition,
        string? rewardDefinition,
        string? antiFraudSettings)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)
        {
            return Result<bool>.Failure("Referral program name is required and must not exceed 160 characters.");
        }

        if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status.Trim()))
        {
            return Result<bool>.Failure("Referral program status must be draft, active, paused, or archived.");
        }

        if (startAt.HasValue && endAt.HasValue && endAt <= startAt)
        {
            return Result<bool>.Failure("Referral program end date must be later than its start date.");
        }

        if (!TryParseRules(ruleDefinition, out _, out var rulesError))
        {
            return Result<bool>.Failure($"Referral program rules are invalid: {rulesError}.");
        }

        if (!TryParseRewards(rewardDefinition, out _, out var rewardsError))
        {
            return Result<bool>.Failure($"Referral program rewards are invalid: {rewardsError}.");
        }

        if (!TryParseObject(antiFraudSettings, out _, out var antiFraudError))
        {
            return Result<bool>.Failure($"Referral program anti-fraud settings are invalid: {antiFraudError}.");
        }

        return Result<bool>.Success(true);
    }

    private static bool TryParseRules(string? json, out ReferralRules rules, out string error)
    {
        rules = new ReferralRules(true, 0, new HashSet<ChannelType>());
        error = string.Empty;
        if (!TryParseObject(json, out var root, out error)) return false;

        var firstPurchaseOnly = true;
        if (root.TryGetProperty("firstPurchaseOnly", out var firstPurchase))
        {
            if (firstPurchase.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Fail("firstPurchaseOnly must be boolean", out error);
            firstPurchaseOnly = firstPurchase.GetBoolean();
        }

        var minimumOrderAmount = 0m;
        if (root.TryGetProperty("minimumOrderAmount", out var minimum)
            && (minimum.ValueKind != JsonValueKind.Number || !minimum.TryGetDecimal(out minimumOrderAmount) || minimumOrderAmount < 0))
        {
            return Fail("minimumOrderAmount must be a non-negative number", out error);
        }

        var channels = new HashSet<ChannelType>();
        if (root.TryGetProperty("allowedChannels", out var allowedChannels))
        {
            if (allowedChannels.ValueKind != JsonValueKind.Array) return Fail("allowedChannels must be an array", out error);
            foreach (var item in allowedChannels.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String
                    || !Enum.TryParse<ChannelType>(item.GetString(), true, out var channel)
                    || !Enum.IsDefined(channel))
                {
                    return Fail("allowedChannels contains an invalid channel", out error);
                }
                channels.Add(channel);
            }
        }

        rules = new ReferralRules(firstPurchaseOnly, minimumOrderAmount, channels);
        return true;
    }

    private static bool TryParseRewards(string? json, out IReadOnlyList<ReferralReward> rewards, out string error)
    {
        rewards = Array.Empty<ReferralReward>();
        error = string.Empty;
        if (!TryParseObject(json, out var root, out error)) return false;

        var parsed = new List<ReferralReward>();
        foreach (var role in new[] { "referrer", "referred" })
        {
            if (!root.TryGetProperty(role, out var value)) continue;
            if (value.ValueKind != JsonValueKind.Object) return Fail($"{role} reward must be an object", out error);
            if (!TryParseReward(role, value, out var reward, out error)) return false;
            if (reward is not null) parsed.Add(reward);
        }

        if (parsed.Count == 0) return Fail("at least one referrer or referred reward is required", out error);
        rewards = parsed;
        return true;
    }

    private static bool TryParseReward(string role, JsonElement value, out ReferralReward? reward, out string error)
    {
        reward = null;
        error = string.Empty;
        if (!value.TryGetProperty("value", out var amount)
            || amount.ValueKind != JsonValueKind.Number
            || !amount.TryGetDecimal(out var parsedAmount)
            || parsedAmount <= 0
            || parsedAmount > 1_000_000)
        {
            return Fail($"{role}.value must be between 0 and 1000000", out error);
        }

        var type = ReadOptionalString(value, "type", "bonus-days");
        var unit = ReadOptionalString(value, "unit", "days");
        if (type is null || type.Length > 64 || unit is null || unit.Length > 32)
        {
            return Fail($"{role} type or unit is invalid", out error);
        }

        var autoApprove = false;
        if (value.TryGetProperty("autoApprove", out var approval))
        {
            if (approval.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Fail($"{role}.autoApprove must be boolean", out error);
            autoApprove = approval.GetBoolean();
        }

        reward = new ReferralReward(role, type, parsedAmount, unit, autoApprove);
        return true;
    }

    private static bool TryParseObject(string? json, out JsonElement root, out string error)
    {
        root = default;
        error = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return Fail("value must be a JSON object", out error);
            root = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return Fail("value must be valid JSON", out error);
        }
    }

    private static string? ReadOptionalString(JsonElement root, string property, string fallback)
    {
        if (!root.TryGetProperty(property, out var value)) return fallback;
        return value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()!.Trim() : null;
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }

    private static Guid DeterministicLedgerId(Guid messageId, Guid programId, string role)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{messageId:N}:{programId:N}:{role}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private sealed record ReferralRules(bool FirstPurchaseOnly, decimal MinimumOrderAmount, HashSet<ChannelType> AllowedChannels);
    private sealed record ReferralReward(string Role, string Type, decimal Value, string Unit, bool AutoApprove);
}
