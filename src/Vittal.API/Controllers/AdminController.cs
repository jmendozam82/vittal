using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Clinica;
using Vittal.DTO.Usuario;

namespace Vittal.API.Controllers;

/// <summary>
/// Controlador de administración global del sistema Vittal.
/// Todos los endpoints requieren ser Super Admin Global.
/// Historia de Usuario: HU-AD01 — Administración Global
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
[RequireSuperAdmin]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IClinicaService _clinicaService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminService adminService,
        IClinicaService clinicaService,
        ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _clinicaService = clinicaService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────
    // Clínicas
    // ────────────────────────────────────────────────────────────────

    /// <summary>Obtiene todas las clínicas del sistema (activas e inactivas).</summary>
    /// <remarks>
    /// Vista global del Super Admin. Incluye todas las clínicas registradas.
    /// </remarks>
    [HttpGet("clinicas")]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto[]>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClinicas([FromQuery] bool incluirInactivos = false)
    {
        var result = await _clinicaService.GetAllAsync(incluirInactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene detalles de una clínica específica por ID.</summary>
    [HttpGet("clinicas/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClinicaById([FromRoute] Guid id)
    {
        var result = await _clinicaService.GetByIdAsync(id);
        return result.ToActionResult();
    }

    // ────────────────────────────────────────────────────────────────
    // Usuarios (multi-tenant)
    // ────────────────────────────────────────────────────────────────

    /// <summary>Obtiene los usuarios de una clínica específica.</summary>
    /// <param name="clinicaId">ID de la clínica a consultar.</param>
    /// <param name="incluirInactivos">Si true, incluye usuarios inactivos.</param>
    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsuariosByClinica(
        [FromQuery] Guid clinicaId,
        [FromQuery] bool incluirInactivos = false)
    {
        if (clinicaId == Guid.Empty)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "El parámetro clinicaId es obligatorio."
            });
        }

        var result = await _adminService.GetUsuariosByClinicaAsync(clinicaId, incluirInactivos);
        return result.ToActionResult();
    }
}
