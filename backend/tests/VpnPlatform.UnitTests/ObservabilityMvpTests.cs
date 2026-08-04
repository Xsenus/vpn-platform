using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Api.Middleware;
using VpnPlatform.Api.Observability;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ObservabilityMvpTests
{
    [Fact]
    public async Task CorrelationIdMiddleware_Should_Normalize_And_Return_Correlation_Header()
    {
        var longCorrelationId = new string('x', 180);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = $"  {longCorrelationId}  ";

        var middleware = new CorrelationIdMiddleware(
            _ =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context);

        var correlationId = Assert.IsType<string>(context.Items["X-Correlation-Id"]);
        Assert.Equal(128, correlationId.Length);
        Assert.Equal(correlationId, context.Response.Headers["X-Correlation-Id"]);
    }

    [Fact]
    public async Task RequestObservabilityMiddleware_Should_Record_Request_Metrics()
    {
        var metrics = new ApiObservabilityMetrics();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/test";
        context.Items["X-Correlation-Id"] = "test-correlation";

        var middleware = new RequestObservabilityMiddleware(
            _ =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            },
            NullLogger<RequestObservabilityMiddleware>.Instance,
            metrics);

        await middleware.Invoke(context);

        var prometheus = metrics.ToPrometheus();
        Assert.Equal(1, metrics.RequestsStarted);
        Assert.Equal(1, metrics.RequestsCompleted);
        Assert.Equal(0, metrics.RequestsInFlight);
        Assert.Contains("vpnplatform_http_requests_total", prometheus);
        Assert.Contains("method=\"POST\"", prometheus);
        Assert.Contains("route=\"/api/test\"", prometheus);
        Assert.Contains("status_family=\"2xx\"", prometheus);
        Assert.Contains("vpnplatform_api_uptime_seconds", prometheus);
    }

    [Fact]
    public async Task ObservabilityHealthService_Should_Return_Ready_Report_For_Sqlite_Database()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        db.OutboxMessages.AddRange(
            new OutboxMessage { Type = "OrderTimelineEvent", CorrelationId = "pending", PayloadJson = "{}" },
            new OutboxMessage { Type = "OrderTimelineEvent", CorrelationId = "failed", PayloadJson = "{}", FailedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var metrics = new ApiObservabilityMetrics();
        metrics.OnRequestStarted();
        metrics.OnRequestCompleted(HttpMethods.Get, "/health/live", StatusCodes.Status200OK, 5);

        var service = new ObservabilityHealthService(
            db,
            new TestHostEnvironment(),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Observability:ServiceName"] = "vpn-platform-api-tests"
                })
                .Build(),
            metrics);

        var report = await service.BuildReadyAsync("health-correlation", CancellationToken.None);

        Assert.Equal(HealthStatuses.Ready, report.Status);
        Assert.Equal("vpn-platform-api-tests", report.Service);
        Assert.Equal("health-correlation", report.CorrelationId);
        Assert.Equal(1, report.RequestsStarted);
        Assert.Equal(1, report.RequestsCompleted);
        Assert.Contains(report.Checks, x => x.Name == "database" && x.Status == HealthStatuses.Ready);
        Assert.Contains(report.Checks, x => x.Name == "runtime" && x.Status == HealthStatuses.Ready);
        var database = Assert.Single(report.Checks, x => x.Name == "database");
        Assert.Equal(1, database.Data!["pendingOutbox"]);
        Assert.Equal(1, database.Data["failedOutbox"]);
    }

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
