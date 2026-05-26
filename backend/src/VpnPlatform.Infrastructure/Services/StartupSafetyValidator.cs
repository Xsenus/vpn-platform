using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.Infrastructure.Persistence;

namespace VpnPlatform.Infrastructure.Services;

public sealed class StartupSafetyValidator : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<StartupSafetyValidator> _logger;
    private readonly DatabaseStartupOptions _databaseOptions;
    private readonly AdminBootstrapOptions _adminOptions;
    private readonly CorsOptions _corsOptions;

    public StartupSafetyValidator(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<StartupSafetyValidator> logger,
        IOptions<DatabaseStartupOptions> databaseOptions,
        IOptions<AdminBootstrapOptions> adminOptions,
        IOptions<CorsOptions> corsOptions)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _databaseOptions = databaseOptions.Value;
        _adminOptions = adminOptions.Value;
        _corsOptions = corsOptions.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var signingKey = _configuration["Jwt:SigningKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32 || signingKey.Contains("replace", StringComparison.OrdinalIgnoreCase) || signingKey.Contains("unsafe", StringComparison.OrdinalIgnoreCase) || signingKey.Contains("set-through", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Jwt:SigningKey must be a non-placeholder secret with at least 32 characters.");
        }

        var secretEncryptionKey = _configuration["Security:SecretEncryptionKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(secretEncryptionKey) || secretEncryptionKey.Length < 32 || secretEncryptionKey.Contains("replace", StringComparison.OrdinalIgnoreCase) || secretEncryptionKey.Contains("unsafe", StringComparison.OrdinalIgnoreCase) || secretEncryptionKey.Contains("set-through", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Security:SecretEncryptionKey must be a non-placeholder secret with at least 32 characters.");
        }

        if (_adminOptions.Enabled && (string.IsNullOrWhiteSpace(_adminOptions.Email) || string.IsNullOrWhiteSpace(_adminOptions.Password) || _adminOptions.Password.Length < 16))
        {
            errors.Add("AdminBootstrap is enabled but Email/Password are invalid. Password must have at least 16 characters.");
        }

        if (_environment.IsProduction())
        {
            if (DatabaseProviderConfigurator.IsSqlite(_databaseOptions.Provider))
            {
                errors.Add("Database:Provider=Sqlite is intended only for local development and is forbidden in Production.");
            }

            if (_databaseOptions.UseEnsureCreatedForLocalSqlite)
            {
                errors.Add("Database:UseEnsureCreatedForLocalSqlite must be false in Production.");
            }

            if (_databaseOptions.ApplyMigrationsOnStartup)
            {
                errors.Add("Database:ApplyMigrationsOnStartup must be false in Production. Run migrations as a controlled deploy step after a backup.");
            }

            if (_databaseOptions.SeedDemoData)
            {
                errors.Add("Database:SeedDemoData must be false in Production.");
            }

            if (string.Equals(_configuration["Swagger:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Swagger:Enabled must not be true in Production unless it is exposed behind a separate authenticated internal network control.");
            }

            if (_corsOptions.AllowedOrigins.Length == 0 || _corsOptions.AllowedOrigins.Any(x => x == "*" || x.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add("Cors:AllowedOrigins must contain explicit production origins and must not contain localhost or wildcard values.");
            }

            foreach (var provider in new[] { "YooMoney", "YooKassa", "RoboKassa", "TelegramStars", "CloudPayments", "TBankAcquiring", "Prodamus", "Stripe", "PayPal" })
            {
                var mode = _configuration[$"Payments:{provider}:Mode"];
                if (string.Equals(mode, "Sandbox", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Payments:{provider}:Mode=Sandbox is forbidden in Production.");
                }
            }

            if (string.Equals(_configuration["Vpn:X3Ui:Mode"], "Sandbox", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Vpn:X3Ui:Mode=Sandbox is forbidden in Production.");
            }

            foreach (var pair in FlattenConfiguration(_configuration))
            {
                if (ContainsForbiddenProductionPlaceholder(pair.Value))
                {
                    errors.Add($"Configuration value {pair.Key} contains a forbidden production placeholder.");
                }
            }

            if (string.Equals(_configuration["TelegramBot:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
            {
                var botToken = _configuration["TelegramBot:BotToken"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(botToken) || botToken.Contains("replace", StringComparison.OrdinalIgnoreCase) || botToken.Contains("unsafe", StringComparison.OrdinalIgnoreCase) || botToken.Contains("set-through", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("TelegramBot:BotToken is required when TelegramBot:Enabled=true in Production.");
                }

                if (string.Equals(_configuration["TelegramBot:Mode"], "Webhook", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(_configuration["TelegramBot:WebhookUrl"]))
                    {
                        errors.Add("TelegramBot:WebhookUrl is required in Webhook mode.");
                    }

                    if (string.IsNullOrWhiteSpace(_configuration["TelegramBot:SecretToken"]))
                    {
                        errors.Add("TelegramBot:SecretToken is required in Webhook mode.");
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                _logger.LogCritical("Startup safety validation failed: {Error}", error);
            }

            throw new InvalidOperationException("Startup safety validation failed: " + string.Join("; ", errors));
        }

        _logger.LogInformation(
            "Startup safety validation passed for environment {Environment}. AutoMigrations={AutoMigrations}, SeedDemoData={SeedDemoData}",
            _environment.EnvironmentName,
            _databaseOptions.ApplyMigrationsOnStartup,
            _databaseOptions.SeedDemoData);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IEnumerable<KeyValuePair<string, string>> FlattenConfiguration(IConfiguration configuration)
    {
        foreach (var child in configuration.AsEnumerable())
        {
            if (!string.IsNullOrWhiteSpace(child.Value))
            {
                yield return new KeyValuePair<string, string>(child.Key, child.Value);
            }
        }
    }

    private static bool ContainsForbiddenProductionPlaceholder(string value)
        => value.Contains("checkout.example", StringComparison.OrdinalIgnoreCase)
           || value.Contains("node.example", StringComparison.OrdinalIgnoreCase)
           || value.Contains("replace-me", StringComparison.OrdinalIgnoreCase)
           || value.Contains("set-through", StringComparison.OrdinalIgnoreCase)
           || value.Contains("unsafe-dev", StringComparison.OrdinalIgnoreCase);
}
