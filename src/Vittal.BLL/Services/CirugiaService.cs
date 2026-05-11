using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Repositories;
using Vittal.DTO.Cirugia;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de cirugías. Implementa ICirugiaService.
/// Historia de Usuario: HU12 — Gestión de Cirugías
/// </summary>
public class CirugiaService : ICirugiaService
{
    private readonly ICirugiaRepository _repo;
    private readonly ILogger<CirugiaService> _logger;

    public CirugiaService(ICirugiaRepository repo, ILogger<CirugiaService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista cirugías de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<CirugiaResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo cirugías de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<CirugiaResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<CirugiaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cirugías de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<CirugiaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de una cirugía por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<CirugiaResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<CirugiaResponseDto>.Failure(
                    "Cirugía no encontrada", ServiceErrorType.NotFound);
            }

            return ServiceResult<CirugiaResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar cirugía {Id}", id);
            return ServiceResult<CirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea una nueva cirugía
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<CirugiaResponseDto>> CreateAsync(
        CirugiaRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando cirugía {Nombre} en clínica {ClinicaId}",
                dto.Nombre, clinicaId);

            // Validate uniqueness of name
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre))
            {
                return ServiceResult<CirugiaResponseDto>.Failure(
                    "Ya existe una cirugía con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            var entity = new Cirugia
            {
                ClinicaId = clinicaId,
                TipoCirugiaId = dto.TipoCirugiaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Cirugía creada con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<CirugiaResponseDto>.Failure(
                    "Cirugía creada pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<CirugiaResponseDto>.Success(
                MapToDto(created), "Cirugía creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cirugía en clínica {ClinicaId}", clinicaId);
            return ServiceResult<CirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos de la cirugía
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<CirugiaResponseDto>> UpdateAsync(
        Guid id, CirugiaRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<CirugiaResponseDto>.Failure(
                    "Cirugía no encontrada", ServiceErrorType.NotFound);
            }

            // Validate uniqueness (exclude current)
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre, id))
            {
                return ServiceResult<CirugiaResponseDto>.Failure(
                    "Ya existe otra cirugía con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            // Update entity fields
            existing.TipoCirugiaId = dto.TipoCirugiaId;
            existing.Nombre = dto.Nombre;
            existing.Descripcion = dto.Descripcion;
            existing.ModificadoPor = modificadoPor;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<CirugiaResponseDto>.Failure(
                    "No se pudo actualizar la cirugía.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<CirugiaResponseDto>.Failure(
                    "Cirugía actualizada pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<CirugiaResponseDto>.Success(
                MapToDto(refreshed), "Cirugía actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cirugía {Id}", id);
            return ServiceResult<CirugiaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva cirugía (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
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

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
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
    // 6. ReactivateAsync — Reactiva cirugía (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando cirugía {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Cirugía no encontrada", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La cirugía ya está activa.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar la cirugía.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Cirugía reactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar cirugía {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de cirugías por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<CirugiaResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando cirugías con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<CirugiaResponseDto>>.Success(
                    new List<CirugiaResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            var entities = await _repo.GetAllAsync(clinicaId);
            var lowerTerm = term.ToLowerInvariant();

            var filtered = new List<CirugiaResponseDto>();
            foreach (var entity in entities)
            {
                var nombreCompleto = entity.NombreCompleto.ToLowerInvariant();
                var hasDescripcion = entity.Descripcion?.ToLowerInvariant().Contains(lowerTerm) ?? false;
                var hasTipo = entity.TipoCirugiaNombre.ToLowerInvariant().Contains(lowerTerm);

                if (nombreCompleto.Contains(lowerTerm) || hasDescripcion || hasTipo)
                {
                    filtered.Add(MapToDto(entity));
                }
            }

            return ServiceResult<IEnumerable<CirugiaResponseDto>>.Success(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar cirugías en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<CirugiaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static CirugiaResponseDto MapToDto(Cirugia c)
    {
        return new CirugiaResponseDto
        {
            Id = c.Id,
            ClinicaId = c.ClinicaId,
            TipoCirugiaId = c.TipoCirugiaId,
            TipoCirugiaNombre = c.TipoCirugiaNombre,
            Nombre = c.Nombre,
            Descripcion = c.Descripcion,
            Activo = c.Activo,
            FechaCreacion = c.FechaCreacion,
            FechaModificacion = c.FechaModificacion,
            NombreCompleto = c.NombreCompleto
        };
    }
}
