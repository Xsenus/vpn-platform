using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Provisioning;

namespace VpnPlatform.Infrastructure.HostedServices;

public sealed class ProvisioningRunCoordinator
{
    private static readonly ProvisioningRunStatus[] QueuedStatuses =
    [
        ProvisioningRunStatus.Pending,
        ProvisioningRunStatus.PrecheckQueued,
        ProvisioningRunStatus.DeployQueued,
        ProvisioningRunStatus.Retrying
    ];

    private static readonly ProvisioningRunStatus[] ProcessingStatuses =
    [
        ProvisioningRunStatus.Running,
        ProvisioningRunStatus.Prechecking,
        ProvisioningRunStatus.Deploying
    ];

    private readonly ApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly TimeSpan _leaseDuration;

    public ProvisioningRunCoordinator(
        ApplicationDbContext db,
        IClock clock,
        IOptions<ProvisioningOptions> options)
    {
        _db = db;
        _clock = clock;
        var timeoutSeconds = Math.Clamp(options.Value.ExecutionTimeoutSeconds, 1, 86_400);
        _leaseDuration = TimeSpan.FromSeconds(timeoutSeconds).Add(TimeSpan.FromMinutes(5));
    }

    public async Task<IReadOnlyList<Guid>> GetClaimableIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 100);
        var candidates = await _db.ProvisioningRuns.AsNoTracking()
            .Where(x => QueuedStatuses.Contains(x.Status))
            .ToListAsync(cancellationToken);
        return candidates
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Take(limit)
            .Select(x => x.Id)
            .ToList();
    }

    public async Task<bool> TryClaimAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var candidate = await _db.ProvisioningRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == runId && QueuedStatuses.Contains(x.Status), cancellationToken);
        if (candidate is null)
        {
            return false;
        }

        var now = _clock.UtcNow;
        var claimedStatus = candidate.DryRun ? ProvisioningRunStatus.Prechecking : ProvisioningRunStatus.Deploying;
        var version = NextVersion(candidate.UpdatedAt, now);
        if (IsInMemoryProvider())
        {
            var tracked = await _db.ProvisioningRuns.FirstOrDefaultAsync(x => x.Id == runId, cancellationToken);
            if (tracked is null || tracked.Status != candidate.Status || tracked.UpdatedAt != candidate.UpdatedAt)
            {
                return false;
            }

            ApplyClaim(tracked, claimedStatus, now, version);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _db.ProvisioningRuns
            .Where(x => x.Id == runId && x.Status == candidate.Status && x.UpdatedAt == candidate.UpdatedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, claimedStatus)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                .SetProperty(x => x.ProcessingStartedAt, now)
                .SetProperty(x => x.LeaseExpiresAt, now.Add(_leaseDuration))
                .SetProperty(x => x.StartedAt, now)
                .SetProperty(x => x.FinishedAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastError, (string?)null)
                .SetProperty(x => x.UpdatedAt, version), cancellationToken);
        return affected == 1;
    }

    public async Task<int> RecoverExpiredClaimsAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var legacyStaleBefore = now.Subtract(_leaseDuration);
        var processing = await _db.ProvisioningRuns.AsNoTracking()
            .Where(x => ProcessingStatuses.Contains(x.Status))
            .ToListAsync(cancellationToken);
        var recovered = 0;
        foreach (var run in processing
                     .Where(x => x.LeaseExpiresAt <= now
                         || (!x.LeaseExpiresAt.HasValue && x.UpdatedAt <= legacyStaleBefore))
                     .OrderBy(x => x.CreatedAt)
                     .ThenBy(x => x.Id))
        {
            if (await MarkClaimFailedAsync(
                    run.Id,
                    run.Status,
                    run.UpdatedAt,
                    "Provisioning worker lease expired. Automatic replay is blocked because an external deploy may have partially completed.",
                    "Worker lease recovery",
                    cancellationToken))
            {
                recovered++;
            }
        }

        return recovered;
    }

    public async Task<bool> FailClaimedRunAsync(
        Guid runId,
        string error,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.ProvisioningRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == runId && ProcessingStatuses.Contains(x.Status), cancellationToken);
        return run is not null
            && await MarkClaimFailedAsync(
                run.Id,
                run.Status,
                run.UpdatedAt,
                error,
                "Worker execution failed",
                cancellationToken);
    }

    private async Task<bool> MarkClaimFailedAsync(
        Guid runId,
        ProvisioningRunStatus expectedStatus,
        DateTimeOffset expectedVersion,
        string error,
        string stepName,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var failedStatus = expectedStatus == ProvisioningRunStatus.Prechecking
            ? ProvisioningRunStatus.PrecheckFailed
            : ProvisioningRunStatus.Failed;
        var redactedError = ProvisioningService.RedactSensitiveText(error, 1000);
        var version = NextVersion(expectedVersion, now);
        int affected;
        if (IsInMemoryProvider())
        {
            var tracked = await _db.ProvisioningRuns.FirstOrDefaultAsync(x => x.Id == runId, cancellationToken);
            if (tracked is null || tracked.Status != expectedStatus || tracked.UpdatedAt != expectedVersion)
            {
                return false;
            }

            ApplyFailure(tracked, failedStatus, redactedError, now, version);
            affected = 1;
        }
        else
        {
            affected = await _db.ProvisioningRuns
                .Where(x => x.Id == runId && x.Status == expectedStatus && x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, failedStatus)
                    .SetProperty(x => x.ProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.LeaseExpiresAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.FinishedAt, now)
                    .SetProperty(x => x.LastError, redactedError)
                    .SetProperty(x => x.UpdatedAt, version), cancellationToken);
        }

        if (affected != 1)
        {
            return false;
        }

        var run = await _db.ProvisioningRuns.FirstAsync(x => x.Id == runId, cancellationToken);
        run.ExecutionLog = ProvisioningService.AppendLog(run.ExecutionLog, redactedError);
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == run.NodeId, cancellationToken);
        if (node is not null)
        {
            node.ProvisioningStatus = failedStatus;
            node.Status = NodeStatus.Error;
            node.IsAvailableForNewUsers = false;
            node.UpdatedAt = now;
        }

        _db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            ProvisioningRunId = run.Id,
            StepName = stepName,
            Status = failedStatus,
            StartedAt = now,
            FinishedAt = now,
            Output = "Provisioning was stopped and requires an explicit operator retry.",
            ErrorText = redactedError
        });
        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = "system",
            ActorId = "provisioning-worker",
            Action = "provisioning.worker_claim_failed",
            EntityType = "ProvisioningRun",
            EntityId = run.Id.ToString(),
            BeforeJson = JsonSerializer.Serialize(new { status = expectedStatus.ToString() }),
            AfterJson = JsonSerializer.Serialize(new { status = failedStatus.ToString(), error = redactedError }),
            Ip = string.Empty,
            UserAgent = string.Empty
        });
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void ApplyClaim(
        ProvisioningRun run,
        ProvisioningRunStatus status,
        DateTimeOffset now,
        DateTimeOffset version)
    {
        run.Status = status;
        run.AttemptCount++;
        run.ProcessingStartedAt = now;
        run.LeaseExpiresAt = now.Add(_leaseDuration);
        run.StartedAt = now;
        run.FinishedAt = null;
        run.LastError = null;
        run.UpdatedAt = version;
    }

    private static void ApplyFailure(
        ProvisioningRun run,
        ProvisioningRunStatus status,
        string error,
        DateTimeOffset now,
        DateTimeOffset version)
    {
        run.Status = status;
        run.ProcessingStartedAt = null;
        run.LeaseExpiresAt = null;
        run.FinishedAt = now;
        run.LastError = error;
        run.UpdatedAt = version;
    }

    private static DateTimeOffset NextVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);

    private bool IsInMemoryProvider()
        => string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
