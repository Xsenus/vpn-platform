using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ChannelProfile> ChannelProfiles => Set<ChannelProfile>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
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
        modelBuilder.Entity<PasswordResetToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasIndex(x => new { x.UserId, x.ExpiresAt });
        modelBuilder.Entity<ChannelProfile>().HasIndex(x => new { x.ProviderType, x.ExternalUserId }).IsUnique();
        modelBuilder.Entity<Tariff>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<PromoCode>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<CheckoutSession>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<PaymentProviderAccount>().HasIndex(x => new { x.Provider, x.Mode, x.Name }).IsUnique();
        modelBuilder.Entity<PaymentProviderSetting>().HasIndex(x => new { x.PaymentProviderAccountId, x.Key }).IsUnique();
        modelBuilder.Entity<PaymentAttempt>().HasIndex(x => new { x.Provider, x.ProviderPaymentId }).IsUnique();
        modelBuilder.Entity<PaymentAttempt>().HasIndex(x => x.IdempotencyKey).IsUnique();
        modelBuilder.Entity<PaymentWebhookEvent>().HasIndex(x => new { x.Provider, x.ExternalEventId, x.ProviderPaymentId }).IsUnique();
        modelBuilder.Entity<PaymentWebhookEvent>().HasIndex(x => x.PayloadSha256);
        modelBuilder.Entity<Refund>().HasIndex(x => new { x.Provider, x.ProviderRefundId }).IsUnique();
        modelBuilder.Entity<Refund>().HasIndex(x => x.IdempotencyKey).IsUnique();
        modelBuilder.Entity<PaymentReceipt>().HasIndex(x => new { x.Provider, x.ProviderReceiptId }).IsUnique();
        modelBuilder.Entity<TelegramAccount>().HasIndex(x => x.TelegramUserId).IsUnique();
        modelBuilder.Entity<TelegramBotUpdate>().HasIndex(x => x.UpdateId).IsUnique();
        modelBuilder.Entity<TelegramBotSession>().HasIndex(x => x.TelegramUserId).IsUnique();
        modelBuilder.Entity<TelegramBotCallbackQuery>().HasIndex(x => x.CallbackQueryId).IsUnique();
        modelBuilder.Entity<TelegramBotPayment>().HasIndex(x => x.TelegramPaymentChargeId).IsUnique();
        modelBuilder.Entity<TelegramBotDeepLink>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<SupportConversation>().HasIndex(x => new { x.TelegramUserId, x.Status });
        modelBuilder.Entity<VpnPanel>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<VpnPanel>().HasIndex(x => x.BaseUrl).IsUnique();
        modelBuilder.Entity<VpnInbound>().HasIndex(x => new { x.VpnPanelId, x.ExternalInboundId }).IsUnique();
        modelBuilder.Entity<VpnInbound>().HasIndex(x => new { x.VpnPanelId, x.IsDefault });
        modelBuilder.Entity<VpnClient>().HasIndex(x => x.SubscriptionId).IsUnique();
        modelBuilder.Entity<VpnClient>().HasIndex(x => new { x.VpnPanelId, x.VpnInboundId, x.Uuid }).IsUnique();
        modelBuilder.Entity<VpnClient>().HasIndex(x => new { x.VpnPanelId, x.ExternalClientId }).IsUnique();
        modelBuilder.Entity<PanelSyncRun>().HasIndex(x => new { x.VpnPanelId, x.StartedAt });
        modelBuilder.Entity<PanelSyncEvent>().HasIndex(x => new { x.PanelSyncRunId, x.EventType });
        modelBuilder.Entity<PanelHealthCheck>().HasIndex(x => new { x.VpnPanelId, x.CheckedAt });
        modelBuilder.Entity<AccessCredentialHistory>().HasIndex(x => new { x.AccessCredentialId, x.CreatedAt });
        modelBuilder.Entity<InboxMessage>().HasIndex(x => new { x.Source, x.ExternalKey }).IsUnique();
        modelBuilder.Entity<OutboxMessage>().HasIndex(x => new { x.Type, x.CorrelationId });
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

        modelBuilder.Entity<UserRefreshToken>().Property(x => x.TokenHash).HasColumnType("text");
        modelBuilder.Entity<UserRefreshToken>().Property(x => x.ReplacedByTokenHash).HasColumnType("text");
        modelBuilder.Entity<PasswordResetToken>().Property(x => x.TokenHash).HasColumnType("text");
        modelBuilder.Entity<VpnNode>().Property(x => x.ProtectedSshCredential).HasColumnType("text");
        modelBuilder.Entity<VpnNode>().Property(x => x.ProtectedPanelPassword).HasColumnType("text");
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
        modelBuilder.Entity<TelegramBotSession>().Property(x => x.PayloadJson).HasColumnType("text");
        modelBuilder.Entity<TelegramBotCommandLog>().Property(x => x.Payload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotMessage>().Property(x => x.Text).HasColumnType("text");
        modelBuilder.Entity<TelegramBotMessage>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotCallbackQuery>().Property(x => x.Data).HasColumnType("text");
        modelBuilder.Entity<TelegramBotCallbackQuery>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotPayment>().Property(x => x.InvoicePayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotPayment>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<TelegramBotDeepLink>().Property(x => x.MetadataJson).HasColumnType("text");
        modelBuilder.Entity<TelegramBotNotification>().Property(x => x.PayloadJson).HasColumnType("text");
        modelBuilder.Entity<TelegramBotNotification>().Property(x => x.ErrorText).HasColumnType("text");
        modelBuilder.Entity<SupportMessage>().Property(x => x.Text).HasColumnType("text");
        modelBuilder.Entity<SupportMessage>().Property(x => x.RawPayload).HasColumnType("text");
        modelBuilder.Entity<SupportMessage>().Property(x => x.AttachmentsJson).HasColumnType("text");
        modelBuilder.Entity<VpnPanel>().Property(x => x.EncryptedPassword).HasColumnType("text");
        modelBuilder.Entity<VpnPanel>().Property(x => x.DefaultInboundTemplateJson).HasColumnType("text");
        modelBuilder.Entity<VpnInbound>().Property(x => x.SettingsJson).HasColumnType("text");
        modelBuilder.Entity<VpnInbound>().Property(x => x.StreamSettingsJson).HasColumnType("text");
        modelBuilder.Entity<VpnInbound>().Property(x => x.SniffingJson).HasColumnType("text");
        modelBuilder.Entity<VpnClient>().Property(x => x.ConfigUri).HasColumnType("text");
        modelBuilder.Entity<VpnClient>().Property(x => x.QrCodePayload).HasColumnType("text");
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
    }
}
