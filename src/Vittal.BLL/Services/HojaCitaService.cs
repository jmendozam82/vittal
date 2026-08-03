using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.HojaCita;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de hojas de cita médica. Implementa IHojaCitaService.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCitaService : IHojaCitaService
{
    private readonly IHojaCitaRepository _repo;
    private readonly ILogger<HojaCitaService> _logger;

    public HojaCitaService(IHojaCitaRepository repo, ILogger<HojaCitaService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista hojas de cita activas
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<HojaCitaResponseDto>>> GetAllAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo hojas de cita de la clínica {ClinicaId}", clinicaId);

            var entities = await _repo.GetAllAsync(clinicaId);
            var dtos = new List<HojaCitaResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<HojaCitaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener hojas de cita de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<HojaCitaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de una hoja de cita por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaCitaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando hoja de cita {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<HojaCitaResponseDto>.Failure(
                    "Hoja de cita no encontrada", ServiceErrorType.NotFound);
            }

            return ServiceResult<HojaCitaResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar hoja de cita {Id}", id);
            return ServiceResult<HojaCitaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. GetByExpedienteIdAsync — Hojas de cita de un expediente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<HojaCitaResponseDto>>> GetByExpedienteIdAsync(
        Guid clinicaId, Guid expedienteId)
    {
        try
        {
            _logger.LogInformation("Buscando hojas de cita del expediente {ExpedienteId}", expedienteId);

            var entities = await _repo.GetByExpedienteIdAsync(clinicaId, expedienteId);
            var dtos = new List<HojaCitaResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<HojaCitaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener hojas de cita del expediente {ExpedienteId}", expedienteId);
            return ServiceResult<IEnumerable<HojaCitaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. CreateAsync — Crea una nueva hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaCitaResponseDto>> CreateAsync(
        HojaCitaRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando hoja de cita para expediente {ExpedienteId}", dto.ExpedienteId);

            // Si la FechaConsulta es solo fecha (00:00:00), normalizar con hora actual del servidor
            var fechaConsulta = NormalizarFechaConsulta(dto.FechaConsulta) ?? DateTime.UtcNow;

            var entity = new HojaCita
            {
                ClinicaId = clinicaId,
                ExpedienteId = dto.ExpedienteId,
                CitaId = dto.CitaId,
                DoctorId = dto.DoctorId,
                FechaConsulta = fechaConsulta,
                MotivoConsulta = dto.MotivoConsulta,
                NotasConsulta = dto.NotasConsulta,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Hoja de cita creada con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<HojaCitaResponseDto>.Failure(
                    "Hoja de cita creada pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaCitaResponseDto>.Success(
                MapToDto(created), "Hoja de cita creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear hoja de cita en clínica {ClinicaId}", clinicaId);
            return ServiceResult<HojaCitaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. UpdateAsync — Actualiza datos de la hoja de cita
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<HojaCitaResponseDto>> UpdateAsync(
        Guid id, HojaCitaRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando hoja de cita {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<HojaCitaResponseDto>.Failure(
                    "Hoja de cita no encontrada", ServiceErrorType.NotFound);
            }

            existing.CitaId = dto.CitaId;
            existing.DoctorId = dto.DoctorId;
            
            // Si el cliente proporcionó una fecha Consulta, normalizarla
            var fechaConsulta = NormalizarFechaConsulta(dto.FechaConsulta);
            existing.FechaConsulta = fechaConsulta ?? existing.FechaConsulta;
            
            existing.MotivoConsulta = dto.MotivoConsulta;
            existing.NotasConsulta = dto.NotasConsulta;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<HojaCitaResponseDto>.Failure(
                    "No se pudo actualizar la hoja de cita.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(clinicaId, id);
            if (refreshed == null)
            {
                return ServiceResult<HojaCitaResponseDto>.Failure(
                    "Hoja de cita actualizada pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<HojaCitaResponseDto>.Success(
                MapToDto(refreshed), "Hoja de cita actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar hoja de cita {Id}", id);
            return ServiceResult<HojaCitaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helper: Normaliza FechaConsulta cuando el cliente envía solo una fecha
    // (input type="date" viene como 2026-08-02 → DateTime 2026-08-02 00:00:00)
    // Devuelve null cuando el cliente NO envió fecha (para conservar la existente en Update).
    // ────────────────────────────────────────────────────────────────────────
    private DateTime? NormalizarFechaConsulta(DateTime? fechaConsulta)
    {
        if (!fechaConsulta.HasValue)
        {
            return null;
        }

        // Si la hora es exactamente medianoche (00:00:00), es probable que el cliente envió solo una fecha
        if (fechaConsulta.Value.TimeOfDay == TimeSpan.Zero)
        {
            // Usar la hora actual del servidor en UTC. La BD usa TIMESTAMPTZ (siempre UTC),
            // y el navegador convierte automáticamente a la zona local (UTC−6 → 20:32 local).
            return DateTime.UtcNow;
        }

        return fechaConsulta.Value;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. DeactivateAsync — Desactiva hoja de cita (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando hoja de cita {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Hoja de cita no encontrada", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La hoja de cita ya está inactiva.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar la hoja de cita.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Hoja de cita desactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar hoja de cita {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static HojaCitaResponseDto MapToDto(HojaCita entity)
    {
        return new HojaCitaResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            ExpedienteId = entity.ExpedienteId,
            CitaId = entity.CitaId,
            DoctorId = entity.DoctorId,
            FechaConsulta = entity.FechaConsulta,
            MotivoConsulta = entity.MotivoConsulta,
            NotasConsulta = entity.NotasConsulta,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            FechaModificacion = entity.FechaModificacion,
            PacienteNombre = entity.PacienteNombre,
            DoctorNombre = entity.DoctorNombre
        };
    }
}
