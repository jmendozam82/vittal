using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.ConfiguracionAlerta;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para Configuración de Alertas de Tiempo de Espera.
/// Gestiona los umbrales de tiempo y preferencias de notificación para alertas de pacientes en espera.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ConfiguracionAlertasController : ControllerBase
{
    private readonly IConfiguracionAlertaService _service;
    private readonly ILogger<ConfiguracionAlertasController> _logger;

    public ConfiguracionAlertasController(IConfiguracionAlertaService service, ILogger<ConfiguracionAlertasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene la configuración de alertas de la clínica actual.</summary>
    [HttpGet]
    [RequirePermission("clinicas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ConfiguracionAlertaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAsync(clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Guarda (crea o actualiza) la configuración de alertas de la clínica.</summary>
    [HttpPut]
    [RequirePermission("clinicas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<ConfiguracionAlertaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Save([FromBody] ConfiguracionAlertaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var result = await _service.SaveAsync(dto, clinicaId, usuarioId);
        return result.ToActionResult();
    }
}
