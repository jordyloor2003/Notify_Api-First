using Microsoft.AspNetCore.Mvc;
using NotifyApi.Application.DTOs;
using NotifyApi.WebApi.Services;

namespace NotifyApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>
    /// Emitir token JWT Bearer mediante credenciales de aplicación (Client Credentials Flow).
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateToken([FromBody] TokenRequest request, CancellationToken ct)
    {
        var tokenResponse = await _tokenService.AuthenticateAsync(request, ct);
        if (tokenResponse == null)
        {
            return Unauthorized(new ProblemDetailsDto
            {
                Type = "https://notify.consultoria.com/errors/invalid-credentials",
                Title = "Credenciales Inválidas",
                Status = 401,
                Detail = "El clientId o clientSecret ingresado no es válido o la aplicación se encuentra inactiva.",
                Instance = HttpContext.Request.Path,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(tokenResponse);
    }
}
