using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Dashboard;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para el Dashboard de KPIs y configuración de widgets.
/// Proporciona datos agregados en tiempo real para la pantalla principal.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService service, ILogger<DashboardController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene los KPIs y datos completos del dashboard para una fecha específica.</summary>
    /// <param name="fecha">Fecha para la cual obtener los datos (por defecto: hoy UTC).</param>
    [HttpGet("data")]
    [RequirePermission("dashboard", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<DashboardConfigResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboardData([FromQuery] DateTime? fecha)
    {
        var clinicaId = User.GetClinicaId();
        // Usar fecha LOCAL (DateTime.Today), no UTC: en zonas negativas (UTC−6) entre 18:00 y 23:59
        // local ya es el día siguiente en UTC y el dashboard consultaría el día equivocado (todo 0).
        var fechaConsulta = fecha?.Date ?? DateTime.Today;

        var result = await _service.GetDashboardDataAsync(clinicaId, fechaConsulta);
        return result.ToActionResult();
    }

    /// <summary>Obtiene la configuración de widgets del dashboard.</summary>
    [HttpGet("config")]
    [RequirePermission("dashboard", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<DashboardConfigResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetConfigAsync(clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Guarda la configuración de widgets del dashboard.</summary>
    [HttpPut("config")]
    [RequirePermission("dashboard", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<DashboardConfigResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveConfig([FromBody] DashboardConfigRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.SaveConfigAsync(dto, clinicaId);
        return result.ToActionResult();
    }
}
