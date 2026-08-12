using System.Security.Claims;
using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Api.Controllers.Me;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class MeSupportControllerTests
{
    private static readonly string[] ForbiddenCabinetConversationFields =
    [
        "UserId", "TelegramUserId", "AssignedToUserId", "InternalNote"
    ];

    private static readonly string[] ForbiddenCabinetMessageFields =
    [
        "UserId", "TelegramUserId", "AttachmentsJson", "IsInternalNote"
    ];

    [Fact]
    public async Task Cabinet_Support_Should_Return_Only_User_Facing_Fields()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var conversation = new SupportConversation
        {
            UserId = userId,
            TelegramUserId = 777001,
            AssignedToUserId = Guid.NewGuid(),
            Channel = "web",
            Status = "open",
            Subject = "Безопасная граница",
            InternalNote = "private-order-context"
        };
        db.SupportConversations.Add(conversation);
        db.SupportMessages.Add(new SupportMessage
        {
            SupportConversationId = conversation.Id,
            UserId = userId,
            TelegramUserId = 777001,
            Direction = "inbound",
            Text = "Пользовательское сообщение",
            AttachmentsJson = "[{\"private\":true}]"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var conversationsResult = await controller.GetSupportConversations(CancellationToken.None);
        var messagesResult = await controller.GetSupportMessages(conversation.Id, CancellationToken.None);

        var conversations = Assert.IsAssignableFrom<System.Collections.IEnumerable>(Assert.IsType<OkObjectResult>(conversationsResult).Value);
        var messages = Assert.IsAssignableFrom<System.Collections.IEnumerable>(Assert.IsType<OkObjectResult>(messagesResult).Value);
        var conversationJson = JsonSerializer.Serialize(conversations.Cast<object>().Single());
        var messageJson = JsonSerializer.Serialize(messages.Cast<object>().Single());
        AssertForbiddenFields(conversationJson, ForbiddenCabinetConversationFields);
        AssertForbiddenFields(messageJson, ForbiddenCabinetMessageFields);
    }

    [Fact]
    public async Task Cabinet_Support_Commands_Should_Return_Only_User_Facing_Fields()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var controller = CreateController(db, userId);

        var createResult = await controller.CreateSupportConversation(
            new CreateMeSupportConversationHttpRequest(
                "Проверка оплаты",
                "Пожалуйста, проверьте состояние оплаты.",
                null,
                null),
            CancellationToken.None);
        var created = Assert.IsType<CabinetSupportConversationDto>(Assert.IsType<OkObjectResult>(createResult).Value);
        var replyResult = await controller.ReplySupportConversation(
            created.Id,
            new MeSupportReplyHttpRequest("Дополнительные сведения", created.Revision),
            CancellationToken.None);
        var reply = Assert.IsType<CabinetSupportMessageDto>(Assert.IsType<OkObjectResult>(replyResult).Value);

        AssertForbiddenFields(JsonSerializer.Serialize(created), ForbiddenCabinetConversationFields);
        AssertForbiddenFields(JsonSerializer.Serialize(reply), ForbiddenCabinetMessageFields);
    }

    [Fact]
    public async Task Cabinet_Support_Should_Apply_History_Limits_In_Sqlite_Queries()
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

        var userId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow.AddDays(-10);
        db.Users.Add(new User
        {
            Id = userId,
            Email = "support-limit@example.test",
            DisplayName = "Support limit user",
            Status = UserStatus.Active
        });
        var conversations = Enumerable.Range(0, 105).Select(index => new SupportConversation
        {
            UserId = userId,
            Channel = "web",
            Status = "open",
            Subject = $"Conversation {index:D3}",
            CreatedAt = baseTime.AddMinutes(index),
            UpdatedAt = baseTime.AddMinutes(index)
        }).ToArray();
        db.SupportConversations.AddRange(conversations);
        var selected = conversations[^1];
        db.SupportMessages.AddRange(Enumerable.Range(0, 205).Select(index => new SupportMessage
        {
            SupportConversationId = selected.Id,
            UserId = userId,
            Direction = "inbound",
            Text = $"Message {index:D3}",
            CreatedAt = baseTime.AddSeconds(index)
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = CreateController(db, userId);
        var conversationsResult = await controller.GetSupportConversations(CancellationToken.None);
        var messagesResult = await controller.GetSupportMessages(selected.Id, CancellationToken.None);

        var conversationItems = Assert.IsAssignableFrom<System.Collections.IEnumerable>(Assert.IsType<OkObjectResult>(conversationsResult).Value).Cast<object>().ToArray();
        var messageItems = Assert.IsAssignableFrom<System.Collections.IEnumerable>(Assert.IsType<OkObjectResult>(messagesResult).Value).Cast<object>().ToArray();
        Assert.Equal(100, conversationItems.Length);
        Assert.Equal(200, messageItems.Length);
        Assert.Contains(interceptor.Commands, command => command.Contains("SupportConversations", StringComparison.OrdinalIgnoreCase) && command.Contains("LIMIT 100", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command => command.Contains("SupportMessages", StringComparison.OrdinalIgnoreCase) && command.Contains("LIMIT 200", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cabinet_Support_Flow_Should_Work_With_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = userId, Email = "support-user@example.test", DisplayName = "Support user", PasswordHash = "hash", ReferralCode = "support-user" },
            new User { Id = adminId, Email = "support-admin@example.test", DisplayName = "Support admin", PasswordHash = "hash", ReferralCode = "support-admin", RolesCsv = UserRoles.SupportAgent, Status = UserStatus.Active });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Support", Slug = "support", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        db.Orders.Add(new Order { Id = orderId, UserId = userId, TariffId = tariffId, Amount = 100, Currency = "RUB", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
        db.Subscriptions.Add(new Subscription { Id = subscriptionId, UserId = userId, TariffId = tariffId, StartAt = DateTimeOffset.UtcNow.AddDays(-1), EndAt = DateTimeOffset.UtcNow.AddDays(29) });
        await db.SaveChangesAsync();

        var cabinet = CreateController(db, userId);
        var createResult = await cabinet.CreateSupportConversation(
            new CreateMeSupportConversationHttpRequest("Не прошла оплата", "Помогите проверить платеж после продления.", orderId, subscriptionId),
            CancellationToken.None);

        var createOk = Assert.IsType<OkObjectResult>(createResult);
        var conversationDto = Assert.IsType<CabinetSupportConversationDto>(createOk.Value);
        Assert.Equal("open", conversationDto.Status);

        var admin = CreateAdminController(db, adminId);
        var replyResult = await admin.ReplySupportConversation(conversationDto.Id, new AdminSupportReplyHttpRequest("Проверили платеж, заказ в обработке.", conversationDto.Revision), CancellationToken.None);
        Assert.IsType<OkObjectResult>(replyResult);

        var noteResult = await admin.AddSupportInternalNote(conversationDto.Id, new AdminSupportNoteHttpRequest("Проверить повторно после webhook.", conversationDto.Revision + 1), CancellationToken.None);
        Assert.IsType<OkObjectResult>(noteResult);

        var statusResult = await admin.UpdateSupportConversationStatus(conversationDto.Id, new AdminSupportStatusHttpRequest("pending", adminId, conversationDto.Revision + 2), CancellationToken.None);
        Assert.IsType<OkObjectResult>(statusResult);
        var adminAudits = await db.AuditLogs.Where(x => x.EntityId == conversationDto.Id.ToString()).ToListAsync();
        Assert.Contains(adminAudits, x => x.Action == "support.reply");
        Assert.Contains(adminAudits, x => x.Action == "support.note.add");
        Assert.Contains(adminAudits, x => x.Action == "support.status.update");
        Assert.All(adminAudits, x => Assert.DoesNotContain("webhook", x.AfterJson, StringComparison.OrdinalIgnoreCase));

        var messagesResult = await cabinet.GetSupportMessages(conversationDto.Id, CancellationToken.None);
        var messagesOk = Assert.IsType<OkObjectResult>(messagesResult);
        var messages = Assert.IsAssignableFrom<IReadOnlyCollection<CabinetSupportMessageDto>>(messagesOk.Value);
        Assert.Equal(2, messages.Count);
        Assert.Contains(messages, x => x.Direction == "inbound" && x.Text.Contains("платеж", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, x => x.Direction == "outbound" && x.Text.Contains("заказ", StringComparison.OrdinalIgnoreCase));

        var closeResult = await cabinet.UpdateSupportConversationStatus(conversationDto.Id, new MeSupportStatusHttpRequest("closed", conversationDto.Revision + 3), CancellationToken.None);
        Assert.IsType<OkObjectResult>(closeResult);
        Assert.Equal("closed", (await db.SupportConversations.SingleAsync(x => x.Id == conversationDto.Id)).Status);

        var reopenResult = await cabinet.ReplySupportConversation(conversationDto.Id, new MeSupportReplyHttpRequest("Вопрос снова актуален.", conversationDto.Revision + 4), CancellationToken.None);
        Assert.IsType<OkObjectResult>(reopenResult);
        var reopened = await db.SupportConversations.SingleAsync(x => x.Id == conversationDto.Id);
        Assert.Equal("open", reopened.Status);
        Assert.Null(reopened.ClosedAt);
        Assert.Equal(conversationDto.Revision + 5, reopened.Revision);
        Assert.Equal(4, await db.SupportMessages.CountAsync(x => x.SupportConversationId == conversationDto.Id));
    }

    [Fact]
    public async Task CreateSupportConversation_Should_Link_User_Order_And_Subscription()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.Orders.Add(new Order { Id = orderId, UserId = userId, TariffId = Guid.NewGuid(), ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
        db.Subscriptions.Add(new Subscription { Id = subscriptionId, UserId = userId, TariffId = Guid.NewGuid(), StartAt = DateTimeOffset.UtcNow, EndAt = DateTimeOffset.UtcNow.AddDays(30) });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var result = await controller.CreateSupportConversation(
            new CreateMeSupportConversationHttpRequest("Не прошла оплата", "Помогите проверить платеж.", orderId, subscriptionId),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CabinetSupportConversationDto>(ok.Value);
        Assert.Equal("web", dto.Channel);
        Assert.Equal("open", dto.Status);

        var conversation = await db.SupportConversations.SingleAsync();
        var message = await db.SupportMessages.SingleAsync();
        Assert.Equal(userId, conversation.UserId);
        Assert.Contains(orderId.ToString(), conversation.InternalNote);
        Assert.Contains(subscriptionId.ToString(), conversation.InternalNote);
        Assert.Equal("inbound", message.Direction);
        Assert.Contains(orderId.ToString(), message.RawPayload);
        Assert.Contains(subscriptionId.ToString(), message.RawPayload);
    }

    [Fact]
    public async Task SupportMessages_Should_Hide_InternalNotes_And_Allow_User_Status_Update()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var conversation = new SupportConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = "web",
            Status = "open",
            Subject = "Помощь"
        };
        db.SupportConversations.Add(conversation);
        db.SupportMessages.Add(new SupportMessage { SupportConversationId = conversation.Id, UserId = userId, Direction = "inbound", Text = "Вопрос" });
        db.SupportMessages.Add(new SupportMessage { SupportConversationId = conversation.Id, Direction = "internal", Text = "Скрытая заметка", IsInternalNote = true });
        db.SupportMessages.Add(new SupportMessage { SupportConversationId = conversation.Id, Direction = "internal", Text = "Legacy скрытая заметка", IsInternalNote = false });
        db.SupportMessages.Add(new SupportMessage { SupportConversationId = conversation.Id, Direction = "Internal", Text = "Legacy заметка с другим регистром", IsInternalNote = false });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var messagesResult = await controller.GetSupportMessages(conversation.Id, CancellationToken.None);
        var messagesOk = Assert.IsType<OkObjectResult>(messagesResult);
        var messages = Assert.IsAssignableFrom<IReadOnlyCollection<CabinetSupportMessageDto>>(messagesOk.Value);
        Assert.Single(messages);

        var statusResult = await controller.UpdateSupportConversationStatus(conversation.Id, new MeSupportStatusHttpRequest("closed", conversation.Revision), CancellationToken.None);
        Assert.IsType<OkObjectResult>(statusResult);

        var updated = await db.SupportConversations.SingleAsync();
        Assert.Equal("closed", updated.Status);
        Assert.NotNull(updated.ClosedAt);
    }

    [Fact]
    public async Task CreateSupportConversation_Should_Reject_Foreign_Order()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.Orders.Add(new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TariffId = Guid.NewGuid(), ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
        await db.SaveChangesAsync();

        var foreignOrderId = await db.Orders.Select(x => x.Id).SingleAsync();
        var controller = CreateController(db, userId);
        var result = await controller.CreateSupportConversation(
            new CreateMeSupportConversationHttpRequest("Оплата", "Нужна помощь.", foreignOrderId, null),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.SupportConversations.ToListAsync());
    }

    [Fact]
    public async Task SupportEndpoints_Should_Reject_Too_Short_Text_And_Invalid_Status()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var conversation = new SupportConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = "web",
            Status = "open",
            Subject = "Оплата"
        };
        db.SupportConversations.Add(conversation);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        Assert.IsType<BadRequestObjectResult>(await controller.CreateSupportConversation(new CreateMeSupportConversationHttpRequest("Опл", "Слишком коротко", null, null), CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.ReplySupportConversation(conversation.Id, new MeSupportReplyHttpRequest(" "), CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.UpdateSupportConversationStatus(conversation.Id, new MeSupportStatusHttpRequest("pending", conversation.Revision), CancellationToken.None));
    }

    [Fact]
    public async Task SupportStatus_Should_Reject_Stale_Revision_Without_Overwriting_State()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var conversation = new SupportConversation
        {
            UserId = userId,
            Channel = "web",
            Status = "open",
            Subject = "Конкурентный статус",
            Revision = 3
        };
        db.Users.Add(new User
        {
            Id = userId,
            Email = "stale-support@example.test",
            DisplayName = "Stale support user",
            Status = UserStatus.Active
        });
        db.SupportConversations.Add(conversation);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var result = await controller.UpdateSupportConversationStatus(
            conversation.Id,
            new MeSupportStatusHttpRequest("closed", 2),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        db.ChangeTracker.Clear();
        var unchanged = await db.SupportConversations.SingleAsync();
        Assert.Equal("open", unchanged.Status);
        Assert.Equal(3, unchanged.Revision);
        Assert.Null(unchanged.ClosedAt);
    }

    private static MeController CreateController(ApplicationDbContext db, Guid userId)
    {
        var configuration = new ConfigurationBuilder().Build();
        return new MeController(db, null!, null!, null!, null!, null!, configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "unit-test"))
                }
            }
        };
    }

    private static AdminOperationsController CreateAdminController(ApplicationDbContext db, Guid userId)
        => new(db, null!, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "unit-test"))
                }
            }
        };

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void AssertForbiddenFields(string json, IEnumerable<string> fields)
    {
        foreach (var field in fields)
        {
            Assert.DoesNotContain($"\"{field}\"", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class CommandCaptureInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
