using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.HojaRecomendacion;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de recomendaciones en hojas de cita. Implementa IHojaRecomendacionService.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaRecomendacionService : IHojaRecomendacionService
{
    private readonly IHojaRecomendacionRepository _repo;
    private readonly ILogger<HojaRecomendacionService> _logger;

    public HojaRecomendacionService(IHojaRecomendacionRepository repo, ILogger<HojaRecomendacionService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Detalle de una recomendación por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaRecomendacionResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando recomendación {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<HojaRecomendacionResponseDto>.Failure(
                    "Recomendación no encontrada", ServiceErrorType.NotFound);
            }

            return ServiceResult<HojaRecomendacionResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar recomendación {Id}", id);
            return ServiceResult<HojaRecomendacionResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Recomendaciones de una hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<HojaRecomendacionResponseDto>>> GetByHojaCitaIdAsync(
        Guid clinicaId, Guid hojaCitaId)
    {
        try
        {
            _logger.LogInformation("Buscando recomendaciones de la hoja de cita {HojaCitaId}", hojaCitaId);

            var entities = await _repo.GetByHojaCitaIdAsync(clinicaId, hojaCitaId);
            var dtos = new List<HojaRecomendacionResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<HojaRecomendacionResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener recomendaciones de la hoja de cita {HojaCitaId}", hojaCitaId);
            return ServiceResult<IEnumerable<HojaRecomendacionResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea una nueva recomendación en la hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaRecomendacionResponseDto>> CreateAsync(
        HojaRecomendacionRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando recomendación en hoja de cita {HojaCitaId}", dto.HojaCitaId);

            var entity = new HojaRecomendacion
            {
                ClinicaId = clinicaId,
                HojaCitaId = dto.HojaCitaId,
                RecomendacionId = dto.RecomendacionId,
                Observaciones = dto.Observaciones,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Recomendación creada con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<HojaRecomendacionResponseDto>.Failure(
                    "Recomendación creada pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaRecomendacionResponseDto>.Success(
                MapToDto(created), "Recomendación agregada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear recomendación en clínica {ClinicaId}", clinicaId);
            return ServiceResult<HojaRecomendacionResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza una recomendación existente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaRecomendacionResponseDto>> UpdateAsync(
        Guid id, HojaRecomendacionRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando recomendación {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<HojaRecomendacionResponseDto>.Failure(
                    "Recomendación no encontrada", ServiceErrorType.NotFound);
            }

            existing.RecomendacionId = dto.RecomendacionId;
            existing.Observaciones = dto.Observaciones;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<HojaRecomendacionResponseDto>.Failure(
                    "No se pudo actualizar la recomendación.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(clinicaId, id);
            if (refreshed == null)
            {
                return ServiceResult<HojaRecomendacionResponseDto>.Failure(
                    "Recomendación actualizada pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaRecomendacionResponseDto>.Success(
                MapToDto(refreshed), "Recomendación actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar recomendación {Id}", id);
            return ServiceResult<HojaRecomendacionResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva recomendación (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando recomendación {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Recomendación no encontrada", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La recomendación ya está inactiva.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar la recomendación.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Recomendación desactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar recomendación {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static HojaRecomendacionResponseDto MapToDto(HojaRecomendacion entity)
    {
        return new HojaRecomendacionResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            HojaCitaId = entity.HojaCitaId,
            RecomendacionId = entity.RecomendacionId,
            Observaciones = entity.Observaciones,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            RecomendacionNombre = entity.RecomendacionNombre
        };
    }
}
