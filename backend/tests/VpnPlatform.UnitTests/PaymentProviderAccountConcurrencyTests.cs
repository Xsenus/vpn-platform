using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentProviderAccountConcurrencyTests
{
    [Fact]
    public async Task Concurrent_Default_Creation_Should_Leave_One_Default_Account()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-payment-account-{Guid.NewGuid():N}.db");
        try
        {
            var options = SqliteOptions(databasePath);
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
            }

            var coordinator = new SaveCoordinator();
            await using var firstDb = new CoordinatedSaveDbContext(options, coordinator);
            await using var secondDb = new CoordinatedSaveDbContext(options, coordinator);
            var first = new PaymentProviderAccountService(firstDb, new TestSecretProtector(), new TestClock());
            var second = new PaymentProviderAccountService(secondDb, new TestSecretProtector(), new TestClock());

            var results = await Task.WhenAll(
                first.UpsertAsync(null, Command("yookassa-first")),
                second.UpsertAsync(null, Command("yookassa-second")));

            Assert.All(results, result => Assert.True(result.IsSuccess, result.Error));
            await using var verify = new ApplicationDbContext(options);
            Assert.Equal(2, await verify.PaymentProviderAccounts.CountAsync(x => x.Provider == PaymentProvider.YooKassa));
            Assert.Equal(1, await verify.PaymentProviderAccounts.CountAsync(x => x.Provider == PaymentProvider.YooKassa && x.IsDefault));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task Database_Should_Reject_Multiple_Default_Accounts_For_Provider()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.PaymentProviderAccounts.AddRange(
            Account("first-default"),
            Account("second-default"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Migration_Should_Clean_Existing_Default_Duplicates_Before_Creating_Index()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("DROP INDEX \"IX_PaymentProviderAccounts_Provider\";");
        var older = Account("older-default");
        older.CreatedAt = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(5));
        older.UpdatedAt = older.CreatedAt;
        var newer = Account("newer-default");
        newer.CreatedAt = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        newer.UpdatedAt = newer.CreatedAt;
        db.PaymentProviderAccounts.AddRange(older, newer);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Equal(
            1,
            await LocalSqliteSchemaRepair.PrepareMigrationsAsync(
                db,
                new DateTimeOffset(2035, 6, 7, 8, 9, 10, TimeSpan.Zero)));

        var migrations = db.GetService<IMigrationsAssembly>();
        var migrationEntry = migrations.Migrations.Single(x => x.Key.EndsWith("_PaymentProviderDefaultUniqueness", StringComparison.Ordinal));
        var migration = migrations.CreateMigration(migrationEntry.Value, db.Database.ProviderName!);
        var sqlGenerator = db.GetService<IMigrationsSqlGenerator>();
        foreach (var command in sqlGenerator.Generate(migration.UpOperations, db.Model))
        {
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        }

        var accounts = await db.PaymentProviderAccounts.AsNoTracking()
            .Where(x => x.Provider == PaymentProvider.YooKassa)
            .ToListAsync();
        Assert.Equal(2, accounts.Count);
        Assert.False(accounts.Single(x => x.Id == older.Id).IsDefault);
        Assert.True(accounts.Single(x => x.Id == newer.Id).IsDefault);
        db.PaymentProviderAccounts.Add(Account("third-default"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Switching_Default_Should_Keep_One_Default_With_Unique_Index()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var first = Account("current-default");
        var second = Account("next-default");
        second.IsDefault = false;
        db.PaymentProviderAccounts.AddRange(first, second);
        await db.SaveChangesAsync();
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());

        var result = await service.UpsertAsync(second.Id, Command(second.Name) with { Revision = second.Revision });

        Assert.True(result.IsSuccess, result.Error);
        db.ChangeTracker.Clear();
        var accounts = await db.PaymentProviderAccounts.AsNoTracking()
            .Where(x => x.Provider == PaymentProvider.YooKassa)
            .ToListAsync();
        Assert.False(accounts.Single(x => x.Id == first.Id).IsDefault);
        Assert.True(accounts.Single(x => x.Id == second.Id).IsDefault);
    }

    [Fact]
    public async Task Unchanged_Provider_Account_Update_Should_Not_Write()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var account = Account("unchanged-provider");
        account.CreatedAt = new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);
        account.UpdatedAt = account.CreatedAt;
        db.PaymentProviderAccounts.Add(account);
        await db.SaveChangesAsync();
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());

        var result = await service.UpsertAsync(account.Id, Command(account.Name) with
        {
            ReturnUrl = string.Empty,
            WebhookUrl = string.Empty,
            SecretKey = string.Empty,
            WebhookSecret = string.Empty,
            UseWebhookIpAllowList = account.UseWebhookIpAllowList,
            Revision = account.Revision
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("измен", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(account.CreatedAt, account.UpdatedAt);
    }

    [Fact]
    public async Task Stale_Provider_Account_Update_Should_Preserve_Winning_Configuration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var account = Account("stale-provider");
        db.PaymentProviderAccounts.Add(account);
        await db.SaveChangesAsync();
        var staleRevision = account.Revision;
        account.PublicName = "Внешнее актуальное название";
        account.Revision = checked(account.Revision + 1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());

        var result = await service.UpsertAsync(account.Id, Command(account.Name) with
        {
            PublicName = "Устаревшее локальное название",
            ReturnUrl = string.Empty,
            WebhookUrl = string.Empty,
            SecretKey = string.Empty,
            WebhookSecret = string.Empty,
            Revision = staleRevision
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(PaymentProviderAccountService.AccountChangedError, result.Error);
        var persisted = await db.PaymentProviderAccounts.AsNoTracking().SingleAsync(x => x.Id == account.Id);
        Assert.Equal("Внешнее актуальное название", persisted.PublicName);
        Assert.Equal(staleRevision + 1, persisted.Revision);
    }

    [Fact]
    public async Task Duplicate_Provider_Mode_Name_Should_Return_Failure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());
        var first = await service.UpsertAsync(null, Command("duplicate-name", isDefault: false));

        var duplicate = await service.UpsertAsync(null, Command("duplicate-name", isDefault: false));

        Assert.True(first.IsSuccess, first.Error);
        Assert.False(duplicate.IsSuccess);
        Assert.Contains("уже существует", duplicate.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.PaymentProviderAccounts.CountAsync(x => x.Name == "duplicate-name"));
    }

    [Fact]
    public async Task Provider_Account_Should_Reject_Undefined_Provider_And_Mode_Without_Persistence()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());
        var valid = Command("invalid-enum", isDefault: false);

        var invalidProvider = await service.UpsertAsync(null, valid with { Provider = (PaymentProvider)999 });
        var invalidMode = await service.UpsertAsync(null, valid with { Mode = (PaymentProviderMode)999 });

        Assert.False(invalidProvider.IsSuccess);
        Assert.Contains("провайдер", invalidProvider.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(invalidMode.IsSuccess);
        Assert.Contains("режим", invalidMode.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.PaymentProviderAccounts.ToListAsync());
    }

    [Theory]
    [InlineData("https://operator:secret@api.example.test", "https://cabinet.example.test/return", "https://api.example.test/webhook", "{}")]
    [InlineData("https://api.example.test", "https://operator:secret@cabinet.example.test/return", "https://api.example.test/webhook", "{}")]
    [InlineData("https://api.example.test", "https://cabinet.example.test/return", "https://operator:secret@api.example.test/webhook", "{}")]
    [InlineData("https://api.example.test", "https://cabinet.example.test/return", "https://api.example.test/webhook", "{\"hostedCheckoutUrl\":\"https://operator:secret@pay.example.test\"}")]
    public async Task Provider_Account_Should_Reject_Credential_Bearing_Urls_Without_Persistence(
        string apiBaseUrl,
        string returnUrl,
        string webhookUrl,
        string extraSettingsJson)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());

        var result = await service.UpsertAsync(null, new UpsertPaymentProviderAccountCommand(
            PaymentProvider.YooKassa,
            PaymentProviderMode.Sandbox,
            "unsafe-url",
            "Unsafe URL",
            IsEnabled: true,
            IsDefault: false,
            ShopId: "unsafe-url",
            ApiBaseUrl: apiBaseUrl,
            ReturnUrl: returnUrl,
            WebhookUrl: webhookUrl,
            SecretKey: "provider-secret",
            WebhookSecret: "webhook-secret",
            UseWebhookIpAllowList: false,
            AllowedWebhookIpRangesCsv: string.Empty,
            ExtraSettingsJson: extraSettingsJson));

        Assert.False(result.IsSuccess);
        Assert.Contains("логин или пароль", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.PaymentProviderAccounts.ToListAsync());
    }

    [Fact]
    public async Task NonUnique_Persistence_Failure_Should_Not_Be_Masked_As_Conflict()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payment-account-failure-{Guid.NewGuid():N}")
            .Options;
        await using var db = new FailingSaveDbContext(options);
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());

        var error = await Assert.ThrowsAsync<DbUpdateException>(
            () => service.UpsertAsync(null, Command("storage-failure", isDefault: false)));

        Assert.Contains("storage unavailable", error.InnerException!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Account_State_Actions_Should_Wait_For_Account_Gate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payment-account-state-{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);
        var account = Account("state-gate");
        db.PaymentProviderAccounts.Add(account);
        await db.SaveChangesAsync();
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());

        await using (var enabledGate = await PaymentProcessingGate.AcquirePaymentProviderAccountAsync(account.Id, CancellationToken.None))
        {
            var enabledTask = service.SetEnabledAsync(account.Id, enabled: false, account.Revision);
            await Task.Delay(100);
            Assert.False(enabledTask.IsCompleted);
            await enabledGate.DisposeAsync();
            Assert.True((await enabledTask).IsSuccess);
        }

        await using (var checkGate = await PaymentProcessingGate.AcquirePaymentProviderAccountAsync(account.Id, CancellationToken.None))
        {
            var checkTask = service.CheckAsync(account.Id);
            await Task.Delay(100);
            Assert.False(checkTask.IsCompleted);
            await checkGate.DisposeAsync();
            Assert.True((await checkTask).IsSuccess);
        }
    }

    [Fact]
    public async Task Legacy_Credential_Bearing_Account_Should_Not_Be_Enabled_Or_Selected_For_Checkout()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var account = Account("legacy-unsafe-url");
        account.IsEnabled = false;
        account.ApiBaseUrl = "https://operator:secret@api.example.test";
        db.PaymentProviderAccounts.Add(account);
        await db.SaveChangesAsync();
        var service = new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock());

        var enable = await service.SetEnabledAsync(account.Id, enabled: true, account.Revision);

        Assert.False(enable.IsSuccess);
        Assert.Contains("логин или пароль", enable.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(account.IsEnabled);

        account.IsEnabled = true;
        await db.SaveChangesAsync();
        var checkout = await service.GetWebCheckoutAccountEntityAsync(PaymentProvider.YooKassa);

        Assert.False(checkout.IsSuccess);
        Assert.Contains("логин или пароль", checkout.Error, StringComparison.OrdinalIgnoreCase);

        var fallback = Account("safe-fallback");
        fallback.IsDefault = false;
        db.PaymentProviderAccounts.Add(fallback);
        await db.SaveChangesAsync();

        var fallbackCheckout = await service.GetWebCheckoutAccountEntityAsync(PaymentProvider.YooKassa);

        Assert.True(fallbackCheckout.IsSuccess, fallbackCheckout.Error);
        Assert.Equal(fallback.Id, fallbackCheckout.Value!.Id);
    }

    private static DbContextOptions<ApplicationDbContext> SqliteOptions(string path)
        => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={path};Default Timeout=30;Pooling=False")
            .Options;

    private static UpsertPaymentProviderAccountCommand Command(string name, bool isDefault = true)
        => new(
            PaymentProvider.YooKassa,
            PaymentProviderMode.Sandbox,
            name,
            name,
            IsEnabled: true,
            IsDefault: isDefault,
            ShopId: name,
            ApiBaseUrl: "https://api.yookassa.ru",
            ReturnUrl: "https://cabinet.example.test/payments/return",
            WebhookUrl: "https://api.example.test/api/webhooks/payments/yookassa",
            SecretKey: $"secret-{name}",
            WebhookSecret: $"webhook-{name}",
            UseWebhookIpAllowList: false,
            AllowedWebhookIpRangesCsv: string.Empty,
            ExtraSettingsJson: "{}");

    private static PaymentProviderAccount Account(string name)
        => new()
        {
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            Name = name,
            PublicName = name,
            IsEnabled = true,
            IsDefault = true,
            ShopId = name,
            ApiBaseUrl = "https://api.yookassa.ru",
            SecretKeyProtected = $"secret-{name}"
        };

    private sealed class SaveCoordinator
    {
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrivals) >= 2)
            {
                _release.TrySetResult(true);
            }

            var timeout = Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            if (await Task.WhenAny(_release.Task, timeout) != _release.Task)
            {
                _release.TrySetResult(true);
            }

            await _release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class CoordinatedSaveDbContext(
        DbContextOptions<ApplicationDbContext> options,
        SaveCoordinator coordinator) : ApplicationDbContext(options)
    {
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await coordinator.WaitAsync(cancellationToken);
            return await base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class FailingSaveDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromException<int>(new DbUpdateException("simulated persistence failure", new InvalidOperationException("storage unavailable")));
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => "***";
    }
}
