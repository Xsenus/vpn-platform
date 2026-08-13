using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminNotificationReadBoundaryTests
{
    [Fact]
    public async Task Admin_Notification_Query_Should_Filter_Order_And_Limit_In_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var start = new DateTimeOffset(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);
        db.NotificationDeliveries.AddRange(Enumerable.Range(0, 505).Select(index => new NotificationDelivery
        {
            TemplateKey = "audit_digest",
            Channel = NotificationChannelType.Email,
            ToAddress = $"bounded-{index:D3}@example.test",
            Status = NotificationDeliveryStatus.Failed,
            PayloadJson = "{}",
            Attempts = 1,
            CreatedAt = start.AddMinutes(index),
            UpdatedAt = start.AddMinutes(index)
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = new AdminOperationsController(db, null!, null!, null!);
        var result = Assert.IsType<OkObjectResult>(await controller.GetNotificationDeliveries(
            new AdminNotificationDeliveryFilters(
                Status: "Failed",
                TemplateKey: "audit_digest",
                Search: "bounded-",
                Limit: 7),
            CancellationToken.None));

        var rows = Assert.IsType<List<AdminNotificationDeliveryDto>>(result.Value);
        Assert.Equal(7, rows.Count);
        Assert.Equal("bo***@example.test", rows[0].MaskedToAddress);
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("NotificationDeliveries", StringComparison.OrdinalIgnoreCase)
            && command.Contains("julianday", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
