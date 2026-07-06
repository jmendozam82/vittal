using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.UsuarioSala;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de asignación de doctores a salas/áreas.
/// Historia de Usuario: HU06 — Asignar Doctores a Salas
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsuariosSalasController : ControllerBase
{
    private readonly IUsuarioSalaService _service;
    private readonly ILogger<UsuariosSalasController> _logger;

    public UsuariosSalasController(IUsuarioSalaService service, ILogger<UsuariosSalasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los doctores asignados a una sala.</summary>
    [HttpGet("sala/{salaId:guid}")]
    [RequirePermission("usuarios_salas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioSalaResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBySala([FromRoute] Guid salaId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllBySalaAsync(clinicaId, salaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene una asignación específica por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("usuarios_salas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioSalaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Asigna un doctor a una sala.</summary>
    [HttpPost]
    [RequirePermission("usuarios_salas", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioSalaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] UsuarioSalaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.CreateAsync(dto, clinicaId);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<UsuarioSalaResponseDto>
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
    /// Desasigna un doctor de una sala (baja lógica: activo = false).
    /// NUNCA elimina el registro.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("usuarios_salas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(id, clinicaId);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
