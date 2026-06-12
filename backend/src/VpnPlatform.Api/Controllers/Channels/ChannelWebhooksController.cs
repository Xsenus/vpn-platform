using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VpnPlatform.Api.Security;

namespace VpnPlatform.Api.Controllers.Channels;

[ApiController]
[Route("api/channels")]
[EnableRateLimiting(ApiRateLimitPolicies.Webhook)]
public class ChannelWebhooksController : ControllerBase
{
    [HttpPost("telegram/webhook")]
    public IActionResult Telegram([FromBody] object payload) => StatusCode(StatusCodes.Status501NotImplemented, new { error = "telegram_bot_not_configured", phase = "Phase 3" });

    [HttpPost("discord/webhook")]
    public IActionResult Discord([FromBody] object payload) => StatusCode(StatusCodes.Status501NotImplemented, new { error = "discord_channel_not_configured" });

    [HttpPost("vk/webhook")]
    public IActionResult Vk([FromBody] object payload) => StatusCode(StatusCodes.Status501NotImplemented, new { error = "vk_channel_not_configured" });

    [HttpPost("whatsapp/webhook")]
    public IActionResult WhatsApp([FromBody] object payload) => StatusCode(StatusCodes.Status501NotImplemented, new { error = "whatsapp_channel_not_configured" });
}
