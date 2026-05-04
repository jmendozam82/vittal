# Controller — Master Template

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para crear API Controllers estándar CRUD.
> **Prerequisito:** skills/controller/SKILL.md

---

## Plantilla Maestra de Controller

```csharp
// src/Vittal.API/Controllers/[Entidad]sController.cs
namespace Vittal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("[Entidad]s")]
public class [Entidad]sController : ControllerBase
{
    private readonly I[Entidad]Service _service;
    private readonly ILogger<[Entidad]sController> _logger;

    public [Entidad]sController(
        I[Entidad]Service service,
        ILogger<[Entidad]sController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Obtiene todos los registros activos de la clínica.</summary>
    [HttpGet]
    [RequirePermission("[modulo_clave]", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<[Entidad]ResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.GetAllAsync(clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Obtiene un registro por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("[modulo_clave]", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<[Entidad]ResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("[modulo_clave]", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<[Entidad]ResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> Create([FromBody] [Entidad]RequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.CreateAsync(dto, clinicaId);
        return result.ToCreatedResult(this, nameof(GetById), new { id = result.Data?.Id });
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("[modulo_clave]", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<[Entidad]ResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] [Entidad]RequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Desactiva un registro (activo = false). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("[modulo_clave]", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.DeactivateAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina registros.
}
```

---

## Checklist de Calidad — Controller Template

### Decoradores
- [ ] `[ApiController]` presente
- [ ] `[Route("api/[controller]")]` presente
- [ ] `[Authorize]` en la clase (no endpoint por endpoint)
- [ ] `[Produces("application/json")]` presente
- [ ] `[Tags("...")]` para Swagger

### Endpoints
- [ ] GET lista → `[RequirePermission(Read)]`
- [ ] GET por ID → `[RequirePermission(Read)]`
- [ ] POST crear → `[RequirePermission(Create)]`
- [ ] PUT actualizar → `[RequirePermission(Update)]`
- [ ] PATCH desactivar → `[RequirePermission(Update)]`
- [ ] **No existe HttpDelete**

### Swagger
- [ ] `[ProducesResponseType]` con tipo y código correcto en cada endpoint
- [ ] `/// <summary>` en español
- [ ] Endpoint visible en `/swagger`

### JWT
- [ ] `clinicaId = User.GetClinicaId()` en cada método
- [ ] Ningún endpoint acepta clinicaId como parámetro

---

*skills/controller/controller-templates.md — Vittal v1.0.0*
