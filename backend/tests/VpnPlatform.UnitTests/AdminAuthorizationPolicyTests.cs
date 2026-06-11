using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
