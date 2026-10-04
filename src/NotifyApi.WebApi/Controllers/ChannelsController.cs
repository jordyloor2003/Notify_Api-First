using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NotifyApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/channels")]
[Produces("application/json")]
[Authorize]
public class ChannelsController : ControllerBase
{
    [HttpGet]
    public IActionResult ListChannels()
    {
        var channels = new[]
        {
            new { code = "EMAIL", name = "Correo Electrónico", isEnabled = true, maxPayloadSizeBytes = 524288, supportsHtml = true },
            new { code = "SMS", name = "Mensajería Móvil SMS", isEnabled = true, maxPayloadSizeBytes = 160, supportsHtml = false },
            new { code = "PUSH", name = "Notificaciones Push Móviles", isEnabled = true, maxPayloadSizeBytes = 4096, supportsHtml = false }
        };

        return Ok(channels);
    }
}
