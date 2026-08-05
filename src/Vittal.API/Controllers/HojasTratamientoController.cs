using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Helpers;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.HojaTratamiento;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Tratamientos y Medicamentos en Hojas de Cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
[ApiController]
[Route("api/hojas-tratamiento")]
[Authorize]
[Produces("application/json")]
public class HojasTratamientoController : ControllerBase
{
    private readonly IHojaTratamientoService _service;
    private readonly IHojaCitaService _hojaCitaService;
    private readonly ILogger<HojasTratamientoController> _logger;

    public HojasTratamientoController(
        IHojaTratamientoService service,
        IHojaCitaService hojaCitaService,
        ILogger<HojasTratamientoController> logger)
    {
        _service = service;
        _hojaCitaService = hojaCitaService;
        _logger = logger;
    }

    /// <summary>Obtiene todos los tratamientos activos de una hoja de cita.</summary>
    [HttpGet("hoja-cita/{hojaCitaId:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<HojaTratamientoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByHojaCita([FromRoute] Guid hojaCitaId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByHojaCitaIdAsync(clinicaId, hojaCitaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un tratamiento por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<HojaTratamientoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>Crea un nuevo tratamiento en la hoja de cita.</summary>
    [HttpPost]
    [BloquearHojaFinalizada]
    [RequirePermission("expedientes", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<HojaTratamientoResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] HojaTratamientoRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();
        var result = await _service.CreateAsync(dto, clinicaId, creadoPor);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<HojaTratamientoResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza un tratamiento existente.</summary>
    [HttpPut("{id:guid}")]
    [BloquearHojaFinalizada]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<HojaTratamientoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] HojaTratamientoRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un tratamiento (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();

        // Guard de integridad: obtener la hoja de cita del tratamiento para bloquear consultas finalizadas.
        var item = await _service.GetByIdAsync(clinicaId, id);
        if (item.IsSuccess && item.Data != null)
        {
            var bloqueo = await ConsultaFinalizadaGuard.ValidateAsync(clinicaId, item.Data.HojaCitaId, _hojaCitaService);
            if (bloqueo != null)
            {
                return bloqueo;
            }
        }

        var result = await _service.DeactivateAsync(clinicaId, id);
        return result.ToActionResult();
    }
}
