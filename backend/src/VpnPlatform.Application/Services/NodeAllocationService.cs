using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class NodeAllocationService
{
    public const string NoAvailableNodeError = "No available VPN node for provisioning access";

    private readonly IApplicationDbContext _db;

    public NodeAllocationService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<VpnNode> SelectNodeAsync(Tariff tariff, WorkScenario? scenario = null, CancellationToken cancellationToken = default)
    {
        var regionHints = SplitHints(tariff.AllowedRegionsCsv);
        var nodeGroupHints = SplitHints(tariff.AllowedNodeGroupsCsv);
        var requiredProtocol = NormalizeProtocol(scenario?.VpnProtocol);
        var serverSelectionRule = NormalizeRule(scenario?.ServerSelectionRule, "least-loaded");

        var query = _db.VpnNodes
            .Include(x => x.NodeGroup)
            .Where(x =>
                x.Status == NodeStatus.Ready &&
                x.IsAvailableForNewUsers &&
                x.HealthStatus != HealthStatus.Unhealthy &&
                x.UsedCapacity < x.Capacity &&
                x.Region != "sandbox" &&
                x.Name != "sandbox-vpn-node" &&
                !x.TagsCsv.ToLower().Contains("sandbox") &&
                (string.IsNullOrWhiteSpace(x.SupportedProtocolsCsv) || x.SupportedProtocolsCsv.ToLower().Contains(requiredProtocol)));

        if (regionHints.Length > 0)
        {
            query = query.Where(x => regionHints.Contains(x.Region));
        }

        if (nodeGroupHints.Length > 0)
        {
            query = query.Where(x => x.NodeGroup != null && nodeGroupHints.Contains(x.NodeGroup.Code));
        }

        var selected = await ApplyNodeOrdering(query, serverSelectionRule).FirstOrDefaultAsync(cancellationToken);

        if (selected is not null)
        {
            return selected;
        }

        var panelQuery = _db.VpnPanels
            .Where(x =>
                x.Status == VpnPanelStatus.Active &&
                x.HealthStatus != HealthStatus.Unhealthy &&
                x.Region != "sandbox" &&
                x.Name != "sandbox-x3ui-panel" &&
                x.BaseUrl != "https://sandbox-node.local" &&
                x.UsedCapacity < x.Capacity);
        if (regionHints.Length > 0)
        {
            panelQuery = panelQuery.Where(x => regionHints.Contains(x.Region));
        }

        var panel = await ApplyPanelOrdering(panelQuery, serverSelectionRule).FirstOrDefaultAsync(cancellationToken);

        if (panel is not null)
        {
            var host = Uri.TryCreate(panel.BaseUrl, UriKind.Absolute, out var uri) ? uri.Host : panel.BaseUrl;
            var node = new VpnNode
            {
                Name = panel.Name,
                Host = host,
                IpAddress = host,
                Provider = "x3ui",
                Region = panel.Region,
                Country = string.Empty,
                Datacenter = string.Empty,
                Status = NodeStatus.Ready,
                Capacity = panel.Capacity,
                UsedCapacity = panel.UsedCapacity,
                HealthStatus = panel.HealthStatus,
                IsAvailableForNewUsers = true,
                SupportedProtocolsCsv = requiredProtocol,
                PanelBaseUrl = panel.BaseUrl,
                PanelUsername = panel.Login,
                PublicHostname = host,
                PublicPort = 443
            };
            _db.VpnNodes.Add(node);
            await _db.SaveChangesAsync(cancellationToken);
            return node;
        }

        throw new InvalidOperationException(NoAvailableNodeError);
    }

    public async Task<VpnNode> SelectOrCreateSandboxNodeAsync(string? protocol, CancellationToken cancellationToken = default)
    {
        var requiredProtocol = NormalizeProtocol(protocol);
        var sandboxNode = await _db.VpnNodes
            .Where(x =>
                x.Provider == "x3ui" &&
                x.Region == "sandbox" &&
                x.Status == NodeStatus.Ready &&
                x.IsAvailableForNewUsers &&
                x.HealthStatus != HealthStatus.Unhealthy &&
                x.UsedCapacity < x.Capacity &&
                (string.IsNullOrWhiteSpace(x.SupportedProtocolsCsv) || x.SupportedProtocolsCsv.ToLower().Contains(requiredProtocol)))
            .OrderBy(x => x.UsedCapacity * 1.0m / Math.Max(1, x.Capacity))
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (sandboxNode is not null)
        {
            return sandboxNode;
        }

        sandboxNode = new VpnNode
        {
            Name = "sandbox-vpn-node",
            Host = "sandbox-node.local",
            IpAddress = "sandbox-node.local",
            Provider = "x3ui",
            Region = "sandbox",
            Country = "sandbox",
            Datacenter = "local",
            Status = NodeStatus.Ready,
            Capacity = 100000,
            UsedCapacity = 0,
            SupportedProtocolsCsv = requiredProtocol,
            HealthStatus = HealthStatus.Healthy,
            LastHealthCheckAt = DateTimeOffset.UtcNow,
            ProvisioningStatus = ProvisioningRunStatus.Succeeded,
            InstalledVersion = "sandbox",
            BackupStatus = "disabled",
            MonitoringStatus = "sandbox",
            LoggingStatus = "sandbox",
            TagsCsv = "sandbox,auto-created",
            Priority = 1000,
            IsAvailableForNewUsers = true,
            PanelBaseUrl = "https://sandbox-node.local",
            PanelUsername = "sandbox",
            PublicHostname = "sandbox-node.local",
            PublicPort = 443
        };

        _db.VpnNodes.Add(sandboxNode);
        await _db.SaveChangesAsync(cancellationToken);
        return sandboxNode;
    }

    public static bool IsSandboxNode(VpnNode? node)
        => node is not null
           && (string.Equals(node.Region, "sandbox", StringComparison.OrdinalIgnoreCase)
               || string.Equals(node.Name, "sandbox-vpn-node", StringComparison.OrdinalIgnoreCase)
               || string.Equals(node.PanelBaseUrl, "https://sandbox-node.local", StringComparison.OrdinalIgnoreCase)
               || SplitHints(node.TagsCsv).Any(x => string.Equals(x, "sandbox", StringComparison.OrdinalIgnoreCase)));

    private static string[] SplitHints(string? value)
        => (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string NormalizeProtocol(string? value)
        => string.IsNullOrWhiteSpace(value) ? "vless" : value.Trim().ToLowerInvariant();

    private static string NormalizeRule(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static IOrderedQueryable<VpnNode> ApplyNodeOrdering(IQueryable<VpnNode> query, string rule)
        => rule switch
        {
            "priority-first" => query.OrderByDescending(x => x.Priority).ThenBy(x => x.UsedCapacity * 1.0m / Math.Max(1, x.Capacity)).ThenBy(x => x.CreatedAt),
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            _ => query.OrderBy(x => x.UsedCapacity * 1.0m / Math.Max(1, x.Capacity)).ThenByDescending(x => x.Priority).ThenBy(x => x.CreatedAt)
        };

    private static IOrderedQueryable<VpnPanel> ApplyPanelOrdering(IQueryable<VpnPanel> query, string rule)
        => rule switch
        {
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            _ => query.OrderBy(x => x.UsedCapacity * 1.0m / Math.Max(1, x.Capacity)).ThenBy(x => x.CreatedAt)
        };
}
