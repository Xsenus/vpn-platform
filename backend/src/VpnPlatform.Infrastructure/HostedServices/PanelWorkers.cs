using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Abstractions;
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
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<X3UiPanelService>();
                var panelCandidates = await db.VpnPanels.AsNoTracking()
                    .Where(x => x.Status == VpnPanelStatus.Active || x.Status == VpnPanelStatus.New)
                    .ToListAsync(stoppingToken);
                var panels = panelCandidates
                    .OrderBy(x => x.LastHealthCheckAt ?? DateTimeOffset.MinValue)
                    .Take(10)
                    .Select(x => x.Id)
                    .ToList();
                foreach (var id in panels)
                {
                    await service.CheckHealthAsync(id, stoppingToken);
                }
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
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<X3UiPanelService>();
                var panelCandidates = await db.VpnPanels.AsNoTracking()
                    .Where(x => x.Status == VpnPanelStatus.Active)
                    .ToListAsync(stoppingToken);
                var panels = panelCandidates
                    .OrderBy(x => x.LastSyncAt ?? DateTimeOffset.MinValue)
                    .Take(5)
                    .Select(x => x.Id)
                    .ToList();
                foreach (var id in panels)
                {
                    await service.SyncPanelAsync(id, stoppingToken);
                }
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
}
