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

        var program = new ReferralProgram { Name = "Welcome", Status = "draft" };
        Assert.IsType<OkObjectResult>(await controller.CreateReferralProgram(program, CancellationToken.None));
        using var patch = JsonDocument.Parse("""{"name":"Welcome Plus","status":"active","ruleDefinition":{"rewardDays":7}}""");
        var patched = Assert.IsType<ReferralProgram>(Assert.IsType<OkObjectResult>(await controller.PatchReferralProgram(program.Id, patch.RootElement, CancellationToken.None)).Value);
        var referrals = Assert.IsType<List<RewardLedger>>(Assert.IsType<OkObjectResult>(await controller.GetReferrals(CancellationToken.None)).Value);

        Assert.Equal("Welcome Plus", patched.Name);
        Assert.Equal("active", patched.Status);
        Assert.Contains("rewardDays", patched.RuleDefinition, StringComparison.Ordinal);
        Assert.Single(referrals);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"name\":123,\"status\":\"active\"}")]
    [InlineData("{\"name\":\"Changed\",\"status\":false}")]
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
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
