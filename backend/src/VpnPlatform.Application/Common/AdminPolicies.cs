namespace VpnPlatform.Application.Common;

public static class AdminPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AdminRead = "AdminRead";
    public const string AdminWrite = "AdminWrite";
    public const string FinanceRead = "FinanceRead";
    public const string FinanceWrite = "FinanceWrite";
    public const string SupportRead = "SupportRead";
    public const string SupportWrite = "SupportWrite";
    public const string ProvisioningManage = "ProvisioningManage";
    public const string VpnManage = "VpnManage";
    public const string BotManage = "BotManage";
    public const string SettingsManage = "SettingsManage";

    public static readonly string[] AllAdminRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.Operator,
        UserRoles.FinanceManager,
        UserRoles.SupportAgent,
        UserRoles.ReadOnly
    };

    public static readonly string[] AdminWriteRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.Operator
    };

    public static readonly string[] FinanceReadRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.FinanceManager,
        UserRoles.ReadOnly
    };

    public static readonly string[] FinanceWriteRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.FinanceManager
    };

    public static readonly string[] SupportReadRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.Operator,
        UserRoles.SupportAgent,
        UserRoles.ReadOnly
    };

    public static readonly string[] SupportWriteRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.Operator,
        UserRoles.SupportAgent
    };

    public static readonly string[] ProvisioningManageRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.Operator
    };

    public static readonly string[] VpnManageRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.Operator
    };

    public static readonly string[] BotManageRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin,
        UserRoles.Operator
    };

    public static readonly string[] SettingsManageRoles = new[]
    {
        UserRoles.SuperAdmin,
        UserRoles.Admin
    };
}
