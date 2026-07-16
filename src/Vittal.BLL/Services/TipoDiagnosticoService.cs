using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.TipoDiagnostico;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de tipos de diagnóstico. Implementa ITipoDiagnosticoService.
/// Historia de Usuario: HU13 — Gestión de Tipos de Diagnóstico
/// </summary>
public class TipoDiagnosticoService : ITipoDiagnosticoService
{
    private readonly ITipoDiagnosticoRepository _repo;
    private readonly ILogger<TipoDiagnosticoService> _logger;

    public TipoDiagnosticoService(ITipoDiagnosticoRepository repo, ILogger<TipoDiagnosticoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista tipos de diagnóstico de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo tipos de diagnóstico de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<TipoDiagnosticoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipos de diagnóstico de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un tipo de diagnóstico por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TipoDiagnosticoResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando tipo de diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<TipoDiagnosticoResponseDto>.Failure(
                    "Tipo de diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<TipoDiagnosticoResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar tipo de diagnóstico {Id}", id);
            return ServiceResult<TipoDiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo tipo de diagnóstico
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TipoDiagnosticoResponseDto>> CreateAsync(
        TipoDiagnosticoRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando tipo de diagnóstico {Nombre} en clínica {ClinicaId}",
                dto.Nombre, clinicaId);

            // Validate uniqueness of name
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre))
            {
                return ServiceResult<TipoDiagnosticoResponseDto>.Failure(
                    "Ya existe un tipo de diagnóstico con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            var entity = new TipoDiagnostico
            {
                ClinicaId = clinicaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Tipo de diagnóstico creado con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<TipoDiagnosticoResponseDto>.Failure(
                    "Tipo de diagnóstico creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<TipoDiagnosticoResponseDto>.Success(
                MapToDto(created), "Tipo de diagnóstico creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de diagnóstico en clínica {ClinicaId}", clinicaId);
            return ServiceResult<TipoDiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del tipo de diagnóstico
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TipoDiagnosticoResponseDto>> UpdateAsync(
        Guid id, TipoDiagnosticoRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando tipo de diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<TipoDiagnosticoResponseDto>.Failure(
                    "Tipo de diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            // Validate uniqueness (exclude current)
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre, id))
            {
                return ServiceResult<TipoDiagnosticoResponseDto>.Failure(
                    "Ya existe otro tipo de diagnóstico con ese nombre en esta clínica.",
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
                return ServiceResult<TipoDiagnosticoResponseDto>.Failure(
                    "No se pudo actualizar el tipo de diagnóstico.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<TipoDiagnosticoResponseDto>.Failure(
                    "Tipo de diagnóstico actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<TipoDiagnosticoResponseDto>.Success(
                MapToDto(refreshed), "Tipo de diagnóstico actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tipo de diagnóstico {Id}", id);
            return ServiceResult<TipoDiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva tipo de diagnóstico (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando tipo de diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Tipo de diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El tipo de diagnóstico ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el tipo de diagnóstico.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Tipo de diagnóstico desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar tipo de diagnóstico {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. ReactivateAsync — Reactiva tipo de diagnóstico (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando tipo de diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Tipo de diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El tipo de diagnóstico ya está activo.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar el tipo de diagnóstico.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Tipo de diagnóstico reactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar tipo de diagnóstico {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de tipos de diagnóstico por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando tipos de diagnóstico con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>.Success(
                    new List<TipoDiagnosticoResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            // Search via SQL ILIKE (no in-memory filter)
            var entities = await _repo.SearchAsync(clinicaId, term.Trim());
            return ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>.Success(
                entities.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar tipos de diagnóstico en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static TipoDiagnosticoResponseDto MapToDto(TipoDiagnostico t)
    {
        return new TipoDiagnosticoResponseDto
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
