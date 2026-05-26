using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Domain.Entities;
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

        var result = await controller.ReplySupportConversation(conversation.Id, new AdminSupportReplyHttpRequest("Ответ администратора"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var message = await db.SupportMessages.SingleAsync();
        var notification = await db.TelegramBotNotifications.SingleAsync();
        Assert.Equal("outbound", message.Direction);
        Assert.Equal(777001, notification.TelegramUserId);
        Assert.Equal("support_reply", notification.Type);
        Assert.Equal("pending", notification.Status);
        Assert.Contains("Ответ администратора", notification.PayloadJson);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
