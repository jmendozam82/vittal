using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.AntecedentesPaciente;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Antecedentes del Paciente por Sala.
/// Historia de Usuario: HU-E05 — Antecedentes del Paciente
/// Los antecedentes se organizan por sala (especialidad) y se usa upsert
/// para mantener un único valor por tipo de antecedente por paciente.
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AntecedentesPacienteController : ControllerBase
{
    private readonly IAntecedentePacienteService _service;
    private readonly ILogger<AntecedentesPacienteController> _logger;

    public AntecedentesPacienteController(IAntecedentePacienteService service, ILogger<AntecedentesPacienteController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los antecedentes activos de un paciente en una sala específica.
    /// </summary>
    [HttpGet("expediente/{expedienteId:guid}/sala/{salaId:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AntecedentePacienteDTOs.Response>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(Guid expedienteId, Guid salaId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, expedienteId, salaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un antecedente por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<AntecedentePacienteDTOs.Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>
    /// Crea o actualiza un antecedente del paciente (Upsert).
    /// Si ya existe un antecedente del mismo tipo para este paciente/sala,
    /// se actualiza el valor. Si no existe, se crea uno nuevo.
    /// </summary>
    [HttpPost]
    [RequirePermission("expedientes", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<AntecedentePacienteDTOs.Response>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromBody] AntecedentePacienteDTOs.Request request)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var result = await _service.UpsertAsync(request, clinicaId, request.ExpedienteId, usuarioId);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<AntecedentePacienteDTOs.Response>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un antecedente del paciente (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(clinicaId, id);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
