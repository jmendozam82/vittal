using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Expediente;
using Vittal.DTO.Paciente;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Expedientes Médicos.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ExpedientesController : ControllerBase
{
    private readonly IExpedienteService _service;
    private readonly IPacienteService _pacienteService;
    private readonly ILogger<ExpedientesController> _logger;

    public ExpedientesController(
        IExpedienteService service,
        IPacienteService pacienteService,
        ILogger<ExpedientesController> logger)
    {
        _service = service;
        _pacienteService = pacienteService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los expedientes activos de la clínica.
    /// Regla 6: si el usuario es doctor, solo ve los expedientes asignados a él.
    /// Admin/SuperAdmin y demás perfiles ven todos los de la clínica.
    /// </summary>
    [HttpGet]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();
        Guid? doctorId = User.EsDoctor() ? User.GetInternalUserId() : null;
        var result = await _service.GetAllAsync(clinicaId, doctorId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Obtiene un expediente por su ID.
    /// Regla 6: si el usuario es doctor, solo puede ver expedientes asignados a él.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        Guid? doctorId = User.EsDoctor() ? User.GetInternalUserId() : null;
        var result = await _service.GetByIdAsync(clinicaId, id, doctorId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Obtiene los datos del paciente asociado a un expediente.
    /// Se rige por el permiso del módulo 'expedientes' (el Doctor tiene acceso),
    /// de modo que la impresión de Receta/Epicrisis/Constancia obtenga los datos
    /// del paciente sin requerir el módulo externo 'pacientes'.
    /// </summary>
    [HttpGet("{id:guid}/paciente-info")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PacienteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientInfo([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        Guid? doctorId = User.EsDoctor() ? User.GetInternalUserId() : null;

        var expediente = await _service.GetByIdAsync(clinicaId, id, doctorId);
        if (!expediente.IsSuccess || expediente.Data == null)
        {
            return expediente.ToActionResult();
        }

        var paciente = await _pacienteService.GetByIdAsync(expediente.Data.PacienteId, clinicaId);
        return paciente.ToActionResult();
    }

    /// <summary>Obtiene el expediente activo de un paciente.</summary>
    [HttpGet("paciente/{pacienteId:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPaciente([FromRoute] Guid pacienteId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByPacienteIdAsync(clinicaId, pacienteId);
        return result.ToActionResult();
    }

    /// <summary>Crea un nuevo expediente médico.</summary>
    [HttpPost]
    [RequirePermission("expedientes", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ExpedienteRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();
        Guid? doctorContexto = User.EsDoctor() ? User.GetInternalUserId() : null;
        var result = await _service.CreateAsync(dto, clinicaId, creadoPor, doctorContexto);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<ExpedienteResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza un expediente existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ExpedienteRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        Guid? doctorContexto = User.EsDoctor() ? User.GetInternalUserId() : null;
        var result = await _service.UpdateAsync(id, dto, clinicaId, doctorContexto);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un expediente (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(clinicaId, id);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
