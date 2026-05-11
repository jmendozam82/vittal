using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.Utility;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.DTO.Medicamento;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Medicamentos.
/// Historia de Usuario: HU08 — Gestión de Medicamentos
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class MedicamentosController : ControllerBase
{
    private readonly IMedicamentoService _service;
    private readonly ILogger<MedicamentosController> _logger;

    public MedicamentosController(IMedicamentoService service, ILogger<MedicamentosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los medicamentos de la clínica. Por defecto solo activos.</summary>
    [HttpGet]
    [RequirePermission("medicamentos", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<MedicamentoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un medicamento por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("medicamentos", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<MedicamentoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Crea un nuevo medicamento.</summary>
    [HttpPost]
    [RequirePermission("medicamentos", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<MedicamentoResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] MedicamentoRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();
        var result = await _service.CreateAsync(dto, clinicaId, creadoPor);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<MedicamentoResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza un medicamento existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("medicamentos", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<MedicamentoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] MedicamentoRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var modificadoPor = User.GetInternalUserId();
        var result = await _service.UpdateAsync(id, dto, clinicaId, modificadoPor);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un medicamento (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("medicamentos", PermissionType.Update)]
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
    /// Reactiva un medicamento desactivado (activo = true).
    /// </summary>
    [HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("medicamentos", PermissionType.Update)]
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
    /// Busca medicamentos por término (nombre, concentración o descripción).
    /// </summary>
    [HttpGet("buscar")]
    [RequirePermission("medicamentos", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<MedicamentoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.SearchAsync(clinicaId, q ?? string.Empty);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
