using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Repositories;
using Vittal.DTO.Examen;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de exámenes. Implementa IExamenService.
/// Historia de Usuario: HU17 — Gestión de Exámenes
/// </summary>
public class ExamenService : IExamenService
{
    private readonly IExamenRepository _repo;
    private readonly ILogger<ExamenService> _logger;

    public ExamenService(IExamenRepository repo, ILogger<ExamenService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista exámenes de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<ExamenResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo exámenes de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<ExamenResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<ExamenResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener exámenes de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<ExamenResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un examen por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExamenResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando examen {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<ExamenResponseDto>.Failure(
                    "Examen no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<ExamenResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar examen {Id}", id);
            return ServiceResult<ExamenResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo examen
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExamenResponseDto>> CreateAsync(
        ExamenRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando examen {Nombre} en clínica {ClinicaId}",
                dto.Nombre, clinicaId);

            // Validate uniqueness of name
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre))
            {
                return ServiceResult<ExamenResponseDto>.Failure(
                    "Ya existe un examen con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            var entity = new Examen
            {
                ClinicaId = clinicaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Examen creado con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<ExamenResponseDto>.Failure(
                    "Examen creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<ExamenResponseDto>.Success(
                MapToDto(created), "Examen creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear examen en clínica {ClinicaId}", clinicaId);
            return ServiceResult<ExamenResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del examen
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ExamenResponseDto>> UpdateAsync(
        Guid id, ExamenRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando examen {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<ExamenResponseDto>.Failure(
                    "Examen no encontrado", ServiceErrorType.NotFound);
            }

            // Validate uniqueness (exclude current)
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre, id))
            {
                return ServiceResult<ExamenResponseDto>.Failure(
                    "Ya existe otro examen con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            // Update entity fields
            existing.Nombre = dto.Nombre;
            existing.Descripcion = dto.Descripcion;
            existing.ModificadoPor = modificadoPor;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<ExamenResponseDto>.Failure(
                    "No se pudo actualizar el examen.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<ExamenResponseDto>.Failure(
                    "Examen actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<ExamenResponseDto>.Success(
                MapToDto(refreshed), "Examen actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar examen {Id}", id);
            return ServiceResult<ExamenResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva examen (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando examen {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
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

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
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
    // 6. ReactivateAsync — Reactiva examen (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando examen {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Examen no encontrado", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El examen ya está activo.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar el examen.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Examen reactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar examen {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de exámenes por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<ExamenResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando exámenes con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<ExamenResponseDto>>.Success(
                    new List<ExamenResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            // Get all active exams and filter in-memory
            var entities = await _repo.GetAllAsync(clinicaId);
            var lowerTerm = term.ToLowerInvariant();

            var filtered = new List<ExamenResponseDto>();
            foreach (var entity in entities)
            {
                var hasNombre = entity.Nombre.ToLowerInvariant().Contains(lowerTerm);
                var hasDescripcion = entity.Descripcion?.ToLowerInvariant().Contains(lowerTerm) ?? false;

                if (hasNombre || hasDescripcion)
                {
                    filtered.Add(MapToDto(entity));
                }
            }

            return ServiceResult<IEnumerable<ExamenResponseDto>>.Success(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar exámenes en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<ExamenResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static ExamenResponseDto MapToDto(Examen e)
    {
        return new ExamenResponseDto
        {
            Id = e.Id,
            ClinicaId = e.ClinicaId,
            Nombre = e.Nombre,
            Descripcion = e.Descripcion,
            Activo = e.Activo,
            FechaCreacion = e.FechaCreacion,
            FechaModificacion = e.FechaModificacion
        };
    }
}
