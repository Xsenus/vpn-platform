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
        var candidates = _db.Database.IsSqlite()
            ? await _db.ProvisioningRuns.FromSqlInterpolated($$"""
                SELECT run.*
                FROM "ProvisioningRuns" AS run
                WHERE run."Status" IN (0, 8, 12, 15)
                  AND EXISTS (
                    SELECT 1
                    FROM "VpnNodes" AS node
                    WHERE node."Id" = run."NodeId"
                      AND node."Status" <> {{(int)NodeStatus.Archived}}
                  )
                ORDER BY julianday(run."CreatedAt"), run."Id"
                LIMIT {{limit}}
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await _db.ProvisioningRuns.AsNoTracking()
                .Where(x => QueuedStatuses.Contains(x.Status)
                    && _db.VpnNodes.Any(node => node.Id == x.NodeId && node.Status != NodeStatus.Archived))
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);
        return candidates
            .Select(x => x.Id)
            .ToList();
    }

    public async Task<bool> TryClaimAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var candidate = await _db.ProvisioningRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == runId
                && QueuedStatuses.Contains(x.Status)
                && _db.VpnNodes.Any(node => node.Id == x.NodeId && node.Status != NodeStatus.Archived), cancellationToken);
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
            var nodeCanRun = tracked is not null && await _db.VpnNodes.AsNoTracking()
                .AnyAsync(node => node.Id == tracked.NodeId && node.Status != NodeStatus.Archived, cancellationToken);
            if (tracked is null || !nodeCanRun || tracked.Status != candidate.Status || tracked.UpdatedAt != candidate.UpdatedAt)
            {
                return false;
            }

            ApplyClaim(tracked, claimedStatus, now, version);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _db.ProvisioningRuns
            .Where(x => x.Id == runId
                && x.Status == candidate.Status
                && x.UpdatedAt == candidate.UpdatedAt
                && _db.VpnNodes.Any(node => node.Id == x.NodeId && node.Status != NodeStatus.Archived))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, claimedStatus)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                .SetProperty(x => x.ProcessingStartedAt, now)
                .SetProperty(x => x.LeaseExpiresAt, now.Add(_leaseDuration))
                .SetProperty(x => x.StartedAt, now)
                .SetProperty(x => x.FinishedAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastError, (string?)null)
                .SetProperty(x => x.Revision, x => x.Revision + 1)
                .SetProperty(x => x.UpdatedAt, version), cancellationToken);
        return affected == 1;
    }

    public async Task<int> RecoverExpiredClaimsAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var legacyStaleBefore = now.Subtract(_leaseDuration);
        const int recoveryBatchSize = 100;
        var processing = _db.Database.IsSqlite()
            ? await _db.ProvisioningRuns.FromSqlInterpolated($$"""
                SELECT *
                FROM "ProvisioningRuns"
                WHERE "Status" IN (1, 9, 13)
                  AND (("LeaseExpiresAt" IS NOT NULL AND julianday("LeaseExpiresAt") <= julianday({{now}}))
                    OR ("LeaseExpiresAt" IS NULL AND julianday("UpdatedAt") <= julianday({{legacyStaleBefore}})))
                ORDER BY julianday(COALESCE("LeaseExpiresAt", "UpdatedAt")), "Id"
                LIMIT {{recoveryBatchSize}}
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await _db.ProvisioningRuns.AsNoTracking()
                .Where(x => ProcessingStatuses.Contains(x.Status)
                    && (x.LeaseExpiresAt <= now
                        || (!x.LeaseExpiresAt.HasValue && x.UpdatedAt <= legacyStaleBefore)))
                .OrderBy(x => x.LeaseExpiresAt ?? x.UpdatedAt)
                .ThenBy(x => x.Id)
                .Take(recoveryBatchSize)
                .ToListAsync(cancellationToken);
        var recovered = 0;
        foreach (var run in processing)
        {
            if (await MarkClaimFailedAsync(
                    run.Id,
                    run.Status,
                    run.Revision,
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
                run.Revision,
                run.UpdatedAt,
                error,
                "Worker execution failed",
                cancellationToken);
    }

    private async Task<bool> MarkClaimFailedAsync(
        Guid runId,
        ProvisioningRunStatus expectedStatus,
        int expectedRevision,
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
            if (tracked is null || tracked.Status != expectedStatus || tracked.Revision != expectedRevision || tracked.UpdatedAt != expectedVersion)
            {
                return false;
            }

            ApplyFailure(tracked, failedStatus, redactedError, now, version);
            affected = 1;
        }
        else
        {
            affected = await _db.ProvisioningRuns
                .Where(x => x.Id == runId && x.Status == expectedStatus && x.Revision == expectedRevision && x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, failedStatus)
                    .SetProperty(x => x.ProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.LeaseExpiresAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.FinishedAt, now)
                    .SetProperty(x => x.LastError, redactedError)
                    .SetProperty(x => x.Revision, x => x.Revision + 1)
                    .SetProperty(x => x.UpdatedAt, version), cancellationToken);
        }

        if (affected != 1)
        {
            return false;
        }

        var run = await _db.ProvisioningRuns.FirstAsync(x => x.Id == runId, cancellationToken);
        await _db.Entry(run).ReloadAsync(cancellationToken);
        run.ExecutionLog = ProvisioningService.AppendLog(run.ExecutionLog, redactedError);
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == run.NodeId, cancellationToken);
        if (node is not null && node.Status != NodeStatus.Archived)
        {
            node.ProvisioningStatus = failedStatus;
            node.Status = NodeStatus.Error;
            node.IsAvailableForNewUsers = false;
            node.Revision = checked(node.Revision + 1);
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
        run.Revision = checked(run.Revision + 1);
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
        run.Revision = checked(run.Revision + 1);
        run.UpdatedAt = version;
    }

    private static DateTimeOffset NextVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);

    private bool IsInMemoryProvider()
        => string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
