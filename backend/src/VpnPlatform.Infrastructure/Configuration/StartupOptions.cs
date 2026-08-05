namespace VpnPlatform.Infrastructure.Configuration;

public sealed class DatabaseStartupOptions
{
    public string Provider { get; set; } = "Postgres";
    public bool ApplyMigrationsOnStartup { get; set; }
    public bool UseEnsureCreatedForLocalSqlite { get; set; }
    public bool SeedDemoData { get; set; }
}

public sealed class AdminBootstrapOptions
{
    public bool Enabled { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Platform Admin";
    public string RolesCsv { get; set; } = "SuperAdmin";
    public bool ResetExistingPassword { get; set; }
}

public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public sealed class TelegramBotOptions
{
    public bool Enabled { get; set; }
    public string Mode { get; set; } = "LongPolling";
    public string BotToken { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string SecretToken { get; set; } = string.Empty;
    public string[] AllowedUpdates { get; set; } = Array.Empty<string>();
    public long? AdminChatId { get; set; }
    public string PublicBotUsername { get; set; } = string.Empty;
    public string WebAppUrl { get; set; } = string.Empty;
}

public sealed class EmailDeliveryOptions
{
    public string Mode { get; set; } = "Disabled";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "VPN Platform";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
