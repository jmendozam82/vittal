using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Expediente;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de expedientes médicos. Implementa IExpedienteService.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteService : IExpedienteService
{
    private readonly IExpedienteRepository _repo;
    private readonly ILogger<ExpedienteService> _logger;

    public ExpedienteService(IExpedienteRepository repo, ILogger<ExpedienteService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista expedientes activos de la clínica.
    //    Si doctorId no es null, filtra por doctor (regla 6).
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<ExpedienteResponseDto>>> GetAllAsync(Guid clinicaId, Guid? doctorId = null)
    {
        try
        {
            _logger.LogInformation("Obteniendo expedientes de la clínica {ClinicaId} (doctor: {DoctorId})",
                clinicaId, doctorId?.ToString() ?? "todos");

            var entities = await _repo.GetAllAsync(clinicaId, doctorId);
            var dtos = new List<ExpedienteResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<ExpedienteResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener expedientes de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<ExpedienteResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un expediente por ID.
    //    Si doctorId no es null, valida que el expediente sea del doctor.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteResponseDto>> GetByIdAsync(Guid clinicaId, Guid id, Guid? doctorId = null)
    {
        try
        {
            _logger.LogInformation("Buscando expediente {Id} en clínica {ClinicaId} (doctor: {DoctorId})",
                id, clinicaId, doctorId?.ToString() ?? "todos");

            var entity = await _repo.GetByIdAsync(clinicaId, id, doctorId);
            if (entity == null)
            {
                return ServiceResult<ExpedienteResponseDto>.Failure(
                    "Expediente no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<ExpedienteResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar expediente {Id}", id);
            return ServiceResult<ExpedienteResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. GetByPacienteIdAsync — Obtiene expediente activo de un paciente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteResponseDto>> GetByPacienteIdAsync(Guid clinicaId, Guid pacienteId)
    {
        try
        {
            _logger.LogInformation("Buscando expediente del paciente {PacienteId} en clínica {ClinicaId}",
                pacienteId, clinicaId);

            var entity = await _repo.GetByPacienteIdAsync(clinicaId, pacienteId);
            if (entity == null)
            {
                return ServiceResult<ExpedienteResponseDto>.Failure(
                    "El paciente no tiene expediente", ServiceErrorType.NotFound);
            }

            return ServiceResult<ExpedienteResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar expediente del paciente {PacienteId}", pacienteId);
            return ServiceResult<ExpedienteResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. CreateAsync — Crea un nuevo expediente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteResponseDto>> CreateAsync(
        ExpedienteRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando expediente para paciente {PacienteId} en clínica {ClinicaId}",
                dto.PacienteId, clinicaId);

            // Validar que el paciente no tenga ya un expediente activo
            var existing = await _repo.GetByPacienteIdAsync(clinicaId, dto.PacienteId);
            if (existing != null)
            {
                return ServiceResult<ExpedienteResponseDto>.Failure(
                    "El paciente ya tiene un expediente activo.", ServiceErrorType.Conflict);
            }

            var entity = new Expediente
            {
                ClinicaId = clinicaId,
                PacienteId = dto.PacienteId,
                DoctorId = dto.DoctorId,
                NotasGenerales = dto.NotasGenerales,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Expediente creado con ID: {NewId}", newId);

            var created = await _repo.GetByIdAsync(clinicaId, newId);
            if (created == null)
            {
                return ServiceResult<ExpedienteResponseDto>.Failure(
                    "Expediente creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<ExpedienteResponseDto>.Success(
                MapToDto(created), "Expediente creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear expediente en clínica {ClinicaId}", clinicaId);
            return ServiceResult<ExpedienteResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. UpdateAsync — Actualiza datos del expediente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExpedienteResponseDto>> UpdateAsync(
        Guid id, ExpedienteRequestDto dto, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Actualizando expediente {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<ExpedienteResponseDto>.Failure(
                    "Expediente no encontrado", ServiceErrorType.NotFound);
            }

            existing.DoctorId = dto.DoctorId;
            existing.NotasGenerales = dto.NotasGenerales;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<ExpedienteResponseDto>.Failure(
                    "No se pudo actualizar el expediente.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(clinicaId, id);
            if (refreshed == null)
            {
                return ServiceResult<ExpedienteResponseDto>.Failure(
                    "Expediente actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<ExpedienteResponseDto>.Success(
                MapToDto(refreshed), "Expediente actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar expediente {Id}", id);
            return ServiceResult<ExpedienteResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. DeactivateAsync — Desactiva expediente (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando expediente {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(clinicaId, id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Expediente no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El expediente ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(clinicaId, id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el expediente.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Expediente desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar expediente {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static ExpedienteResponseDto MapToDto(Expediente entity)
    {
        return new ExpedienteResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            PacienteId = entity.PacienteId,
            DoctorId = entity.DoctorId,
            NotasGenerales = entity.NotasGenerales,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            FechaModificacion = entity.FechaModificacion,
            PacienteNombre = entity.PacienteNombre,
            DoctorNombre = entity.DoctorNombre
        };
    }
}
