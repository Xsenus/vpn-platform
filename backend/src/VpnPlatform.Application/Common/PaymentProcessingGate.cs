using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Common;

public static class PaymentProcessingGate
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, GateEntry> Gates = new(StringComparer.Ordinal);

    public static async ValueTask<IAsyncDisposable> AcquireOrderAsync(Guid orderId, CancellationToken cancellationToken)
        => await AcquireAsync($"order:{orderId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquirePaymentProviderConfigurationAsync(PaymentProvider provider, CancellationToken cancellationToken)
        => await AcquireAsync($"payment-provider-config:{(int)provider}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquirePaymentProviderAccountAsync(Guid accountId, CancellationToken cancellationToken)
        => await AcquireAsync($"payment-provider-account:{accountId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireWebhookAsync(string provider, string externalEventId, string providerPaymentId, CancellationToken cancellationToken)
        => await AcquireAsync($"webhook:{provider}:{externalEventId}:{providerPaymentId}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireTelegramUpdateAsync(long updateId, CancellationToken cancellationToken)
        => await AcquireAsync($"telegram-update:{updateId}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireTelegramDeliveryAsync(long updateId, CancellationToken cancellationToken)
        => await AcquireAsync($"telegram-delivery:{updateId}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireTelegramNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
        => await AcquireAsync($"telegram-notification:{notificationId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireOutboxMessageAsync(Guid messageId, CancellationToken cancellationToken)
        => await AcquireAsync($"outbox:{messageId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireProvisioningNodeAsync(Guid nodeId, CancellationToken cancellationToken)
        => await AcquireVpnNodeStateAsync(nodeId, cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireVpnNodeStateAsync(Guid nodeId, CancellationToken cancellationToken)
        => await AcquireAsync($"vpn-node-state:{nodeId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireSubscriptionLifecycleAsync(Guid subscriptionId, CancellationToken cancellationToken)
        => await AcquireAsync($"subscription-lifecycle:{subscriptionId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquirePanelHealthAsync(Guid panelId, CancellationToken cancellationToken)
        => await AcquireAsync($"panel-health:{panelId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquirePanelSyncAsync(Guid panelId, CancellationToken cancellationToken)
        => await AcquireAsync($"panel-sync:{panelId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireVpnPanelStateAsync(Guid panelId, CancellationToken cancellationToken)
        => await AcquireAsync($"vpn-panel-state:{panelId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireVpnSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
        => await AcquireAsync($"vpn-subscription:{subscriptionId:N}", cancellationToken);

    private static async ValueTask<IAsyncDisposable> AcquireAsync(string key, CancellationToken cancellationToken)
    {
        GateEntry entry;
        lock (SyncRoot)
        {
            if (!Gates.TryGetValue(key, out entry!))
            {
                entry = new GateEntry();
                Gates.Add(key, entry);
            }

            entry.ReferenceCount++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
            return new Releaser(key, entry);
        }
        catch
        {
            ReleaseReference(key, entry);
            throw;
        }
    }

    private static void ReleaseReference(string key, GateEntry entry)
    {
        lock (SyncRoot)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0
                && Gates.TryGetValue(key, out var current)
                && ReferenceEquals(current, entry))
            {
                Gates.Remove(key);
                entry.Semaphore.Dispose();
            }
        }
    }

    private sealed class GateEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private readonly string _key;
        private readonly GateEntry _entry;
        private bool _released;

        public Releaser(string key, GateEntry entry)
        {
            _key = key;
            _entry = entry;
        }

        public ValueTask DisposeAsync()
        {
            if (!_released)
            {
                _released = true;
                _entry.Semaphore.Release();
                ReleaseReference(_key, _entry);
            }

            return ValueTask.CompletedTask;
        }
    }
}
