using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Recomendacion;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de recomendaciones. Implementa IRecomendacionService.
/// Historia de Usuario: HU16 — Gestión de Recomendaciones
/// </summary>
public class RecomendacionService : IRecomendacionService
{
    private readonly IRecomendacionRepository _repo;
    private readonly ILogger<RecomendacionService> _logger;

    public RecomendacionService(IRecomendacionRepository repo, ILogger<RecomendacionService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista recomendaciones de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<RecomendacionResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo recomendaciones de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<RecomendacionResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<RecomendacionResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener recomendaciones de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<RecomendacionResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de una recomendación por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<RecomendacionResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando recomendación {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<RecomendacionResponseDto>.Failure(
                    "Recomendación no encontrada", ServiceErrorType.NotFound);
            }

            return ServiceResult<RecomendacionResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar recomendación {Id}", id);
            return ServiceResult<RecomendacionResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea una nueva recomendación
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<RecomendacionResponseDto>> CreateAsync(
        RecomendacionRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando recomendación {Nombre} en clínica {ClinicaId}",
                dto.Nombre, clinicaId);

            // Validate uniqueness of name
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre))
            {
                return ServiceResult<RecomendacionResponseDto>.Failure(
                    "Ya existe una recomendación con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            var entity = new Recomendacion
            {
                ClinicaId = clinicaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Recomendación creada con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<RecomendacionResponseDto>.Failure(
                    "Recomendación creada pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<RecomendacionResponseDto>.Success(
                MapToDto(created), "Recomendación creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear recomendación en clínica {ClinicaId}", clinicaId);
            return ServiceResult<RecomendacionResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos de la recomendación
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<RecomendacionResponseDto>> UpdateAsync(
        Guid id, RecomendacionRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando recomendación {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<RecomendacionResponseDto>.Failure(
                    "Recomendación no encontrada", ServiceErrorType.NotFound);
            }

            // Validate uniqueness (exclude current)
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre, id))
            {
                return ServiceResult<RecomendacionResponseDto>.Failure(
                    "Ya existe otra recomendación con ese nombre en esta clínica.",
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
                return ServiceResult<RecomendacionResponseDto>.Failure(
                    "No se pudo actualizar la recomendación.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<RecomendacionResponseDto>.Failure(
                    "Recomendación actualizada pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<RecomendacionResponseDto>.Success(
                MapToDto(refreshed), "Recomendación actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar recomendación {Id}", id);
            return ServiceResult<RecomendacionResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva recomendación (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando recomendación {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
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

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
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
    // 6. ReactivateAsync — Reactiva recomendación (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando recomendación {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Recomendación no encontrada", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La recomendación ya está activa.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar la recomendación.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Recomendación reactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar recomendación {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de recomendaciones por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<RecomendacionResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando recomendaciones con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<RecomendacionResponseDto>>.Success(
                    new List<RecomendacionResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            // Get all active recommendations and filter in-memory
            var entities = await _repo.GetAllAsync(clinicaId);
            var lowerTerm = term.ToLowerInvariant();

            var filtered = new List<RecomendacionResponseDto>();
            foreach (var entity in entities)
            {
                var hasNombre = entity.Nombre.ToLowerInvariant().Contains(lowerTerm);
                var hasDescripcion = entity.Descripcion?.ToLowerInvariant().Contains(lowerTerm) ?? false;

                if (hasNombre || hasDescripcion)
                {
                    filtered.Add(MapToDto(entity));
                }
            }

            return ServiceResult<IEnumerable<RecomendacionResponseDto>>.Success(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar recomendaciones en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<RecomendacionResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static RecomendacionResponseDto MapToDto(Recomendacion r)
    {
        return new RecomendacionResponseDto
        {
            Id = r.Id,
            ClinicaId = r.ClinicaId,
            Nombre = r.Nombre,
            Descripcion = r.Descripcion,
            Activo = r.Activo,
            FechaCreacion = r.FechaCreacion,
            FechaModificacion = r.FechaModificacion
        };
    }
}
