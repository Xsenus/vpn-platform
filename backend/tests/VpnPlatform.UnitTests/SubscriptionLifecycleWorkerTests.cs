using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.HostedServices;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SubscriptionLifecycleWorkerTests
{
    [Fact]
    public async Task Worker_Iteration_Should_Process_Subscriptions_When_Order_Batch_Fails()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var services = new ServiceCollection()
            .AddScoped(_ => new ApplicationDbContext(options))
            .AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>())
            .AddSingleton<IClock, WorkerClock>()
            .AddSingleton<IVpnProviderFactory, UnusedVpnProviderFactory>()
            .AddScoped<NodeAllocationService>()
            .AddScoped<SubscriptionService>()
            .BuildServiceProvider();
        Guid orderId;
        Guid subscriptionId;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            orderId = Guid.NewGuid();
            subscriptionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var tariffId = Guid.NewGuid();
            db.Users.Add(new User { Id = userId, Email = "worker@example.test", DisplayName = "Worker", Status = UserStatus.Active });
            db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Worker tariff", Slug = "worker-tariff", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
            db.Orders.Add(new Order
            {
                Id = orderId,
                UserId = userId,
                TariffId = tariffId,
                Status = OrderStatus.PendingPayment,
                ExpiresAt = WorkerClock.Now.AddMinutes(-1),
                Currency = "RUB"
            });
            db.Subscriptions.Add(new Subscription
            {
                Id = subscriptionId,
                UserId = userId,
                TariffId = tariffId,
                Status = SubscriptionStatus.GracePeriod,
                StartAt = WorkerClock.Now.AddDays(-31),
                EndAt = WorkerClock.Now.AddDays(-1),
                GracePeriodEndAt = WorkerClock.Now.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }
        var logger = new CapturingLogger<SubscriptionLifecycleWorker>();
        var worker = new SubscriptionLifecycleWorker(services, logger);

        await worker.ProcessIterationAsync(CancellationToken.None);

        Assert.IsType<InvalidOperationException>(logger.LastException);
        using var assertScope = services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(OrderStatus.PendingPayment, (await assertDb.Orders.SingleAsync(x => x.Id == orderId)).Status);
        Assert.Equal(SubscriptionStatus.Expired, (await assertDb.Subscriptions.SingleAsync(x => x.Id == subscriptionId)).Status);
    }

    private sealed class WorkerClock : IClock
    {
        public static DateTimeOffset Now { get; } = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class UnusedVpnProviderFactory : IVpnProviderFactory
    {
        public IVpnProvider Get(string providerName) => throw new InvalidOperationException("VPN provider should not be used before grace period ends.");
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public Exception? LastException { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LastException = exception ?? LastException;
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
