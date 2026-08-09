using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Helpers;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.HojaCita;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Hojas de Cita Médica.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class HojasCitaController : ControllerBase
{
    private readonly IHojaCitaService _service;
    private readonly ILogger<HojasCitaController> _logger;

    public HojasCitaController(IHojaCitaService service, ILogger<HojasCitaController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todas las hojas de cita activas de la clínica.</summary>
    [HttpGet]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<HojaCitaResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();
        Guid? doctorId = User.EsDoctor() ? User.GetInternalUserId() : null;
        var result = await _service.GetAllAsync(clinicaId, doctorId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene todas las hojas de cita activas de un expediente.</summary>
    [HttpGet("expediente/{expedienteId:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<HojaCitaResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByExpediente([FromRoute] Guid expedienteId)
    {
        var clinicaId = User.GetClinicaId();
        Guid? doctorId = User.EsDoctor() ? User.GetInternalUserId() : null;
        var result = await _service.GetByExpedienteIdAsync(clinicaId, expedienteId, doctorId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene una hoja de cita por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<HojaCitaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        Guid? doctorId = User.EsDoctor() ? User.GetInternalUserId() : null;
        var result = await _service.GetByIdAsync(clinicaId, id, doctorId);
        return result.ToActionResult();
    }

    /// <summary>Crea una nueva hoja de cita.</summary>
    [HttpPost]
    [RequirePermission("expedientes", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<HojaCitaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] HojaCitaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();
        var result = await _service.CreateAsync(dto, clinicaId, creadoPor);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<HojaCitaResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza una hoja de cita existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<HojaCitaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] HojaCitaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();

        // Guard de integridad: no se puede editar una hoja de cita de una consulta finalizada.
        var bloqueo = await ConsultaFinalizadaGuard.ValidateAsync(clinicaId, id, _service);
        if (bloqueo != null)
        {
            return bloqueo;
        }

        var result = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva una hoja de cita (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();

        // Guard de integridad: no se puede desactivar una hoja de cita de una consulta finalizada.
        var bloqueo = await ConsultaFinalizadaGuard.ValidateAsync(clinicaId, id, _service);
        if (bloqueo != null)
        {
            return bloqueo;
        }

        var result = await _service.DeactivateAsync(clinicaId, id);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
