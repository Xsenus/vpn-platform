using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Services;
using VpnPlatform.Infrastructure.Persistence;

namespace VpnPlatform.Infrastructure.HostedServices;

public class SubscriptionLifecycleWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionLifecycleWorker> _logger;

    public SubscriptionLifecycleWorker(IServiceProvider serviceProvider, ILogger<SubscriptionLifecycleWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredService<OrderService>();
            var subscriptions = scope.ServiceProvider.GetRequiredService<SubscriptionService>();

            var expiredOrders = await orders.ExpirePendingOrdersAsync(stoppingToken);
            var processedSubscriptions = await subscriptions.ProcessLifecycleAsync(stoppingToken);

            if (expiredOrders > 0 || processedSubscriptions > 0)
            {
                _logger.LogInformation("Lifecycle worker updated orders={ExpiredOrders}, subscriptions={ProcessedSubscriptions}", expiredOrders, processedSubscriptions);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

public class OutboxDispatcherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcherWorker> _logger;

    public OutboxDispatcherWorker(IServiceProvider serviceProvider, ILogger<OutboxDispatcherWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOutboxBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var messages = await db.OutboxMessages
            .Where(x => x.ProcessedAt == null && x.Attempts < 10)
            .ToListAsync(cancellationToken);

        foreach (var message in messages.OrderBy(x => x.CreatedAt).Take(100))
        {
            try
            {
                message.Attempts += 1;
                message.ProcessedAt = DateTimeOffset.UtcNow;
                _logger.LogInformation("Outbox dispatched: {Type} {CorrelationId}", message.Type, message.CorrelationId);
            }
            catch (Exception ex)
            {
                message.LastError = ex.Message;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
