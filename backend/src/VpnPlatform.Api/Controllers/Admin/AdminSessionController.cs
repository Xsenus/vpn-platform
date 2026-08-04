using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VpnPlatform.Application.Common;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin/session")]
public sealed class AdminSessionController : ControllerBase
{
    [HttpGet]
    public ActionResult<AdminSessionResponse> Get()
    {
        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new AdminSessionResponse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? string.Empty,
            roles,
            new AdminSessionCapabilitiesResponse(
                AdminPolicies.HasAccess(roles, AdminPolicies.AdminRead),
                AdminPolicies.HasAccess(roles, AdminPolicies.AdminWrite),
                AdminPolicies.HasAccess(roles, AdminPolicies.FinanceRead),
                AdminPolicies.HasAccess(roles, AdminPolicies.FinanceWrite),
                AdminPolicies.HasAccess(roles, AdminPolicies.SupportRead),
                AdminPolicies.HasAccess(roles, AdminPolicies.SupportWrite),
                AdminPolicies.HasAccess(roles, AdminPolicies.ProvisioningManage),
                AdminPolicies.HasAccess(roles, AdminPolicies.VpnManage),
                AdminPolicies.HasAccess(roles, AdminPolicies.BotManage),
                AdminPolicies.HasAccess(roles, AdminPolicies.SettingsManage))));
    }
}

public sealed record AdminSessionResponse(
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    AdminSessionCapabilitiesResponse Capabilities);

public sealed record AdminSessionCapabilitiesResponse(
    bool AdminRead,
    bool AdminWrite,
    bool FinanceRead,
    bool FinanceWrite,
    bool SupportRead,
    bool SupportWrite,
    bool ProvisioningManage,
    bool VpnManage,
    bool BotManage,
    bool SettingsManage);
