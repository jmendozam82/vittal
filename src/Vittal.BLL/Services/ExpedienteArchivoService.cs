using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.ExpedienteArchivo;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de archivos adjuntos a expedientes. Implementa IExpedienteArchivoService.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteArchivoService : IExpedienteArchivoService
{
    private readonly IExpedienteArchivoRepository _repo;
    private readonly ILogger<ExpedienteArchivoService> _logger;

    public ExpedienteArchivoService(IExpedienteArchivoRepository repo, ILogger<ExpedienteArchivoService> logger)
    {
        _repo = repo;
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
    // 5. CreateAsync — Sube un nuevo archivo al expediente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteArchivoResponseDto>> CreateAsync(
        ExpedienteArchivoRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Subiendo archivo {NombreArchivo} al expediente {ExpedienteId}",
                dto.NombreArchivo, dto.ExpedienteId);

            var entity = new ExpedienteArchivo
            {
                ClinicaId = clinicaId,
                ExpedienteId = dto.ExpedienteId,
                HojaCitaId = dto.HojaCitaId,
                NombreArchivo = dto.NombreArchivo,
                TipoMime = dto.TipoMime,
                StoragePath = dto.StoragePath,
                UrlPublica = dto.UrlPublica,
                TamanoBytes = dto.TamanoBytes,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPor = creadoPor
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Archivo creado con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "Archivo creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<ExpedienteArchivoResponseDto>.Success(
                MapToDto(created), "Archivo subido exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir archivo en clínica {ClinicaId}", clinicaId);
            return ServiceResult<ExpedienteArchivoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. UpdateAsync — Actualiza metadatos de un archivo
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteArchivoResponseDto>> UpdateAsync(
        Guid id, ExpedienteArchivoRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando archivo {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<ExpedienteArchivoResponseDto>.Failure(
                    "Archivo no encontrado", ServiceErrorType.NotFound);
            }

            existing.NombreArchivo = dto.NombreArchivo;
            existing.TipoMime = dto.TipoMime;
            existing.StoragePath = dto.StoragePath;
            existing.UrlPublica = dto.UrlPublica;
            existing.TamanoBytes = dto.TamanoBytes;
            existing.HojaCitaId = dto.HojaCitaId;

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
    // 7. DeleteFromStorageAsync — Desactiva archivo y elimina del storage físico
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeleteFromStorageAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Eliminando archivo {Id} del storage en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Archivo no encontrado", ServiceErrorType.NotFound);
            }

            var deleted = await _repo.DeleteFromStorageAsync(clinicaId, id);
            if (!deleted)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo eliminar el archivo del storage.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Archivo eliminado del storage exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar archivo {Id} del storage", id);
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
