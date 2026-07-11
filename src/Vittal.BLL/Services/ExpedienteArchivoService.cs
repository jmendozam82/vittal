using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.ExpedienteArchivo;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de archivos adjuntos a expedientes. Implementa IExpedienteArchivoService.
/// Historia de Usuario: HU20 — Expedientes
/// Enfoque A: Upload server-side (Browser → API BLL → Supabase Storage)
/// </summary>
public class ExpedienteArchivoService : IExpedienteArchivoService
{
    private readonly IExpedienteArchivoRepository _repo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpedienteArchivoService> _logger;

    // Tipos MIME permitidos
    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        // Imágenes
        "image/jpeg", "image/png", "image/webp", "image/gif",
        // PDFs
        "application/pdf",
        // Documentos Office
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        // Texto
        "text/plain"
    };

    // Tamaño máximo: 50 MB
    private const long MaxFileSizeBytes = 50L * 1024 * 1024;

    // Bucket de Supabase Storage
    private const string BucketName = "expedientes";

    // URL de firmado temporal: 3600 segundos (1 hora)
    private const int SignedUrlExpirationSeconds = 3600;

    public ExpedienteArchivoService(
        IExpedienteArchivoRepository repo,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ExpedienteArchivoService> logger)
    {
        _repo = repo;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista archivos activos de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>> GetAllAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo archivos de expedientes de la clínica {ClinicaId}", clinicaId);

            var entities = await _repo.GetAllAsync(clinicaId);
            var dtos = new List<ExpedienteArchivoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener archivos de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un archivo por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteArchivoResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando archivo {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "Archivo no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<ExpedienteArchivoResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar archivo {Id}", id);
            return ServiceResult<ExpedienteArchivoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. GetByExpedienteIdAsync — Archivos de un expediente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>> GetByExpedienteIdAsync(
        Guid clinicaId, Guid expedienteId)
    {
        try
        {
            _logger.LogInformation("Buscando archivos del expediente {ExpedienteId}", expedienteId);

            var entities = await _repo.GetByExpedienteIdAsync(clinicaId, expedienteId);
            var dtos = new List<ExpedienteArchivoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener archivos del expediente {ExpedienteId}", expedienteId);
            return ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. GetByHojaCitaIdAsync — Archivos de una hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>> GetByHojaCitaIdAsync(
        Guid clinicaId, Guid hojaCitaId)
    {
        try
        {
            _logger.LogInformation("Buscando archivos de la hoja de cita {HojaCitaId}", hojaCitaId);

            var entities = await _repo.GetByHojaCitaIdAsync(clinicaId, hojaCitaId);
            var dtos = new List<ExpedienteArchivoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener archivos de la hoja de cita {HojaCitaId}", hojaCitaId);
            return ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. UploadAsync — Sube archivo a Supabase Storage y crea registro en BD
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteArchivoResponseDto>> UploadAsync(
        Stream fileStream, string fileName, string contentType, long fileSize,
        Guid expedienteId, Guid? hojaCitaId, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation(
                "Subiendo archivo {Nombre} ({Tamano} bytes) al expediente {ExpedienteId}, hojaCita {HojaCitaId}",
                fileName, fileSize, expedienteId, hojaCitaId);

            // 1. Validar tipo MIME
            if (!AllowedMimeTypes.Contains(contentType))
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    $"Tipo de archivo no permitido: {contentType}. " +
                    $"Tipos permitidos: PDF, imágenes (JPEG/PNG/WebP/GIF), Word, Excel, TXT.",
                    ServiceErrorType.Validation);
            }

            // 2. Validar tamaño
            if (fileSize > MaxFileSizeBytes)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    $"El archivo supera el tamaño máximo de {MaxFileSizeBytes / (1024 * 1024)} MB.",
                    ServiceErrorType.Validation);
            }

            if (fileSize == 0)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "El archivo está vacío.", ServiceErrorType.Validation);
            }

            // 3. Construir storagePath: {clinicaId}/{expedienteId}/{Guid}{extension}
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension)) extension = ".bin";
            var fileGuid = Guid.NewGuid();
            var storagePath = $"{clinicaId}/{expedienteId}/{fileGuid}{extension}";

            // 4. Obtener configuración de Supabase
            var supabaseUrl = _configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase:Url no está configurado.");
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceRoleKey no está configurado.");

            // 5. Subir a Supabase Storage via REST API (PUT)
            var fileBytes = await ReadFullyAsync(fileStream);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

            var content = new ByteArrayContent(fileBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var uploadUrl = $"{supabaseUrl}/storage/v1/object/{BucketName}/{storagePath}";
            var response = await client.PutAsync(uploadUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error subiendo archivo a Supabase Storage: {Status} - {Error}",
                    response.StatusCode, errorBody);
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "Error al subir el archivo al almacenamiento. Intente nuevamente.",
                    ServiceErrorType.InternalError);
            }

            _logger.LogInformation("Archivo subido exitosamente a Supabase Storage: {StoragePath}", storagePath);

            // 6. Crear registro en BD
            var entity = new ExpedienteArchivo
            {
                ClinicaId = clinicaId,
                ExpedienteId = expedienteId,
                HojaCitaId = hojaCitaId,
                NombreArchivo = fileName,
                TipoMime = contentType,
                StoragePath = storagePath,
                UrlPublica = null, // Las URLs firmadas se generan bajo demanda
                TamanoBytes = fileSize,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPor = creadoPor
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Registro de archivo creado en BD con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "Archivo subido pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<ExpedienteArchivoResponseDto>.Success(
                MapToDto(created), "Archivo subido exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir archivo al expediente {ExpedienteId}", expedienteId);
            return ServiceResult<ExpedienteArchivoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    /// <summary>Lee un stream completo a un array de bytes.</summary>
    private static async Task<byte[]> ReadFullyAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. UpdateAsync — Actualiza metadatos del archivo (solo nombre)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteArchivoResponseDto>> UpdateAsync(
        Guid id, ExpedienteArchivoRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando nombre del archivo {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "Archivo no encontrado", ServiceErrorType.NotFound);
            }

            // Solo actualizamos el nombre del archivo — el resto es inmutable
            existing.NombreArchivo = dto.NombreArchivo;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "No se pudo actualizar el archivo.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(clinicaId, id);
            if (refreshed == null)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "Archivo actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<ExpedienteArchivoResponseDto>.Success(
                MapToDto(refreshed), "Archivo actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar archivo {Id}", id);
            return ServiceResult<ExpedienteArchivoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. GetSignedUrlAsync — Genera URL firmada temporal (3600s) para descargar
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<string>> GetSignedUrlAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Generando URL firmada para archivo {Id}", id);

            var entity = await _repo.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<string>.Failure(
                    "Archivo no encontrado", ServiceErrorType.NotFound);
            }

            var supabaseUrl = _configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase:Url no está configurado.");
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceRoleKey no está configurado.");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

            // POST /storage/v1/object/sign/{bucket}/{path}
            var signUrl = $"{supabaseUrl}/storage/v1/object/sign/{BucketName}/{entity.StoragePath}";
            var payload = new { expiresIn = SignedUrlExpirationSeconds };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(signUrl, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error generando URL firmada: {Status} - {Error}",
                    response.StatusCode, errorBody);
                return ServiceResult<string>.Failure(
                    "No se pudo generar la URL de descarga.", ServiceErrorType.InternalError);
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var signedPath = doc.RootElement.GetProperty("signedURL").GetString() ?? "";

            // La URL firmada viene como "/object/sign/..."
            // Necesitamos prepend "/storage/v1" para la URL completa
            var signedUrl = $"{supabaseUrl}/storage/v1{signedPath}";

            return ServiceResult<string>.Success(signedUrl, "URL firmada generada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar URL firmada para archivo {Id}", id);
            return ServiceResult<string>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 8. DeleteAsync — Elimina de Supabase Storage + desactiva en BD
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeleteAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Eliminando archivo {Id} de Storage y BD", id);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Archivo no encontrado", ServiceErrorType.NotFound);
            }

            // 1. Eliminar de Supabase Storage (DELETE)
            var supabaseUrl = _configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase:Url no está configurado.");
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceRoleKey no está configurado.");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

            var deleteUrl = $"{supabaseUrl}/storage/v1/object/{BucketName}/{existing.StoragePath}";
            var response = await client.DeleteAsync(deleteUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error eliminando archivo de Supabase Storage (no fatal): {Status} - {Error}",
                    response.StatusCode, errorBody);
                // No retornamos error — el archivo puede no existir en storage
                // pero el registro en BD sigue siendo válido desactivar
            }
            else
            {
                _logger.LogInformation("Archivo eliminado de Supabase Storage: {StoragePath}", existing.StoragePath);
            }

            // 2. Desactivar en BD (activo = false)
            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el registro del archivo.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Archivo eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar archivo {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 8. DeactivateAsync — Desactiva archivo (activo = false). No elimina físico.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando archivo {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Archivo no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El archivo ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el archivo.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Archivo desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar archivo {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static ExpedienteArchivoResponseDto MapToDto(ExpedienteArchivo entity)
    {
        return new ExpedienteArchivoResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            ExpedienteId = entity.ExpedienteId,
            HojaCitaId = entity.HojaCitaId,
            NombreArchivo = entity.NombreArchivo,
            TipoMime = entity.TipoMime,
            StoragePath = entity.StoragePath,
            UrlPublica = entity.UrlPublica,
            TamanoBytes = entity.TamanoBytes,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion
        };
    }
}
