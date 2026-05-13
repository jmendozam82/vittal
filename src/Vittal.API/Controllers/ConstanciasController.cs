using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Constancia;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Constancias Médicas.
/// Las constancias son documentos legales: NO se pueden editar después de emitidas.
/// Solo se pueden crear y anular (no eliminar).
/// Historia de Usuario: HU-E07 — Constancias Médicas
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ConstanciasController : ControllerBase
{
    private readonly IConstanciaService _service;
    private readonly ILogger<ConstanciasController> _logger;

    public ConstanciasController(IConstanciaService service, ILogger<ConstanciasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las constancias de la clínica.
    /// Opcionalmente filtra por expediente de un paciente específico.
    /// </summary>
    [HttpGet]
    [RequirePermission("constancias", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConstanciaResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? expedienteId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, expedienteId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene una constancia por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("constancias", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ConstanciaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>Emite una nueva constancia médica.</summary>
    [HttpPost]
    [RequirePermission("constancias", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ConstanciaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ConstanciaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var result = await _service.CreateAsync(dto, clinicaId, usuarioId);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<ConstanciaResponseDto>
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
    /// Anula una constancia (activo = false). NUNCA elimina.
    /// Las constancias son documentos legales: no se editan, solo se anulan.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("constancias", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(clinicaId, id);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
    // NOTA: No existe PUT — las constancias son documentos legales, no se editan.
}
