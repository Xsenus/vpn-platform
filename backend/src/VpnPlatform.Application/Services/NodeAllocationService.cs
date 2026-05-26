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

    public async Task<VpnNode> SelectNodeAsync(Tariff tariff, CancellationToken cancellationToken = default)
    {
        var regionHints = SplitHints(tariff.AllowedRegionsCsv);
        var nodeGroupHints = SplitHints(tariff.AllowedNodeGroupsCsv);
        const string requiredProtocol = "vless";

        var query = _db.VpnNodes
            .Include(x => x.NodeGroup)
            .Where(x =>
                x.Status == NodeStatus.Ready &&
                x.IsAvailableForNewUsers &&
                x.HealthStatus != HealthStatus.Unhealthy &&
                x.UsedCapacity < x.Capacity &&
                (string.IsNullOrWhiteSpace(x.SupportedProtocolsCsv) || x.SupportedProtocolsCsv.ToLower().Contains(requiredProtocol)));

        if (regionHints.Length > 0)
        {
            query = query.Where(x => regionHints.Contains(x.Region));
        }

        if (nodeGroupHints.Length > 0)
        {
            query = query.Where(x => x.NodeGroup != null && nodeGroupHints.Contains(x.NodeGroup.Code));
        }

        var selected = await query
            .OrderBy(x => x.UsedCapacity * 1.0m / Math.Max(1, x.Capacity))
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (selected is not null)
        {
            return selected;
        }

        var panelQuery = _db.VpnPanels
            .Where(x =>
                x.Status == VpnPanelStatus.Active &&
                x.HealthStatus != HealthStatus.Unhealthy &&
                x.UsedCapacity < x.Capacity);
        if (regionHints.Length > 0)
        {
            panelQuery = panelQuery.Where(x => regionHints.Contains(x.Region));
        }

        var panel = await panelQuery
            .OrderBy(x => x.UsedCapacity * 1.0m / Math.Max(1, x.Capacity))
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

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
                SupportedProtocolsCsv = "vless",
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

    private static string[] SplitHints(string? value)
        => (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
