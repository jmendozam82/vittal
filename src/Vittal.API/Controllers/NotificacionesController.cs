using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Notificacion;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para Notificaciones del Sistema.
/// Gestiona las notificaciones push internas para usuarios de la clínica.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionService _service;
    private readonly ILogger<NotificacionesController> _logger;

    public NotificacionesController(INotificacionService service, ILogger<NotificacionesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene las notificaciones de la clínica, opcionalmente filtradas.</summary>
    /// <param name="leida">Filtrar por estado de lectura (null = todas).</param>
    /// <param name="limit">Cantidad máxima de resultados.</param>
    [HttpGet]
    [RequirePermission("dashboard", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<List<NotificacionResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool? leida = null, [FromQuery] int? limit = null)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();
        var result = await _service.GetAllAsync(clinicaId, usuarioId, leida, limit);
        return result.ToActionResult();
    }

    /// <summary>Obtiene la cantidad de notificaciones no leídas.</summary>
    [HttpGet("no-leidas-count")]
    [RequirePermission("dashboard", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNoLeidasCount()
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();
        var result = await _service.GetNoLeidasCountAsync(clinicaId, usuarioId);
        return result.ToActionResult();
    }

    /// <summary>Marca una notificación específica como leída.</summary>
    /// <param name="id">ID de la notificación.</param>
    [HttpPut("{id:guid}/leer")]
    [RequirePermission("dashboard", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarLeida(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();
        var result = await _service.MarcarLeidaAsync(clinicaId, usuarioId, id);
        return result.ToActionResult();
    }

    /// <summary>Marca todas las notificaciones de la clínica como leídas.</summary>
    [HttpPut("leer-todas")]
    [RequirePermission("dashboard", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarcarTodasLeidas()
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();
        var result = await _service.MarcarTodasLeidasAsync(clinicaId, usuarioId);
        return result.ToActionResult();
    }
}
