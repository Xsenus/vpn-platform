using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin")]
public class AdminVpnPanelsController : ControllerBase
{
    private readonly X3UiPanelService _panels;

    public AdminVpnPanelsController(X3UiPanelService panels)
    {
        _panels = panels;
    }

    [HttpGet("vpn-panels")]
    public async Task<IActionResult> GetPanels(CancellationToken cancellationToken)
        => Ok(await _panels.GetPanelsAsync(cancellationToken));

    [HttpPost("vpn-panels")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> CreatePanel([FromBody] CreateVpnPanelCommand request, CancellationToken cancellationToken)
    {
        var result = await _panels.CreatePanelAsync(request, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("vpn-panels/{id:guid}")]
    public async Task<IActionResult> GetPanel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _panels.GetPanelAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPatch("vpn-panels/{id:guid}")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> UpdatePanel(Guid id, [FromBody] UpdateVpnPanelCommand request, CancellationToken cancellationToken)
    {
        if (!request.Revision.HasValue || request.Revision.Value < 0)
        {
            return BadRequest(new { error = "VPN panel revision is required and must be a non-negative integer." });
        }
        var result = await _panels.UpdatePanelAsync(id, request, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpDelete("vpn-panels/{id:guid}")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> DeletePanel(Guid id, CancellationToken cancellationToken, [FromQuery] int? revision = null)
    {
        if (!revision.HasValue || revision.Value < 0)
        {
            return BadRequest(new { error = "VPN panel revision is required and must be a non-negative integer." });
        }
        var result = await _panels.DeletePanelAsync(id, revision, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpPost("vpn-panels/{id:guid}/test-connection")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken cancellationToken)
    {
        var result = await _panels.CheckHealthAsync(id, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("vpn-panels/{id:guid}/health-check")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> HealthCheck(Guid id, CancellationToken cancellationToken)
    {
        var result = await _panels.CheckHealthAsync(id, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("vpn-panels/{id:guid}/sync")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> Sync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _panels.SyncPanelAsync(id, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("vpn-panels/{id:guid}/inbounds")]
    public async Task<IActionResult> GetInbounds(Guid id, CancellationToken cancellationToken)
        => Ok(await _panels.GetInboundsAsync(id, cancellationToken));

    [HttpPost("vpn-panels/{id:guid}/adopt-ready-node")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> AdoptReadyNode(
        Guid id,
        [FromBody] AdoptVpnPanelNodeCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _panels.AdoptReadyNodeAsync(id, request, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("vpn-inbounds")]
    public async Task<IActionResult> GetAllInbounds(CancellationToken cancellationToken)
        => Ok(await _panels.GetInboundsAsync(cancellationToken));

    [HttpPost("vpn-panels/{id:guid}/inbounds")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> CreateInbound(Guid id, [FromBody] CreateVpnInboundCommand request, CancellationToken cancellationToken)
    {
        var result = await _panels.CreateInboundAsync(id, request, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("vpn-inbounds/{id:guid}")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> PatchInbound(Guid id, [FromBody] CreateVpnInboundCommand request, CancellationToken cancellationToken)
    {
        if (!request.Revision.HasValue || request.Revision.Value < 0)
        {
            return BadRequest(new { error = "VPN inbound revision is required and must be a non-negative integer." });
        }
        var result = await _panels.PatchInboundAsync(id, request, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpPost("vpn-inbounds/{id:guid}/set-default")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> SetDefaultInbound(Guid id, CancellationToken cancellationToken, [FromQuery] int? revision = null)
    {
        if (!revision.HasValue || revision.Value < 0)
        {
            return BadRequest(new { error = "VPN inbound revision is required and must be a non-negative integer." });
        }
        var result = await _panels.SetDefaultInboundAsync(id, revision, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpGet("vpn-panels/{id:guid}/clients")]
    public async Task<IActionResult> GetClients(Guid id, CancellationToken cancellationToken)
        => Ok(await _panels.GetClientsAsync(id, cancellationToken));

    [HttpPost("vpn-clients/{id:guid}/enable")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> EnableClient(Guid id, CancellationToken cancellationToken, [FromQuery] int? revision = null)
    {
        if (!revision.HasValue || revision.Value < 0) return BadRequest(new { error = "VPN client revision is required and must be a non-negative integer." });
        var result = await _panels.EnableClientAsync(id, revision, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpPost("vpn-clients/{id:guid}/disable")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> DisableClient(Guid id, CancellationToken cancellationToken, [FromQuery] int? revision = null)
    {
        if (!revision.HasValue || revision.Value < 0) return BadRequest(new { error = "VPN client revision is required and must be a non-negative integer." });
        var result = await _panels.DisableClientAsync(id, revision, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpPost("vpn-clients/{id:guid}/sync")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> SyncClient(Guid id, CancellationToken cancellationToken, [FromQuery] int? revision = null)
    {
        if (!revision.HasValue || revision.Value < 0) return BadRequest(new { error = "VPN client revision is required and must be a non-negative integer." });
        var result = await _panels.SyncClientAsync(id, revision, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpPost("vpn-clients/{id:guid}/reset-traffic")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> ResetClientTraffic(Guid id, CancellationToken cancellationToken, [FromQuery] int? revision = null)
    {
        if (!revision.HasValue || revision.Value < 0) return BadRequest(new { error = "VPN client revision is required and must be a non-negative integer." });
        var result = await _panels.ResetClientTrafficAsync(id, revision, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpPost("vpn-clients/{id:guid}/migrate")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> MigrateClient(Guid id, [FromBody] MigrateVpnClientCommand request, CancellationToken cancellationToken)
    {
        if (!request.Revision.HasValue || request.Revision.Value < 0) return BadRequest(new { error = "VPN client revision is required and must be a non-negative integer." });
        var result = await _panels.MigrateClientAsync(id, request, ResolveUserId(), cancellationToken);
        return MutationResult(result);
    }

    [HttpGet("vpn-panels/{id:guid}/sync-runs")]
    public async Task<IActionResult> GetSyncRuns(Guid id, CancellationToken cancellationToken)
        => Ok(await _panels.GetSyncRunsAsync(id, cancellationToken));

    [HttpGet("vpn-panel-sync-runs/{id:guid}/events")]
    public async Task<IActionResult> GetSyncEvents(Guid id, CancellationToken cancellationToken)
        => Ok(await _panels.GetSyncEventsAsync(id, cancellationToken));

    [HttpGet("vpn-panels/{id:guid}/health-checks")]
    public async Task<IActionResult> GetHealthChecks(Guid id, CancellationToken cancellationToken)
        => Ok(await _panels.GetHealthChecksAsync(id, cancellationToken));

    private Guid? ResolveUserId()
    {
        var principal = HttpContext?.User;
        var raw = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal?.FindFirstValue("sub");
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private IActionResult MutationResult<T>(Result<T> result)
        => result.IsSuccess
            ? Ok(result.Value)
            : result.Error?.Contains("changed. Reload it and retry.", StringComparison.Ordinal) == true
                ? Conflict(new { error = result.Error })
                : BadRequest(new { error = result.Error });
}
