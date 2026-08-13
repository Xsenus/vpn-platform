using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminSupportReadBoundaryTests
{
    [Fact]
    public async Task Admin_Support_Lists_Should_Select_Latest_Rows_In_Sql()
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

        var now = new DateTimeOffset(2026, 8, 13, 6, 0, 0, TimeSpan.Zero);
        var conversations = Enumerable.Range(0, 205).Select(index => new SupportConversation
        {
            Channel = "web",
            Status = "open",
            Subject = $"Bounded support {index}",
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.SupportConversations.AddRange(conversations);
        await db.SaveChangesAsync();

        var selectedConversation = conversations[^1];
        var messages = Enumerable.Range(0, 205).Select(index => new SupportMessage
        {
            SupportConversationId = selectedConversation.Id,
            Direction = "inbound",
            Text = $"Bounded message {index}",
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.SupportMessages.AddRange(messages);
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = new AdminOperationsController(db, null!, null!, null!);
        var conversationsResult = Assert.IsType<OkObjectResult>(
            await controller.GetSupportConversations(CancellationToken.None));
        var messagesResult = Assert.IsType<OkObjectResult>(
            await controller.GetSupportMessages(selectedConversation.Id, CancellationToken.None));

        using var conversationsJson = JsonDocument.Parse(JsonSerializer.Serialize(conversationsResult.Value));
        using var messagesJson = JsonDocument.Parse(JsonSerializer.Serialize(messagesResult.Value));
        Assert.Equal(200, conversationsJson.RootElement.GetArrayLength());
        Assert.Equal(conversations[^1].Id, conversationsJson.RootElement[0].GetProperty("Id").GetGuid());
        Assert.Equal(conversations[5].Id, conversationsJson.RootElement[199].GetProperty("Id").GetGuid());
        Assert.Equal(200, messagesJson.RootElement.GetArrayLength());
        Assert.Equal(messages[5].Id, messagesJson.RootElement[0].GetProperty("Id").GetGuid());
        Assert.Equal(messages[^1].Id, messagesJson.RootElement[199].GetProperty("Id").GetGuid());

        Assert.Contains(interceptor.Commands, command =>
            command.Contains("SupportConversations", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 200", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("SupportMessages", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 200", StringComparison.OrdinalIgnoreCase));
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
