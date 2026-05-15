using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.TipoCirugia;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de tipos de cirugía. Implementa ITipoCirugiaService.
/// Historia de Usuario: HU11 — Gestión de Tipos de Cirugías
/// </summary>
public class TipoCirugiaService : ITipoCirugiaService
{
    private readonly ITipoCirugiaRepository _repo;
    private readonly ILogger<TipoCirugiaService> _logger;

    public TipoCirugiaService(ITipoCirugiaRepository repo, ILogger<TipoCirugiaService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista tipos de cirugía de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<TipoCirugiaResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo tipos de cirugía de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<TipoCirugiaResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<TipoCirugiaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipos de cirugía de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<TipoCirugiaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un tipo de cirugía por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TipoCirugiaResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando tipo de cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<TipoCirugiaResponseDto>.Failure(
                    "Tipo de cirugía no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<TipoCirugiaResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar tipo de cirugía {Id}", id);
            return ServiceResult<TipoCirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo tipo de cirugía
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TipoCirugiaResponseDto>> CreateAsync(
        TipoCirugiaRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando tipo de cirugía {Nombre} en clínica {ClinicaId}",
                dto.Nombre, clinicaId);

            // Validate uniqueness of name
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre))
            {
                return ServiceResult<TipoCirugiaResponseDto>.Failure(
                    "Ya existe un tipo de cirugía con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            var entity = new TipoCirugia
            {
                ClinicaId = clinicaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Tipo de cirugía creado con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<TipoCirugiaResponseDto>.Failure(
                    "Tipo de cirugía creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<TipoCirugiaResponseDto>.Success(
                MapToDto(created), "Tipo de cirugía creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de cirugía en clínica {ClinicaId}", clinicaId);
            return ServiceResult<TipoCirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del tipo de cirugía
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TipoCirugiaResponseDto>> UpdateAsync(
        Guid id, TipoCirugiaRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando tipo de cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<TipoCirugiaResponseDto>.Failure(
                    "Tipo de cirugía no encontrado", ServiceErrorType.NotFound);
            }

            // Validate uniqueness (exclude current)
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre, id))
            {
                return ServiceResult<TipoCirugiaResponseDto>.Failure(
                    "Ya existe otro tipo de cirugía con ese nombre en esta clínica.",
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
                return ServiceResult<TipoCirugiaResponseDto>.Failure(
                    "No se pudo actualizar el tipo de cirugía.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<TipoCirugiaResponseDto>.Failure(
                    "Tipo de cirugía actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<TipoCirugiaResponseDto>.Success(
                MapToDto(refreshed), "Tipo de cirugía actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tipo de cirugía {Id}", id);
            return ServiceResult<TipoCirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva tipo de cirugía (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando tipo de cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Tipo de cirugía no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El tipo de cirugía ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el tipo de cirugía.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Tipo de cirugía desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar tipo de cirugía {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. ReactivateAsync — Reactiva tipo de cirugía (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando tipo de cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Tipo de cirugía no encontrado", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El tipo de cirugía ya está activo.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar el tipo de cirugía.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Tipo de cirugía reactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar tipo de cirugía {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de tipos de cirugía por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<TipoCirugiaResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando tipos de cirugía con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<TipoCirugiaResponseDto>>.Success(
                    new List<TipoCirugiaResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            // Get all types and filter in-memory
            var entities = await _repo.GetAllAsync(clinicaId);
            var lowerTerm = term.ToLowerInvariant();

            var filtered = new List<TipoCirugiaResponseDto>();
            foreach (var entity in entities)
            {
                var nombreLower = entity.Nombre.ToLowerInvariant();
                var hasDescripcion = entity.Descripcion?.ToLowerInvariant().Contains(lowerTerm) ?? false;

                if (nombreLower.Contains(lowerTerm) || hasDescripcion)
                {
                    filtered.Add(MapToDto(entity));
                }
            }

            return ServiceResult<IEnumerable<TipoCirugiaResponseDto>>.Success(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar tipos de cirugía en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<TipoCirugiaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static TipoCirugiaResponseDto MapToDto(TipoCirugia t)
    {
        return new TipoCirugiaResponseDto
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
