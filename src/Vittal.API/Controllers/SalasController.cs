using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Sala;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Salas/Áreas.
/// Historia de Usuario: HU06 — Gestión de Salas
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SalasController : ControllerBase
{
    private readonly ISalaService _service;
    private readonly ILogger<SalasController> _logger;

    public SalasController(ISalaService service, ILogger<SalasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todas las salas de la clínica. Por defecto solo activas.</summary>
    [HttpGet]
    [RequirePermission("salas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<SalaResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene una sala por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("salas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<SalaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Crea una nueva sala en la clínica.</summary>
    [HttpPost]
    [RequirePermission("salas", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<SalaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] SalaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.CreateAsync(dto, clinicaId);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<SalaResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza una sala existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("salas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<SalaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SalaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva una sala (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("salas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Reactiva una sala desactivada (activo = true).
    /// </summary>
    [HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("salas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.ReactivateAsync(id, clinicaId);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
