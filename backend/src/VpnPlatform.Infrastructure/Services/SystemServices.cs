using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.Infrastructure.Persistence;

namespace VpnPlatform.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public class PasswordService : IPasswordService
{
    public string Hash(string input)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(input, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string input, string hash)
    {
        var parts = hash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);

        var pbkdf2 = new Rfc2898DeriveBytes(input, salt, 100_000, HashAlgorithmName.SHA256);
        var actual = pbkdf2.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}

public class DbInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbInitializer> _logger;
    private readonly IOptions<DatabaseStartupOptions> _databaseOptions;
    private readonly IOptions<AdminBootstrapOptions> _adminOptions;

    public DbInitializer(
        IServiceProvider serviceProvider,
        ILogger<DbInitializer> logger,
        IOptions<DatabaseStartupOptions> databaseOptions,
        IOptions<AdminBootstrapOptions> adminOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _databaseOptions = databaseOptions;
        _adminOptions = adminOptions;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseOptions = _databaseOptions.Value;
        var adminOptions = _adminOptions.Value;

        if (databaseOptions.ApplyMigrationsOnStartup)
        {
            if (DatabaseProviderConfigurator.IsSqlite(databaseOptions.Provider) && databaseOptions.UseEnsureCreatedForLocalSqlite)
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
            else
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
        }

        if (adminOptions.Enabled)
        {
            await BootstrapAdminAsync(scope.ServiceProvider, db, adminOptions, cancellationToken);
        }

        await scope.ServiceProvider.GetRequiredService<AppReleaseSeedService>().SyncAsync(db, cancellationToken);

        if (databaseOptions.SeedDemoData)
        {
            await SeedDemoDataAsync(db, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Database startup tasks completed. MigrateOnStartup={MigrateOnStartup}, SeedDemoData={SeedDemoData}, AdminBootstrap={AdminBootstrap}",
            databaseOptions.ApplyMigrationsOnStartup,
            databaseOptions.SeedDemoData,
            adminOptions.Enabled);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task BootstrapAdminAsync(IServiceProvider serviceProvider, ApplicationDbContext db, AdminBootstrapOptions options, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(options.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new InvalidOperationException("AdminBootstrap:Email is required when AdminBootstrap:Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(options.Password) || options.Password.Length < 16)
        {
            throw new InvalidOperationException("AdminBootstrap:Password must contain at least 16 characters.");
        }

        var passwordService = serviceProvider.GetRequiredService<IPasswordService>();
        var admin = await db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (admin is null)
        {
            db.Users.Add(new User
            {
                Email = normalizedEmail,
                DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? "Platform Admin" : options.DisplayName.Trim(),
                PasswordHash = passwordService.Hash(options.Password),
                RolesCsv = UserRoles.NormalizeCsv(options.RolesCsv),
                Status = UserStatus.Active,
                ReferralCode = $"ADM-{Guid.NewGuid():N}"[..10]
            });
        }
        else
        {
            admin.RolesCsv = UserRoles.NormalizeCsv(options.RolesCsv);
            admin.Status = UserStatus.Active;
            admin.IsBlocked = false;
            admin.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Tariffs.AnyAsync(cancellationToken))
        {
            db.Tariffs.AddRange(
                new Tariff { Name = "1 месяц", Slug = "one-month", Description = "Базовый месячный тариф", FullDescription = "Подходит для личного использования и проверки качества VPN-доступа.", FeaturesJson = "[\"3 устройства\",\"Автоматическая выдача доступа\",\"QR-код и ссылка в кабинете\"]", Badge = "Популярный", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, TariffType = TariffType.Monthly, SortOrder = 10, Category = "standard", ProvisioningScenario = "auto", AfterPaymentText = "После оплаты система создаст VPN-доступ и покажет ссылку подключения в личном кабинете." },
                new Tariff { Name = "3 месяца", Slug = "three-months", Description = "Оптимальный квартальный тариф", FullDescription = "Для пользователей, которые хотят стабильный доступ на несколько месяцев без ежемесячного продления.", FeaturesJson = "[\"5 устройств\",\"Цена ниже помесячной\",\"Автоматическое продление доступа после оплаты\"]", Badge = "Выгодно", DurationDays = 90, Price = 1290m, Currency = "RUB", MaxDevices = 5, TariffType = TariffType.Quarterly, SortOrder = 20, Category = "standard", ProvisioningScenario = "auto", AfterPaymentText = "После оплаты подписка активируется на 90 дней, а данные подключения появятся в кабинете." },
                new Tariff { Name = "Пробный", Slug = "trial", Description = "Пробный доступ на 7 дней", FullDescription = "Короткий тестовый доступ для проверки скорости, региона и удобства подключения.", FeaturesJson = "[\"2 устройства\",\"7 дней доступа\",\"Можно перейти на платный тариф\"]", Badge = "Пробный", DurationDays = 7, Price = 0m, Currency = "RUB", MaxDevices = 2, TariffType = TariffType.Trial, IsTrial = true, SortOrder = 5, Category = "trial", ProvisioningScenario = "auto", AfterPaymentText = "Пробный доступ будет создан автоматически после оформления." }
            );
        }

        if (!await db.NotificationTemplates.AnyAsync(cancellationToken))
        {
            db.NotificationTemplates.Add(new NotificationTemplate
            {
                Key = "subscription_activated",
                Channel = NotificationChannelType.Email,
                Language = "ru",
                Subject = "Ваш VPN доступ готов",
                Body = "Подписка активирована. Данные подключения доступны в личном кабинете."
            });
        }

        if (!await db.FaqEntries.AnyAsync(cancellationToken))
        {
            db.FaqEntries.AddRange(
                new FaqEntry { Question = "Как подключиться?", Answer = "После оплаты вы получите ссылку, QR-код и инструкцию в личном кабинете.", Category = "Подключение", SortOrder = 10 },
                new FaqEntry { Question = "Можно ли продлить заранее?", Answer = "Да. При продлении срок подписки увеличивается корректно и не теряет уже оплаченные дни.", Category = "Оплата", SortOrder = 20 },
                new FaqEntry { Question = "Что делать, если доступ перестал работать?", Answer = "Откройте обращение в поддержку или проверьте актуальную ссылку подключения в кабинете.", Category = "Поддержка", SortOrder = 30 }
            );
        }
    }

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
