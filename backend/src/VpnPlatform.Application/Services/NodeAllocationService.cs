using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
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
        if (!VpnProtocolPolicy.IsSupported(requiredProtocol))
        {
            throw new InvalidOperationException("Unsupported VPN protocol.");
        }
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
                !x.TagsCsv.ToLower().Contains("sandbox"));

        if (regionHints.Length > 0)
        {
            query = query.Where(x => regionHints.Contains(x.Region));
        }

        if (nodeGroupHints.Length > 0)
        {
            query = query.Where(x => x.NodeGroup != null && nodeGroupHints.Contains(x.NodeGroup.Code));
        }

        var protocolToken = $",{requiredProtocol},";
        query = query.Where(x =>
            x.SupportedProtocolsCsv.Trim() == string.Empty
            || ("," + x.SupportedProtocolsCsv.Replace(" ", string.Empty).ToLower() + ",").Contains(protocolToken));
        var selected = IsSqliteProvider()
            ? await SelectSqliteNodeAsync(regionHints, nodeGroupHints, requiredProtocol, serverSelectionRule, sandbox: false, cancellationToken)
            : await ApplyNodeOrdering(query, serverSelectionRule).FirstOrDefaultAsync(cancellationToken);

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

        var panel = IsSqliteProvider()
            ? await SelectSqlitePanelAsync(regionHints, serverSelectionRule, cancellationToken)
            : await ApplyPanelOrdering(panelQuery, serverSelectionRule).FirstOrDefaultAsync(cancellationToken);

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
        if (!VpnProtocolPolicy.IsSupported(requiredProtocol))
        {
            throw new InvalidOperationException("Unsupported VPN protocol.");
        }
        var protocolToken = $",{requiredProtocol},";
        var sandboxQuery = _db.VpnNodes
            .Where(x =>
                x.Provider == "x3ui" &&
                x.Region == "sandbox" &&
                x.Status == NodeStatus.Ready &&
                x.IsAvailableForNewUsers &&
                x.HealthStatus != HealthStatus.Unhealthy &&
                x.UsedCapacity < x.Capacity &&
                (x.SupportedProtocolsCsv.Trim() == string.Empty
                    || ("," + x.SupportedProtocolsCsv.Replace(" ", string.Empty).ToLower() + ",").Contains(protocolToken)));
        var sandboxNode = IsSqliteProvider()
            ? await SelectSqliteNodeAsync([], [], requiredProtocol, "least-loaded", sandbox: true, cancellationToken)
            : await ApplyNodeOrdering(sandboxQuery, "least-loaded").FirstOrDefaultAsync(cancellationToken);

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
        => string.IsNullOrWhiteSpace(value) ? "vless" : VpnProtocolPolicy.Normalize(value);

    private static string NormalizeRule(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static IOrderedQueryable<VpnNode> ApplyNodeOrdering(IQueryable<VpnNode> query, string rule)
        => rule switch
        {
            "priority-first" => query.OrderByDescending(x => x.Priority)
                .ThenBy(x => x.UsedCapacity * 1.0 / (x.Capacity == 0 ? 1 : x.Capacity))
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id),
            "newest" => query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.UsedCapacity * 1.0 / (x.Capacity == 0 ? 1 : x.Capacity))
                .ThenByDescending(x => x.Priority)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
        };

    private static IOrderedQueryable<VpnPanel> ApplyPanelOrdering(IQueryable<VpnPanel> query, string rule)
        => rule switch
        {
            "newest" => query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.UsedCapacity * 1.0 / (x.Capacity == 0 ? 1 : x.Capacity))
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
        };

    private async Task<VpnNode?> SelectSqliteNodeAsync(
        IReadOnlyCollection<string> regionHints,
        IReadOnlyCollection<string> nodeGroupHints,
        string requiredProtocol,
        string rule,
        bool sandbox,
        CancellationToken cancellationToken)
    {
        var parameters = new List<object>();
        string AddParameter(object value)
        {
            var placeholder = $"{{{parameters.Count}}}";
            parameters.Add(value);
            return placeholder;
        }

        var ready = AddParameter((int)NodeStatus.Ready);
        var unhealthy = AddParameter((int)HealthStatus.Unhealthy);
        var protocol = AddParameter($",{requiredProtocol},");
        var where = new List<string>
        {
            $"n.\"Status\" = {ready}",
            "n.\"IsAvailableForNewUsers\" = 1",
            $"n.\"HealthStatus\" <> {unhealthy}",
            "n.\"UsedCapacity\" < n.\"Capacity\"",
            $"(trim(n.\"SupportedProtocolsCsv\") = '' OR instr(',' || lower(replace(n.\"SupportedProtocolsCsv\", ' ', '')) || ',', {protocol}) > 0)"
        };

        if (sandbox)
        {
            where.Add($"n.\"Provider\" = {AddParameter("x3ui")}");
            where.Add($"n.\"Region\" = {AddParameter("sandbox")}");
        }
        else
        {
            where.Add($"n.\"Region\" <> {AddParameter("sandbox")}");
            where.Add($"n.\"Name\" <> {AddParameter("sandbox-vpn-node")}");
            where.Add("instr(lower(n.\"TagsCsv\"), 'sandbox') = 0");
            if (regionHints.Count > 0)
            {
                where.Add($"n.\"Region\" IN ({string.Join(", ", regionHints.Select(AddParameter))})");
            }
            if (nodeGroupHints.Count > 0)
            {
                where.Add($"g.\"Code\" IN ({string.Join(", ", nodeGroupHints.Select(AddParameter))})");
            }
        }

        var orderBy = rule switch
        {
            "priority-first" => "n.\"Priority\" DESC, CAST(n.\"UsedCapacity\" AS REAL) / CASE WHEN n.\"Capacity\" = 0 THEN 1 ELSE n.\"Capacity\" END, julianday(n.\"CreatedAt\"), n.\"Id\"",
            "newest" => "julianday(n.\"CreatedAt\") DESC, n.\"Id\"",
            _ => "CAST(n.\"UsedCapacity\" AS REAL) / CASE WHEN n.\"Capacity\" = 0 THEN 1 ELSE n.\"Capacity\" END, n.\"Priority\" DESC, julianday(n.\"CreatedAt\"), n.\"Id\""
        };
        var sql = $"""
            SELECT n.*
            FROM "VpnNodes" AS n
            LEFT JOIN "NodeGroups" AS g ON n."NodeGroupId" = g."Id"
            WHERE {string.Join(" AND ", where)}
            ORDER BY {orderBy}
            LIMIT 1
            """;

        return await _db.VpnNodes
            .FromSqlRaw(sql, parameters.ToArray())
            .Include(x => x.NodeGroup)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<VpnPanel?> SelectSqlitePanelAsync(
        IReadOnlyCollection<string> regionHints,
        string rule,
        CancellationToken cancellationToken)
    {
        var parameters = new List<object>();
        string AddParameter(object value)
        {
            var placeholder = $"{{{parameters.Count}}}";
            parameters.Add(value);
            return placeholder;
        }

        var where = new List<string>
        {
            $"\"Status\" = {AddParameter((int)VpnPanelStatus.Active)}",
            $"\"HealthStatus\" <> {AddParameter((int)HealthStatus.Unhealthy)}",
            $"\"Region\" <> {AddParameter("sandbox")}",
            $"\"Name\" <> {AddParameter("sandbox-x3ui-panel")}",
            $"\"BaseUrl\" <> {AddParameter("https://sandbox-node.local")}",
            "\"UsedCapacity\" < \"Capacity\""
        };
        if (regionHints.Count > 0)
        {
            where.Add($"\"Region\" IN ({string.Join(", ", regionHints.Select(AddParameter))})");
        }

        var orderBy = rule == "newest"
            ? "julianday(\"CreatedAt\") DESC, \"Id\""
            : "CAST(\"UsedCapacity\" AS REAL) / CASE WHEN \"Capacity\" = 0 THEN 1 ELSE \"Capacity\" END, julianday(\"CreatedAt\"), \"Id\"";
        var sql = $"""
            SELECT *
            FROM "VpnPanels"
            WHERE {string.Join(" AND ", where)}
            ORDER BY {orderBy}
            LIMIT 1
            """;

        return await _db.VpnPanels
            .FromSqlRaw(sql, parameters.ToArray())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private bool IsSqliteProvider()
        => _db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);
}
