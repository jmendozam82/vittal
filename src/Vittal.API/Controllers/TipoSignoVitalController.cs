using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Catalogos;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Tipos de Signos Vitales por Sala.
/// Historia de Usuario: HU-E04 — Tipos de Signo Vital por Sala
/// Cada sala (especialidad) define sus propios signos vitales a medir.
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TipoSignoVitalController : ControllerBase
{
    private readonly ITipoSignoVitalService _service;
    private readonly ILogger<TipoSignoVitalController> _logger;

    public TipoSignoVitalController(ITipoSignoVitalService service, ILogger<TipoSignoVitalController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los tipos de signo vital activos de una sala.</summary>
    [HttpGet("sala/{salaId:guid}")]
    [RequirePermission("tipos_signo_vital", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TipoSignoVitalDTOs.Response>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(Guid salaId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, salaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un tipo de signo vital por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("tipos_signo_vital", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<TipoSignoVitalDTOs.Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>Crea un nuevo tipo de signo vital para una sala.</summary>
    [HttpPost]
    [RequirePermission("tipos_signo_vital", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<TipoSignoVitalDTOs.Response>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] TipoSignoVitalDTOs.Request request)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var result = await _service.CreateAsync(clinicaId, usuarioId, request);

        if (result.IsSuccess && result.Data != Guid.Empty)
        {
            var response = new ApiResponse<Guid>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza un tipo de signo vital existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("tipos_signo_vital", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TipoSignoVitalDTOs.Request request)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.UpdateAsync(clinicaId, id, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un tipo de signo vital (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("tipos_signo_vital", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(clinicaId, id);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
