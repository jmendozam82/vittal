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
using Vittal.DTO.Perfil;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Perfiles.
/// Historia de Usuario: HU03 — Gestión de Perfiles
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PerfilesController : ControllerBase
{
    private readonly IPerfilService _service;
    private readonly ILogger<PerfilesController> _logger;

    public PerfilesController(IPerfilService service, ILogger<PerfilesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los perfiles de la clínica. Por defecto solo activos.</summary>
    [HttpGet]
    [RequirePermission("perfiles", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PerfilResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un perfil por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("perfiles", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PerfilResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Crea un nuevo perfil en la clínica.</summary>
    [HttpPost]
    [RequirePermission("perfiles", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<PerfilResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] PerfilRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.CreateAsync(dto, clinicaId);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<PerfilResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza un perfil existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("perfiles", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<PerfilResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] PerfilRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un perfil (activo = false). NUNCA elimina.
    /// Falla si el perfil tiene usuarios asignados.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("perfiles", PermissionType.Update)]
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
    /// Reactiva un perfil desactivado (activo = true).
    /// </summary>
    [HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("perfiles", PermissionType.Update)]
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
