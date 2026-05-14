using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Alerta;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para Alertas de Tiempo de Espera de Pacientes.
/// Gestiona la detección y resolución de alertas cuando un paciente excede el tiempo de espera.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AlertasController : ControllerBase
{
    private readonly IAlertaEsperaService _service;
    private readonly ILogger<AlertasController> _logger;

    public AlertasController(IAlertaEsperaService service, ILogger<AlertasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todas las alertas de espera de la clínica, opcionalmente filtradas.</summary>
    /// <param name="resuelta">Filtrar por estado resuelta (null = todas).</param>
    [HttpGet]
    [RequirePermission("dashboard", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<List<AlertaEsperaResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool? resuelta = null)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, resuelta);
        return result.ToActionResult();
    }

    /// <summary>Obtiene solo las alertas de espera no resueltas.</summary>
    [HttpGet("no-resueltas")]
    [RequirePermission("dashboard", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<List<AlertaEsperaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNoResueltas()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetNoResueltasAsync(clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Resuelve una alerta de tiempo de espera manualmente.</summary>
    /// <param name="alertaId">ID de la alerta a resolver.</param>
    [HttpPost("{alertaId:guid}/resolver")]
    [RequirePermission("dashboard", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolver(Guid alertaId)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var dto = new AlertaEsperaResolveDto { AlertaId = alertaId };
        var result = await _service.ResolverAlertaAsync(clinicaId, dto, usuarioId);
        return result.ToActionResult();
    }

    /// <summary>Ejecuta la verificación manual de tiempos de espera y genera alertas si es necesario.</summary>
    [HttpPost("verificar")]
    [RequirePermission("dashboard", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Verificar()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.VerificarTiemposEsperaAsync(clinicaId);
        return result.ToActionResult();
    }
}
