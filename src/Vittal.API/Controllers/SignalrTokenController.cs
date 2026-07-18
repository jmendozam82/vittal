using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Services;

namespace Vittal.API.Controllers;

/// <summary>
/// Endpoint para emitir JWT de corta vida (60s) exclusivo para conexiones SignalR.
/// Seguridad: El token Supabase (ES256, vida larga) se usa para REST API.
/// El token HMAC-SHA256 (60s) se usa solo para WebSocket/SignalR,
/// minimizando la ventana de exposición del token en la URL del query string.
/// Historia de Usuario: HU23 — Alertas Configurables (mejora de seguridad)
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
[Produces("application/json")]
public class SignalrTokenController : ControllerBase
{
    private readonly SignalrTokenService _signalrTokenService;
    private readonly ILogger<SignalrTokenController> _logger;

    public SignalrTokenController(
        SignalrTokenService signalrTokenService,
        ILogger<SignalrTokenController> logger)
    {
        _signalrTokenService = signalrTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Genera un JWT de corta vida (60 segundos) para conexión SignalR WebSocket.
    /// </summary>
    /// <remarks>
    /// El token retornado debe usarse como access_token en la conexión SignalR.
    /// El token original (Supabase) se usa para autenticar esta petición.
    /// El token retornado tiene una vida de solo 60 segundos.
    /// </remarks>
    /// <returns>JWT de corta vida para SignalR.</returns>
    /// <response code="200">Token generado exitosamente.</response>
    /// <response code="401">Token Supabase inválido o expirado.</response>
    [HttpPost("signalr-token")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetSignalrToken()
    {
        // Obtener el token Supabase original del header Authorization
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new { success = false, message = "Token de autenticación requerido." });
        }

        var originalToken = authHeader["Bearer ".Length..].Trim();

        try
        {
            var shortLivedToken = _signalrTokenService.GenerateSignalrToken(originalToken);

            _logger.LogDebug(
                "Token SignalR de corta vida emitido para usuario {UserId}",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown");

            return Ok(new
            {
                success = true,
                token = shortLivedToken,
                expiresIn = 60
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando token SignalR de corta vida");
            return Unauthorized(new { success = false, message = "Error generando token." });
        }
    }
}
