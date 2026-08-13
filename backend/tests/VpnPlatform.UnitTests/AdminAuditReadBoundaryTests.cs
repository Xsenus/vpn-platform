using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminAuditReadBoundaryTests
{
    [Fact]
    public async Task Admin_Audit_Query_Should_Apply_Date_Window_And_Limit_In_Sqlite()
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
        db.AuditLogs.AddRange(Enumerable.Range(0, 505).Select(index => new AuditLog
        {
            ActorType = "system",
            ActorId = "audit-boundary",
            Action = "auth.session.read",
            EntityType = "Auth",
            EntityId = $"audit-{index:D3}",
            BeforeJson = "{}",
            AfterJson = "{}",
            CreatedAt = start.AddMinutes(index),
            UpdatedAt = start.AddMinutes(index)
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = new AdminOperationsController(db, null!, null!, null!);
        var result = Assert.IsType<OkObjectResult>(await controller.GetAuditLogs(
            new AdminAuditLogFilters(
                Action: "auth.session",
                ActorType: "system",
                Search: "audit-",
                From: start.AddMinutes(490),
                To: start.AddMinutes(496),
                Limit: 7),
            CancellationToken.None));

        var rows = Assert.IsType<List<AdminAuditLogDto>>(result.Value);
        Assert.Equal(7, rows.Count);
        Assert.Equal("audit-496", rows[0].EntityId);
        Assert.Equal("audit-490", rows[^1].EntityId);
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("AuditLogs", StringComparison.OrdinalIgnoreCase)
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
