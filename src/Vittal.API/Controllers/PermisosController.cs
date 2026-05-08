using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.DTO.Permiso;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de permisos por perfil.
/// Historia de Usuario: HU05 — Gestión de Permisos
/// Permite consultar y actualizar los permisos granulares (READ, CREATE, UPDATE)
/// asignados a cada perfil sobre los módulos del sistema.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PermisosController : ControllerBase
{
    private readonly IPermisoService _service;
    private readonly ILogger<PermisosController> _logger;

    public PermisosController(IPermisoService service, ILogger<PermisosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los permisos de un perfil (un registro por módulo del sistema).
    /// Módulos sin permiso explícito retornan con valores false.
    /// </summary>
    [HttpGet("perfil/{perfilId:guid}")]
    [RequirePermission("permisos", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PermisoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPerfil([FromRoute] Guid perfilId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetPermisosByPerfilAsync(clinicaId, perfilId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Actualiza los permisos de un perfil (batch upsert).
    /// Recibe una lista de módulos con sus flags de permiso.
    /// </summary>
    [HttpPut("perfil/{perfilId:guid}")]
    [RequirePermission("permisos", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateByPerfil(
        [FromRoute] Guid perfilId,
        [FromBody] PermisoUpdateRequestDto request)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();
        var result = await _service.UpdatePermisosAsync(clinicaId, perfilId, request, usuarioId);
        return result.ToActionResult();
    }
}
