using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.HojaDiagnostico;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de diagnósticos en hojas de cita. Implementa IHojaDiagnosticoService.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaDiagnosticoService : IHojaDiagnosticoService
{
    private readonly IHojaDiagnosticoRepository _repo;
    private readonly ILogger<HojaDiagnosticoService> _logger;

    public HojaDiagnosticoService(IHojaDiagnosticoRepository repo, ILogger<HojaDiagnosticoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Detalle de un diagnóstico por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaDiagnosticoResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<HojaDiagnosticoResponseDto>.Failure(
                    "Diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<HojaDiagnosticoResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar diagnóstico {Id}", id);
            return ServiceResult<HojaDiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Diagnósticos de una hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<HojaDiagnosticoResponseDto>>> GetByHojaCitaIdAsync(
        Guid clinicaId, Guid hojaCitaId)
    {
        try
        {
            _logger.LogInformation("Buscando diagnósticos de la hoja de cita {HojaCitaId}", hojaCitaId);

            var entities = await _repo.GetByHojaCitaIdAsync(clinicaId, hojaCitaId);
            var dtos = new List<HojaDiagnosticoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<HojaDiagnosticoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener diagnósticos de la hoja de cita {HojaCitaId}", hojaCitaId);
            return ServiceResult<IEnumerable<HojaDiagnosticoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo diagnóstico en la hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaDiagnosticoResponseDto>> CreateAsync(
        HojaDiagnosticoRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando diagnóstico en hoja de cita {HojaCitaId}", dto.HojaCitaId);

            var entity = new HojaDiagnostico
            {
                ClinicaId = clinicaId,
                HojaCitaId = dto.HojaCitaId,
                DiagnosticoId = dto.DiagnosticoId,
                Observaciones = dto.Observaciones,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Diagnóstico creado con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<HojaDiagnosticoResponseDto>.Failure(
                    "Diagnóstico creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaDiagnosticoResponseDto>.Success(
                MapToDto(created), "Diagnóstico agregado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear diagnóstico en clínica {ClinicaId}", clinicaId);
            return ServiceResult<HojaDiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza un diagnóstico existente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaDiagnosticoResponseDto>> UpdateAsync(
        Guid id, HojaDiagnosticoRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<HojaDiagnosticoResponseDto>.Failure(
                    "Diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            existing.DiagnosticoId = dto.DiagnosticoId;
            existing.Observaciones = dto.Observaciones;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<HojaDiagnosticoResponseDto>.Failure(
                    "No se pudo actualizar el diagnóstico.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(clinicaId, id);
            if (refreshed == null)
            {
                return ServiceResult<HojaDiagnosticoResponseDto>.Failure(
                    "Diagnóstico actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaDiagnosticoResponseDto>.Success(
                MapToDto(refreshed), "Diagnóstico actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar diagnóstico {Id}", id);
            return ServiceResult<HojaDiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva diagnóstico (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El diagnóstico ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el diagnóstico.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Diagnóstico desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar diagnóstico {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static HojaDiagnosticoResponseDto MapToDto(HojaDiagnostico entity)
    {
        return new HojaDiagnosticoResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            HojaCitaId = entity.HojaCitaId,
            DiagnosticoId = entity.DiagnosticoId,
            Observaciones = entity.Observaciones,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            DiagnosticoNombre = entity.DiagnosticoNombre,
            TipoDiagnosticoNombre = entity.TipoDiagnosticoNombre
        };
    }
}
