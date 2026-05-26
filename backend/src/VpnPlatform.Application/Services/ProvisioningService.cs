using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public sealed record OwnVpsProvisioningCommand(
    Guid? UserId,
    long? TelegramUserId,
    string Host,
    int SshPort,
    string Username,
    string AuthMethod,
    string Credential,
    string? DisplayName,
    string? Location,
    string Source,
    bool AutoDeployAfterPrecheck = true,
    bool ValidationMode = true);

public class ProvisioningService
{
    public const string LiveDeployDisabledError = "Live provisioning is disabled by default. Use validation/dry-run mode or enable explicit live provisioning on an approved staging target.";

    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly ISecretProtector? _secretProtector;

    public ProvisioningService(IApplicationDbContext db, IClock clock, ISecretProtector? secretProtector = null)
    {
        _db = db;
        _clock = clock;
        _secretProtector = secretProtector;
    }

    public async Task<Result<ProvisioningRun>> QueueAsync(Guid nodeId, bool dryRun, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken);
        if (node is null)
        {
            return Result<ProvisioningRun>.Failure("Node not found.");
        }

        if (!dryRun && !IsValidationNode(node) && !HasTag(node, "explicit-live-provisioning", "true"))
        {
            return Result<ProvisioningRun>.Failure(LiveDeployDisabledError);
        }

        if (string.IsNullOrWhiteSpace(node.Host) && string.IsNullOrWhiteSpace(node.IpAddress))
        {
            return Result<ProvisioningRun>.Failure("Target host or IP is required.");
        }

        if (node.SshPort <= 0 || node.SshPort > 65535)
        {
            return Result<ProvisioningRun>.Failure("SSH port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(node.SshUser))
        {
            return Result<ProvisioningRun>.Failure("SSH username is required.");
        }

        if (IsOwnVpsNode(node) && !CredentialsConfigured(node))
        {
            return Result<ProvisioningRun>.Failure("SSH credentials are required for own VPS provisioning.");
        }

        var alreadyRunning = await _db.ProvisioningRuns.AnyAsync(
            x => x.NodeId == nodeId && (x.Status == ProvisioningRunStatus.Pending
                || x.Status == ProvisioningRunStatus.Running
                || x.Status == ProvisioningRunStatus.PrecheckQueued
                || x.Status == ProvisioningRunStatus.Prechecking
                || x.Status == ProvisioningRunStatus.DeployQueued
                || x.Status == ProvisioningRunStatus.Deploying
                || x.Status == ProvisioningRunStatus.Retrying),
            cancellationToken);

        if (alreadyRunning)
        {
            return Result<ProvisioningRun>.Failure("Provisioning already queued for this node.");
        }

        var now = _clock.UtcNow;
        var status = dryRun ? ProvisioningRunStatus.PrecheckQueued : ProvisioningRunStatus.DeployQueued;
        var run = new ProvisioningRun
        {
            NodeId = nodeId,
            Status = status,
            RequestedByUserId = requestedByUserId,
            DryRun = dryRun,
            StartedAt = now,
            ExecutionLog = dryRun ? "Precheck queued." : "Deploy queued."
        };

        _db.ProvisioningRuns.Add(run);
        _db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            ProvisioningRunId = run.Id,
            StepName = dryRun ? "Precheck queued" : "Deploy queued",
            Status = status,
            StartedAt = now,
            FinishedAt = now,
            Output = dryRun ? "Safe precheck run queued." : "Safe deploy run queued."
        });

        node.ProvisioningStatus = status;
        node.UpdatedAt = now;
        if (!dryRun)
        {
            node.Status = NodeStatus.Provisioning;
            node.IsAvailableForNewUsers = false;
        }

