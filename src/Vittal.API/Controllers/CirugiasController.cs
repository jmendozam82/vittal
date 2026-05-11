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
using Vittal.DTO.Cirugia;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Cirugías.
/// Historia de Usuario: HU12 — Gestión de Cirugías
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CirugiasController : ControllerBase
{
    private readonly ICirugiaService _service;
    private readonly ILogger<CirugiasController> _logger;

    public CirugiasController(ICirugiaService service, ILogger<CirugiasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todas las cirugías de la clínica. Por defecto solo activos.</summary>
    [HttpGet]
    [RequirePermission("cirugias", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<CirugiaResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene una cirugía por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("cirugias", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<CirugiaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Crea una nueva cirugía.</summary>
    [HttpPost]
    [RequirePermission("cirugias", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<CirugiaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CirugiaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();
        var result = await _service.CreateAsync(dto, clinicaId, creadoPor);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<CirugiaResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza una cirugía existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("cirugias", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<CirugiaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CirugiaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var modificadoPor = User.GetInternalUserId();
        var result = await _service.UpdateAsync(id, dto, clinicaId, modificadoPor);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva una cirugía (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("cirugias", PermissionType.Update)]
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
    /// Reactiva una cirugía desactivada (activo = true).
    /// </summary>
    [HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("cirugias", PermissionType.Update)]
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
    /// Busca cirugías por término (nombre o tipo de cirugía).
    /// </summary>
    [HttpGet("buscar")]
    [RequirePermission("cirugias", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<CirugiaResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.SearchAsync(clinicaId, q ?? string.Empty);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
