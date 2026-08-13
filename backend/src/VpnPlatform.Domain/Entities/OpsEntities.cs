using VpnPlatform.Domain.Common;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Domain.Entities;

public class ReferralProgram : AuditableEntity
{
    public int Revision { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string RuleDefinition { get; set; } = "{}";
    public string RewardDefinition { get; set; } = "{}";
    public string AntiFraudSettings { get; set; } = "{}";
}

public class ReferralRelationship : AuditableEntity
{
    public Guid ReferrerUserId { get; set; }
    public Guid ReferredUserId { get; set; }
    public ChannelType SourceChannel { get; set; }
    public bool IsSuspicious { get; set; }
}

public class RewardLedger : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? SourceUserId { get; set; }
    public Guid? ReferralProgramId { get; set; }
    public string Type { get; set; } = "bonus-days";
    public RewardStatus Status { get; set; } = RewardStatus.Pending;
    public decimal Value { get; set; }
    public string CurrencyOrUnit { get; set; } = "days";
    public DateTimeOffset? ProcessedAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public class NotificationTemplate : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public NotificationChannelType Channel { get; set; }
    public string Language { get; set; } = "ru";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class NotificationDelivery : AuditableEntity
{
    public Guid? UserId { get; set; }
    public Guid? SourceOutboxMessageId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public NotificationChannelType Channel { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;
    public string PayloadJson { get; set; } = "{}";
    public int Attempts { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? ErrorText { get; set; }
}

public class OutboxMessage : AuditableEntity
{
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string CorrelationId { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? LastError { get; set; }
}

public class InboxMessage : AuditableEntity
{
    public string Source { get; set; } = string.Empty;
    public string ExternalKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "received";
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
}

public class AuditLog : AuditableEntity
{
    public string ActorType { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public string Ip { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class ProvisioningRun : AuditableEntity
{
    public Guid NodeId { get; set; }
    public int Revision { get; set; }
    public ProvisioningRunStatus Status { get; set; } = ProvisioningRunStatus.Pending;
    public Guid? RequestedByUserId { get; set; }
    public bool DryRun { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public string? LastError { get; set; }
    public string ExecutionLog { get; set; } = string.Empty;

    public ICollection<ProvisioningStepRun> Steps { get; set; } = new List<ProvisioningStepRun>();
}

public class ProvisioningStepRun : AuditableEntity
{
    public Guid ProvisioningRunId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public ProvisioningRunStatus Status { get; set; } = ProvisioningRunStatus.Pending;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public string Output { get; set; } = string.Empty;
    public string ErrorText { get; set; } = string.Empty;
}

public class MigrationJob : AuditableEntity
{
    public Guid SourceNodeId { get; set; }
    public Guid? TargetNodeId { get; set; }
    public MigrationJobStatus Status { get; set; } = MigrationJobStatus.Planned;
    public string Type { get; set; } = "manual";
    public Guid? RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    public ICollection<MigrationItem> Items { get; set; } = new List<MigrationItem>();
}

public class MigrationItem : AuditableEntity
{
    public Guid MigrationJobId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid? OldAccessId { get; set; }
    public Guid? NewAccessId { get; set; }
    public MigrationJobStatus Status { get; set; } = MigrationJobStatus.Planned;
    public string ErrorText { get; set; } = string.Empty;
}

public class NodeHealthCheck : AuditableEntity
{
    public Guid NodeId { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;
    public long LatencyMs { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public string ErrorText { get; set; } = string.Empty;
}
