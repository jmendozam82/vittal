using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.Utility;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.DTO.Diagnostico;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Diagnósticos.
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DiagnosticosController : ControllerBase
{
    private readonly IDiagnosticoService _service;
    private readonly ILogger<DiagnosticosController> _logger;

    public DiagnosticosController(IDiagnosticoService service, ILogger<DiagnosticosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los diagnósticos de la clínica. Por defecto solo activos.</summary>
    [HttpGet]
    [RequirePermission("diagnosticos", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<DiagnosticoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un diagnóstico por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("diagnosticos", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<DiagnosticoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Crea un nuevo diagnóstico.</summary>
    [HttpPost]
    [RequirePermission("diagnosticos", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<DiagnosticoResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] DiagnosticoRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();
        var result = await _service.CreateAsync(dto, clinicaId, creadoPor);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<DiagnosticoResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza un diagnóstico existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("diagnosticos", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<DiagnosticoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] DiagnosticoRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var modificadoPor = User.GetInternalUserId();
        var result = await _service.UpdateAsync(id, dto, clinicaId, modificadoPor);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un diagnóstico (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("diagnosticos", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Reactiva un diagnóstico desactivado (activo = true).
    /// </summary>
    [HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("diagnosticos", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.ReactivateAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Busca diagnósticos por término (nombre, código CIE-10 o tipo de diagnóstico).
    /// </summary>
    [HttpGet("buscar")]
    [RequirePermission("diagnosticos", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<DiagnosticoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.SearchAsync(clinicaId, q ?? string.Empty);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
