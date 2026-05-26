using Microsoft.AspNetCore.Mvc;

namespace VpnPlatform.Api.Controllers.Public;

[ApiController]
[Route("api/public/content")]
public class ContentController : ControllerBase
{
    [HttpGet("faq")]
    public IActionResult GetFaq()
        => Ok(new[]
        {
            new { question = "Как подключиться?", answer = "После оплаты вы получите ссылку, QR и инструкцию." },
            new { question = "Можно ли продлить заранее?", answer = "Да, срок подписки увеличится корректно." },
            new { question = "Что делать, если доступ перестал работать?", answer = "Откройте поддержку или запросите перевыдачу доступа." }
        });
}
