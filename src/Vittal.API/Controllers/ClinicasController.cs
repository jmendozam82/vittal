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
using Vittal.DTO.Clinica;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Clínicas.
/// CASO ESPECIAL: Tabla raíz multi-tenant — NO usa filtro de clinicaId.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ClinicasController : ControllerBase
{
    private readonly IClinicaService _service;
    private readonly ILogger<ClinicasController> _logger;

    public ClinicasController(IClinicaService service, ILogger<ClinicasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene la clínica del usuario actual (desde el JWT / contexto).</summary>
    /// <remarks>
    /// IMPORTANTE: Este endpoint debe ir ANTES que /api/Clinicas/{id:guid}
    /// para evitar conflictos de enrutamiento (no confundir "mi-clinica" con un GUID).
    /// </remarks>
    [HttpGet("mi-clinica")]
    [RequirePermission("clinicas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMiClinica()
    {
        var result = await _service.GetCurrentClinicaAsync();
        return result.ToActionResult();
    }

    /// <summary>Obtiene todas las clínicas del sistema. Por defecto solo activas.</summary>
    /// <param name="inactivos">Si true, incluye clínicas inactivas en el listado.</param>
    [HttpGet]
    [RequirePermission("clinicas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var result = await _service.GetAllAsync(inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene una clínica por su ID.</summary>
    /// <param name="id">GUID de la clínica</param>
    [HttpGet("{id:guid}")]
    [RequirePermission("clinicas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.ToActionResult();
    }

    /// <summary>Crea una nueva clínica en el sistema.</summary>
    /// <remarks>
    /// El nombre de la clínica debe ser único en todo el sistema.
    /// Esta operacion normalmente es reservada para Administradores del Sistema (SaaS).
    /// </remarks>
    [HttpPost]
    [RequirePermission("clinicas", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ClinicaRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<ClinicaResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza una clínica existente.</summary>
    /// <param name="id">GUID de la clínica a actualizar</param>
    /// <param name="dto">Datos actualizados de la clínica</param>
    [HttpPut("{id:guid}")]
    [RequirePermission("clinicas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ClinicaRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva una clínica (activo = false). NUNCA elimina.
    /// </summary>
    /// <remarks>
    /// Las clínicas no se eliminan del sistema, solo se desactivan.
    /// Una clínica inactiva no puede iniciar sesión sus usuarios.
    /// </remarks>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("clinicas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var result = await _service.DeactivateAsync(id);
        return result.ToActionResult();
    }

    /// <summary>
    /// Reactiva una clínica desactivada (activo = true).
    /// </summary>
    /// <remarks>
    /// Permite volver a habilitar una clínica que fue desactivada previamente.
    /// </remarks>
    [HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("clinicas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reactivar([FromRoute] Guid id)
    {
        var result = await _service.ReactivateAsync(id);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina clínicas.
}
