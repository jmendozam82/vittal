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
using Vittal.DTO.Agenda;
using Vittal.DTO.Paciente;
using Vittal.DTO.Sala;
using Vittal.DTO.Usuario;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para los catálogos de la pantalla de Agenda (D2).
/// Agrupa pacientes, doctores y salas en una sola llamada para alimentar
/// los combos y filtros del calendario.
/// Si el usuario autenticado es doctor (es_doctor), SOLO se devuelven sus
/// pacientes; la recepcionista y admins ven todos los de la clínica.
/// Historia de Usuario: HU21 — Agenda (perfil Doctor)
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/agenda/catalogos")]
[Authorize]
[Produces("application/json")]
public class AgendaCatalogosController : ControllerBase
{
    private readonly IPacienteService _pacienteService;
    private readonly IUsuarioService _usuarioService;
    private readonly ISalaService _salaService;
    private readonly ILogger<AgendaCatalogosController> _logger;

    public AgendaCatalogosController(
        IPacienteService pacienteService,
        IUsuarioService usuarioService,
        ISalaService salaService,
        ILogger<AgendaCatalogosController> logger)
    {
        _pacienteService = pacienteService;
        _usuarioService = usuarioService;
        _salaService = salaService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene los catálogos de la agenda (pacientes, doctores y salas).
    /// Si el usuario es doctor, los pacientes se filtran a los asignados a él.
    /// </summary>
    [HttpGet]
    [RequirePermission("agenda", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<AgendaCatalogosResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();

        // ── Pacientes ────────────────────────────────────────────
        // El doctor solo ve sus pacientes; la recepcionista/admin ve todos.
        ServiceResult<IEnumerable<PacienteResponseDto>> pacientesResult = User.EsDoctor()
            ? await _pacienteService.GetByDoctorAsync(clinicaId, User.GetInternalUserId())
            : await _pacienteService.GetAllAsync(clinicaId);

        if (!pacientesResult.IsSuccess)
        {
            return pacientesResult.ToActionResult();
        }

        var pacientes = pacientesResult.Data?.ToList() ?? new List<PacienteResponseDto>();

        // ── Doctores ─────────────────────────────────────────────
        // Todos los usuarios de la clínica con es_doctor = true.
        var doctoresResult = await _usuarioService.GetAllAsync(clinicaId);

        if (!doctoresResult.IsSuccess)
        {
            return doctoresResult.ToActionResult();
        }

        var doctores = (doctoresResult.Data ?? Enumerable.Empty<UsuarioResponseDto>())
            .Where(u => u.EsDoctor)
            .ToList();

        // ── Salas ────────────────────────────────────────────────
        var salasResult = await _salaService.GetAllAsync(clinicaId);

        if (!salasResult.IsSuccess)
        {
            return salasResult.ToActionResult();
        }

        var salas = salasResult.Data?.ToList() ?? new List<SalaResponseDto>();

        var data = new AgendaCatalogosResponseDto
        {
            Pacientes = pacientes,
            Doctores = doctores,
            Salas = salas
        };

        return Ok(new ApiResponse<AgendaCatalogosResponseDto>
        {
            Success = true,
            Message = "Catálogos de agenda obtenidos exitosamente.",
            Data = data
        });
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
