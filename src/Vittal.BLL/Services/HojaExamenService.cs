using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.HojaExamen;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de exámenes en hojas de cita. Implementa IHojaExamenService.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaExamenService : IHojaExamenService
{
    private readonly IHojaExamenRepository _repo;
    private readonly ILogger<HojaExamenService> _logger;

    public HojaExamenService(IHojaExamenRepository repo, ILogger<HojaExamenService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Detalle de un examen por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaExamenResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando examen {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<HojaExamenResponseDto>.Failure(
                    "Examen no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<HojaExamenResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar examen {Id}", id);
            return ServiceResult<HojaExamenResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Exámenes de una hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<HojaExamenResponseDto>>> GetByHojaCitaIdAsync(
        Guid clinicaId, Guid hojaCitaId)
    {
        try
        {
            _logger.LogInformation("Buscando exámenes de la hoja de cita {HojaCitaId}", hojaCitaId);

            var entities = await _repo.GetByHojaCitaIdAsync(clinicaId, hojaCitaId);
            var dtos = new List<HojaExamenResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<HojaExamenResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener exámenes de la hoja de cita {HojaCitaId}", hojaCitaId);
            return ServiceResult<IEnumerable<HojaExamenResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo examen en la hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaExamenResponseDto>> CreateAsync(
        HojaExamenRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando examen en hoja de cita {HojaCitaId}", dto.HojaCitaId);

            var entity = new HojaExamen
            {
                ClinicaId = clinicaId,
                HojaCitaId = dto.HojaCitaId,
                ExamenId = dto.ExamenId,
                Resultado = dto.Resultado,
                ArchivoUrl = dto.ArchivoUrl,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Examen creado con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<HojaExamenResponseDto>.Failure(
                    "Examen creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaExamenResponseDto>.Success(
                MapToDto(created), "Examen agregado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear examen en clínica {ClinicaId}", clinicaId);
            return ServiceResult<HojaExamenResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza un examen existente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaExamenResponseDto>> UpdateAsync(
        Guid id, HojaExamenRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando examen {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<HojaExamenResponseDto>.Failure(
                    "Examen no encontrado", ServiceErrorType.NotFound);
            }

            existing.ExamenId = dto.ExamenId;
            existing.Resultado = dto.Resultado;
            existing.ArchivoUrl = dto.ArchivoUrl;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<HojaExamenResponseDto>.Failure(
                    "No se pudo actualizar el examen.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(clinicaId, id);
            if (refreshed == null)
            {
                return ServiceResult<HojaExamenResponseDto>.Failure(
                    "Examen actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaExamenResponseDto>.Success(
                MapToDto(refreshed), "Examen actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar examen {Id}", id);
            return ServiceResult<HojaExamenResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva examen (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando examen {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Examen no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El examen ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el examen.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Examen desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar examen {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static HojaExamenResponseDto MapToDto(HojaExamen entity)
    {
        return new HojaExamenResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            HojaCitaId = entity.HojaCitaId,
            ExamenId = entity.ExamenId,
            Resultado = entity.Resultado,
            ArchivoUrl = entity.ArchivoUrl,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            ExamenNombre = entity.ExamenNombre
        };
    }
}
