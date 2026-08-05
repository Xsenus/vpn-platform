using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.Infrastructure.Services;
using VpnPlatform.Application.Abstractions;
using Xunit;

namespace VpnPlatform.UnitTests;

public sealed class EmailStartupSafetyTests
{
    [Fact]
    public async Task Production_Should_Reject_Disabled_Email_Delivery()
    {
        var validator = CreateValidator(new EmailDeliveryOptions());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => validator.StartAsync(CancellationToken.None));

        Assert.Contains("Email:Mode=Smtp", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_Should_Accept_Complete_Smtp_Configuration()
    {
        var validator = CreateValidator(new EmailDeliveryOptions
        {
            Mode = "Smtp",
            Host = "smtp.mailhost.test",
            Port = 587,
            UseSsl = true,
            FromAddress = "no-reply@mailhost.test",
            Username = "mailer",
            Password = "smtp-production-password"
        });

        await validator.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Smtp_Sender_Should_Fail_Closed_When_Mode_Is_Disabled()
    {
        var sender = new SmtpEmailSender(Options.Create(new EmailDeliveryOptions()));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(
            new EmailMessage("user@example.test", "Subject", "Body"),
            CancellationToken.None));

        Assert.Contains("disabled", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Smtp_Mode_Should_Reject_Incomplete_Connection_Settings()
    {
        var validator = CreateValidator(new EmailDeliveryOptions { Mode = "Smtp", Port = 587 });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => validator.StartAsync(CancellationToken.None));

        Assert.Contains("SMTP host", error.Message, StringComparison.Ordinal);
        Assert.Contains("FromAddress", error.Message, StringComparison.Ordinal);
    }

    private static StartupSafetyValidator CreateValidator(EmailDeliveryOptions email)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "production-jwt-signing-key-000000000000000000000",
            ["Security:SecretEncryptionKey"] = "production-secret-encryption-key-000000000000000",
            ["Database:Provider"] = "Postgres",
            ["Swagger:Enabled"] = "false",
            ["Vpn:X3Ui:Mode"] = "Production",
            ["TelegramBot:Enabled"] = "false"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new StartupSafetyValidator(
            configuration,
            new ProductionEnvironment(),
            NullLogger<StartupSafetyValidator>.Instance,
            Options.Create(new DatabaseStartupOptions { Provider = "Postgres" }),
            Options.Create(new AdminBootstrapOptions()),
            Options.Create(new CorsOptions { AllowedOrigins = ["https://vpn.mailhost.test"] }),
            Options.Create(email));
    }

    private sealed class ProductionEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
