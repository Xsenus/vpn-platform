namespace VpnPlatform.Application.Common;

public static class PaymentProcessingGate
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, GateEntry> Gates = new(StringComparer.Ordinal);

    public static async ValueTask<IAsyncDisposable> AcquireOrderAsync(Guid orderId, CancellationToken cancellationToken)
        => await AcquireAsync($"order:{orderId:N}", cancellationToken);

    public static async ValueTask<IAsyncDisposable> AcquireWebhookAsync(string provider, string externalEventId, string providerPaymentId, CancellationToken cancellationToken)
        => await AcquireAsync($"webhook:{provider}:{externalEventId}:{providerPaymentId}", cancellationToken);

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
