using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Me;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class MeSupportControllerTests
{
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
        var dto = Assert.IsType<SupportConversationDto>(ok.Value);
        Assert.Equal("web", dto.Channel);
        Assert.Equal("open", dto.Status);
        Assert.Equal(string.Empty, dto.InternalNote);

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
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var messagesResult = await controller.GetSupportMessages(conversation.Id, CancellationToken.None);
        var messagesOk = Assert.IsType<OkObjectResult>(messagesResult);
        var messages = Assert.IsAssignableFrom<IReadOnlyCollection<SupportMessageDto>>(messagesOk.Value);
        Assert.Single(messages);
        Assert.DoesNotContain(messages, x => x.IsInternalNote);

        var statusResult = await controller.UpdateSupportConversationStatus(conversation.Id, new MeSupportStatusHttpRequest("closed"), CancellationToken.None);
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

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
