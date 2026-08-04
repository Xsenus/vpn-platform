using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Services;

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
            try
            {
                await ProcessIterationAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal async Task ProcessIterationAsync(CancellationToken cancellationToken)
    {
        var expiredOrders = 0;
        var processedSubscriptions = 0;

        try
        {
            using var orderScope = _serviceProvider.CreateScope();
            var orders = orderScope.ServiceProvider.GetRequiredService<OrderService>();
            expiredOrders = await orders.ExpirePendingOrdersAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pending order expiry iteration failed");
        }

        try
        {
            using var subscriptionScope = _serviceProvider.CreateScope();
            var subscriptions = subscriptionScope.ServiceProvider.GetRequiredService<SubscriptionService>();
            processedSubscriptions = await subscriptions.ProcessLifecycleAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscription lifecycle iteration failed");
        }

        if (expiredOrders > 0 || processedSubscriptions > 0)
        {
            _logger.LogInformation("Lifecycle worker updated orders={ExpiredOrders}, subscriptions={ProcessedSubscriptions}", expiredOrders, processedSubscriptions);
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
        var delivery = scope.ServiceProvider.GetRequiredService<OutboxMessageDeliveryService>();
        var messageIds = await delivery.GetDispatchableIdsAsync(100, cancellationToken);
        foreach (var messageId in messageIds)
        {
            var result = await delivery.DeliverAsync(messageId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Outbox message {MessageId} delivery deferred or failed: {Error}",
                    messageId,
                    result.Error);
            }
        }
    }
}
