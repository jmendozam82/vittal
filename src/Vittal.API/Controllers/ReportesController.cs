using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.Utility;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Reporte;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para Reportes del Sistema.
/// Genera y exporta reportes de citas, pacientes, doctores y tiempos de espera.
/// Historia de Usuario: HU22 — Reportes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _service;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(IReporteService service, ILogger<ReportesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los reportes generados de la clínica.</summary>
    [HttpGet]
    [RequirePermission("reportes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<List<ReporteResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un reporte específico por ID.</summary>
    /// <param name="id">ID del reporte.</param>
    [HttpGet("{id:guid}")]
    [RequirePermission("reportes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ReporteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>Genera un nuevo reporte con los filtros especificados.</summary>
    [HttpPost("generar")]
    [RequirePermission("reportes", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ReporteResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generar([FromBody] ReporteRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var usuarioId = User.GetInternalUserId();

        var result = await _service.GenerarReporteAsync(dto, clinicaId, usuarioId);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<ReporteResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    /// <summary>Exporta un reporte existente en el formato especificado.</summary>
    /// <param name="id">ID del reporte.</param>
    /// <param name="formato">Formato de exportación: pdf | excel | csv | json.</param>
    [HttpGet("{id:guid}/exportar")]
    [RequirePermission("reportes", PermissionType.Read)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exportar(Guid id, [FromQuery] string formato = "csv")
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.ExportarAsync(clinicaId, id, formato);

        if (!result.IsSuccess || result.Data == null)
        {
            return result.ToActionResult();
        }

        var contentType = formato.ToLower() switch
        {
            "csv" => "text/csv",
            "json" => "application/json",
            "pdf" => "application/pdf",
            "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };

        var fileName = $"reporte_{id:N}.{formato}";
        return File(result.Data, contentType, fileName);
    }
}
