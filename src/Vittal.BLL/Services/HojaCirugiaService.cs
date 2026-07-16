using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.HojaCirugia;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de cirugías en hojas de cita. Implementa IHojaCirugiaService.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCirugiaService : IHojaCirugiaService
{
    private readonly IHojaCirugiaRepository _repo;
    private readonly ILogger<HojaCirugiaService> _logger;

    public HojaCirugiaService(IHojaCirugiaRepository repo, ILogger<HojaCirugiaService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Detalle de una cirugía por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaCirugiaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<HojaCirugiaResponseDto>.Failure(
                    "Cirugía no encontrada", ServiceErrorType.NotFound);
            }

            return ServiceResult<HojaCirugiaResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar cirugía {Id}", id);
            return ServiceResult<HojaCirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Cirugías de una hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<HojaCirugiaResponseDto>>> GetByHojaCitaIdAsync(
        Guid clinicaId, Guid hojaCitaId)
    {
        try
        {
            _logger.LogInformation("Buscando cirugías de la hoja de cita {HojaCitaId}", hojaCitaId);

            var entities = await _repo.GetByHojaCitaIdAsync(clinicaId, hojaCitaId);
            var dtos = new List<HojaCirugiaResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<HojaCirugiaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cirugías de la hoja de cita {HojaCitaId}", hojaCitaId);
            return ServiceResult<IEnumerable<HojaCirugiaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea una nueva cirugía en la hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaCirugiaResponseDto>> CreateAsync(
        HojaCirugiaRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando cirugía en hoja de cita {HojaCitaId}", dto.HojaCitaId);

            var entity = new HojaCirugia
            {
                ClinicaId = clinicaId,
                HojaCitaId = dto.HojaCitaId,
                CirugiaId = dto.CirugiaId,
                FechaCirugia = dto.FechaCirugia,
                Observaciones = dto.Observaciones,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Cirugía creada con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<HojaCirugiaResponseDto>.Failure(
                    "Cirugía creada pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaCirugiaResponseDto>.Success(
                MapToDto(created), "Cirugía agregada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cirugía en clínica {ClinicaId}", clinicaId);
            return ServiceResult<HojaCirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza una cirugía existente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaCirugiaResponseDto>> UpdateAsync(
        Guid id, HojaCirugiaRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<HojaCirugiaResponseDto>.Failure(
                    "Cirugía no encontrada", ServiceErrorType.NotFound);
            }

            existing.CirugiaId = dto.CirugiaId;
            existing.FechaCirugia = dto.FechaCirugia;
            existing.Observaciones = dto.Observaciones;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<HojaCirugiaResponseDto>.Failure(
                    "No se pudo actualizar la cirugía.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(clinicaId, id);
            if (refreshed == null)
            {
                return ServiceResult<HojaCirugiaResponseDto>.Failure(
                    "Cirugía actualizada pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaCirugiaResponseDto>.Success(
                MapToDto(refreshed), "Cirugía actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cirugía {Id}", id);
            return ServiceResult<HojaCirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva cirugía (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Cirugía no encontrada", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La cirugía ya está inactiva.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar la cirugía.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Cirugía desactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar cirugía {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static HojaCirugiaResponseDto MapToDto(HojaCirugia entity)
    {
        return new HojaCirugiaResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            HojaCitaId = entity.HojaCitaId,
            CirugiaId = entity.CirugiaId,
            FechaCirugia = entity.FechaCirugia,
            Observaciones = entity.Observaciones,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            CirugiaNombre = entity.CirugiaNombre,
            TipoCirugiaNombre = entity.TipoCirugiaNombre
        };
    }
}
