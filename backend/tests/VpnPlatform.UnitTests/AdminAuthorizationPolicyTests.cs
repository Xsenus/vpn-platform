using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Common;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminAuthorizationPolicyTests
{
    [Fact]
    public void ReadOnly_Should_Not_Be_In_Write_Or_Manage_Policies()
    {
        Assert.DoesNotContain(UserRoles.ReadOnly, AdminPolicies.AdminWriteRoles);
        Assert.DoesNotContain(UserRoles.ReadOnly, AdminPolicies.FinanceWriteRoles);
        Assert.DoesNotContain(UserRoles.ReadOnly, AdminPolicies.SupportWriteRoles);
        Assert.DoesNotContain(UserRoles.ReadOnly, AdminPolicies.ProvisioningManageRoles);
        Assert.DoesNotContain(UserRoles.ReadOnly, AdminPolicies.VpnManageRoles);
        Assert.DoesNotContain(UserRoles.ReadOnly, AdminPolicies.BotManageRoles);
        Assert.DoesNotContain(UserRoles.ReadOnly, AdminPolicies.SettingsManageRoles);
    }

    [Fact]
    public void SupportAgent_Should_Not_Manage_Finance_Vpn_Provisioning_Bots_Or_Settings()
    {
        Assert.DoesNotContain(UserRoles.SupportAgent, AdminPolicies.FinanceWriteRoles);
        Assert.DoesNotContain(UserRoles.SupportAgent, AdminPolicies.ProvisioningManageRoles);
        Assert.DoesNotContain(UserRoles.SupportAgent, AdminPolicies.VpnManageRoles);
        Assert.DoesNotContain(UserRoles.SupportAgent, AdminPolicies.BotManageRoles);
        Assert.DoesNotContain(UserRoles.SupportAgent, AdminPolicies.SettingsManageRoles);
        Assert.Contains(UserRoles.SupportAgent, AdminPolicies.SupportWriteRoles);
    }

    [Fact]
    public void FinanceManager_Should_Not_Manage_Vpn_Provisioning_Bots_Or_Admin_Write_Actions()
    {
        Assert.Contains(UserRoles.FinanceManager, AdminPolicies.FinanceWriteRoles);
        Assert.DoesNotContain(UserRoles.FinanceManager, AdminPolicies.AdminWriteRoles);
        Assert.DoesNotContain(UserRoles.FinanceManager, AdminPolicies.ProvisioningManageRoles);
        Assert.DoesNotContain(UserRoles.FinanceManager, AdminPolicies.VpnManageRoles);
        Assert.DoesNotContain(UserRoles.FinanceManager, AdminPolicies.BotManageRoles);
        Assert.DoesNotContain(UserRoles.FinanceManager, AdminPolicies.SettingsManageRoles);
    }

    [Theory]
    [InlineData(UserRoles.SuperAdmin)]
    [InlineData(UserRoles.Admin)]
    public void Admins_Should_Have_All_Critical_Write_And_Manage_Policies(string role)
    {
        Assert.Contains(role, AdminPolicies.AdminWriteRoles);
        Assert.Contains(role, AdminPolicies.FinanceWriteRoles);
        Assert.Contains(role, AdminPolicies.SupportWriteRoles);
        Assert.Contains(role, AdminPolicies.ProvisioningManageRoles);
        Assert.Contains(role, AdminPolicies.VpnManageRoles);
        Assert.Contains(role, AdminPolicies.BotManageRoles);
        Assert.Contains(role, AdminPolicies.SettingsManageRoles);
    }

    [Fact]
    public void Operator_Should_Manage_Provisioning_And_Vpn_But_Not_Finance_Or_Settings()
    {
        Assert.Contains(UserRoles.Operator, AdminPolicies.ProvisioningManageRoles);
        Assert.Contains(UserRoles.Operator, AdminPolicies.VpnManageRoles);
        Assert.DoesNotContain(UserRoles.Operator, AdminPolicies.FinanceWriteRoles);
        Assert.DoesNotContain(UserRoles.Operator, AdminPolicies.SettingsManageRoles);
    }

    [Fact]
    public void Policy_Role_Map_Should_Contain_All_Admin_Policies_Without_User_Role()
    {
        var expectedPolicies = new[]
        {
            AdminPolicies.AdminOnly,
            AdminPolicies.AdminRead,
            AdminPolicies.AdminWrite,
            AdminPolicies.FinanceRead,
            AdminPolicies.FinanceWrite,
            AdminPolicies.SupportRead,
            AdminPolicies.SupportWrite,
            AdminPolicies.ProvisioningManage,
            AdminPolicies.VpnManage,
            AdminPolicies.BotManage,
            AdminPolicies.SettingsManage
        };

        Assert.Equal(expectedPolicies.OrderBy(x => x), AdminPolicies.PolicyRoles.Keys.OrderBy(x => x));
        foreach (var roles in AdminPolicies.PolicyRoles.Values)
        {
            Assert.NotEmpty(roles);
            Assert.DoesNotContain(UserRoles.User, roles);
            Assert.All(roles, role => Assert.Contains(role, UserRoles.Parse(role)));
        }
    }

    [Theory]
    [InlineData(AdminPolicies.AdminRead, UserRoles.ReadOnly, true)]
    [InlineData(AdminPolicies.AdminWrite, UserRoles.ReadOnly, false)]
    [InlineData(AdminPolicies.SupportWrite, UserRoles.SupportAgent, true)]
    [InlineData(AdminPolicies.FinanceWrite, UserRoles.SupportAgent, false)]
    [InlineData(AdminPolicies.FinanceWrite, UserRoles.FinanceManager, true)]
    [InlineData(AdminPolicies.ProvisioningManage, UserRoles.FinanceManager, false)]
    [InlineData(AdminPolicies.ProvisioningManage, UserRoles.Operator, true)]
    [InlineData(AdminPolicies.BotManage, UserRoles.Operator, true)]
    [InlineData(AdminPolicies.SettingsManage, UserRoles.Operator, false)]
    [InlineData(AdminPolicies.AdminRead, UserRoles.User, false)]
    public async Task Runtime_Authorization_Should_Allow_Only_Configured_Roles(string policyName, string role, bool expected)
    {
        var authorizationService = BuildAuthorizationService();
        var user = PrincipalWithRole(role);

        var result = await authorizationService.AuthorizeAsync(user, resource: null, policyName);

        Assert.Equal(expected, result.Succeeded);
    }

    [Theory]
    [InlineData(nameof(AdminOperationsController.DisableAccessCredential))]
    [InlineData(nameof(AdminOperationsController.EnableAccessCredential))]
    [InlineData(nameof(AdminOperationsController.SyncAccessCredential))]
    [InlineData(nameof(AdminOperationsController.ResetAccessTraffic))]
    [InlineData(nameof(AdminOperationsController.SyncSubscriptionAccess))]
    public void Vpn_Access_Action_Endpoints_Should_Require_VpnManage(string methodName)
    {
        var method = typeof(AdminOperationsController).GetMethod(methodName);
        Assert.NotNull(method);
        var policies = method!.GetCustomAttributes<AuthorizeAttribute>().Select(x => x.Policy).ToList();
        Assert.Contains(AdminPolicies.VpnManage, policies);
    }


    [Theory]
    [InlineData(nameof(AdminOperationsController.Precheck))]
    [InlineData(nameof(AdminOperationsController.Provision))]
    [InlineData(nameof(AdminOperationsController.RetryProvisioningRun))]
    [InlineData(nameof(AdminOperationsController.DeployProvisioningRun))]
    [InlineData(nameof(AdminOperationsController.CancelProvisioningRun))]
    [InlineData(nameof(AdminOperationsController.MarkProvisioningSupportNeeded))]
    public void Provisioning_Action_Endpoints_Should_Require_ProvisioningManage(string methodName)
    {
        var method = typeof(AdminOperationsController).GetMethod(methodName);
        Assert.NotNull(method);
        var policies = method!.GetCustomAttributes<AuthorizeAttribute>().Select(x => x.Policy).ToList();
        Assert.Contains(AdminPolicies.ProvisioningManage, policies);
    }

    [Theory]
    [InlineData(nameof(AdminOperationsController.RecheckPayment))]
    [InlineData(nameof(AdminOperationsController.RecheckOrderPayment))]
    [InlineData(nameof(AdminOperationsController.RefundPayment))]
    public void Finance_Write_Endpoints_Should_Require_FinanceWrite(string methodName)
    {
        var method = typeof(AdminOperationsController).GetMethod(methodName);
        Assert.NotNull(method);
        var policies = method!.GetCustomAttributes<AuthorizeAttribute>().Select(x => x.Policy).ToList();
        Assert.Contains(AdminPolicies.FinanceWrite, policies);
    }

    [Fact]
    public void Telegram_Bot_Settings_Write_Endpoint_Should_Require_BotManage()
    {
        var method = typeof(AdminTelegramBotSettingsController).GetMethod(nameof(AdminTelegramBotSettingsController.UpdateSettings));
        Assert.NotNull(method);
        var policies = method!.GetCustomAttributes<AuthorizeAttribute>().Select(x => x.Policy).ToList();
        Assert.Contains(AdminPolicies.BotManage, policies);
    }

    [Fact]
    public void Admin_Write_Endpoints_Should_Use_Specific_Method_Level_Policies_Not_Legacy_AdminOnly()
    {
        var adminControllerTypes = new[]
        {
            typeof(AdminOperationsController),
            typeof(AdminUsersController),
            typeof(AdminVpnPanelsController),
            typeof(AdminTelegramBotSettingsController)
        };

        var writeMethods = adminControllerTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsWriteEndpoint)
                .Select(method => new { Controller = type.Name, Method = method }))
            .ToList();

        Assert.NotEmpty(writeMethods);
        foreach (var item in writeMethods)
        {
            var policies = item.Method.GetCustomAttributes<AuthorizeAttribute>().Select(x => x.Policy).ToList();
            Assert.NotEmpty(policies);
            Assert.DoesNotContain(AdminPolicies.AdminOnly, policies);
            Assert.DoesNotContain(AdminPolicies.AdminRead, policies);
        }
    }

    private static bool IsWriteEndpoint(MethodInfo method)
        => method.GetCustomAttributes().Any(attribute =>
            attribute is HttpPostAttribute
            || attribute is HttpPutAttribute
            || attribute is HttpPatchAttribute
            || attribute is HttpDeleteAttribute);

    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore(options =>
        {
            foreach (var policy in AdminPolicies.PolicyRoles)
            {
                options.AddPolicy(policy.Key, builder => builder.RequireRole(policy.Value));
            }
        });

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal PrincipalWithRole(string role)
        => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
}