        AddAudit("provisioning.queue", "ProvisioningRun", run.Id, requestedByUserId, "{}", JsonSerializer.Serialize(new { nodeId, dryRun, status = status.ToString(), validationMode = IsValidationNode(node) }));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ProvisioningRun>.Success(run);
    }

    public async Task<Result<ProvisioningRun>> CreateOwnVpsRequestAsync(OwnVpsProvisioningCommand command, CancellationToken cancellationToken = default)
    {
        var validation = ValidateOwnVpsCommand(command);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            return Result<ProvisioningRun>.Failure(validation);
        }

        var now = _clock.UtcNow;
        var host = NormalizeHost(command.Host);
        var authMethod = NormalizeAuthMethod(command.AuthMethod);
        var location = string.IsNullOrWhiteSpace(command.Location) ? "customer" : command.Location.Trim();
        var displayName = string.IsNullOrWhiteSpace(command.DisplayName) ? $"Customer VPS {host}" : command.DisplayName.Trim();
        var protectedCredential = IsProtectedCredential(command.Credential) ? command.Credential.Trim() : ProtectCredential(command.Credential);
        var node = new VpnNode
        {
            Name = displayName,
            Host = host,
            IpAddress = IPAddress.TryParse(host, out _) ? host : string.Empty,
            Provider = "customer-vps",
            Region = location,
            Country = location,
            Datacenter = "customer-owned",
            Capacity = 10,
            SupportedProtocolsCsv = "vless",
            Priority = 500,
            TagsCsv = BuildTags(new Dictionary<string, string?>
            {
                ["source"] = command.Source,
                ["owner"] = "customer",
                ["own-vps"] = "true",
                ["ssh-auth"] = authMethod,
                ["credentials"] = "protected",
                ["validation-mode"] = command.ValidationMode ? "true" : "false",
                ["autodeploy-after-precheck"] = command.AutoDeployAfterPrecheck ? "true" : "false",
                ["telegram-user-id"] = command.TelegramUserId?.ToString(),
                ["requested-user-id"] = command.UserId?.ToString()
            }),
            SshUser = command.Username.Trim(),
            SshPort = command.SshPort,
            ProtectedSshCredential = protectedCredential,
            SshCredentialRef = string.IsNullOrWhiteSpace(protectedCredential) ? string.Empty : $"secretref:ssh:{Guid.NewGuid():N}",
            SshPrivateKeyPath = string.Empty,
            SkipHostKeyChecking = true,
            PanelBaseUrl = $"https://{host}:2053",
            PanelUsername = "admin",
            PanelPassword = string.Empty,
            PanelInboundId = 1,
            PublicHostname = host,
            PublicPort = 443,
            Status = NodeStatus.New,
            HealthStatus = HealthStatus.Unknown,
            ProvisioningStatus = ProvisioningRunStatus.Requested,
            IsAvailableForNewUsers = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.VpnNodes.Add(node);
        var run = new ProvisioningRun
        {
            NodeId = node.Id,
            Status = ProvisioningRunStatus.PrecheckQueued,
            RequestedByUserId = command.UserId,
            DryRun = true,
            StartedAt = now,
            ExecutionLog = $"Own VPS precheck queued for {host}. Credentials are protected and never returned by API."
        };
        _db.ProvisioningRuns.Add(run);
        _db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            ProvisioningRunId = run.Id,
            StepName = "Validate input",
            Status = ProvisioningRunStatus.Succeeded,
            StartedAt = now,
            FinishedAt = now,
            Output = $"Host={host}; Port={command.SshPort}; Username={command.Username.Trim()}; AuthMethod={authMethod}; CredentialsConfigured=True; ValidationMode={command.ValidationMode}"
        });
        AddAudit("own_vps.request", "ProvisioningRun", run.Id, command.UserId, "{}", JsonSerializer.Serialize(new
        {
            host,
            command.SshPort,
            username = command.Username.Trim(),
            authMethod,
            credentialsConfigured = true,
            source = command.Source,
            validationMode = command.ValidationMode
        }));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ProvisioningRun>.Success(run);
    }

    public async Task<Result<ProvisioningRun>> QueueDeployAsync(Guid runId, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var original = await _db.ProvisioningRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId, cancellationToken);
        if (original is null)
        {
            return Result<ProvisioningRun>.Failure("Provisioning run not found.");
        }

        if (original.Status != ProvisioningRunStatus.ReadyToDeploy && original.Status != ProvisioningRunStatus.Succeeded)
        {
            return Result<ProvisioningRun>.Failure("Provisioning run is not ready to deploy.");
        }

        return await QueueAsync(original.NodeId, false, requestedByUserId, cancellationToken);
    }

    public async Task<Result<ProvisioningRun>> RetryAsync(Guid runId, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var original = await _db.ProvisioningRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId, cancellationToken);
        if (original is null)
        {
            return Result<ProvisioningRun>.Failure("Provisioning run not found.");
        }

        var queued = await QueueAsync(original.NodeId, original.DryRun, requestedByUserId, cancellationToken);
        if (queued.IsSuccess && queued.Value is not null)
        {
            queued.Value.Status = ProvisioningRunStatus.Retrying;
            queued.Value.ExecutionLog = "Retry queued for provisioning run.";
            await _db.SaveChangesAsync(cancellationToken);
        }
        return queued;
    }

    public async Task<Result<string>> CancelAsync(Guid runId, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await _db.ProvisioningRuns.FirstOrDefaultAsync(x => x.Id == runId, cancellationToken);
        if (run is null)
        {
            return Result<string>.Failure("Provisioning run not found.");
        }

        if (run.Status is ProvisioningRunStatus.Deployed or ProvisioningRunStatus.Succeeded or ProvisioningRunStatus.Failed or ProvisioningRunStatus.PrecheckFailed)
        {
            return Result<string>.Failure("Only pending/running provisioning runs can be cancelled.");
        }

        run.Status = ProvisioningRunStatus.Cancelled;
        run.FinishedAt = _clock.UtcNow;
        run.ExecutionLog = AppendLog(run.ExecutionLog, "Provisioning run cancelled by operator.");
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == run.NodeId, cancellationToken);
        if (node is not null)
        {
            node.ProvisioningStatus = ProvisioningRunStatus.Cancelled;
            node.Status = NodeStatus.New;
            node.UpdatedAt = _clock.UtcNow;
        }
        AddAudit("provisioning.cancel", "ProvisioningRun", run.Id, requestedByUserId, "{}", JsonSerializer.Serialize(new { runId }));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<string>.Success("cancelled");
    }

    public async Task<Result<string>> MarkSupportNeededAsync(Guid runId, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await _db.ProvisioningRuns.FirstOrDefaultAsync(x => x.Id == runId, cancellationToken);
        if (run is null)
        {
            return Result<string>.Failure("Provisioning run not found.");
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.NodeId, cancellationToken);
        var userId = run.RequestedByUserId;
        var telegramUserId = node is null ? null : ExtractLongTag(node.TagsCsv, "telegram-user-id");
        var conversation = await EnsureSupportConversationAsync(userId, telegramUserId, "Own VPS provisioning needs support", $"Provisioning run {run.Id} requires support. Node: {node?.Name ?? run.NodeId.ToString()}. Status: {run.Status}. {run.ExecutionLog}", cancellationToken);
        AddAudit("provisioning.support_needed", "ProvisioningRun", run.Id, requestedByUserId, "{}", JsonSerializer.Serialize(new { supportConversationId = conversation.Id }));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<string>.Success(conversation.Id.ToString());
    }

    private static string? ValidateOwnVpsCommand(OwnVpsProvisioningCommand command)
    {
        if (command.UserId is null || command.UserId == Guid.Empty)
        {
            return "Telegram account must be linked or registered before own VPS provisioning.";
        }

        var host = NormalizeHost(command.Host);
        if (!IsValidHost(host))
        {
            return "Invalid host: enter a valid IPv4/IPv6 address or DNS hostname.";
        }

        if (command.SshPort <= 0 || command.SshPort > 65535)
        {
            return "Invalid SSH port: enter a value between 1 and 65535.";
        }

        if (string.IsNullOrWhiteSpace(command.Username))
        {
            return "SSH username is required.";
        }

        var authMethod = NormalizeAuthMethod(command.AuthMethod);
        if (authMethod != "password" && authMethod != "ssh_key")
        {
            return "Unsupported auth method. Use password or ssh_key.";
        }

        if (string.IsNullOrWhiteSpace(command.Credential))
        {
            return "SSH password/private key is required.";
        }

        return null;
    }

    public static bool CredentialsConfigured(VpnNode node)
        => IsProtectedCredential(node.ProtectedSshCredential)
            || !string.IsNullOrWhiteSpace(node.SshCredentialRef)
            || (!string.IsNullOrWhiteSpace(node.SshPrivateKeyPath)
                && (node.SshPrivateKeyPath.StartsWith("v1:", StringComparison.Ordinal)
                    || node.SshPrivateKeyPath.StartsWith("validation-placeholder:", StringComparison.Ordinal)
                    || !node.SshPrivateKeyPath.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase)));

    public static bool PanelPasswordConfigured(VpnNode node)
        => IsProtectedCredential(node.ProtectedPanelPassword)
            || !string.IsNullOrWhiteSpace(node.PanelSecretRef)
            || !string.IsNullOrWhiteSpace(node.PanelPassword);

    public static string GetSshAuthMethod(VpnNode node)
        => ExtractTag(node.TagsCsv, "ssh-auth")
            ?? (CredentialsConfigured(node) ? "ssh_key" : "not_configured");

    public static bool IsOwnVpsNode(VpnNode node)
        => HasTag(node, "own-vps", "true") || string.Equals(node.Provider, "customer-vps", StringComparison.OrdinalIgnoreCase);

    public static bool IsValidationNode(VpnNode node)
        => HasTag(node, "validation-mode", "true");

    public static bool ShouldAutoDeployAfterPrecheck(VpnNode node)
        => HasTag(node, "autodeploy-after-precheck", "true");

    public static string RedactSensitiveText(string? value, int maxLength = 4000)
        => SensitiveDataRedactor.Redact(value, maxLength: maxLength);

    public static string AppendLog(string current, string line)
        => string.IsNullOrWhiteSpace(current) ? line : current.TrimEnd() + Environment.NewLine + line;

    public static bool IsProtectedCredential(string? credential)
        => !string.IsNullOrWhiteSpace(credential) && (credential.StartsWith("v1:", StringComparison.Ordinal) || credential.StartsWith("validation-placeholder:", StringComparison.Ordinal));

    public string ProtectCredential(string credential)
    {
        if (_secretProtector is not null)
        {
            return _secretProtector.Protect(credential.Trim());
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(credential.Trim()));
        return "validation-placeholder:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string NormalizeAuthMethod(string? authMethod)
    {
        var normalized = (authMethod ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_');
        return normalized switch
        {
            "key" or "sshkey" or "private_key" or "ssh_private_key" => "ssh_key",
            "pass" or "passwd" or "password" => "password",
            _ => normalized
        };
    }

    public static string NormalizeHost(string host)
        => (host ?? string.Empty).Trim().TrimEnd('/');

    public static bool IsValidHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host.Length > 253)
        {
            return false;
        }

        if (IPAddress.TryParse(host, out _))
        {
            return true;
        }

        if (host.Contains('/') || host.Contains(' ') || host.Contains(':'))
        {
            return false;
        }

        return Regex.IsMatch(host, @"^(?=.{1,253}$)([a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)*[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$", RegexOptions.CultureInvariant);
    }

    public static string BuildTags(IReadOnlyDictionary<string, string?> tags)
        => string.Join(',', tags.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"{x.Key}:{x.Value}"));

    public static string? ExtractTag(string tagsCsv, string key)
    {
        foreach (var raw in (tagsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = raw.IndexOf(':');
            if (separator <= 0) continue;
            var tagKey = raw[..separator];
            if (string.Equals(tagKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return raw[(separator + 1)..];
            }
        }
        return null;
    }

    public static long? ExtractLongTag(string tagsCsv, string key)
        => long.TryParse(ExtractTag(tagsCsv, key), out var value) ? value : null;

    public static bool HasTag(VpnNode node, string key, string value)
        => string.Equals(ExtractTag(node.TagsCsv, key), value, StringComparison.OrdinalIgnoreCase);

    private async Task<SupportConversation> EnsureSupportConversationAsync(Guid? userId, long? telegramUserId, string subject, string message, CancellationToken cancellationToken)
    {
        var conversation = await _db.SupportConversations
            .Where(x => x.UserId == userId && x.TelegramUserId == telegramUserId && x.Status == "open" && x.Subject == subject)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            conversation = new SupportConversation
            {
                UserId = userId,
                TelegramUserId = telegramUserId,
                Channel = "telegram",
                Status = "open",
                Subject = subject
            };
            _db.SupportConversations.Add(conversation);
        }

        _db.SupportMessages.Add(new SupportMessage
        {
            SupportConversation = conversation,
            UserId = userId,
            TelegramUserId = telegramUserId,
            Direction = "internal",
            Text = RedactSensitiveText(message),
            IsInternalNote = true,
            AttachmentsJson = "[]"
        });
        conversation.UpdatedAt = _clock.UtcNow;
        return conversation;
    }

    private void AddAudit(string action, string entityType, Guid entityId, Guid? actorId, string beforeJson, string afterJson)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = actorId.HasValue ? "user" : "system",
            ActorId = actorId?.ToString() ?? "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            BeforeJson = RedactSensitiveText(beforeJson),
            AfterJson = RedactSensitiveText(afterJson),
            Ip = string.Empty,
            UserAgent = string.Empty
        });
    }
}
