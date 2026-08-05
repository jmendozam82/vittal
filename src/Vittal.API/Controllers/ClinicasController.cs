using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.Utility;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Clinica;
using Vittal.DTO.Auth;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Clínicas.
/// CASO ESPECIAL: Tabla raíz multi-tenant — NO usa filtro de clinicaId.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ClinicasController : ControllerBase
{
    private readonly IClinicaService _service;
    private readonly IAdminService _adminService;
    private readonly ILogger<ClinicasController> _logger;

    public ClinicasController(
        IClinicaService service,
        IAdminService adminService,
        ILogger<ClinicasController> logger)
    {
        _service = service;
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>Obtiene la clínica del usuario actual (desde el JWT / contexto).</summary>
    /// <remarks>
    /// IMPORTANTE: Este endpoint debe ir ANTES que /api/Clinicas/{id:guid}
    /// para evitar conflictos de enrutamiento (no confundir "mi-clinica" con un GUID).
    /// </remarks>
    [HttpGet("mi-clinica")]
    [RequirePermission("clinicas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMiClinica()
    {
        var result = await _service.GetCurrentClinicaAsync();
        return result.ToActionResult();
    }

    /// <summary>
    /// Devuelve solo el logo de la clínica actual del usuario.
    /// Endpoint ligero para el sidebar — solo requiere autenticación, sin permiso de módulo.
    /// Usa el clinica_id del JWT directamente (no depende de PostgreSQL session variable).
    /// </summary>
    [HttpGet("current-logo")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentLogo()
    {
        var clinicaId = User.GetClinicaId();
        if (clinicaId == Guid.Empty)
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { logoUrl = (string?)null }
            });
        }

        var result = await _service.GetByIdAsync(clinicaId);
        if (result.IsSuccess && result.Data != null)
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { logoUrl = result.Data.LogoUrl }
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new { logoUrl = (string?)null }
        });
    }

    /// <summary>
    /// Devuelve los datos completos de la clínica actual del usuario (encabezados/pies de documentos).
    /// Endpoint ligero — solo requiere autenticación, sin permiso de módulo.
    /// Usa el clinica_id del JWT directamente (no depende de PostgreSQL session variable).
    /// </summary>
    [HttpGet("current-info")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentInfo()
    {
        var clinicaId = User.GetClinicaId();
        if (clinicaId == Guid.Empty)
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = null
            });
        }

        var result = await _service.GetByIdAsync(clinicaId);
        if (result.IsSuccess && result.Data != null)
        {
            return Ok(new ApiResponse<ClinicaResponseDto>
            {
                Success = true,
                Data = result.Data
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = null
        });
    }

    /// <summary>
    /// Devuelve el horario de atención de la clínica actual del usuario.
    /// Endpoint ligero para agenda — solo requiere autenticación, sin permiso de módulo.
    /// </summary>
    [HttpGet("current-schedule")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentSchedule()
    {
        var clinicaId = User.GetClinicaId();
        if (clinicaId == Guid.Empty)
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { horarioApertura = (string?)null, horarioCierre = (string?)null, diasAtencion = (string?)null }
            });
        }

        var result = await _service.GetByIdAsync(clinicaId);
        if (result.IsSuccess && result.Data != null)
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    horarioApertura = result.Data.HorarioApertura,
                    horarioCierre = result.Data.HorarioCierre,
                    diasAtencion = result.Data.DiasAtencion
                }
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new { horarioApertura = (string?)null, horarioCierre = (string?)null, diasAtencion = (string?)null }
        });
    }

    /// <summary>Obtiene todas las clínicas del sistema. Por defecto solo activas.</summary>
    /// <param name="inactivos">Si true, incluye clínicas inactivas en el listado.</param>
    [HttpGet]
    [RequirePermission("clinicas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] bool inactivos = false)
    {
        var result = await _service.GetAllAsync(inactivos);
        return result.ToActionResult();
    }

    /// <summary>Obtiene una clínica por su ID.</summary>
    /// <param name="id">GUID de la clínica</param>
    [HttpGet("{id:guid}")]
    [RequirePermission("clinicas", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.ToActionResult();
    }

    /// <summary>Crea una nueva clínica en el sistema.</summary>
    /// <remarks>
    /// El nombre de la clínica debe ser único en todo el sistema.
    /// Esta operación está restringida exclusivamente al Super Admin Global.
    /// </remarks>
    [HttpPost]
    [RequireSuperAdmin]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ClinicaRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<ClinicaResponseDto>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, response);
        }

        return result.ToActionResult();
    }

    // ────────────────────────────────────────────────────────────────
    // PROVISIONAR — Creación completa de clínica + admin + permisos
    // ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Crea una nueva clínica con provisionamiento completo:
    /// clínica + perfil admin + permisos + usuario Supabase Auth + config por defecto.
    /// Exclusivo del Super Admin Global.
    /// </summary>
    /// <remarks>
    /// Este endpoint orquesta la creación completa de una clínica en una sola llamada:
    /// 1. Crea la clínica
    /// 2. Crea el perfil Administrador para esa clínica
    /// 3. Seedear permisos READ + CREATE + UPDATE para todos los módulos
    /// 4. Crea el usuario en Supabase Auth (email + password)
    /// 5. Crea el usuario local en la tabla usuarios
    /// 6. Seedear configuracion_alertas y dashboard_config por defecto
    ///
    /// Si algún paso falla, se realiza rollback automático (desactivación de clínica,
    /// eliminación de usuario en Supabase Auth).
    /// </remarks>
    [HttpPost("provisionar")]
    [RequireSuperAdmin]
    [ProducesResponseType(typeof(ApiResponse<ClinicaProvisionResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Provisionar([FromBody] ClinicaProvisionRequestDto dto)
    {
        var superAdminUsuarioId = User.GetInternalUserId();
        var result = await _adminService.ProvisionClinicaAsync(dto, superAdminUsuarioId);

        if (result.IsSuccess && result.Data != null)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Data.ClinicaId },
                new ApiResponse<ClinicaProvisionResponseDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
        }

        return result.ToActionResult();
    }

    /// <summary>Actualiza una clínica existente.</summary>
    /// <param name="id">GUID de la clínica a actualizar</param>
    /// <param name="dto">Datos actualizados de la clínica</param>
    [HttpPut("{id:guid}")]
    [RequirePermission("clinicas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<ClinicaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ClinicaRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva una clínica (activo = false). NUNCA elimina.
    /// </summary>
    /// <remarks>
    /// Las clínicas no se eliminan del sistema, solo se desactivan.
    /// Una clínica inactiva no puede iniciar sesión sus usuarios.
    /// </remarks>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("clinicas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var result = await _service.DeactivateAsync(id);
        return result.ToActionResult();
    }

    /// <summary>
    /// Reactiva una clínica desactivada (activo = true).
    /// </summary>
    /// <remarks>
    /// Permite volver a habilitar una clínica que fue desactivada previamente.
    /// </remarks>
    [HttpPatch("{id:guid}/reactivar")]
    [RequirePermission("clinicas", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reactivar([FromRoute] Guid id)
    {
        var result = await _service.ReactivateAsync(id);
        return result.ToActionResult();
    }

    // ────────────────────────────────────────────────────────────────
    // LOGO — Subir logo de la clínica a Supabase Storage
    // ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Sube el logo de la clínica a Supabase Storage (bucket avatares).
    /// Reemplaza el logo anterior si ya existe.
    /// </summary>
    /// <param name="id">GUID de la clínica</param>
    /// <param name="file">Archivo de imagen (JPEG, PNG o WebP, máx 5MB)</param>
    [HttpPost("{id:guid}/logo")]
    [RequireSuperAdmin]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadLogo([FromRoute] Guid id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "No se proporcionó ningún archivo."
            });
        }

        using var stream = file.OpenReadStream();
        var result = await _service.UploadLogoAsync(
            stream, file.FileName, file.ContentType, file.Length, id);

        if (result.IsSuccess && result.Data != null)
        {
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            });
        }

        return result.ToActionResult();
    }

    // NOTA: No existe DELETE — el sistema Vittal nunca elimina clínicas.
}
