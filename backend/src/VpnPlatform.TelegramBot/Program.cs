using Serilog;
using VpnPlatform.Application;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Infrastructure;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.TelegramBot;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.Enrich.FromLogContext().WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, includeHostedServices: true, includeOperationalWorkers: false);
builder.Services.AddHttpClient<TelegramHttpClient>();
builder.Services.AddTransient<ITelegramInvoiceProvider>(sp => sp.GetRequiredService<TelegramHttpClient>());
builder.Services.AddHostedService<TelegramLongPollingService>();
builder.Services.AddHostedService<TelegramNotificationDispatcherService>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

await app.RunAsync();
