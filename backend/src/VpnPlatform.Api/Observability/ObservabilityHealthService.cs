using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Observability;

public sealed class ObservabilityHealthService
{
    private readonly IApplicationDbContext _db;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ApiObservabilityMetrics _metrics;

    public ObservabilityHealthService(
        IApplicationDbContext db,
        IHostEnvironment environment,
        IConfiguration configuration,
        ApiObservabilityMetrics metrics)
    {
        _db = db;
        _environment = environment;
        _configuration = configuration;
        _metrics = metrics;
    }

    public async Task<ReadyHealthReport> BuildReadyAsync(string? correlationId, CancellationToken cancellationToken)
    {
        var checks = new List<HealthCheckReport>();

        await AddDatabaseCheckAsync(checks, cancellationToken);
        AddRuntimeCheck(checks);

        var status = checks.Any(x => string.Equals(x.Status, HealthStatuses.Unhealthy, StringComparison.Ordinal))
            ? HealthStatuses.Unhealthy
            : HealthStatuses.Ready;

        return new ReadyHealthReport(
            status,
            _configuration["Observability:ServiceName"] ?? "vpn-platform-api",
            _environment.EnvironmentName,
            correlationId ?? string.Empty,
            DateTimeOffset.UtcNow,
            (long)Math.Max(0, _metrics.Uptime.TotalSeconds),
            _metrics.RequestsStarted,
            _metrics.RequestsCompleted,
            _metrics.RequestsInFlight,
            checks);
    }

    private async Task AddDatabaseCheckAsync(List<HealthCheckReport> checks, CancellationToken cancellationToken)
    {
        try
        {
            var usersCount = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
            var activeTariffsCount = await _db.Tariffs.AsNoTracking()
                .CountAsync(x => x.IsActive, cancellationToken);
            var enabledPaymentProvidersCount = await _db.PaymentProviderAccounts.AsNoTracking()
                .CountAsync(x => x.IsEnabled && x.Mode != PaymentProviderMode.Disabled, cancellationToken);
            var pendingOutboxCount = await _db.OutboxMessages.AsNoTracking()
                .CountAsync(x => x.ProcessedAt == null && x.FailedAt == null, cancellationToken);
            var failedOutboxCount = await _db.OutboxMessages.AsNoTracking()
                .CountAsync(x => x.FailedAt != null, cancellationToken);
            var failedProvisioningCount = await _db.ProvisioningRuns.AsNoTracking()
                .CountAsync(x => x.Status == ProvisioningRunStatus.Failed || x.Status == ProvisioningRunStatus.PrecheckFailed, cancellationToken);
            var unhealthyNodesCount = await _db.VpnNodes.AsNoTracking()
                .CountAsync(x => x.HealthStatus == HealthStatus.Unhealthy || x.Status == NodeStatus.Error, cancellationToken);

            checks.Add(new HealthCheckReport(
                "database",
                HealthStatuses.Ready,
                "Запросы к БД выполняются.",
                new Dictionary<string, object>
                {
                    ["users"] = usersCount,
                    ["activeTariffs"] = activeTariffsCount,
                    ["enabledPaymentProviders"] = enabledPaymentProvidersCount,
                    ["pendingOutbox"] = pendingOutboxCount,
                    ["failedOutbox"] = failedOutboxCount,
                    ["failedProvisioning"] = failedProvisioningCount,
                    ["unhealthyNodes"] = unhealthyNodesCount
                }));
        }
        catch (Exception exception)
        {
            checks.Add(new HealthCheckReport(
                "database",
                HealthStatuses.Unhealthy,
                $"БД недоступна: {TrimMessage(exception.Message)}",
                null));
        }
    }

    private void AddRuntimeCheck(List<HealthCheckReport> checks)
    {
        checks.Add(new HealthCheckReport(
            "runtime",
            HealthStatuses.Ready,
            "Процесс API запущен.",
            new Dictionary<string, object>
            {
                ["startedAt"] = _metrics.StartedAt,
                ["uptimeSeconds"] = (long)Math.Max(0, _metrics.Uptime.TotalSeconds),
                ["requestsStarted"] = _metrics.RequestsStarted,
                ["requestsCompleted"] = _metrics.RequestsCompleted,
                ["requestsInFlight"] = _metrics.RequestsInFlight
            }));
    }

    private static string TrimMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "неизвестная ошибка";
        }

        var trimmed = message.Trim();
        return trimmed.Length <= 300 ? trimmed : trimmed[..300];
    }
}

public static class HealthStatuses
{
    public const string Ready = "Ready";
    public const string Unhealthy = "Unhealthy";
}

public sealed record ReadyHealthReport(
    string Status,
    string Service,
    string Environment,
    string CorrelationId,
    DateTimeOffset CheckedAt,
    long UptimeSeconds,
    long RequestsStarted,
    long RequestsCompleted,
    long RequestsInFlight,
    IReadOnlyList<HealthCheckReport> Checks);

public sealed record HealthCheckReport(
    string Name,
    string Status,
    string Message,
    IReadOnlyDictionary<string, object>? Data);
