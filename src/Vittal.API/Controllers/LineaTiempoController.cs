using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.LineaTiempo;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para la Línea de Tiempo de atención de pacientes.
/// Gestiona los pasos secuenciales por los que pasa un paciente durante su consulta.
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LineaTiempoController : ControllerBase
{
    private readonly ILineaTiempoService _service;
    private readonly ILogger<LineaTiempoController> _logger;

    public LineaTiempoController(ILineaTiempoService service, ILogger<LineaTiempoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene la línea de tiempo completa de una cita, ordenada por orden del paso.</summary>
    /// <param name="citaId">ID de la cita.</param>
    [HttpGet("cita/{citaId:guid}")]
    [RequirePermission("linea_tiempo", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<List<LineaTiempoResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimelineByCita(Guid citaId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetTimelineByCitaAsync(clinicaId, citaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene la línea de tiempo del día para la clínica, opcionalmente filtrada por doctor.</summary>
    /// <param name="fecha">Fecha a consultar (por defecto: hoy UTC).</param>
    /// <param name="doctorId">Opcional: ID del doctor para filtrar.</param>
    [HttpGet("dia")]
    [RequirePermission("linea_tiempo", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<List<LineaTiempoResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimelineDelDia([FromQuery] DateTime? fecha, [FromQuery] Guid? doctorId)
    {
        var clinicaId = User.GetClinicaId();
        // Fecha LOCAL (DateTime.Today) no UTC — mismo patrón de zona horaria que Dashboard
        var fechaConsulta = fecha?.Date ?? DateTime.Today;
        var result = await _service.GetTimelineDelDiaAsync(clinicaId, doctorId, fechaConsulta);
        return result.ToActionResult();
    }

    /// <summary>Inicia un paso de la línea de tiempo (cambia estado a "en_sala").</summary>
    /// <param name="pasoId">ID del paso a iniciar.</param>
    [HttpPost("{pasoId:guid}/iniciar")]
    [RequirePermission("linea_tiempo", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<LineaTiempoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IniciarPaso(Guid pasoId)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var result = await _service.IniciarPasoAsync(clinicaId, pasoId, usuarioId);
        return result.ToActionResult();
    }

    /// <summary>Finaliza un paso de la línea de tiempo (cambia estado a "completado").</summary>
    /// <param name="pasoId">ID del paso a finalizar.</param>
    [HttpPost("{pasoId:guid}/finalizar")]
    [RequirePermission("linea_tiempo", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<LineaTiempoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FinalizarPaso(Guid pasoId)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var result = await _service.FinalizarPasoAsync(clinicaId, pasoId, usuarioId);
        return result.ToActionResult();
    }

    /// <summary>Salta un paso de la línea de tiempo (cambia estado a "saltado").</summary>
    /// <param name="pasoId">ID del paso a saltar.</param>
    [HttpPost("{pasoId:guid}/saltar")]
    [RequirePermission("linea_tiempo", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaltarPaso(Guid pasoId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.SaltarPasoAsync(clinicaId, pasoId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Fuerza la verificación de una cita: si todos los pasos están finalizados, la marca como "atendida".
    /// Endpoint de reparación para citas atascadas.
    /// </summary>
    /// <param name="citaId">ID de la cita a verificar/completar.</param>
    [HttpPost("{citaId:guid}/forzar-completar")]
    [RequirePermission("linea_tiempo", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForzarCompletarCita(Guid citaId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.ForzarCompletarCitaAsync(clinicaId, citaId);
        return result.ToActionResult();
    }
}
