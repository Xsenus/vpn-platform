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

public sealed record AdminBootstrapResult(
    string Email,
    string RolesCsv,
    bool Created,
    bool ExistingPasswordReset);

public sealed class AdminBootstrapService
{
    private readonly IPasswordService _passwordService;

    public AdminBootstrapService(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    public async Task<AdminBootstrapResult> BootstrapAsync(ApplicationDbContext db, AdminBootstrapOptions options, CancellationToken cancellationToken)
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

        var rolesCsv = UserRoles.NormalizeCsv(options.RolesCsv);
        var admin = await db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (admin is null)
        {
            db.Users.Add(new User
            {
                Email = normalizedEmail,
                DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? "Platform Admin" : options.DisplayName.Trim(),
                PasswordHash = _passwordService.Hash(options.Password),
                RolesCsv = rolesCsv,
                Status = UserStatus.Active,
                ReferralCode = $"ADM-{Guid.NewGuid():N}"[..10]
            });

            return new AdminBootstrapResult(normalizedEmail, rolesCsv, Created: true, ExistingPasswordReset: true);
        }

        admin.RolesCsv = rolesCsv;
        admin.Status = UserStatus.Active;
        admin.IsBlocked = false;
        admin.UpdatedAt = DateTimeOffset.UtcNow;

        if (options.ResetExistingPassword)
        {
            admin.PasswordHash = _passwordService.Hash(options.Password);
        }

        return new AdminBootstrapResult(normalizedEmail, rolesCsv, Created: false, ExistingPasswordReset: options.ResetExistingPassword);
    }

    private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();
}

