using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Common;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminSessionControllerTests
{
    public static TheoryData<string, bool[]> RoleCapabilities => new()
    {
        { UserRoles.SuperAdmin, [true, true, true, true, true, true, true, true, true, true] },
        { UserRoles.Admin, [true, true, true, true, true, true, true, true, true, true] },
        { UserRoles.Operator, [true, true, false, false, true, true, true, true, true, false] },
        { UserRoles.FinanceManager, [true, false, true, true, false, false, false, false, false, false] },
        { UserRoles.SupportAgent, [true, false, false, false, true, true, false, false, false, false] },
        { UserRoles.ReadOnly, [true, false, true, false, true, false, false, false, false, false] }
    };

    [Theory]
    [MemberData(nameof(RoleCapabilities))]
    public void Get_Returns_Capabilities_For_Current_Role(string role, bool[] expected)
    {
        var controller = CreateController(role);

        var result = controller.Get();

        var response = Assert.IsType<AdminSessionResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("00000000-0000-4000-8000-000000000001", response.UserId);
        Assert.Equal("admin@example.test", response.Email);
        Assert.Equal("Admin Test", response.DisplayName);
        Assert.Equal([role], response.Roles);
        Assert.Equal(expected, new[]
        {
            response.Capabilities.AdminRead,
            response.Capabilities.AdminWrite,
            response.Capabilities.FinanceRead,
            response.Capabilities.FinanceWrite,
            response.Capabilities.SupportRead,
            response.Capabilities.SupportWrite,
            response.Capabilities.ProvisioningManage,
            response.Capabilities.VpnManage,
            response.Capabilities.BotManage,
            response.Capabilities.SettingsManage
        });
    }

    [Fact]
    public void Controller_Requires_AdminRead_Policy()
    {
        var authorize = typeof(AdminSessionController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal(AdminPolicies.AdminRead, authorize.Policy);
    }

    private static AdminSessionController CreateController(string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "00000000-0000-4000-8000-000000000001"),
            new Claim(ClaimTypes.Email, "admin@example.test"),
            new Claim(ClaimTypes.Name, "Admin Test"),
            new Claim(ClaimTypes.Role, role)
        }, "test");

        return new AdminSessionController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }
}
