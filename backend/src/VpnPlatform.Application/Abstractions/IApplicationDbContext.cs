using Microsoft.EntityFrameworkCore;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserRefreshToken> UserRefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<ChannelProfile> ChannelProfiles { get; }
    DbSet<Tariff> Tariffs { get; }
    DbSet<PromoCode> PromoCodes { get; }
    DbSet<FaqEntry> FaqEntries { get; }
    DbSet<SiteContentBlock> SiteContentBlocks { get; }
    DbSet<WorkScenario> WorkScenarios { get; }
    DbSet<CheckoutSession> CheckoutSessions { get; }
    DbSet<Order> Orders { get; }
    DbSet<PaymentProviderAccount> PaymentProviderAccounts { get; }
    DbSet<PaymentProviderSetting> PaymentProviderSettings { get; }
    DbSet<PaymentAttempt> Payments { get; }
    DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<PaymentReceipt> PaymentReceipts { get; }
    DbSet<TelegramAccount> TelegramAccounts { get; }
    DbSet<TelegramBotUpdate> TelegramBotUpdates { get; }
    DbSet<TelegramBotSession> TelegramBotSessions { get; }
    DbSet<TelegramBotCommandLog> TelegramBotCommandLogs { get; }
    DbSet<TelegramBotMessage> TelegramBotMessages { get; }
    DbSet<TelegramBotCallbackQuery> TelegramBotCallbackQueries { get; }
    DbSet<TelegramBotPayment> TelegramBotPayments { get; }
    DbSet<TelegramBotDeepLink> TelegramBotDeepLinks { get; }
    DbSet<TelegramBotNotification> TelegramBotNotifications { get; }
    DbSet<SupportConversation> SupportConversations { get; }
    DbSet<SupportMessage> SupportMessages { get; }
    DbSet<VpnPanel> VpnPanels { get; }
    DbSet<VpnInbound> VpnInbounds { get; }
    DbSet<VpnClient> VpnClients { get; }
    DbSet<PanelSyncRun> PanelSyncRuns { get; }
    DbSet<PanelSyncEvent> PanelSyncEvents { get; }
    DbSet<PanelHealthCheck> PanelHealthChecks { get; }
    DbSet<AccessCredentialHistory> AccessCredentialHistories { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<AccessCredential> AccessCredentials { get; }
    DbSet<VpnNode> VpnNodes { get; }
    DbSet<NodeGroup> NodeGroups { get; }
    DbSet<ReferralProgram> ReferralPrograms { get; }
    DbSet<ReferralRelationship> ReferralRelationships { get; }
    DbSet<RewardLedger> RewardLedgers { get; }
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<InboxMessage> InboxMessages { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ProvisioningRun> ProvisioningRuns { get; }
    DbSet<ProvisioningStepRun> ProvisioningStepRuns { get; }
    DbSet<MigrationJob> MigrationJobs { get; }
    DbSet<MigrationItem> MigrationItems { get; }
    DbSet<NodeHealthCheck> NodeHealthChecks { get; }
    DbSet<AppRelease> AppReleases { get; }
    DbSet<AppReleaseItem> AppReleaseItems { get; }
    DbSet<AppReleaseSeen> AppReleaseSeen { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
