using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Cirugia;
using Vittal.DTO.Diagnostico;
using Vittal.DTO.Examen;
using Vittal.DTO.Expediente;
using Vittal.DTO.Medicamento;
using Vittal.DTO.Recomendacion;
using Vittal.DTO.Tratamiento;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para los catálogos de la pantalla de Expedientes (D3).
/// Agrupa diagnósticos, medicamentos, tratamientos, recomendaciones,
/// cirugías, exámenes, tipos de signo vital y tipos de antecedente en una
/// sola llamada para alimentar los combos de la hoja de cita clínica.
/// El doctor puede LEER estos catálogos internamente aunque no los vea
/// en el sidebar (los permisos de lectura se verifican por módulo).
/// Historia de Usuario: HU20 — Expedientes
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/expedientes/catalogos")]
[Authorize]
[Produces("application/json")]
public class ExpedientesCatalogosController : ControllerBase
{
    private readonly IDiagnosticoService _diagnosticoService;
    private readonly IMedicamentoService _medicamentoService;
    private readonly ITratamientoService _tratamientoService;
    private readonly IRecomendacionService _recomendacionService;
    private readonly ICirugiaService _cirugiaService;
    private readonly IExamenService _examenService;
    private readonly ITipoSignoVitalService _tipoSignoVitalService;
    private readonly ITipoAntecedenteService _tipoAntecedenteService;
    private readonly ILogger<ExpedientesCatalogosController> _logger;

    public ExpedientesCatalogosController(
        IDiagnosticoService diagnosticoService,
        IMedicamentoService medicamentoService,
        ITratamientoService tratamientoService,
        IRecomendacionService recomendacionService,
        ICirugiaService cirugiaService,
        IExamenService examenService,
        ITipoSignoVitalService tipoSignoVitalService,
        ITipoAntecedenteService tipoAntecedenteService,
        ILogger<ExpedientesCatalogosController> logger)
    {
        _diagnosticoService = diagnosticoService;
        _medicamentoService = medicamentoService;
        _tratamientoService = tratamientoService;
        _recomendacionService = recomendacionService;
        _cirugiaService = cirugiaService;
        _examenService = examenService;
        _tipoSignoVitalService = tipoSignoVitalService;
        _tipoAntecedenteService = tipoAntecedenteService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene los catálogos de expediente de la clínica.
    /// Los tipos de signo vital y antecedente se listan sin filtrar por sala
    /// (salaId null = todos los de la clínica).
    /// </summary>
    [HttpGet]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExpedientesCatalogosResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();

        // ── Diagnósticos ─────────────────────────────────────────
        var diagnosticosResult = await _diagnosticoService.GetAllAsync(clinicaId);
        if (!diagnosticosResult.IsSuccess)
        {
            return diagnosticosResult.ToActionResult();
        }

        // ── Medicamentos ─────────────────────────────────────────
        var medicamentosResult = await _medicamentoService.GetAllAsync(clinicaId);
        if (!medicamentosResult.IsSuccess)
        {
            return medicamentosResult.ToActionResult();
        }

        // ── Tratamientos ─────────────────────────────────────────
        var tratamientosResult = await _tratamientoService.GetAllAsync(clinicaId);
        if (!tratamientosResult.IsSuccess)
        {
            return tratamientosResult.ToActionResult();
        }

        // ── Recomendaciones ──────────────────────────────────────
        var recomendacionesResult = await _recomendacionService.GetAllAsync(clinicaId);
        if (!recomendacionesResult.IsSuccess)
        {
            return recomendacionesResult.ToActionResult();
        }

        // ── Cirugías ─────────────────────────────────────────────
        var cirugiasResult = await _cirugiaService.GetAllAsync(clinicaId);
        if (!cirugiasResult.IsSuccess)
        {
            return cirugiasResult.ToActionResult();
        }

        // ── Exámenes ─────────────────────────────────────────────
        var examenesResult = await _examenService.GetAllAsync(clinicaId);
        if (!examenesResult.IsSuccess)
        {
            return examenesResult.ToActionResult();
        }

        // ── Tipos de signo vital (todos los de la clínica) ───────
        var tiposSignoVitalResult = await _tipoSignoVitalService.GetAllAsync(clinicaId, (Guid?)null);
        if (!tiposSignoVitalResult.IsSuccess)
        {
            return tiposSignoVitalResult.ToActionResult();
        }

        // ── Tipos de antecedente (todos los de la clínica) ───────
        var tiposAntecedenteResult = await _tipoAntecedenteService.GetAllAsync(clinicaId, (Guid?)null);
        if (!tiposAntecedenteResult.IsSuccess)
        {
            return tiposAntecedenteResult.ToActionResult();
        }

        var data = new ExpedientesCatalogosResponseDto
        {
            Diagnosticos = diagnosticosResult.Data?.ToList() ?? new List<DiagnosticoResponseDto>(),
            Medicamentos = medicamentosResult.Data?.ToList() ?? new List<MedicamentoResponseDto>(),
            Tratamientos = tratamientosResult.Data?.ToList() ?? new List<TratamientoResponseDto>(),
            Recomendaciones = recomendacionesResult.Data?.ToList() ?? new List<RecomendacionResponseDto>(),
            Cirugias = cirugiasResult.Data?.ToList() ?? new List<CirugiaResponseDto>(),
            Examenes = examenesResult.Data?.ToList() ?? new List<ExamenResponseDto>(),
            TiposSignoVital = tiposSignoVitalResult.Data?.ToList() ?? new List<Vittal.DTO.Catalogos.TipoSignoVitalDTOs.Response>(),
            TiposAntecedente = tiposAntecedenteResult.Data?.ToList() ?? new List<Vittal.DTO.Catalogos.TipoAntecedenteDTOs.Response>()
        };

        return Ok(new ApiResponse<ExpedientesCatalogosResponseDto>
        {
            Success = true,
            Message = "Catálogos de expediente obtenidos exitosamente.",
            Data = data
        });
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
