using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.Utility;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.BLL.Interfaces;
using Vittal.DTO.TipoDiagnostico;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Tipos de Diagnóstico.
/// Historia de Usuario: HU13 — Gestión de Tipos de Diagnóstico
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TiposDiagnosticoController : ControllerBase
{
    private readonly ITipoDiagnosticoService _service;
    private readonly ILogger<TiposDiagnosticoController> _logger;

    public TiposDiagnosticoController(ITipoDiagnosticoService service, ILogger<TiposDiagnosticoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los tipos de diagnóstico de la clínica. Por defecto solo activos.</summary>
[HttpGet]
    [RequirePermission("tipos_dx", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<TipoDiagnosticoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un tipo de diagnóstico por su ID.</summary>
[HttpGet("{id:guid}")]
    [RequirePermission("tipos_dx", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<TipoDiagnosticoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Crea un nuevo tipo de diagnóstico.</summary>
[HttpPost]
    [RequirePermission("tipos_dx", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<TipoDiagnosticoResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] TipoDiagnosticoRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();
        var result = await _service.CreateAsync(dto, clinicaId, creadoPor);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<TipoDiagnosticoResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza un tipo de diagnóstico existente.</summary>
[HttpPut("{id:guid}")]
    [RequirePermission("tipos_dx", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<TipoDiagnosticoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] TipoDiagnosticoRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var modificadoPor = User.GetInternalUserId();
        var result = await _service.UpdateAsync(id, dto, clinicaId, modificadoPor);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un tipo de diagnóstico (activo = false). NUNCA elimina.
    /// </summary>
[HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("tipos_dx", PermissionType.Update)]
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
    /// Reactiva un tipo de diagnóstico desactivado (activo = true).
    /// </summary>
[HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("tipos_dx", PermissionType.Update)]
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
    /// Busca tipos de diagnóstico por término (nombre o descripción).
    /// </summary>
[HttpGet("buscar")]
    [RequirePermission("tipos_dx", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<TipoDiagnosticoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.SearchAsync(clinicaId, q ?? string.Empty);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
