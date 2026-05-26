using Serilog;
using VpnPlatform.Application;
using VpnPlatform.Application.Services;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Infrastructure;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.TelegramBot;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.Enrich.FromLogContext().WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, includeHostedServices: true, includeOperationalWorkers: false);
builder.Services.AddHttpClient<TelegramHttpClient>();
builder.Services.AddTransient<ITelegramInvoiceProvider>(sp => sp.GetRequiredService<TelegramHttpClient>());
builder.Services.AddHostedService<TelegramLongPollingService>();
builder.Services.AddHostedService<TelegramNotificationDispatcherService>();

var app = builder.Build();

app.MapPost("/telegram/webhook", async (HttpRequest request, TelegramBotService service, TelegramHttpClient client, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    if (!configuration.GetValue<bool>("TelegramBot:Enabled"))
    {
        return Results.NotFound(new { error = "Telegram bot is disabled." });
    }

    if (!string.Equals(configuration["TelegramBot:Mode"], "Webhook", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Telegram bot is not configured for Webhook mode." });
    }

    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync(cancellationToken);
    var headers = request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    var result = await service.ProcessUpdateAsync(rawBody, headers, configuration["TelegramBot:SecretToken"], cancellationToken);
    if (!result.IsSuccess || result.Value is null)
    {
        return Results.BadRequest(new { error = result.Error });
    }

    if (!string.IsNullOrWhiteSpace(result.Value.PreCheckoutQueryId) && result.Value.PreCheckoutOk.HasValue)
    {
        await client.AnswerPreCheckoutQueryAsync(result.Value.PreCheckoutQueryId, result.Value.PreCheckoutOk.Value, result.Value.PreCheckoutError, cancellationToken);
    }

    if (result.Value.Processed && result.Value.ChatId.HasValue && !string.IsNullOrWhiteSpace(result.Value.ResponseText))
    {
        await client.SendMessageAsync(result.Value.ChatId.Value, result.Value.ResponseText, result.Value.ReplyMarkupJson, cancellationToken);
    }

    return Results.Ok(new { status = result.Value.Processed ? "processed" : "duplicate" });
});

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

await app.RunAsync();
