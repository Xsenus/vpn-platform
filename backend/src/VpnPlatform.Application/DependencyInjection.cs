using Microsoft.Extensions.DependencyInjection;
using VpnPlatform.Application.Services;

namespace VpnPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CatalogService>();
        services.AddScoped<OrderService>();
        services.AddScoped<CheckoutSessionService>();
        services.AddScoped<ReferralRewardService>();
        services.AddScoped<PaymentProviderAccountService>();
        services.AddScoped<NodeAllocationService>();
        services.AddScoped<VpnAccessLifecycleService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<PaymentOrchestrator>();
        services.AddScoped<TelegramBotService>();
        services.AddScoped<TelegramUpdateDeliveryService>();
        services.AddScoped<TelegramNotificationDeliveryService>();
        services.AddScoped<OutboxMessageDeliveryService>();
        services.AddScoped<EmailNotificationDeliveryService>();
        services.AddScoped<X3UiPanelService>();
        services.AddScoped<ProvisioningService>();
        return services;
    }
}
