using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using VpnPlatform.Api.Middleware;
using VpnPlatform.Application;
using VpnPlatform.Application.Common;
using VpnPlatform.Infrastructure;
using VpnPlatform.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, includeHostedServices: true, includeOperationalWorkers: true);

builder.Services.AddControllers();
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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminPolicies.AdminOnly, p => p.RequireRole(AdminPolicies.AllAdminRoles));
    options.AddPolicy(AdminPolicies.AdminRead, p => p.RequireRole(AdminPolicies.AllAdminRoles));
    options.AddPolicy(AdminPolicies.AdminWrite, p => p.RequireRole(AdminPolicies.AdminWriteRoles));
    options.AddPolicy(AdminPolicies.FinanceRead, p => p.RequireRole(AdminPolicies.FinanceReadRoles));
    options.AddPolicy(AdminPolicies.FinanceWrite, p => p.RequireRole(AdminPolicies.FinanceWriteRoles));
    options.AddPolicy(AdminPolicies.SupportRead, p => p.RequireRole(AdminPolicies.SupportReadRoles));
    options.AddPolicy(AdminPolicies.SupportWrite, p => p.RequireRole(AdminPolicies.SupportWriteRoles));
    options.AddPolicy(AdminPolicies.ProvisioningManage, p => p.RequireRole(AdminPolicies.ProvisioningManageRoles));
    options.AddPolicy(AdminPolicies.VpnManage, p => p.RequireRole(AdminPolicies.VpnManageRoles));
    options.AddPolicy(AdminPolicies.BotManage, p => p.RequireRole(AdminPolicies.BotManageRoles));
    options.AddPolicy(AdminPolicies.SettingsManage, p => p.RequireRole(AdminPolicies.SettingsManageRoles));
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("default");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/metrics", () => Results.Text("# HELP vpnplatform_api_info VPN Platform API info\n# TYPE vpnplatform_api_info gauge\nvpnplatform_api_info 1\n", "text/plain; version=0.0.4; charset=utf-8"));

app.Run();