public class DbInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbInitializer> _logger;
    private readonly IOptions<DatabaseStartupOptions> _databaseOptions;
    private readonly IOptions<AdminBootstrapOptions> _adminOptions;
    private readonly AdminBootstrapService _adminBootstrapService;

    public DbInitializer(
        IServiceProvider serviceProvider,
        ILogger<DbInitializer> logger,
        IOptions<DatabaseStartupOptions> databaseOptions,
        IOptions<AdminBootstrapOptions> adminOptions,
        AdminBootstrapService adminBootstrapService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _databaseOptions = databaseOptions;
        _adminOptions = adminOptions;
        _adminBootstrapService = adminBootstrapService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseOptions = _databaseOptions.Value;
        var adminOptions = _adminOptions.Value;

        if (DatabaseProviderConfigurator.IsSqlite(databaseOptions.Provider) && databaseOptions.UseEnsureCreatedForLocalSqlite)
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            var repairedColumns = await LocalSqliteSchemaRepair.ApplyAsync(db, cancellationToken);
            if (repairedColumns > 0)
            {
                _logger.LogInformation("Local SQLite schema repaired. ColumnsAdded={ColumnsAdded}", repairedColumns);
            }
        }
        else if (databaseOptions.ApplyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (adminOptions.Enabled)
        {
            var result = await _adminBootstrapService.BootstrapAsync(db, adminOptions, cancellationToken);
            _logger.LogInformation(
                "Admin bootstrap completed. Email={Email}, Roles={Roles}, Created={Created}, ExistingPasswordReset={ExistingPasswordReset}",
                result.Email,
                result.RolesCsv,
                result.Created,
                result.ExistingPasswordReset);
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

    internal static async Task SeedDemoDataAsync(ApplicationDbContext db, CancellationToken cancellationToken)
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

        var existingPaymentProviders = await db.PaymentProviderAccounts
            .Select(x => x.Provider)
            .Distinct()
            .ToListAsync(cancellationToken);
        var existingPaymentProviderSet = existingPaymentProviders.ToHashSet();
        var now = DateTimeOffset.UtcNow;
        var missingLocalSandboxProviders = LocalSandboxPaymentProviders(now)
            .Where(x => !existingPaymentProviderSet.Contains(x.Provider))
            .ToArray();
        if (missingLocalSandboxProviders.Length > 0)
        {
            db.PaymentProviderAccounts.AddRange(missingLocalSandboxProviders);
        }

        if (!await db.SiteContentBlocks.AnyAsync(cancellationToken))
        {
            db.SiteContentBlocks.AddRange(
                SiteContent("home.hero.eyebrow", "VPN Platform", "Hero eyebrow", "Надзаголовок первого экрана", 10),
                SiteContent("home.hero.title", "Быстрый VPN-доступ с оплатой и автоматической выдачей", "Hero title", "Главный заголовок лендинга", 20),
                SiteContent("home.hero.subtitle", "Выберите тариф, оплатите удобным способом и получите готовую ссылку подключения. Платформа объединяет витрину, личный кабинет, Telegram-бота, платежи, тарифы и управление серверами.", "Hero subtitle", "Текст под главным заголовком", 30, "textarea"),
                SiteContent("home.hero.primaryCta", "Выбрать тариф", "Основная CTA", "Текст кнопки перехода к тарифам", 40),
                SiteContent("home.hero.secondaryCta", "Войти или зарегистрироваться", "Вторичная CTA", "Текст кнопки перехода в аккаунт", 50),
                SiteContent("home.seo.title", "VPN Platform — быстрый VPN-доступ с автоматической выдачей", "SEO title", "Заголовок страницы для браузера и поисковиков", 60),
                SiteContent("home.seo.description", "Купите VPN-доступ онлайн: тарифы, оплата, личный кабинет, Telegram-бот и автоматическая выдача подключения.", "SEO description", "Описание главной страницы для поисковиков", 70, "textarea"),
                SiteContent("home.features.title", "Все ключевые сценарии продажи VPN в одной системе", "Заголовок возможностей", "Заголовок блока возможностей", 110),
                SiteContent("home.features.subtitle", "Лендинг ведет пользователя к тарифу, кабинет помогает завершить покупку, а админка дает контроль над тарифами, провайдерами, ботами, серверами и выдачей доступа.", "Описание возможностей", "Описание блока возможностей", 120, "textarea"),
                SiteContent("home.features.item1", "Автоматическая выдача VPN-доступа после подтверждения оплаты.", "Преимущество 1", "Пункт списка преимуществ на главной", 130, "textarea"),
                SiteContent("home.features.item2", "Тарифы, платежи, Telegram-боты и серверы управляются из админки.", "Преимущество 2", "Пункт списка преимуществ на главной", 140, "textarea"),
                SiteContent("home.features.item3", "Поддержка нескольких платежных провайдеров и безопасного sandbox-режима.", "Преимущество 3", "Пункт списка преимуществ на главной", 150, "textarea"),
                SiteContent("home.features.item4", "Личный кабинет хранит заказы, ссылки подключения и статус подписки.", "Преимущество 4", "Пункт списка преимуществ на главной", 160, "textarea"),
                SiteContent("home.pricing.title", "Понятные планы для разных сценариев", "Заголовок тарифов", "Заголовок preview-блока тарифов", 210),
                SiteContent("home.network.title", "Глобальная логика VPN-сервиса без ручной рутины", "Заголовок сети", "Заголовок блока сети", 310),
                SiteContent("home.network.subtitle", "Подключайте свои VPS, панели 3x-ui и правила выдачи доступов. Пользователь видит простой продукт, администратор управляет инфраструктурой.", "Описание сети", "Текст блока сети и локаций", 320, "textarea"),
                SiteContent("home.testimonials.title", "Пользовательский путь остается простым", "Заголовок отзывов", "Заголовок блока отзывов", 410),
                SiteContent("home.testimonials.item1.name", "Алексей", "Отзыв 1: имя", "Имя автора первого отзыва", 420),
                SiteContent("home.testimonials.item1.role", "предприниматель", "Отзыв 1: роль", "Роль автора первого отзыва", 421),
                SiteContent("home.testimonials.item1.text", "Оплатил тариф, получил ссылку подключения и сразу добавил ее на телефон и ноутбук.", "Отзыв 1: текст", "Текст первого отзыва", 422, "textarea"),
                SiteContent("home.testimonials.item2.name", "Марина", "Отзыв 2: имя", "Имя автора второго отзыва", 430),
                SiteContent("home.testimonials.item2.role", "удаленная работа", "Отзыв 2: роль", "Роль автора второго отзыва", 431),
                SiteContent("home.testimonials.item2.text", "Понравилось, что не нужно писать в поддержку после оплаты: доступ появляется автоматически.", "Отзыв 2: текст", "Текст второго отзыва", 432, "textarea"),
                SiteContent("home.testimonials.item3.name", "Игорь", "Отзыв 3: имя", "Имя автора третьего отзыва", 440),
                SiteContent("home.testimonials.item3.role", "администратор сервиса", "Отзыв 3: роль", "Роль автора третьего отзыва", 441),
                SiteContent("home.testimonials.item3.text", "В админке видно пользователей, платежи, тарифы и состояние VPN-серверов в одном месте.", "Отзыв 3: текст", "Текст третьего отзыва", 442, "textarea"),
                SiteContent("home.finalCta.title", "Готовы проверить покупку VPN?", "Финальный CTA", "Заголовок финального призыва", 510),
                SiteContent("home.finalCta.subtitle", "Начните с тарифа или войдите в кабинет, чтобы привязать заказ и получить ссылку подключения.", "Описание финального CTA", "Текст финального призыва", 520, "textarea"),
                SiteContent("home.footer.text", "VPN Platform объединяет продажи, оплату, выдачу и поддержку VPN-доступов в одном интерфейсе.", "Footer text", "Основной текст footer главной страницы", 610, "textarea"),
                SiteContent("home.footer.support", "Поддержка доступна через личный кабинет и Telegram-бота.", "Footer support", "Короткая подпись поддержки в footer", 620),
                SiteContent("home.errors.tariffsLoad", "Не удалось загрузить тарифы. Обновите страницу или попробуйте позже.", "Ошибка загрузки тарифов", "Сообщение на публичной странице тарифов", 710, "textarea"),
                SiteContent("home.errors.paymentProvidersLoad", "Не удалось загрузить способы оплаты. Покупка временно недоступна.", "Ошибка загрузки способов оплаты", "Сообщение при ошибке public providers API", 720, "textarea"),
                SiteContent("home.errors.noPaymentProviders", "Нет доступных платежных провайдеров. Попробуйте позже или обратитесь в поддержку.", "Нет платежных провайдеров", "Сообщение при попытке купить без доступного провайдера", 730, "textarea"),
                SiteContent("home.errors.checkoutCreate", "Не удалось создать покупку.", "Ошибка создания покупки", "Fallback-сообщение при ошибке checkout flow", 740, "textarea"),
                SiteContent("home.checkout.unavailable.loading", "Загружаем способы оплаты...", "Checkout loading", "Подсказка на кнопке покупки во время загрузки провайдеров", 750),
                SiteContent("home.checkout.unavailable.noProviders", "Оплата временно недоступна: нет включенных способов оплаты.", "Checkout no providers", "Подсказка на кнопке покупки без провайдеров", 760, "textarea"),
                SiteContent("home.checkout.unavailable.chooseProvider", "Выберите способ оплаты перед покупкой.", "Checkout choose provider", "Подсказка на кнопке покупки без выбранного провайдера", 770, "textarea"),
                SiteContent("home.checkout.providersEmptyTitle", "Нет доступных способов оплаты", "Заголовок пустого списка оплат", "Empty state title для способов оплаты", 780),
                SiteContent("home.checkout.providersEmptyDescription", "Покупка временно недоступна: нет включенного и настроенного способа оплаты.", "Описание пустого списка оплат", "Empty state description для способов оплаты", 790, "textarea"),
                SiteContent("home.checkout.settingsHint", "Если вы еще не вошли, мы сохраним выбранный тариф и попросим авторизоваться перед оплатой.", "Подсказка оформления", "Текст под настройками оформления покупки", 800, "textarea"),
                SiteContent("home.checkout.pendingAuthNotice", "Покупка создана. Войдите или зарегистрируйтесь, чтобы привязать заказ и перейти к оплате.", "Покупка ожидает входа", "Сообщение после создания checkout session без авторизации", 810, "textarea"),
                SiteContent("home.checkout.resultTitle", "Последняя покупка", "Заголовок результата покупки", "Заголовок блока с созданным платежом", 820),
                SiteContent("home.checkout.afterPaymentText", "После оплаты вернитесь в кабинет: статус заказа обновится автоматически, а VPN-доступ появится после подтверждения платежа.", "Текст после оплаты", "Инструкция в блоке созданной покупки", 830, "textarea"),
                SiteContent("home.checkout.openPaymentCta", "Открыть оплату", "CTA оплаты", "Текст кнопки перехода на оплату", 840),
                SiteContent("home.checkout.copyPaymentLink", "Скопировать ссылку", "Copy payment link", "Текст кнопки копирования ссылки оплаты", 850)
            );
        }

        if (!await db.WorkScenarios.AnyAsync(cancellationToken))
        {
            db.WorkScenarios.Add(new WorkScenario
            {
                Name = "Автоматическая выдача VPN",
                Key = "auto",
                IsActive = true,
                AllowedTariffIdsJson = "[]",
                VpnProtocol = "vless",
                ServerSelectionRule = "least-loaded",
                InboundSelectionRule = "default",
                ProvisioningMode = "auto",
                OnPaymentSucceeded = "create_subscription_and_access",
                OnPaymentFailed = "keep_order_pending",
                OnRefund = "disable_access",
                OnSubscriptionExpired = "disable_access_after_grace",
                OnRenewal = "extend_subscription",
                CabinetText = "После успешной оплаты VPN-доступ появится в личном кабинете вместе со ссылкой и QR-кодом.",
                TelegramText = "Оплата получена. VPN-доступ готов, ссылка подключения доступна в личном кабинете.",
                GenerateQrCode = true,
                MaxDevices = 3,
                SortOrder = 10
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

    private static SiteContentBlock SiteContent(string key, string value, string label, string description, int sortOrder, string inputType = "text")
        => new()
        {
            Key = key,
            Value = value,
            Group = "home",
            Label = label,
            Description = description,
            InputType = inputType,
            SortOrder = sortOrder,
            IsActive = true
        };

    private static PaymentProviderAccount LocalSandboxProvider(
        PaymentProvider provider,
        string name,
        string publicName,
        string shopId,
        string apiBaseUrl,
        DateTimeOffset now,
        bool yookassaIps = false,
        string extraSettingsJson = "{}")
        => new()
        {
            Provider = provider,
            Mode = PaymentProviderMode.Sandbox,
            Name = name,
            PublicName = publicName,
            IsEnabled = true,
            IsDefault = true,
            ShopId = shopId,
            ApiBaseUrl = apiBaseUrl,
            ReturnUrl = "http://localhost:5174/payments",
            WebhookUrl = $"http://localhost:8080/api/webhooks/payments/{provider.ToString().ToLowerInvariant()}",
            SecretKeyProtected = string.Empty,
            WebhookSecretProtected = string.Empty,
            UseWebhookIpAllowList = yookassaIps,
            AllowedWebhookIpRangesCsv = yookassaIps ? "185.71.76.0/27,185.71.77.0/27,77.75.153.0/25,77.75.156.11,77.75.156.35,77.75.154.128/25,2a02:5180::/32" : string.Empty,
            ExtraSettingsJson = extraSettingsJson,
            HealthStatus = HealthStatus.Unknown,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static IReadOnlyCollection<PaymentProviderAccount> LocalSandboxPaymentProviders(DateTimeOffset now)
        => new[]
        {
            LocalSandboxProvider(PaymentProvider.YooKassa, "yookassa-local", "YooKassa Sandbox", "local-yookassa-shop", "https://api.yookassa.ru/v3", now, yookassaIps: true),
            LocalSandboxProvider(PaymentProvider.RoboKassa, "robokassa-local", "RoboKassa Sandbox", "local-robokassa-merchant", "https://auth.robokassa.ru/Merchant/Index.aspx", now),
            LocalSandboxProvider(PaymentProvider.YooMoney, "yoomoney-local", "YooMoney Sandbox", "410000000000000", "https://yoomoney.ru/quickpay/confirm", now),
            LocalSandboxProvider(PaymentProvider.CloudPayments, "cloudpayments-local", "CloudPayments Sandbox", "local-cloudpayments-public-id", string.Empty, now, extraSettingsJson: """{"hostedCheckoutUrl":"http://localhost:5174/payments/cloudpayments-widget"}"""),
            LocalSandboxProvider(PaymentProvider.TBankAcquiring, "tbank-local", "TBank Sandbox", "local-tbank-terminal", "https://securepay.tinkoff.ru", now),
            LocalSandboxProvider(PaymentProvider.Prodamus, "prodamus-local", "Prodamus Sandbox", "local-prodamus-shop", "https://demo.payform.ru", now),
            LocalSandboxProvider(PaymentProvider.Stripe, "stripe-local", "Stripe Sandbox", "local-stripe-account", "https://api.stripe.com", now),
            LocalSandboxProvider(PaymentProvider.PayPal, "paypal-local", "PayPal Sandbox", "local-paypal-client", "https://api-m.sandbox.paypal.com", now),
            new PaymentProviderAccount
            {
                Provider = PaymentProvider.TelegramStars,
                Mode = PaymentProviderMode.Disabled,
                Name = "telegram-stars-bot-only",
                PublicName = "Telegram Stars (только Telegram-бот)",
                IsEnabled = false,
                IsDefault = false,
                ShopId = string.Empty,
                ApiBaseUrl = string.Empty,
                ReturnUrl = string.Empty,
                WebhookUrl = string.Empty,
                SecretKeyProtected = string.Empty,
                WebhookSecretProtected = string.Empty,
                ExtraSettingsJson = """{"status":"bot-only"}""",
                HealthStatus = HealthStatus.Unknown,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
