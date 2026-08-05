using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ReferralProgramControllerTests
{
    [Fact]
    public async Task Admin_Referral_Actions_Should_Create_Patch_And_List_Rewards()
    {
        await using var db = CreateDb();
        var controller = new AdminOperationsController(db, null!, null!, null!);
        db.RewardLedgers.Add(new RewardLedger { UserId = Guid.NewGuid(), Value = 7, CurrencyOrUnit = "days" });
        await db.SaveChangesAsync();

        var request = ValidRequest("Welcome", "draft");
        var created = Assert.IsType<ReferralProgramDto>(Assert.IsType<OkObjectResult>(await controller.CreateReferralProgram(request, CancellationToken.None)).Value);
        using var patch = JsonDocument.Parse("""{"name":"Welcome Plus","status":"active","ruleDefinition":{"firstPurchaseOnly":true,"minimumOrderAmount":500}}""");
        var patched = Assert.IsType<ReferralProgramDto>(Assert.IsType<OkObjectResult>(await controller.PatchReferralProgram(created.Id, patch.RootElement, CancellationToken.None)).Value);
        var programs = Assert.IsType<List<ReferralProgramDto>>(Assert.IsType<OkObjectResult>(await controller.GetReferralPrograms(CancellationToken.None)).Value);
        var referrals = Assert.IsType<List<AdminRewardLedgerDto>>(Assert.IsType<OkObjectResult>(await controller.GetReferrals(CancellationToken.None)).Value);

        Assert.Equal("Welcome Plus", patched.Name);
        Assert.Equal("active", patched.Status);
        Assert.Contains("minimumOrderAmount", patched.RuleDefinition, StringComparison.Ordinal);
        Assert.Single(programs);
        Assert.Single(referrals);
        var audits = await db.AuditLogs.ToListAsync();
        Assert.Equal(2, audits.Count);
        Assert.Contains(audits, x => x.Action == "referral_program.create" && x.EntityId == created.Id.ToString());
        Assert.Contains(audits, x => x.Action == "referral_program.update" && x.EntityId == created.Id.ToString() && x.BeforeJson != x.AfterJson);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"name\":123,\"status\":\"active\"}")]
    [InlineData("{\"name\":\"Changed\",\"status\":false}")]
    [InlineData("{\"name\":\"Changed\",\"status\":\"active\",\"ruleDefinition\":\"{}\",\"rewardDefinition\":\"{}\"}")]
    public async Task Admin_Referral_Patch_Should_Reject_Invalid_Types_Without_Mutating_Program(string rawPayload)
    {
        await using var db = CreateDb();
        var program = new ReferralProgram { Name = "Original", Status = "draft" };
        db.ReferralPrograms.Add(program);
        await db.SaveChangesAsync();
        using var payload = JsonDocument.Parse(rawPayload);

        var result = await new AdminOperationsController(db, null!, null!, null!).PatchReferralProgram(program.Id, payload.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Original", program.Name);
        Assert.Equal("draft", program.Status);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Admin_Referral_Create_Should_Reject_Invalid_Date_Range()
    {
        await using var db = CreateDb();
        var controller = new AdminOperationsController(db, null!, null!, null!);
        var request = ValidRequest("Invalid dates", "active") with
        {
            StartAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            EndAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var result = await controller.CreateReferralProgram(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.ReferralPrograms.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    private static ReferralProgramUpsertHttpRequest ValidRequest(string name, string status)
        => new(
            name,
            status,
            null,
            null,
            "{\"firstPurchaseOnly\":true,\"allowedChannels\":[\"Web\"]}",
            "{\"referrer\":{\"type\":\"bonus-days\",\"value\":7,\"unit\":\"days\",\"autoApprove\":true}}",
            "{}");

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
