using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Helpers;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.SignosVitalesHoja;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Signos Vitales por Consulta (Hoja de Cita).
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// El campo FueraDeRango se calcula automáticamente mediante trigger en BD.
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SignosVitalesHojaController : ControllerBase
{
    private readonly ISignosVitalesHojaService _service;
    private readonly IHojaCitaService _hojaCitaService;
    private readonly ILogger<SignosVitalesHojaController> _logger;

    public SignosVitalesHojaController(
        ISignosVitalesHojaService service,
        IHojaCitaService hojaCitaService,
        ILogger<SignosVitalesHojaController> logger)
    {
        _service = service;
        _hojaCitaService = hojaCitaService;
        _logger = logger;
    }

    /// <summary>Obtiene todos los signos vitales activos de una hoja de cita.</summary>
    [HttpGet("hoja/{hojaCitaId:guid}")]
    [RequirePermission("signos_vitales_hoja", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SignosVitalesHojaResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(Guid hojaCitaId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId, hojaCitaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un signo vital por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("signos_vitales_hoja", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<SignosVitalesHojaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>Registra un nuevo signo vital en una hoja de cita.</summary>
    [HttpPost]
    [BloquearHojaFinalizada]
    [RequirePermission("signos_vitales_hoja", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<SignosVitalesHojaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SignosVitalesHojaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var result = await _service.CreateAsync(dto, clinicaId, usuarioId);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<SignosVitalesHojaResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza un registro de signo vital existente.</summary>
    [HttpPut("{id:guid}")]
    [BloquearHojaFinalizada]
    [RequirePermission("signos_vitales_hoja", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<SignosVitalesHojaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SignosVitalesHojaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un signo vital (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("signos_vitales_hoja", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var clinicaId = User.GetClinicaId();

        // Guard de integridad: obtener la hoja de cita del signo vital para bloquear consultas finalizadas.
        var item = await _service.GetByIdAsync(clinicaId, id);
        if (item.IsSuccess && item.Data != null)
        {
            var bloqueo = await ConsultaFinalizadaGuard.ValidateAsync(clinicaId, item.Data.HojaCitaId, _hojaCitaService);
            if (bloqueo != null)
            {
                return bloqueo;
            }
        }

        var result = await _service.DeactivateAsync(clinicaId, id);
        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
