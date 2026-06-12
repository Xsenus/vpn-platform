using System.Collections.Concurrent;

namespace VpnPlatform.Application.Common;

public static class PaymentProcessingGate
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);

    public static async ValueTask<IAsyncDisposable> AcquireOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var key = $"order:{orderId:N}";
        var gate = Gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        return new Releaser(gate);
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private readonly SemaphoreSlim _gate;
        private bool _released;

        public Releaser(SemaphoreSlim gate) => _gate = gate;

        public ValueTask DisposeAsync()
        {
            if (!_released)
            {
                _released = true;
                _gate.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
