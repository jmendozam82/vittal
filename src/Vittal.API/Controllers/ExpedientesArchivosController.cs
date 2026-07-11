using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.ExpedienteArchivo;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de Archivos Adjuntos a Expedientes.
/// Historia de Usuario: HU20 — Expedientes
/// Enfoque A: Upload server-side (Browser → API → Supabase Storage)
/// </summary>
[ApiController]
[Route("api/expedientes-archivos")]
[Authorize]
[Produces("application/json")]
public class ExpedientesArchivosController : ControllerBase
{
    private readonly IExpedienteArchivoService _service;
    private readonly ILogger<ExpedientesArchivosController> _logger;

    public ExpedientesArchivosController(IExpedienteArchivoService service, ILogger<ExpedientesArchivosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los archivos activos de la clínica.</summary>
    [HttpGet]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteArchivoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene todos los archivos activos de un expediente.</summary>
    [HttpGet("expediente/{expedienteId:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteArchivoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByExpediente([FromRoute] Guid expedienteId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByExpedienteIdAsync(clinicaId, expedienteId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene todos los archivos activos de una hoja de cita.</summary>
    [HttpGet("hoja-cita/{hojaCitaId:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteArchivoResponseDto[]>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByHojaCita([FromRoute] Guid hojaCitaId)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByHojaCitaIdAsync(clinicaId, hojaCitaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un archivo por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteArchivoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>
    /// Sube un archivo al expediente (Enfoque A: server-side upload).
    /// El archivo se envía como multipart/form-data.
    /// El storagePath se genera automáticamente: {clinicaId}/{expedienteId}/{Guid}{ext}
    /// </summary>
    [HttpPost("upload")]
    [RequirePermission("expedientes", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ExpedienteArchivoResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB max
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] Guid expedienteId,
        [FromForm] Guid? hojaCitaId = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Debe seleccionar un archivo para subir."
            });
        }

        var clinicaId = User.GetClinicaId();
        var creadoPor = User.GetInternalUserId();

        // Convertir IFormFile → Stream para BLL (sin dependencia ASP.NET Core)
        using var stream = file.OpenReadStream();
        var result = await _service.UploadAsync(
            stream, file.FileName, file.ContentType, file.Length,
            expedienteId, hojaCitaId, clinicaId, creadoPor);

        if (result.IsSuccess && result.Data != null)
        {
            var response = new ApiResponse<ExpedienteArchivoResponseDto>
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
    /// Obtiene una URL firmada temporal (3600s) para descargar el archivo desde Supabase Storage.
    /// </summary>
    [HttpGet("{id:guid}/signed-url")]
    [RequirePermission("expedientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSignedUrl([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetSignedUrlAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>Elimina un archivo de Supabase Storage y desactiva el registro en BD.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeleteAsync(clinicaId, id);
        return result.ToActionResult();
    }

    /// <summary>
    /// Desactiva un archivo (activo = false). No elimina el archivo físico.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("expedientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(clinicaId, id);
        return result.ToActionResult();
    }
}
