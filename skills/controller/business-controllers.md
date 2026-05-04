# Controller — Business Controllers

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para PacientesController y CitasController como referencia.
> **Prerequisito:** skills/controller/SKILL.md, skills/controller/controller-templates.md

---

## PacientesController (HU07) — Métodos Especializados

```csharp
// src/Vittal.API/Controllers/PacientesController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Pacientes")]
public class PacientesController : ControllerBase
{
    private readonly IPacienteService _service;
    private readonly ILogger<PacientesController> _logger;

    public PacientesController(IPacienteService service, ILogger<PacientesController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Obtiene todos los pacientes activos de la clínica.</summary>
    [HttpGet]
    [RequirePermission("pacientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PacienteResponseDto>>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Obtiene pacientes asignados a un doctor.</summary>
    [HttpGet("doctor/{doctorId:guid}")]
    [RequirePermission("pacientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PacienteResponseDto>>), 200)]
    public async Task<IActionResult> GetByDoctor([FromRoute] Guid doctorId)
    {
        var clinicaId = User.GetClinicaId();
        if (!User.EsAdmin() && User.GetUsuarioId() != doctorId)
            return StatusCode(403, ApiResponse<object>.Fail(
                "Solo puede visualizar sus propios pacientes."));

        var result = await _service.GetByDoctorAsync(doctorId, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Busca pacientes por nombre, apellido o email.</summary>
    [HttpGet("buscar")]
    [RequirePermission("pacientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PacienteResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Buscar([FromQuery] string termino)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.SearchAsync(termino, clinicaId);
        return result.ToActionResult(this);
    }

    // + GetById, Create, Update, Desactivar (plantilla estándar)
}
```

---

## CitasController (HU21 + HU18) — Métodos Especializados

```csharp
// src/Vittal.API/Controllers/CitasController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Citas y Agenda")]
public class CitasController : ControllerBase
{
    private readonly ICitaService _service;
    private readonly ILogger<CitasController> _logger;

    public CitasController(ICitaService service, ILogger<CitasController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Obtiene la cola de espera del día actual.</summary>
    [HttpGet("cola-espera")]
    [RequirePermission("cola_espera", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CitaResponseDto>>), 200)]
    public async Task<IActionResult> GetColaEspera([FromQuery] Guid? doctorId)
    {
        var clinicaId = User.GetClinicaId();
        var doctorIdFiltro = User.EsAdmin() ? doctorId : User.GetUsuarioId();
        var result = await _service.GetColaEsperaAsync(clinicaId, doctorIdFiltro);
        return result.ToActionResult(this);
    }

    /// <summary>Obtiene citas de un doctor para una fecha (Agenda).</summary>
    [HttpGet("agenda")]
    [RequirePermission("agenda", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CitaResponseDto>>), 200)]
    public async Task<IActionResult> GetAgenda(
        [FromQuery] Guid doctorId, [FromQuery] DateOnly fecha)
    {
        var clinicaId = User.GetClinicaId();
        if (!User.EsAdmin() && User.GetUsuarioId() != doctorId)
            return StatusCode(403, ApiResponse<object>.Fail(
                "Solo puede visualizar su propia agenda."));

        var result = await _service.GetByDoctorAndFechaAsync(doctorId, clinicaId, fecha);
        return result.ToActionResult(this);
    }

    /// <summary>Registra la llegada de un paciente.</summary>
    [HttpPatch("{id:guid}/llegada")]
    [RequirePermission("cola_espera", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RegistrarLlegada([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.RegistrarLlegadaAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Marca paciente como "en atención".</summary>
    [HttpPatch("{id:guid}/atender")]
    [RequirePermission("cola_espera", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Atender([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.AtenderPacienteAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Cancela una cita (activo = false, estado = 'cancelada').</summary>
    [HttpPatch("{id:guid}/cancelar")]
    [RequirePermission("agenda", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancelar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    // + GetById, Create, Update (plantilla estándar)
}
```

---

## Checklist de Calidad — Business Controllers

### PacientesController
- [ ] Endpoint `GET /doctor/{doctorId}` verifica que no-admin solo ve sus pacientes
- [ ] Endpoint `GET /buscar` acepta `termino` como query param
- [ ] Permiso "pacientes" en todos los RequirePermission

### CitasController
- [ ] `GET /cola-espera` fuerza doctorId al usuario si no es admin
- [ ] `GET /agenda` verifica acceso propio del doctor
- [ ] `PATCH /llegada` usa módulo "cola_espera"
- [ ] `PATCH /atender` usa módulo "cola_espera"
- [ ] `PATCH /cancelar` usa módulo "agenda"
- [ ] No existe DELETE en ningún endpoint

### General
- [ ] User.EsAdmin() usado para restricciones de doctor
- [ ] User.GetUsuarioId() usado para verificación de identidad
- [ ] 403 retornado cuando la restricción de doctor no se cumple
- [ ] Todos los métodos usan `clinicaId = User.GetClinicaId()`

---

*skills/controller/business-controllers.md — Vittal v1.0.0*
