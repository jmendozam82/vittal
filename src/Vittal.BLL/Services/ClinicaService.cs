using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Clinica;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de clínicas. Implementa IClinicaService.
/// CASO ESPECIAL: Tabla raíz multi-tenant — NO tiene clinicaId.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// </summary>
public class ClinicaService : IClinicaService
{
    private readonly IClinicaRepository _repo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClinicaService> _logger;

    // Tipos MIME permitidos para logo
    private static readonly HashSet<string> AllowedLogoMimeTypes = new()
    {
        "image/jpeg", "image/png", "image/webp"
    };

    // Tamaño máximo: 5 MB
    private const long MaxLogoSizeBytes = 5L * 1024 * 1024;

    // Bucket de Supabase Storage
    private const string BucketName = "avatares";

    public ClinicaService(
        IClinicaRepository repo,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ClinicaService> logger)
    {
        _repo = repo;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista clínicas activas (sin tenant filter)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<ClinicaResponseDto>>> GetAllAsync(
        bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo clínicas (inactivos: {Incluir})",
                incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync()
                : await _repo.GetAllAsync();

            var dtos = new List<ClinicaResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapClinicaToDto(entity));
            }

            return ServiceResult<IEnumerable<ClinicaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clínicas");
            return ServiceResult<IEnumerable<ClinicaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de una clínica por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaResponseDto>> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando clínica {Id}", id);

            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Clínica no encontrada", ServiceErrorType.NotFound);
            }

            return ServiceResult<ClinicaResponseDto>.Success(MapClinicaToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar clínica {Id}", id);
            return ServiceResult<ClinicaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. GetCurrentClinicaAsync — Obtiene la clínica del usuario actual
    //    Usa app.current_clinica_id del contexto PostgreSQL
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaResponseDto>> GetCurrentClinicaAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo clínica del contexto actual");

            var entity = await _repo.GetCurrentClinicaAsync();
            if (entity == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "No se pudo determinar la clínica actual del usuario.",
                    ServiceErrorType.NotFound);
            }

            return ServiceResult<ClinicaResponseDto>.Success(
                MapClinicaToDto(entity), "Clínica actual cargada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clínica actual");
            return ServiceResult<ClinicaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. CreateAsync — Crea una nueva clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaResponseDto>> CreateAsync(ClinicaRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Creando clínica {Nombre}", dto.Nombre);

            // Validar nombre único
            if (await _repo.ExistsByNameAsync(dto.Nombre))
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Ya existe una clínica con ese nombre.",
                    ServiceErrorType.Conflict);
            }

            var entity = new Clinica
            {
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Email = dto.Email,
                LogoUrl = dto.LogoUrl,
                TiempoEsperaMinutos = dto.TiempoEsperaMinutos,
                BdExterna1 = dto.BdExterna1,
                BdExterna2 = dto.BdExterna2,
                HorarioApertura = dto.HorarioApertura,
                HorarioCierre = dto.HorarioCierre,
                DiasAtencion = dto.DiasAtencion,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Clínica creada con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId);
            if (created == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Clínica creada pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<ClinicaResponseDto>.Success(
                MapClinicaToDto(created), "Clínica creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear clínica {Nombre}", dto.Nombre);
            return ServiceResult<ClinicaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. UpdateAsync — Actualiza datos de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaResponseDto>> UpdateAsync(
        Guid id, ClinicaRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Actualizando clínica {Id}", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Clínica no encontrada", ServiceErrorType.NotFound);
            }

            // Validar nombre único (excluyendo la propia clínica)
            if (await _repo.ExistsByNameAsync(dto.Nombre, id))
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Ya existe otra clínica con ese nombre.",
                    ServiceErrorType.Conflict);
            }

            // Update entity fields
            existing.Nombre = dto.Nombre;
            existing.Direccion = dto.Direccion;
            existing.Telefono = dto.Telefono;
            existing.Email = dto.Email;
            existing.LogoUrl = dto.LogoUrl;
            existing.TiempoEsperaMinutos = dto.TiempoEsperaMinutos;
            existing.BdExterna1 = dto.BdExterna1;
            existing.BdExterna2 = dto.BdExterna2;
            existing.HorarioApertura = dto.HorarioApertura;
            existing.HorarioCierre = dto.HorarioCierre;
            existing.DiasAtencion = dto.DiasAtencion;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "No se pudo actualizar la clínica.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id);
            if (refreshed == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Clínica actualizada pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<ClinicaResponseDto>.Success(
                MapClinicaToDto(refreshed), "Clínica actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar clínica {Id}", id);
            return ServiceResult<ClinicaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. DeactivateAsync — Desactiva clínica (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando clínica {Id}", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Clínica no encontrada", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La clínica ya está inactiva.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar la clínica.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Clínica desactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar clínica {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. ReactivateAsync — Reactiva clínica (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Reactivando clínica {Id}", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Clínica no encontrada", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La clínica ya está activa.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar la clínica.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Clínica reactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar clínica {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 8. UploadLogoAsync — Sube logo a Supabase Storage (bucket avatares)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<string>> UploadLogoAsync(
        Stream fileStream, string fileName, string contentType, long fileSize, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation(
                "Subiendo logo para clínica {ClinicaId}: {Nombre} ({Tamano} bytes)",
                clinicaId, fileName, fileSize);

            // 1. Validar tipo MIME
            if (!AllowedLogoMimeTypes.Contains(contentType))
            {
                return ServiceResult<string>.Failure(
                    $"Tipo de imagen no permitido: {contentType}. " +
                    $"Tipos permitidos: JPEG, PNG, WebP.",
                    ServiceErrorType.Validation);
            }

            // 2. Validar tamaño
            if (fileSize > MaxLogoSizeBytes)
            {
                return ServiceResult<string>.Failure(
                    $"El archivo supera el tamaño máximo de {MaxLogoSizeBytes / (1024 * 1024)} MB.",
                    ServiceErrorType.Validation);
            }

            if (fileSize == 0)
            {
                return ServiceResult<string>.Failure(
                    "El archivo está vacío.", ServiceErrorType.Validation);
            }

            // 3. Verificar que la clínica existe
            var clinica = await _repo.GetByIdAsync(clinicaId);
            if (clinica == null)
            {
                return ServiceResult<string>.Failure(
                    "Clínica no encontrada.", ServiceErrorType.NotFound);
            }

            // 4. Construir storagePath: logos/{clinicaId}/{Guid}{extension}
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension)) extension = ".png";
            var fileGuid = Guid.NewGuid();
            var storagePath = $"logos/{clinicaId}/{fileGuid}{extension}";

            // 5. Obtener configuración de Supabase
            var supabaseUrl = _configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase:Url no está configurado.");
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceRoleKey no está configurado.");

            // 6. Subir a Supabase Storage via REST API (PUT)
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
                _logger.LogError("Error subiendo logo a Supabase Storage: {Status} - {Error}",
                    response.StatusCode, errorBody);
                return ServiceResult<string>.Failure(
                    "Error al subir el logo al almacenamiento. Intente nuevamente.",
                    ServiceErrorType.InternalError);
            }

            _logger.LogInformation("Logo subido exitosamente a Supabase Storage: {StoragePath}", storagePath);

            // 7. Construir URL pública (bucket avatares es público)
            var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{BucketName}/{storagePath}";

            // 8. Actualizar logo_url en la clínica
            clinica.LogoUrl = publicUrl;
            clinica.FechaModificacion = DateTime.UtcNow;
            var updated = await _repo.UpdateAsync(clinica);

            if (!updated)
            {
                _logger.LogWarning("Logo subido pero no se pudo actualizar logo_url en clínica {ClinicaId}", clinicaId);
                // No es error fatal — el archivo ya está en storage
            }

            return ServiceResult<string>.Success(publicUrl, "Logo subido exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir logo para clínica {ClinicaId}", clinicaId);
            return ServiceResult<string>.Failure($"Error interno: {ex.Message}");
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
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static ClinicaResponseDto MapClinicaToDto(Clinica c)
    {
        return new ClinicaResponseDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Direccion = c.Direccion,
            Telefono = c.Telefono,
            Email = c.Email,
            LogoUrl = c.LogoUrl,
            TiempoEsperaMinutos = c.TiempoEsperaMinutos,
            BdExterna1 = c.BdExterna1,
            BdExterna2 = c.BdExterna2,
            HorarioApertura = c.HorarioApertura,
            HorarioCierre = c.HorarioCierre,
            DiasAtencion = c.DiasAtencion,
            Activo = c.Activo,
            FechaCreacion = c.FechaCreacion,
            FechaModificacion = c.FechaModificacion
        };
    }
}
