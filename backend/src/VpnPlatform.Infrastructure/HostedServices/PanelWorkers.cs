using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Infrastructure.HostedServices;

public class PanelHealthWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PanelHealthWorker> _logger;

    public PanelHealthWorker(IServiceScopeFactory scopeFactory, ILogger<PanelHealthWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessIterationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Panel health worker iteration failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    internal async Task ProcessIterationAsync(CancellationToken cancellationToken)
    {
        List<(Guid Id, DateTimeOffset? LastHealthCheckAt)> panels;
        using (var selectionScope = _scopeFactory.CreateScope())
        {
            var db = selectionScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var selected = db is DbContext dbContext && dbContext.Database.IsSqlite()
                ? await db.VpnPanels.FromSqlInterpolated($$"""
                    SELECT *
                    FROM "VpnPanels"
                    WHERE "Status" IN ({{VpnPanelStatus.Active}}, {{VpnPanelStatus.New}})
                    ORDER BY
                        CASE WHEN "LastHealthCheckAt" IS NULL THEN 0 ELSE 1 END,
                        julianday("LastHealthCheckAt"),
                        "Id"
                    LIMIT 10
                    """).AsNoTracking().ToListAsync(cancellationToken)
                : await db.VpnPanels.AsNoTracking()
                    .Where(x => x.Status == VpnPanelStatus.Active || x.Status == VpnPanelStatus.New)
                    .OrderBy(x => x.LastHealthCheckAt ?? DateTimeOffset.MinValue)
                    .ThenBy(x => x.Id)
                    .Take(10)
                    .ToListAsync(cancellationToken);
            panels = selected
                .Select(x => (x.Id, x.LastHealthCheckAt))
                .ToList();
        }

        foreach (var panel in panels)
        {
            try
            {
                using var panelScope = _scopeFactory.CreateScope();
                var db = panelScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var current = await db.VpnPanels.AsNoTracking()
                    .Where(x => x.Id == panel.Id)
                    .Select(x => new { x.Status, x.LastHealthCheckAt })
                    .FirstOrDefaultAsync(cancellationToken);
                if (current is null
                    || current.Status is not (VpnPanelStatus.Active or VpnPanelStatus.New)
                    || current.LastHealthCheckAt != panel.LastHealthCheckAt)
                {
                    continue;
                }

                var service = panelScope.ServiceProvider.GetRequiredService<X3UiPanelService>();
                var result = await service.CheckHealthIfCurrentAsync(panel.Id, panel.LastHealthCheckAt, cancellationToken);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Panel {PanelId} health check failed: {Error}", panel.Id, result.Error);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Panel {PanelId} health check iteration failed", panel.Id);
            }
        }
    }
}

public class PanelSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PanelSyncWorker> _logger;

    public PanelSyncWorker(IServiceScopeFactory scopeFactory, ILogger<PanelSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessIterationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Panel sync worker iteration failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    internal async Task ProcessIterationAsync(CancellationToken cancellationToken)
    {
        List<(Guid Id, DateTimeOffset? LastSyncAt)> panels;
        using (var selectionScope = _scopeFactory.CreateScope())
        {
            var db = selectionScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var selected = db is DbContext dbContext && dbContext.Database.IsSqlite()
                ? await db.VpnPanels.FromSqlInterpolated($$"""
                    SELECT *
                    FROM "VpnPanels"
                    WHERE "Status" = {{VpnPanelStatus.Active}}
                    ORDER BY
                        CASE WHEN "LastSyncAt" IS NULL THEN 0 ELSE 1 END,
                        julianday("LastSyncAt"),
                        "Id"
                    LIMIT 5
                    """).AsNoTracking().ToListAsync(cancellationToken)
                : await db.VpnPanels.AsNoTracking()
                    .Where(x => x.Status == VpnPanelStatus.Active)
                    .OrderBy(x => x.LastSyncAt ?? DateTimeOffset.MinValue)
                    .ThenBy(x => x.Id)
                    .Take(5)
                    .ToListAsync(cancellationToken);
            panels = selected
                .Select(x => (x.Id, x.LastSyncAt))
                .ToList();
        }

        foreach (var panel in panels)
        {
            try
            {
                using var panelScope = _scopeFactory.CreateScope();
                var db = panelScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var current = await db.VpnPanels.AsNoTracking()
                    .Where(x => x.Id == panel.Id)
                    .Select(x => new { x.Status, x.LastSyncAt })
                    .FirstOrDefaultAsync(cancellationToken);
                if (current is null
                    || current.Status != VpnPanelStatus.Active
                    || current.LastSyncAt != panel.LastSyncAt)
                {
                    continue;
                }

                var service = panelScope.ServiceProvider.GetRequiredService<X3UiPanelService>();
                var result = await service.SyncPanelIfCurrentAsync(panel.Id, panel.LastSyncAt, cancellationToken);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Panel {PanelId} sync failed: {Error}", panel.Id, result.Error);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Panel {PanelId} sync iteration failed", panel.Id);
            }
        }
    }
}
