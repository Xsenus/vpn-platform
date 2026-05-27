using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Infrastructure.Auth;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.Infrastructure.HostedServices;
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Provisioning;
using VpnPlatform.Infrastructure.Security;
using VpnPlatform.Infrastructure.Services;
using VpnPlatform.Infrastructure.Vpn;

namespace VpnPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool includeHostedServices = true, bool includeOperationalWorkers = true)
    {
        services.AddOptions<DatabaseStartupOptions>()
            .Bind(configuration.GetSection("Database"));
        services.AddOptions<AdminBootstrapOptions>()
            .Bind(configuration.GetSection("AdminBootstrap"));
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection("Cors"));
        services.AddOptions<TelegramBotOptions>()
            .Bind(configuration.GetSection("TelegramBot"));

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseConfiguredDatabase(configuration);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddHttpClient("YooKassa", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("VpnPlatform/1.0");
        });
        foreach (var clientName in new[] { "Stripe", "PayPal", "TBankAcquiring" })
        {
            services.AddHttpClient(clientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("VpnPlatform/1.0");
            });
        }

        services.AddHttpClient("X3Ui", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("VpnPlatform/1.0");
        });

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddScoped<ITelegramInvoiceProvider, DisabledTelegramInvoiceProvider>();

        services.AddScoped<IPaymentProvider, YooMoneyPaymentProvider>();
        services.AddScoped<IPaymentProvider, YooKassaPaymentProvider>();
        services.AddScoped<IPaymentProvider, RoboKassaPaymentProvider>();
        services.AddScoped<IPaymentProvider, TelegramStarsPaymentProvider>();
        services.AddScoped<IPaymentProvider, CloudPaymentsPaymentProvider>();
        services.AddScoped<IPaymentProvider, TBankAcquiringPaymentProvider>();
        services.AddScoped<IPaymentProvider, ProdamusPaymentProvider>();
        services.AddScoped<IPaymentProvider, StripePaymentProvider>();
        services.AddScoped<IPaymentProvider, PayPalPaymentProvider>();
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentWebhookVerifier, YooKassaPaymentProvider>();
        services.AddScoped<IPaymentWebhookVerifier, RoboKassaPaymentProvider>();
        services.AddScoped<IPaymentWebhookVerifier, YooMoneyPaymentProvider>();
        services.AddScoped<IPaymentWebhookVerifier, CloudPaymentsPaymentProvider>();
        services.AddScoped<IPaymentWebhookVerifier, TBankAcquiringPaymentProvider>();
        services.AddScoped<IPaymentWebhookVerifier, ProdamusPaymentProvider>();
        services.AddScoped<IPaymentWebhookVerifier, StripePaymentProvider>();
        services.AddScoped<IPaymentWebhookVerifier, PayPalPaymentProvider>();
        services.AddScoped<IPaymentStatusMapper, YooKassaPaymentProvider>();
        services.AddScoped<IPaymentStatusMapper, RoboKassaPaymentProvider>();
        services.AddScoped<IPaymentStatusMapper, YooMoneyPaymentProvider>();
        services.AddScoped<IPaymentStatusMapper, CloudPaymentsPaymentProvider>();
        services.AddScoped<IPaymentStatusMapper, TBankAcquiringPaymentProvider>();
        services.AddScoped<IPaymentStatusMapper, ProdamusPaymentProvider>();
        services.AddScoped<IPaymentStatusMapper, StripePaymentProvider>();
        services.AddScoped<IPaymentStatusMapper, PayPalPaymentProvider>();
        services.AddScoped<IPaymentWebhookProcessor, VpnPlatform.Application.Services.PaymentOrchestrator>();

        services.Configure<ProvisioningOptions>(configuration.GetSection("Provisioning"));

        services.AddScoped<IX3UiClient, X3UiHttpClient>();
        services.AddScoped<IQrCodeGenerator, SvgQrCodeGenerator>();
        services.AddScoped<IVpnProvider, X3UiVpnProvider>();
        services.AddScoped<IVpnProviderFactory, VpnProviderFactory>();
        services.AddScoped<IProvisioningExecutor, AnsibleProvisioningExecutor>();
        services.AddScoped<AppReleaseSeedService>();

        if (includeHostedServices)
        {
            services.AddHostedService<StartupSafetyValidator>();
            services.AddHostedService<DbInitializer>();

            if (includeOperationalWorkers)
            {
                services.AddHostedService<SubscriptionLifecycleWorker>();
                services.AddHostedService<OutboxDispatcherWorker>();
                services.AddHostedService<ProvisioningWorker>();
                services.AddHostedService<PanelHealthWorker>();
                services.AddHostedService<PanelSyncWorker>();
            }
        }

        return services;
    }

    public static SymmetricSecurityKey BuildJwtSigningKey(this IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }
}
