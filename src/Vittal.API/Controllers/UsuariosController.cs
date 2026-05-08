using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.Utility;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.DTO.Usuario;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestion de Usuarios del sistema.
/// Historia de Usuario: HU04 -- Gestion de Usuarios
/// Todos los endpoints requieren autenticacion JWT de Supabase.
/// Integra con Supabase Auth para creacion, actualizacion de password y baneo.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(IUsuarioService service, ILogger<UsuariosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene los usuarios de la clinica. Por defecto solo activos.</summary>
    [HttpGet]
    [RequirePermission("usuarios", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un usuario por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("usuarios", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Crea un nuevo usuario.
    /// Crea la cuenta en Supabase Auth y luego registra en la base de datos.
    /// </summary>
    [HttpPost]
    [RequirePermission("usuarios", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] UsuarioRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "La contrasena es obligatoria para crear un usuario.",
                Errors = new List<string> { "La contrasena es obligatoria." }
            });
        }

        var result = await _service.CreateAsync(dto, clinicaId, creadoPor);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<UsuarioResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.UsuarioId }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>
    /// Actualiza datos del usuario. Si se envia Password, se actualiza en Supabase Auth.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("usuarios", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UsuarioRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var modificadoPor = User.GetInternalUserId();
        var result = await _service.UpdateAsync(id, dto, clinicaId, modificadoPor);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un usuario (activo = false). NUNCA elimina.
    /// Banea al usuario en Supabase Auth.
    /// Falla si el usuario es doctor con expedientes activos o tiene citas futuras.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("usuarios", PermissionType.Update)]
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
    /// Reactiva un usuario desactivado (activo = true).
    /// Quita el ban en Supabase Auth.
    /// </summary>
    [HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("usuarios", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.ReactivateAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Lista solo doctores activos de la clinica (para dropdowns).</summary>
    [HttpGet("doctores")]
    [RequirePermission("usuarios", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDoctores()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetDoctoresAsync(clinicaId);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE -- el sistema Vittal nunca elimina registros.
}
