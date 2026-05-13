using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.HojaTratamiento;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de tratamientos y medicamentos en hojas de cita. Implementa IHojaTratamientoService.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaTratamientoService : IHojaTratamientoService
{
    private readonly IHojaTratamientoRepository _repo;
    private readonly ILogger<HojaTratamientoService> _logger;

    public HojaTratamientoService(IHojaTratamientoRepository repo, ILogger<HojaTratamientoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Detalle de un tratamiento por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaTratamientoResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando tratamiento {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<HojaTratamientoResponseDto>.Failure(
                    "Tratamiento no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<HojaTratamientoResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar tratamiento {Id}", id);
            return ServiceResult<HojaTratamientoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Tratamientos de una hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<HojaTratamientoResponseDto>>> GetByHojaCitaIdAsync(
        Guid clinicaId, Guid hojaCitaId)
    {
        try
        {
            _logger.LogInformation("Buscando tratamientos de la hoja de cita {HojaCitaId}", hojaCitaId);

            var entities = await _repo.GetByHojaCitaIdAsync(clinicaId, hojaCitaId);
            var dtos = new List<HojaTratamientoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<HojaTratamientoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tratamientos de la hoja de cita {HojaCitaId}", hojaCitaId);
            return ServiceResult<IEnumerable<HojaTratamientoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo tratamiento en la hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaTratamientoResponseDto>> CreateAsync(
        HojaTratamientoRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando tratamiento en hoja de cita {HojaCitaId}", dto.HojaCitaId);

            var entity = new HojaTratamiento
            {
                ClinicaId = clinicaId,
                HojaCitaId = dto.HojaCitaId,
                MedicamentoId = dto.MedicamentoId,
                TratamientoId = dto.TratamientoId,
                Dosis = dto.Dosis,
                Frecuencia = dto.Frecuencia,
                Duracion = dto.Duracion,
                Instrucciones = dto.Instrucciones,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Tratamiento creado con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<HojaTratamientoResponseDto>.Failure(
                    "Tratamiento creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaTratamientoResponseDto>.Success(
                MapToDto(created), "Tratamiento agregado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tratamiento en clínica {ClinicaId}", clinicaId);
            return ServiceResult<HojaTratamientoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza un tratamiento existente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaTratamientoResponseDto>> UpdateAsync(
        Guid id, HojaTratamientoRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando tratamiento {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<HojaTratamientoResponseDto>.Failure(
                    "Tratamiento no encontrado", ServiceErrorType.NotFound);
            }

            existing.MedicamentoId = dto.MedicamentoId;
            existing.TratamientoId = dto.TratamientoId;
            existing.Dosis = dto.Dosis;
            existing.Frecuencia = dto.Frecuencia;
            existing.Duracion = dto.Duracion;
            existing.Instrucciones = dto.Instrucciones;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<HojaTratamientoResponseDto>.Failure(
                    "No se pudo actualizar el tratamiento.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(clinicaId, id);
            if (refreshed == null)
            {
                return ServiceResult<HojaTratamientoResponseDto>.Failure(
                    "Tratamiento actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaTratamientoResponseDto>.Success(
                MapToDto(refreshed), "Tratamiento actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tratamiento {Id}", id);
            return ServiceResult<HojaTratamientoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva tratamiento (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando tratamiento {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Tratamiento no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El tratamiento ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el tratamiento.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Tratamiento desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar tratamiento {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static HojaTratamientoResponseDto MapToDto(HojaTratamiento entity)
    {
        return new HojaTratamientoResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            HojaCitaId = entity.HojaCitaId,
            MedicamentoId = entity.MedicamentoId,
            TratamientoId = entity.TratamientoId,
            Dosis = entity.Dosis,
            Frecuencia = entity.Frecuencia,
            Duracion = entity.Duracion,
            Instrucciones = entity.Instrucciones,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            MedicamentoNombre = entity.MedicamentoNombre,
            TratamientoNombre = entity.TratamientoNombre
        };
    }
}
