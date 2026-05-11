using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Repositories;
using Vittal.DTO.Tratamiento;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de tratamientos. Implementa ITratamientoService.
/// Historia de Usuario: HU15 — Gestión de Tratamientos
/// </summary>
public class TratamientoService : ITratamientoService
{
    private readonly ITratamientoRepository _repo;
    private readonly ILogger<TratamientoService> _logger;

    public TratamientoService(ITratamientoRepository repo, ILogger<TratamientoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista tratamientos de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<TratamientoResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo tratamientos de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<TratamientoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<TratamientoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tratamientos de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<TratamientoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un tratamiento por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TratamientoResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando tratamiento {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<TratamientoResponseDto>.Failure(
                    "Tratamiento no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<TratamientoResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar tratamiento {Id}", id);
            return ServiceResult<TratamientoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo tratamiento
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TratamientoResponseDto>> CreateAsync(
        TratamientoRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando tratamiento {Nombre} en clínica {ClinicaId}",
                dto.Nombre, clinicaId);

            // Validate uniqueness of name
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre))
            {
                return ServiceResult<TratamientoResponseDto>.Failure(
                    "Ya existe un tratamiento con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            var entity = new Tratamiento
            {
                ClinicaId = clinicaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Tratamiento creado con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<TratamientoResponseDto>.Failure(
                    "Tratamiento creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<TratamientoResponseDto>.Success(
                MapToDto(created), "Tratamiento creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tratamiento en clínica {ClinicaId}", clinicaId);
            return ServiceResult<TratamientoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del tratamiento
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TratamientoResponseDto>> UpdateAsync(
        Guid id, TratamientoRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando tratamiento {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<TratamientoResponseDto>.Failure(
                    "Tratamiento no encontrado", ServiceErrorType.NotFound);
            }

            // Validate uniqueness (exclude current)
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre, id))
            {
                return ServiceResult<TratamientoResponseDto>.Failure(
                    "Ya existe otro tratamiento con ese nombre en esta clínica.",
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
                return ServiceResult<TratamientoResponseDto>.Failure(
                    "No se pudo actualizar el tratamiento.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<TratamientoResponseDto>.Failure(
                    "Tratamiento actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<TratamientoResponseDto>.Success(
                MapToDto(refreshed), "Tratamiento actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tratamiento {Id}", id);
            return ServiceResult<TratamientoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva tratamiento (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando tratamiento {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
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

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
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
    // 6. ReactivateAsync — Reactiva tratamiento (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando tratamiento {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Tratamiento no encontrado", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El tratamiento ya está activo.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar el tratamiento.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Tratamiento reactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar tratamiento {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de tratamientos por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<TratamientoResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando tratamientos con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<TratamientoResponseDto>>.Success(
                    new List<TratamientoResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            // Get all active treatments and filter in-memory
            var entities = await _repo.GetAllAsync(clinicaId);
            var lowerTerm = term.ToLowerInvariant();

            var filtered = new List<TratamientoResponseDto>();
            foreach (var entity in entities)
            {
                var hasNombre = entity.Nombre.ToLowerInvariant().Contains(lowerTerm);
                var hasDescripcion = entity.Descripcion?.ToLowerInvariant().Contains(lowerTerm) ?? false;

                if (hasNombre || hasDescripcion)
                {
                    filtered.Add(MapToDto(entity));
                }
            }

            return ServiceResult<IEnumerable<TratamientoResponseDto>>.Success(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar tratamientos en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<TratamientoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static TratamientoResponseDto MapToDto(Tratamiento t)
    {
        return new TratamientoResponseDto
        {
            Id = t.Id,
            ClinicaId = t.ClinicaId,
            Nombre = t.Nombre,
            Descripcion = t.Descripcion,
            Activo = t.Activo,
            FechaCreacion = t.FechaCreacion,
            FechaModificacion = t.FechaModificacion
        };
    }
}
