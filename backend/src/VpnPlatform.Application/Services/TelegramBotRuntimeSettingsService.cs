using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Application.Abstractions;

namespace VpnPlatform.Application.Services;

public sealed class TelegramBotRuntimeSettingsService
{
    public const string SettingsGroup = "telegram_bot";
    public const string EnabledKey = "telegram_bot.enabled";
    public const string ModeKey = "telegram_bot.mode";
    public const string BotTokenProtectedKey = "telegram_bot.bot_token_protected";
    public const string SecretTokenProtectedKey = "telegram_bot.secret_token_protected";

    private readonly IApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ISecretProtector _secretProtector;

    public TelegramBotRuntimeSettingsService(
        IApplicationDbContext db,
        IConfiguration configuration,
        ISecretProtector secretProtector)
    {
        _db = db;
        _configuration = configuration;
        _secretProtector = secretProtector;
    }

    public async Task<TelegramBotRuntimeSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.SiteContentBlocks
            .AsNoTracking()
            .Where(x => x.Group == SettingsGroup)
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);

        return new TelegramBotRuntimeSettings(
            ReadBoolSetting(settings, EnabledKey, ReadConfigBool("TelegramBot:Enabled")),
            NormalizeMode(ReadSetting(settings, ModeKey, _configuration["TelegramBot:Mode"] ?? "LongPolling")),
            ReadProtectedSetting(settings, BotTokenProtectedKey) ?? _configuration["TelegramBot:BotToken"] ?? string.Empty,
            ReadProtectedSetting(settings, SecretTokenProtectedKey) ?? _configuration["TelegramBot:SecretToken"] ?? string.Empty);
    }

    private string? ReadProtectedSetting(IReadOnlyDictionary<string, string> settings, string key)
    {
        if (!settings.TryGetValue(key, out var protectedValue) || string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        try
        {
            return _secretProtector.Unprotect(protectedValue);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ReadSetting(IReadOnlyDictionary<string, string> settings, string key, string fallback)
        => settings.TryGetValue(key, out var value) ? value : fallback;

    private static bool ReadBoolSetting(IReadOnlyDictionary<string, string> settings, string key, bool fallback)
        => settings.TryGetValue(key, out var value) ? string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) : fallback;

    private bool ReadConfigBool(string key)
        => bool.TryParse(_configuration[key], out var parsed) && parsed;

    private static string NormalizeMode(string? mode)
        => string.Equals(mode, "Webhook", StringComparison.OrdinalIgnoreCase) ? "Webhook" : "LongPolling";
}

public sealed record TelegramBotRuntimeSettings(bool Enabled, string Mode, string BotToken, string SecretToken);
