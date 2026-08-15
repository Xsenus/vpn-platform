using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Services;

namespace VpnPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IClock _clock;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IClock? clock = null) : base(options)
    {
        _clock = clock ?? new SystemClock();
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<PasswordResetState> PasswordResetStates => Set<PasswordResetState>();
    public DbSet<TelegramLinkState> TelegramLinkStates => Set<TelegramLinkState>();
    public DbSet<ChannelProfile> ChannelProfiles => Set<ChannelProfile>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<FaqEntry> FaqEntries => Set<FaqEntry>();
    public DbSet<SiteContentBlock> SiteContentBlocks => Set<SiteContentBlock>();
    public DbSet<WorkScenario> WorkScenarios => Set<WorkScenario>();
    public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PaymentProviderAccount> PaymentProviderAccounts => Set<PaymentProviderAccount>();
    public DbSet<PaymentProviderSetting> PaymentProviderSettings => Set<PaymentProviderSetting>();
    public DbSet<PaymentAttempt> Payments => Set<PaymentAttempt>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();
    public DbSet<TelegramAccount> TelegramAccounts => Set<TelegramAccount>();
    public DbSet<TelegramBotUpdate> TelegramBotUpdates => Set<TelegramBotUpdate>();
    public DbSet<TelegramBotSession> TelegramBotSessions => Set<TelegramBotSession>();
    public DbSet<TelegramBotCommandLog> TelegramBotCommandLogs => Set<TelegramBotCommandLog>();
    public DbSet<TelegramBotMessage> TelegramBotMessages => Set<TelegramBotMessage>();
    public DbSet<TelegramBotCallbackQuery> TelegramBotCallbackQueries => Set<TelegramBotCallbackQuery>();
    public DbSet<TelegramBotPayment> TelegramBotPayments => Set<TelegramBotPayment>();
    public DbSet<TelegramBotDeepLink> TelegramBotDeepLinks => Set<TelegramBotDeepLink>();
    public DbSet<TelegramBotNotification> TelegramBotNotifications => Set<TelegramBotNotification>();
    public DbSet<SupportConversation> SupportConversations => Set<SupportConversation>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<VpnPanel> VpnPanels => Set<VpnPanel>();
    public DbSet<VpnInbound> VpnInbounds => Set<VpnInbound>();
    public DbSet<VpnClient> VpnClients => Set<VpnClient>();
    public DbSet<PanelSyncRun> PanelSyncRuns => Set<PanelSyncRun>();
    public DbSet<PanelSyncEvent> PanelSyncEvents => Set<PanelSyncEvent>();
    public DbSet<PanelHealthCheck> PanelHealthChecks => Set<PanelHealthCheck>();
    public DbSet<AccessCredentialHistory> AccessCredentialHistories => Set<AccessCredentialHistory>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<AccessCredential> AccessCredentials => Set<AccessCredential>();
    public DbSet<VpnNode> VpnNodes => Set<VpnNode>();
    public DbSet<NodeGroup> NodeGroups => Set<NodeGroup>();
    public DbSet<ReferralProgram> ReferralPrograms => Set<ReferralProgram>();
    public DbSet<ReferralRelationship> ReferralRelationships => Set<ReferralRelationship>();
    public DbSet<RewardLedger> RewardLedgers => Set<RewardLedger>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProvisioningRun> ProvisioningRuns => Set<ProvisioningRun>();
    public DbSet<ProvisioningStepRun> ProvisioningStepRuns => Set<ProvisioningStepRun>();
    public DbSet<MigrationJob> MigrationJobs => Set<MigrationJob>();
    public DbSet<MigrationItem> MigrationItems => Set<MigrationItem>();
    public DbSet<NodeHealthCheck> NodeHealthChecks => Set<NodeHealthCheck>();
    public DbSet<AppRelease> AppReleases => Set<AppRelease>();
    public DbSet<AppReleaseItem> AppReleaseItems => Set<AppReleaseItem>();
    public DbSet<AppReleaseSeen> AppReleaseSeen => Set<AppReleaseSeen>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.ReferralCode).IsUnique();
        modelBuilder.Entity<UserRefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<UserRefreshToken>().HasIndex(x => new { x.UserId, x.ExpiresAt });
        modelBuilder.Entity<UserRefreshToken>().HasIndex(x => new { x.UserId, x.SessionVersion, x.FamilyId });
        modelBuilder.Entity<UserRefreshToken>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<User>().Property(x => x.UpdatedAt).IsConcurrencyToken();
        modelBuilder.Entity<PasswordResetToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasIndex(x => new { x.UserId, x.ExpiresAt });
        modelBuilder.Entity<PasswordResetToken>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<PasswordResetState>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<PasswordResetState>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<TelegramLinkState>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<TelegramLinkState>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<ChannelProfile>().HasIndex(x => new { x.ProviderType, x.ExternalUserId }).IsUnique();
        modelBuilder.Entity<VpnNode>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<Tariff>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<Tariff>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<FaqEntry>().HasIndex(x => new { x.IsActive, x.ShowOnFaqPage, x.SortOrder });
        modelBuilder.Entity<FaqEntry>().HasIndex(x => new { x.Category, x.SortOrder });
        modelBuilder.Entity<FaqEntry>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<SiteContentBlock>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<SiteContentBlock>().HasIndex(x => new { x.Group, x.IsActive, x.SortOrder });
        modelBuilder.Entity<SiteContentBlock>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<WorkScenario>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<WorkScenario>().HasIndex(x => new { x.IsActive, x.SortOrder });
        modelBuilder.Entity<WorkScenario>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<PromoCode>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<ReferralProgram>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<AppRelease>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<CheckoutSession>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<Order>()
            .HasIndex(x => x.PendingIntentKey)
            .HasDatabaseName("IX_Orders_Pending_IntentKey")
            .HasFilter("\"Status\" = 1 AND \"PendingIntentKey\" IS NOT NULL")
            .IsUnique();
        modelBuilder.Entity<PaymentProviderAccount>().HasIndex(x => new { x.Provider, x.Mode, x.Name }).IsUnique();
        modelBuilder.Entity<PaymentProviderAccount>()
            .HasIndex(x => x.Provider)
            .HasFilter("\"IsDefault\" = true")
            .IsUnique();
        modelBuilder.Entity<PaymentProviderAccount>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<PaymentProviderSetting>().HasIndex(x => new { x.PaymentProviderAccountId, x.Key }).IsUnique();
        modelBuilder.Entity<PaymentAttempt>().HasIndex(x => new { x.Provider, x.ProviderPaymentId }).IsUnique();
        modelBuilder.Entity<PaymentAttempt>().HasIndex(x => x.IdempotencyKey).IsUnique();
        modelBuilder.Entity<PaymentWebhookEvent>().HasIndex(x => new { x.Provider, x.ExternalEventId, x.ProviderPaymentId }).IsUnique();
        modelBuilder.Entity<PaymentWebhookEvent>().HasIndex(x => x.PayloadSha256);
        modelBuilder.Entity<Refund>().HasIndex(x => new { x.Provider, x.ProviderRefundId }).IsUnique();
        modelBuilder.Entity<Refund>().HasIndex(x => x.IdempotencyKey).IsUnique();
        modelBuilder.Entity<PaymentReceipt>().HasIndex(x => new { x.Provider, x.ProviderReceiptId }).IsUnique();
        modelBuilder.Entity<TelegramAccount>().HasIndex(x => x.TelegramUserId).IsUnique();
        modelBuilder.Entity<TelegramAccount>()
            .HasIndex(x => x.UserId)
            .HasFilter("\"UserId\" IS NOT NULL")
            .IsUnique();
        modelBuilder.Entity<TelegramBotUpdate>().HasIndex(x => x.UpdateId).IsUnique();
        modelBuilder.Entity<TelegramBotSession>().HasIndex(x => x.TelegramUserId).IsUnique();
        modelBuilder.Entity<TelegramBotCallbackQuery>().HasIndex(x => x.CallbackQueryId).IsUnique();
        modelBuilder.Entity<TelegramBotPayment>().HasIndex(x => x.TelegramPaymentChargeId).IsUnique();
        modelBuilder.Entity<TelegramBotDeepLink>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<TelegramBotDeepLink>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<TelegramBotNotification>().HasIndex(x => x.DeduplicationKey).IsUnique();
        modelBuilder.Entity<SupportConversation>().HasIndex(x => new { x.TelegramUserId, x.Status });
        modelBuilder.Entity<SupportConversation>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<VpnPanel>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<VpnPanel>().HasIndex(x => x.BaseUrl).IsUnique();
        modelBuilder.Entity<VpnPanel>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<VpnPanel>().Property(x => x.UsedCapacity).IsConcurrencyToken();
        modelBuilder.Entity<VpnInbound>().HasIndex(x => new { x.VpnPanelId, x.ExternalInboundId }).IsUnique();
        modelBuilder.Entity<VpnInbound>().HasIndex(x => new { x.VpnPanelId, x.IsDefault });
        modelBuilder.Entity<VpnInbound>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<VpnInbound>().Property(x => x.UsedCapacity).IsConcurrencyToken();
        modelBuilder.Entity<VpnClient>().HasIndex(x => x.SubscriptionId).IsUnique();
        modelBuilder.Entity<VpnClient>().HasIndex(x => new { x.VpnPanelId, x.VpnInboundId, x.Uuid }).IsUnique();
        modelBuilder.Entity<VpnClient>().HasIndex(x => new { x.VpnPanelId, x.ExternalClientId }).IsUnique();
        modelBuilder.Entity<VpnClient>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<PanelSyncRun>().HasIndex(x => new { x.VpnPanelId, x.StartedAt });
        modelBuilder.Entity<PanelSyncRun>()
            .HasIndex(x => x.VpnPanelId)
            .HasDatabaseName("IX_PanelSyncRuns_Running_VpnPanelId")
            .HasFilter("\"Status\" = 1")
            .IsUnique();
        modelBuilder.Entity<PanelSyncEvent>().HasIndex(x => new { x.PanelSyncRunId, x.EventType });
        modelBuilder.Entity<PanelHealthCheck>().HasIndex(x => new { x.VpnPanelId, x.CheckedAt });
        modelBuilder.Entity<AccessCredentialHistory>().HasIndex(x => new { x.AccessCredentialId, x.CreatedAt });
        modelBuilder.Entity<ProvisioningRun>()
            .HasIndex(x => x.NodeId)
            .HasDatabaseName("IX_ProvisioningRuns_Active_NodeId")
            .HasFilter("\"Status\" IN (0, 1, 8, 9, 12, 13, 15)")
            .IsUnique();
        modelBuilder.Entity<ProvisioningRun>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<InboxMessage>().HasIndex(x => new { x.Source, x.ExternalKey }).IsUnique();
        modelBuilder.Entity<NotificationDelivery>().HasIndex(x => x.SourceOutboxMessageId).IsUnique();
        modelBuilder.Entity<NotificationDelivery>().HasIndex(x => new { x.Status, x.NextAttemptAt });
        modelBuilder.Entity<OutboxMessage>().HasIndex(x => new { x.Type, x.CorrelationId }).IsUnique();
        modelBuilder.Entity<AppRelease>().HasIndex(x => x.ReleaseId).IsUnique();
        modelBuilder.Entity<AppRelease>().HasIndex(x => new { x.IsActive, x.ReleasedAt });
        modelBuilder.Entity<AppReleaseItem>().HasIndex(x => new { x.AppReleaseId, x.SortOrder });
        modelBuilder.Entity<AppReleaseSeen>().HasIndex(x => new { x.UserId, x.AppReleaseId }).IsUnique();

        modelBuilder.Entity<UserRefreshToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetState>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TelegramLinkState>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CheckoutSession>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CheckoutSession>()
            .HasOne(x => x.Tariff)
            .WithMany()
            .HasForeignKey(x => x.TariffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CheckoutSession>()
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.Tariff)
            .WithMany()
            .HasForeignKey(x => x.TariffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.CheckoutSession)
            .WithMany()
            .HasForeignKey(x => x.CheckoutSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PaymentProviderSetting>()
            .HasOne(x => x.PaymentProviderAccount)
            .WithMany(x => x.Settings)
            .HasForeignKey(x => x.PaymentProviderAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentAttempt>()
            .HasOne(x => x.Order)
            .WithMany(x => x.PaymentAttempts)
            .HasForeignKey(x => x.OrderId);

        modelBuilder.Entity<PaymentAttempt>()
            .HasOne(x => x.PaymentProviderAccount)
            .WithMany()
            .HasForeignKey(x => x.PaymentProviderAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PaymentWebhookEvent>()
            .HasOne(x => x.PaymentAttempt)
            .WithMany()
            .HasForeignKey(x => x.PaymentAttemptId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PaymentWebhookEvent>()
            .HasOne(x => x.PaymentProviderAccount)
            .WithMany()
            .HasForeignKey(x => x.PaymentProviderAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TelegramAccount>()
            .HasOne(x => x.User)
            .WithMany(x => x.TelegramAccounts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TelegramBotMessage>()
            .HasOne(x => x.TelegramAccount)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.TelegramAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TelegramBotPayment>()
            .HasOne(x => x.PaymentAttempt)
            .WithMany()
            .HasForeignKey(x => x.PaymentAttemptId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TelegramBotDeepLink>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupportConversation>()
            .HasOne(x => x.User)
            .WithMany(x => x.SupportConversations)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SupportMessage>()
            .HasOne(x => x.SupportConversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.SupportConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Refund>()
            .HasOne(x => x.PaymentAttempt)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.PaymentAttemptId);

        modelBuilder.Entity<PaymentReceipt>()
            .HasOne(x => x.PaymentAttempt)
            .WithMany(x => x.Receipts)
            .HasForeignKey(x => x.PaymentAttemptId);

        modelBuilder.Entity<Subscription>()
            .HasOne(x => x.User)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subscription>()
            .HasOne(x => x.Tariff)
            .WithMany()
            .HasForeignKey(x => x.TariffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subscription>()
            .HasOne(x => x.CurrentAccess)
            .WithOne()
            .HasForeignKey<Subscription>(x => x.CurrentAccessId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Subscription>()
            .HasOne(x => x.CurrentServer)
            .WithMany()
            .HasForeignKey(x => x.CurrentServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AccessCredential>()
            .HasOne(x => x.Subscription)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId);

        modelBuilder.Entity<AccessCredential>()
            .HasOne(x => x.Server)
            .WithMany()
            .HasForeignKey(x => x.ServerId);

        modelBuilder.Entity<VpnInbound>()
            .HasOne(x => x.VpnPanel)
            .WithMany(x => x.Inbounds)
            .HasForeignKey(x => x.VpnPanelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VpnClient>()
            .HasOne(x => x.User)
            .WithMany(x => x.VpnClients)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VpnClient>()
            .HasOne(x => x.Subscription)
            .WithOne(x => x.VpnClient)
            .HasForeignKey<VpnClient>(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VpnClient>()
            .HasOne(x => x.VpnPanel)
            .WithMany(x => x.Clients)
            .HasForeignKey(x => x.VpnPanelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VpnClient>()
            .HasOne(x => x.VpnInbound)
            .WithMany(x => x.Clients)
            .HasForeignKey(x => x.VpnInboundId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PanelSyncRun>()
            .HasOne(x => x.VpnPanel)
            .WithMany(x => x.SyncRuns)
            .HasForeignKey(x => x.VpnPanelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PanelSyncEvent>()
            .HasOne(x => x.PanelSyncRun)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.PanelSyncRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PanelHealthCheck>()
            .HasOne(x => x.VpnPanel)
            .WithMany(x => x.HealthChecks)
            .HasForeignKey(x => x.VpnPanelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AccessCredentialHistory>()
            .HasOne(x => x.AccessCredential)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.AccessCredentialId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AccessCredentialHistory>()
            .HasOne(x => x.Subscription)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppReleaseItem>()
            .HasOne(x => x.AppRelease)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.AppReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppReleaseSeen>()
            .HasOne(x => x.AppRelease)
            .WithMany(x => x.SeenByUsers)
            .HasForeignKey(x => x.AppReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppReleaseSeen>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VpnNode>()
            .HasOne(x => x.NodeGroup)
            .WithMany(x => x.Nodes)
            .HasForeignKey(x => x.NodeGroupId);

        modelBuilder.Entity<ProvisioningStepRun>()
            .HasOne<ProvisioningRun>()
            .WithMany(x => x.Steps)
            .HasForeignKey(x => x.ProvisioningRunId);

        modelBuilder.Entity<MigrationItem>()
            .HasOne<MigrationJob>()
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.MigrationJobId);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(string)))
            {
                property.SetMaxLength(4000);
            }
        }

        modelBuilder.Entity<Order>().Property(x => x.PendingIntentKey).HasMaxLength(64);
        modelBuilder.Entity<UserRefreshToken>().Property(x => x.TokenHash).HasColumnType("text");
        modelBuilder.Entity<UserRefreshToken>().Property(x => x.ReplacedByTokenHash).HasColumnType("text");
        modelBuilder.Entity<PasswordResetToken>().Property(x => x.TokenHash).HasColumnType("text");
        modelBuilder.Entity<VpnNode>().Property(x => x.ProtectedSshCredential).HasColumnType("text");
        modelBuilder.Entity<VpnNode>().Property(x => x.ProtectedPanelPassword).HasColumnType("text");
        modelBuilder.Entity<VpnNode>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<VpnNode>().Property(x => x.Host).HasMaxLength(253);
        modelBuilder.Entity<VpnNode>().Property(x => x.IpAddress).HasMaxLength(64);
        modelBuilder.Entity<VpnNode>().Property(x => x.Provider).HasMaxLength(120);
        modelBuilder.Entity<VpnNode>().Property(x => x.Region).HasMaxLength(120);
        modelBuilder.Entity<VpnNode>().Property(x => x.Country).HasMaxLength(80);
        modelBuilder.Entity<VpnNode>().Property(x => x.Datacenter).HasMaxLength(120);
        modelBuilder.Entity<VpnNode>().Property(x => x.SupportedProtocolsCsv).HasMaxLength(80);
        modelBuilder.Entity<VpnNode>().Property(x => x.TagsCsv).HasMaxLength(2000);
        modelBuilder.Entity<VpnNode>().Property(x => x.SshUser).HasMaxLength(64);
        modelBuilder.Entity<VpnNode>().Property(x => x.SshPrivateKeyPath).HasMaxLength(4000);
        modelBuilder.Entity<VpnNode>().Property(x => x.SshCredentialRef).HasMaxLength(120);
        modelBuilder.Entity<VpnNode>().Property(x => x.PanelBaseUrl).HasMaxLength(2000);
        modelBuilder.Entity<VpnNode>().Property(x => x.PanelUsername).HasMaxLength(200);
        modelBuilder.Entity<VpnNode>().Property(x => x.PanelPassword).HasMaxLength(4000);
        modelBuilder.Entity<VpnNode>().Property(x => x.PanelSecretRef).HasMaxLength(120);
        modelBuilder.Entity<VpnNode>().Property(x => x.PublicHostname).HasMaxLength(253);
        modelBuilder.Entity<PaymentAttempt>().Property(x => x.RawRequest).HasColumnType("text");
        modelBuilder.Entity<PaymentAttempt>().Property(x => x.RawResponse).HasColumnType("text");
        modelBuilder.Entity<PaymentAttempt>().Property(x => x.WebhookPayload).HasColumnType("text");
        modelBuilder.Entity<PaymentWebhookEvent>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<PaymentWebhookEvent>().Property(x => x.HeadersJson).HasColumnType("text");
        modelBuilder.Entity<PaymentWebhookEvent>().Property(x => x.ErrorText).HasColumnType("text");
        modelBuilder.Entity<Refund>().Property(x => x.RawRequest).HasColumnType("text");
        modelBuilder.Entity<Refund>().Property(x => x.RawResponse).HasColumnType("text");
        modelBuilder.Entity<PaymentReceipt>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotUpdate>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotUpdate>().Property(x => x.ErrorText).HasColumnType("text");
        modelBuilder.Entity<TelegramBotUpdate>().Property(x => x.ResponseText).HasColumnType("text");
        modelBuilder.Entity<TelegramBotUpdate>().Property(x => x.ResponseReplyMarkupJson).HasColumnType("text");
        modelBuilder.Entity<TelegramBotUpdate>().Property(x => x.PreCheckoutError).HasColumnType("text");
        modelBuilder.Entity<TelegramBotUpdate>().Property(x => x.DeliveryErrorText).HasColumnType("text");
        modelBuilder.Entity<TelegramBotSession>().Property(x => x.PayloadJson).HasColumnType("text");
        modelBuilder.Entity<TelegramBotCommandLog>().Property(x => x.Payload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotMessage>().Property(x => x.Text).HasColumnType("text");
        modelBuilder.Entity<TelegramBotMessage>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotCallbackQuery>().Property(x => x.Data).HasColumnType("text");
        modelBuilder.Entity<TelegramBotCallbackQuery>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotPayment>().Property(x => x.InvoicePayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotPayment>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotDeepLink>().Property(x => x.MetadataJson).HasColumnType("text");
        modelBuilder.Entity<TelegramBotNotification>().Property(x => x.DeduplicationKey).HasMaxLength(64);
        modelBuilder.Entity<TelegramBotNotification>().Property(x => x.PayloadJson).HasColumnType("text");
        modelBuilder.Entity<TelegramBotNotification>().Property(x => x.ErrorText).HasColumnType("text");
        modelBuilder.Entity<SupportMessage>().Property(x => x.Text).HasColumnType("text");
        modelBuilder.Entity<SupportMessage>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<SupportMessage>().Property(x => x.AttachmentsJson).HasColumnType("text");
        modelBuilder.Entity<VpnPanel>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<VpnPanel>().Property(x => x.BaseUrl).HasMaxLength(2048);
        modelBuilder.Entity<VpnPanel>().Property(x => x.Region).HasMaxLength(120);
        modelBuilder.Entity<VpnPanel>().Property(x => x.Login).HasMaxLength(200);
        modelBuilder.Entity<VpnPanel>().Property(x => x.EncryptedPassword).HasColumnType("text").HasMaxLength(8192);
        modelBuilder.Entity<VpnPanel>().Property(x => x.DefaultInboundTemplateJson).HasColumnType("text").HasMaxLength(32768);
        modelBuilder.Entity<VpnPanel>().Property(x => x.LastError).HasMaxLength(2000);
        modelBuilder.Entity<VpnPanel>().Property(x => x.Version).HasMaxLength(120);
        modelBuilder.Entity<VpnInbound>().Property(x => x.ExternalInboundId).HasMaxLength(200);
        modelBuilder.Entity<VpnInbound>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<VpnInbound>().Property(x => x.Protocol).HasMaxLength(32);
        modelBuilder.Entity<VpnInbound>().Property(x => x.Listen).HasMaxLength(255);
        modelBuilder.Entity<VpnInbound>().Property(x => x.SettingsJson).HasColumnType("text").HasMaxLength(32768);
        modelBuilder.Entity<VpnInbound>().Property(x => x.StreamSettingsJson).HasColumnType("text").HasMaxLength(32768);
        modelBuilder.Entity<VpnInbound>().Property(x => x.SniffingJson).HasColumnType("text").HasMaxLength(32768);
        modelBuilder.Entity<VpnClient>().Property(x => x.ExternalClientId).HasMaxLength(200);
        modelBuilder.Entity<VpnClient>().Property(x => x.Email).HasMaxLength(320);
        modelBuilder.Entity<VpnClient>().Property(x => x.Uuid).HasMaxLength(100);
        modelBuilder.Entity<VpnClient>().Property(x => x.Flow).HasMaxLength(100);
        modelBuilder.Entity<VpnClient>().Property(x => x.ConfigUri).HasColumnType("text").HasMaxLength(8192);
        modelBuilder.Entity<VpnClient>().Property(x => x.QrCodePayload).HasColumnType("text").HasMaxLength(8192);
        modelBuilder.Entity<VpnClient>().Property(x => x.SyncStatus).HasMaxLength(100);
        modelBuilder.Entity<PanelSyncRun>().Property(x => x.SummaryJson).HasColumnType("text");
        modelBuilder.Entity<PanelSyncRun>().Property(x => x.ErrorMessage).HasColumnType("text");
        modelBuilder.Entity<PanelSyncEvent>().Property(x => x.PayloadJson).HasColumnType("text");
        modelBuilder.Entity<PanelSyncEvent>().Property(x => x.Message).HasColumnType("text");
        modelBuilder.Entity<PanelHealthCheck>().Property(x => x.ErrorMessage).HasColumnType("text");
        modelBuilder.Entity<AccessCredentialHistory>().Property(x => x.OldValueJson).HasColumnType("text");
        modelBuilder.Entity<AccessCredentialHistory>().Property(x => x.NewValueJson).HasColumnType("text");
        modelBuilder.Entity<AppRelease>().Property(x => x.ReleaseId).HasMaxLength(160);
        modelBuilder.Entity<AppRelease>().Property(x => x.Version).HasMaxLength(40);
        modelBuilder.Entity<AppRelease>().Property(x => x.Title).HasMaxLength(200);
        modelBuilder.Entity<AppRelease>().Property(x => x.Source).HasMaxLength(40);
        modelBuilder.Entity<AppRelease>().Property(x => x.CreatedByUserName).HasMaxLength(200);
        modelBuilder.Entity<AppRelease>().Property(x => x.UpdatedByUserName).HasMaxLength(200);
        modelBuilder.Entity<AppRelease>().Property(x => x.Summary).HasColumnType("text");
        modelBuilder.Entity<AppReleaseItem>().Property(x => x.Type).HasMaxLength(40);
        modelBuilder.Entity<AppReleaseItem>().Property(x => x.Text).HasColumnType("text");
        modelBuilder.Entity<FaqEntry>().Property(x => x.Question).HasMaxLength(300);
        modelBuilder.Entity<FaqEntry>().Property(x => x.Answer).HasColumnType("text");
        modelBuilder.Entity<FaqEntry>().Property(x => x.Category).HasMaxLength(120);
        modelBuilder.Entity<SiteContentBlock>().Property(x => x.Key).HasMaxLength(160);
        modelBuilder.Entity<SiteContentBlock>().Property(x => x.Group).HasMaxLength(80);
        modelBuilder.Entity<SiteContentBlock>().Property(x => x.Label).HasMaxLength(200);
        modelBuilder.Entity<SiteContentBlock>().Property(x => x.Description).HasColumnType("text");
        modelBuilder.Entity<SiteContentBlock>().Property(x => x.InputType).HasMaxLength(40);
        modelBuilder.Entity<SiteContentBlock>().Property(x => x.Value).HasColumnType("text");
        modelBuilder.Entity<WorkScenario>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<WorkScenario>().Property(x => x.Key).HasMaxLength(120);
        modelBuilder.Entity<WorkScenario>().Property(x => x.AllowedTariffIdsJson).HasColumnType("text");
        modelBuilder.Entity<WorkScenario>().Property(x => x.VpnProtocol).HasMaxLength(40);
        modelBuilder.Entity<WorkScenario>().Property(x => x.ServerSelectionRule).HasMaxLength(120);
        modelBuilder.Entity<WorkScenario>().Property(x => x.InboundSelectionRule).HasMaxLength(120);
        modelBuilder.Entity<WorkScenario>().Property(x => x.ProvisioningMode).HasMaxLength(40);
        modelBuilder.Entity<WorkScenario>().Property(x => x.OnPaymentSucceeded).HasColumnType("text");
        modelBuilder.Entity<WorkScenario>().Property(x => x.OnPaymentFailed).HasColumnType("text");
        modelBuilder.Entity<WorkScenario>().Property(x => x.OnRefund).HasColumnType("text");
        modelBuilder.Entity<WorkScenario>().Property(x => x.OnSubscriptionExpired).HasColumnType("text");
        modelBuilder.Entity<WorkScenario>().Property(x => x.OnRenewal).HasColumnType("text");
        modelBuilder.Entity<WorkScenario>().Property(x => x.CabinetText).HasColumnType("text");
        modelBuilder.Entity<WorkScenario>().Property(x => x.TelegramText).HasColumnType("text");
        modelBuilder.Entity<Tariff>().Property(x => x.Description).HasColumnType("text");
        modelBuilder.Entity<Tariff>().Property(x => x.FullDescription).HasColumnType("text");
        modelBuilder.Entity<Tariff>().Property(x => x.FeaturesJson).HasColumnType("text");
        modelBuilder.Entity<Tariff>().Property(x => x.Badge).HasMaxLength(80);
        modelBuilder.Entity<Tariff>().Property(x => x.ProvisioningScenario).HasMaxLength(120);
        modelBuilder.Entity<Tariff>().Property(x => x.AfterPaymentText).HasColumnType("text");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareAddedAuditTimestamps();
        var notifications = ExtractAddedTelegramNotifications();
        var outboxMessages = ExtractAddedOutboxMessages();
        if ((notifications.Count == 0 && outboxMessages.Count == 0) || !Database.IsRelational())
        {
            PrepareNonRelationalNotifications(notifications);
            PrepareNonRelationalOutboxMessages(outboxMessages);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        using var transaction = BeginTransactionIfNeeded();
        try
        {
            var affected = base.SaveChanges(acceptAllChangesOnSuccess: false);
            affected += UpsertTelegramNotifications(notifications);
            affected += UpsertOutboxMessages(outboxMessages);
            transaction?.Commit();
            if (acceptAllChangesOnSuccess)
            {
                ChangeTracker.AcceptAllChanges();
            }

            return affected;
        }
        catch
        {
            transaction?.Rollback();
            RestoreTelegramNotifications(notifications);
            RestoreOutboxMessages(outboxMessages);
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareAddedAuditTimestamps();
        var notifications = ExtractAddedTelegramNotifications();
        var outboxMessages = ExtractAddedOutboxMessages();
        if ((notifications.Count == 0 && outboxMessages.Count == 0) || !Database.IsRelational())
        {
            PrepareNonRelationalNotifications(notifications);
            PrepareNonRelationalOutboxMessages(outboxMessages);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        await using var transaction = await BeginTransactionIfNeededAsync(cancellationToken);
        try
        {
            var affected = await base.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);
            affected += await UpsertTelegramNotificationsAsync(notifications, cancellationToken);
            affected += await UpsertOutboxMessagesAsync(outboxMessages, cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            if (acceptAllChangesOnSuccess)
            {
                ChangeTracker.AcceptAllChanges();
            }

            return affected;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            RestoreTelegramNotifications(notifications);
            RestoreOutboxMessages(outboxMessages);
            throw;
        }
    }

    private List<TelegramBotNotification> ExtractAddedTelegramNotifications()
    {
        var notifications = ChangeTracker.Entries<TelegramBotNotification>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity)
            .ToList();
        foreach (var notification in notifications)
        {
            notification.DeduplicationKey = TelegramNotificationDeduplication.CreateKey(
                notification.TelegramUserId,
                notification.Type,
                notification.PayloadJson);
        }

        var unique = new List<TelegramBotNotification>(notifications.Count);
        foreach (var group in notifications.GroupBy(x => x.DeduplicationKey, StringComparer.Ordinal))
        {
            unique.Add(group.First());
            foreach (var duplicate in group.Skip(1))
            {
                Entry(duplicate).State = EntityState.Detached;
            }
        }

        if (Database.IsRelational())
        {
            foreach (var notification in unique)
            {
                Entry(notification).State = EntityState.Detached;
            }
        }

        return unique;
    }

    private void PrepareAddedAuditTimestamps()
    {
        var now = _clock.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>().Where(x => x.State == EntityState.Added))
        {
            entry.Entity.CreatedAt = entry.Entity.CreatedAt == default ? now : entry.Entity.CreatedAt;
            entry.Entity.UpdatedAt = entry.Entity.UpdatedAt == default ? entry.Entity.CreatedAt : entry.Entity.UpdatedAt;
        }
    }

    private List<OutboxMessage> ExtractAddedOutboxMessages()
    {
        var messages = ChangeTracker.Entries<OutboxMessage>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity)
            .ToList();
        foreach (var message in messages)
        {
            message.Type = message.Type.Trim();
            message.CorrelationId = message.CorrelationId.Trim();
            if (message.Type.Length == 0 || message.CorrelationId.Length == 0)
            {
                throw new InvalidOperationException("Outbox Type and CorrelationId are required.");
            }
        }

        var unique = new List<OutboxMessage>(messages.Count);
        foreach (var group in messages.GroupBy(x => (x.Type, x.CorrelationId)))
        {
            unique.Add(group.First());
            foreach (var duplicate in group.Skip(1))
            {
                Entry(duplicate).State = EntityState.Detached;
            }
        }

        if (Database.IsRelational())
        {
            foreach (var message in unique)
            {
                Entry(message).State = EntityState.Detached;
            }
        }

        return unique;
    }

    private void PrepareNonRelationalNotifications(IReadOnlyCollection<TelegramBotNotification> notifications)
    {
        foreach (var notification in notifications)
        {
            var existing = TelegramBotNotifications.Local.FirstOrDefault(x =>
                !ReferenceEquals(x, notification)
                && string.Equals(x.DeduplicationKey, notification.DeduplicationKey, StringComparison.Ordinal));
            if (existing is null)
            {
                existing = TelegramBotNotifications.FirstOrDefault(x => x.DeduplicationKey == notification.DeduplicationKey);
            }

            if (existing is null)
            {
                continue;
            }

            Entry(notification).State = EntityState.Detached;
            ReviveTerminalNotification(existing, notification);
        }
    }

    private void PrepareNonRelationalOutboxMessages(IReadOnlyCollection<OutboxMessage> messages)
    {
        foreach (var message in messages)
        {
            var existing = OutboxMessages.Local.FirstOrDefault(x =>
                !ReferenceEquals(x, message)
                && string.Equals(x.Type, message.Type, StringComparison.Ordinal)
                && string.Equals(x.CorrelationId, message.CorrelationId, StringComparison.Ordinal));
            if (existing is null)
            {
                existing = OutboxMessages.FirstOrDefault(x =>
                    x.Type == message.Type && x.CorrelationId == message.CorrelationId);
            }

            if (existing is null)
            {
                continue;
            }

            Entry(message).State = EntityState.Detached;
            ReviveFailedOutboxMessage(existing, message);
        }
    }

    private void RestoreTelegramNotifications(IEnumerable<TelegramBotNotification> notifications)
    {
        foreach (var notification in notifications)
        {
            if (Entry(notification).State == EntityState.Detached)
            {
                TelegramBotNotifications.Add(notification);
            }
        }
    }

    private void RestoreOutboxMessages(IEnumerable<OutboxMessage> messages)
    {
        foreach (var message in messages)
        {
            if (Entry(message).State == EntityState.Detached)
            {
                OutboxMessages.Add(message);
            }
        }
    }

    private IDbContextTransaction? BeginTransactionIfNeeded()
        => Database.CurrentTransaction is null ? Database.BeginTransaction() : null;

    private async Task<IDbContextTransaction?> BeginTransactionIfNeededAsync(CancellationToken cancellationToken)
        => Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

    private int UpsertTelegramNotifications(IEnumerable<TelegramBotNotification> notifications)
        => notifications.Sum(UpsertTelegramNotification);

    private async Task<int> UpsertTelegramNotificationsAsync(
        IEnumerable<TelegramBotNotification> notifications,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var notification in notifications)
        {
            affected += await UpsertTelegramNotificationAsync(notification, cancellationToken);
        }

        return affected;
    }

    private int UpsertOutboxMessages(IEnumerable<OutboxMessage> messages)
        => messages.Sum(UpsertOutboxMessage);

    private async Task<int> UpsertOutboxMessagesAsync(
        IEnumerable<OutboxMessage> messages,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var message in messages)
        {
            affected += await UpsertOutboxMessageAsync(message, cancellationToken);
        }

        return affected;
    }

    private int UpsertTelegramNotification(TelegramBotNotification notification)
        => Database.ExecuteSqlInterpolated($"""
            INSERT INTO "TelegramBotNotifications"
                ("Id", "TelegramUserId", "Type", "PayloadJson", "DeduplicationKey", "Status", "AttemptCount", "NextAttemptAt", "SentAt", "ErrorText", "CreatedAt", "UpdatedAt")
            VALUES
                ({notification.Id}, {notification.TelegramUserId}, {notification.Type}, {notification.PayloadJson}, {notification.DeduplicationKey}, {notification.Status}, {notification.AttemptCount}, {notification.NextAttemptAt}, {notification.SentAt}, {notification.ErrorText}, {notification.CreatedAt}, {notification.UpdatedAt})
            ON CONFLICT ("DeduplicationKey") DO UPDATE SET
                "Status" = 'pending',
                "AttemptCount" = 0,
                "NextAttemptAt" = excluded."NextAttemptAt",
                "SentAt" = NULL,
                "ErrorText" = '',
                "UpdatedAt" = excluded."UpdatedAt"
            WHERE "Status" IN ('failed', 'cancelled');
            """);

    private Task<int> UpsertTelegramNotificationAsync(
        TelegramBotNotification notification,
        CancellationToken cancellationToken)
        => Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "TelegramBotNotifications"
                ("Id", "TelegramUserId", "Type", "PayloadJson", "DeduplicationKey", "Status", "AttemptCount", "NextAttemptAt", "SentAt", "ErrorText", "CreatedAt", "UpdatedAt")
            VALUES
                ({notification.Id}, {notification.TelegramUserId}, {notification.Type}, {notification.PayloadJson}, {notification.DeduplicationKey}, {notification.Status}, {notification.AttemptCount}, {notification.NextAttemptAt}, {notification.SentAt}, {notification.ErrorText}, {notification.CreatedAt}, {notification.UpdatedAt})
            ON CONFLICT ("DeduplicationKey") DO UPDATE SET
                "Status" = 'pending',
                "AttemptCount" = 0,
                "NextAttemptAt" = excluded."NextAttemptAt",
                "SentAt" = NULL,
                "ErrorText" = '',
                "UpdatedAt" = excluded."UpdatedAt"
            WHERE "Status" IN ('failed', 'cancelled');
            """, cancellationToken);

    private int UpsertOutboxMessage(OutboxMessage message)
        => Database.ExecuteSqlInterpolated($"""
            INSERT INTO "OutboxMessages"
                ("Id", "Type", "PayloadJson", "CorrelationId", "Attempts", "ProcessingStartedAt", "NextAttemptAt", "ProcessedAt", "FailedAt", "LastError", "CreatedAt", "UpdatedAt")
            VALUES
                ({message.Id}, {message.Type}, {message.PayloadJson}, {message.CorrelationId}, {message.Attempts}, {message.ProcessingStartedAt}, {message.NextAttemptAt}, {message.ProcessedAt}, {message.FailedAt}, {message.LastError}, {message.CreatedAt}, {message.UpdatedAt})
            ON CONFLICT ("Type", "CorrelationId") DO UPDATE SET
                "PayloadJson" = excluded."PayloadJson",
                "Attempts" = 0,
                "ProcessingStartedAt" = NULL,
                "NextAttemptAt" = excluded."NextAttemptAt",
                "ProcessedAt" = NULL,
                "FailedAt" = NULL,
                "LastError" = NULL,
                "UpdatedAt" = excluded."UpdatedAt"
            WHERE "FailedAt" IS NOT NULL;
            """);

    private Task<int> UpsertOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
        => Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OutboxMessages"
                ("Id", "Type", "PayloadJson", "CorrelationId", "Attempts", "ProcessingStartedAt", "NextAttemptAt", "ProcessedAt", "FailedAt", "LastError", "CreatedAt", "UpdatedAt")
            VALUES
                ({message.Id}, {message.Type}, {message.PayloadJson}, {message.CorrelationId}, {message.Attempts}, {message.ProcessingStartedAt}, {message.NextAttemptAt}, {message.ProcessedAt}, {message.FailedAt}, {message.LastError}, {message.CreatedAt}, {message.UpdatedAt})
            ON CONFLICT ("Type", "CorrelationId") DO UPDATE SET
                "PayloadJson" = excluded."PayloadJson",
                "Attempts" = 0,
                "ProcessingStartedAt" = NULL,
                "NextAttemptAt" = excluded."NextAttemptAt",
                "ProcessedAt" = NULL,
                "FailedAt" = NULL,
                "LastError" = NULL,
                "UpdatedAt" = excluded."UpdatedAt"
            WHERE "FailedAt" IS NOT NULL;
            """, cancellationToken);

    private void ReviveTerminalNotification(TelegramBotNotification existing, TelegramBotNotification replacement)
    {
        if (existing.Status is not ("failed" or "cancelled"))
        {
            return;
        }

        existing.Status = "pending";
        existing.AttemptCount = 0;
        existing.NextAttemptAt = replacement.NextAttemptAt;
        existing.SentAt = null;
        existing.ErrorText = string.Empty;
        existing.UpdatedAt = replacement.UpdatedAt;
    }

    private static void ReviveFailedOutboxMessage(OutboxMessage existing, OutboxMessage replacement)
    {
        if (!existing.FailedAt.HasValue)
        {
            return;
        }

        existing.PayloadJson = replacement.PayloadJson;
        existing.Attempts = 0;
        existing.ProcessingStartedAt = null;
        existing.NextAttemptAt = replacement.NextAttemptAt;
        existing.ProcessedAt = null;
        existing.FailedAt = null;
        existing.LastError = null;
        existing.UpdatedAt = replacement.UpdatedAt;
    }
}
