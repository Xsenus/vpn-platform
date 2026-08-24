using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using VpnPlatform.Api.Middleware;
using VpnPlatform.Api.Observability;
using VpnPlatform.Api.Security;
using VpnPlatform.Application;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Infrastructure;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var isAdminBootstrapCommand = args.Any(x => string.Equals(x, "admin-bootstrap", StringComparison.OrdinalIgnoreCase));
var isDatabaseMigrateCommand = args.Any(x => string.Equals(x, "database-migrate", StringComparison.OrdinalIgnoreCase));
var isMaintenanceCommand = isAdminBootstrapCommand || isDatabaseMigrateCommand;

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, includeHostedServices: !isMaintenanceCommand, includeOperationalWorkers: !isMaintenanceCommand);
builder.Services.AddSingleton<ApiObservabilityMetrics>();
builder.Services.AddScoped<ObservabilityHealthService>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
if (builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    builder.Services.AddSwaggerGen();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new CorsOptions();
        if (corsOptions.AllowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy.WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddProblemDetails();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false;
});
builder.Services.AddRateLimiter(ApiRateLimitPolicies.Configure);

var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
    var fullDataProtectionKeyPath = Path.IsPathRooted(dataProtectionKeyPath)
        ? dataProtectionKeyPath
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, dataProtectionKeyPath));
    Directory.CreateDirectory(fullDataProtectionKeyPath);
    builder.Services.AddDataProtection()
        .SetApplicationName("VpnPlatform.Api")
        .PersistKeysToFileSystem(new DirectoryInfo(fullDataProtectionKeyPath));
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = builder.Configuration.BuildJwtSigningKey()
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ActiveUserAccessValidator.ValidateAsync
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var policy in AdminPolicies.PolicyRoles)
    {
        options.AddPolicy(policy.Key, p => p.RequireRole(policy.Value));
    }
});

var app = builder.Build();

if (isAdminBootstrapCommand)
{
    await RunAdminBootstrapCommandAsync(app.Services);
    return;
}

if (isDatabaseMigrateCommand)
{
    await RunDatabaseMigrateCommandAsync(app.Services, app.Configuration);
    return;
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health/live", (HttpContext context, IHostEnvironment environment, ApiObservabilityMetrics metrics) => Results.Ok(new
{
    status = "ok",
    service = app.Configuration["Observability:ServiceName"] ?? "vpn-platform-api",
    environment = environment.EnvironmentName,
    correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? context.TraceIdentifier,
    uptimeSeconds = (long)Math.Max(0, metrics.Uptime.TotalSeconds)
}));
app.MapGet("/health/ready", async (HttpContext context, ObservabilityHealthService healthService, CancellationToken cancellationToken) =>
{
    var report = await healthService.BuildReadyAsync(
        context.Items["X-Correlation-Id"]?.ToString() ?? context.TraceIdentifier,
        cancellationToken);

    return string.Equals(report.Status, HealthStatuses.Ready, StringComparison.Ordinal)
        ? Results.Ok(report)
        : Results.Json(report, statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapGet("/metrics", (ApiObservabilityMetrics metrics) => Results.Text(metrics.ToPrometheus(), "text/plain; version=0.0.4; charset=utf-8"));

app.Run();

static async Task RunDatabaseMigrateCommandAsync(IServiceProvider services, IConfiguration configuration)
{
    var backupDirectory = configuration["DatabaseMaintenance:BackupDirectory"];
    if (string.IsNullOrWhiteSpace(backupDirectory))
    {
        throw new InvalidOperationException("DatabaseMaintenance:BackupDirectory is required for the database-migrate command.");
    }

    using var scope = services.CreateScope();
    var result = await scope.ServiceProvider
        .GetRequiredService<PostgresMigrationRunner>()
        .RunAsync(backupDirectory, CancellationToken.None);

    if (result.AppliedMigrations.Count == 0)
    {
        Console.WriteLine("Database is already up to date; no backup or migration was needed.");
        return;
    }

    Console.WriteLine($"Verified pre-migration backup: {result.BackupPath}");
    Console.WriteLine($"Applied migrations: {result.AppliedMigrations.Count}");
    foreach (var migration in result.AppliedMigrations)
    {
        Console.WriteLine($"- {migration}");
    }
}

static async Task RunAdminBootstrapCommandAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var provider = scope.ServiceProvider;
    var db = provider.GetRequiredService<ApplicationDbContext>();
    var databaseOptions = provider.GetRequiredService<IOptions<DatabaseStartupOptions>>().Value;
    var adminOptions = provider.GetRequiredService<IOptions<AdminBootstrapOptions>>().Value;
    var repairAt = provider.GetRequiredService<IClock>().UtcNow;

    if (!adminOptions.Enabled)
    {
        throw new InvalidOperationException("AdminBootstrap:Enabled must be true for the admin-bootstrap command.");
    }

    if (databaseOptions.ApplyMigrationsOnStartup)
    {
        if (DatabaseProviderConfigurator.IsSqlite(databaseOptions.Provider) && databaseOptions.UseEnsureCreatedForLocalSqlite)
        {
            await db.Database.EnsureCreatedAsync();
            await LocalSqliteSchemaRepair.ApplyAsync(db, repairAt);
        }
        else
        {
            var isSqlite = db.Database.IsSqlite();
            if (isSqlite)
            {
                await LocalSqliteSchemaRepair.PrepareMigrationsAsync(db, repairAt);
            }

            await db.Database.MigrateAsync();
            if (isSqlite)
            {
                await LocalSqliteSchemaRepair.ApplyAsync(db, repairAt);
            }
        }
    }

    var commandOptions = new AdminBootstrapOptions
    {
        Enabled = adminOptions.Enabled,
        Email = adminOptions.Email,
        Password = adminOptions.Password,
        DisplayName = adminOptions.DisplayName,
        RolesCsv = adminOptions.RolesCsv,
        ResetExistingPassword = true
    };
    var result = await provider.GetRequiredService<AdminBootstrapService>().BootstrapAsync(db, commandOptions, CancellationToken.None);

    Console.WriteLine("Admin bootstrap completed.");
    Console.WriteLine($"Email: {result.Email}");
    Console.WriteLine($"Roles: {result.RolesCsv}");
    Console.WriteLine($"Created: {result.Created}");
    Console.WriteLine($"Existing password reset: {result.ExistingPasswordReset}");
}
