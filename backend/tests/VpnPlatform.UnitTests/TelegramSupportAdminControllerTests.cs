using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Common;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TelegramSupportAdminControllerTests
{
    [Fact]
    public async Task Admin_Reply_Should_Queue_Telegram_Notification()
    {
        await using var db = CreateDbContext();
        var conversation = new SupportConversation
        {
            Id = Guid.NewGuid(),
            TelegramUserId = 777001,
            Channel = "telegram",
            Status = "open",
            Subject = "Telegram support"
        };
        db.SupportConversations.Add(conversation);
        await db.SaveChangesAsync();

        var controller = new AdminOperationsController(db, null!, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    }, "unit-test"))
                }
            }
        };

        var result = await controller.ReplySupportConversation(conversation.Id, new AdminSupportReplyHttpRequest("Ответ администратора", conversation.Revision), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var message = await db.SupportMessages.SingleAsync();
        var notification = await db.TelegramBotNotifications.SingleAsync();
        Assert.Equal("outbound", message.Direction);
        Assert.Equal(777001, notification.TelegramUserId);
        Assert.Equal("support_reply", notification.Type);
        Assert.Equal("pending", notification.Status);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "support.reply");
        Assert.Equal(conversation.Id.ToString(), audit.EntityId);
        Assert.DoesNotContain(message.Text, audit.AfterJson, StringComparison.Ordinal);
        Assert.Contains("Ответ администратора", notification.PayloadJson);
    }

    [Fact]
    public async Task Status_Should_Reject_Assignment_To_User_Without_Support_Write_Access()
    {
        await using var db = CreateDbContext();
        var regularUser = new User
        {
            Email = "regular@example.test",
            DisplayName = "Regular user",
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active
        };
        var conversation = new SupportConversation { Status = "open", Subject = "Assignment" };
        db.AddRange(regularUser, conversation);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.UpdateSupportConversationStatus(
            conversation.Id,
            new AdminSupportStatusHttpRequest("pending", regularUser.Id, conversation.Revision),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(conversation.AssignedToUserId);
        Assert.Equal("open", conversation.Status);
        Assert.Equal(0, conversation.Revision);
    }

    [Fact]
    public async Task Status_Should_Allow_Assignment_To_Active_Support_Agent()
    {
        await using var db = CreateDbContext();
        var supportAgent = new User
        {
            Email = "support@example.test",
            DisplayName = "Support agent",
            RolesCsv = UserRoles.SupportAgent,
            Status = UserStatus.Active
        };
        var conversation = new SupportConversation { Status = "open", Subject = "Assignment" };
        db.AddRange(supportAgent, conversation);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.UpdateSupportConversationStatus(
            conversation.Id,
            new AdminSupportStatusHttpRequest("pending", supportAgent.Id, conversation.Revision),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(supportAgent.Id, conversation.AssignedToUserId);
        Assert.Equal("pending", conversation.Status);
        Assert.Equal(1, conversation.Revision);
    }

    private static AdminOperationsController CreateController(ApplicationDbContext db)
        => new(db, null!, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
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
}
