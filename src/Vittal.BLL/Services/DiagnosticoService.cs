using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Diagnostico;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de catálogo de diagnósticos.
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// </summary>
public class DiagnosticoService : IDiagnosticoService
{
    private readonly IDiagnosticoRepository _repo;
    private readonly ILogger<DiagnosticoService> _logger;

    public DiagnosticoService(IDiagnosticoRepository repo, ILogger<DiagnosticoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista diagnósticos de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<DiagnosticoResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo diagnósticos de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<DiagnosticoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<DiagnosticoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener diagnósticos de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<DiagnosticoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un diagnóstico por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<DiagnosticoResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "Diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<DiagnosticoResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar diagnóstico {Id}", id);
            return ServiceResult<DiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo diagnóstico en el catálogo
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<DiagnosticoResponseDto>> CreateAsync(
        DiagnosticoRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando diagnóstico '{Nombre}' en clínica {ClinicaId}",
                dto.Nombre, clinicaId);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length < 2)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "El nombre del diagnóstico debe tener al menos 2 caracteres.",
                    ServiceErrorType.Validation);
            }

            if (dto.TipoDiagnosticoId == Guid.Empty)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "Debe seleccionar un tipo de diagnóstico.",
                    ServiceErrorType.Validation);
            }

            // Validate uniqueness of name per clinic
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre.Trim()))
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "Ya existe un diagnóstico con ese nombre en su clínica.",
                    ServiceErrorType.Conflict);
            }

            var entity = new Diagnostico
            {
                ClinicaId = clinicaId,
                Nombre = dto.Nombre.Trim(),
                CodigoCie10 = dto.CodigoCie10?.Trim(),
                TipoDiagnosticoId = dto.TipoDiagnosticoId,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Diagnóstico '{Nombre}' creado con ID: {NewId}", dto.Nombre, newId);

            // Fetch created entity to return full DTO (with JOIN data)
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "Diagnóstico creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<DiagnosticoResponseDto>.Success(
                MapToDto(created), "Diagnóstico creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear diagnóstico en clínica {ClinicaId}", clinicaId);
            return ServiceResult<DiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del diagnóstico
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<DiagnosticoResponseDto>> UpdateAsync(
        Guid id, DiagnosticoRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "Diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length < 2)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "El nombre del diagnóstico debe tener al menos 2 caracteres.",
                    ServiceErrorType.Validation);
            }

            if (dto.TipoDiagnosticoId == Guid.Empty)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "Debe seleccionar un tipo de diagnóstico.",
                    ServiceErrorType.Validation);
            }

            // Validate uniqueness (exclude current)
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre.Trim(), id))
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "Ya existe otro diagnóstico con ese nombre en su clínica.",
                    ServiceErrorType.Conflict);
            }

            // Update entity fields
            existing.Nombre = dto.Nombre.Trim();
            existing.CodigoCie10 = dto.CodigoCie10?.Trim();
            existing.TipoDiagnosticoId = dto.TipoDiagnosticoId;
            existing.ModificadoPor = modificadoPor;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "No se pudo actualizar el diagnóstico.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<DiagnosticoResponseDto>.Failure(
                    "Diagnóstico actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<DiagnosticoResponseDto>.Success(
                MapToDto(refreshed), "Diagnóstico actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar diagnóstico {Id}", id);
            return ServiceResult<DiagnosticoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva diagnóstico (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
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

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
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
    // 6. ReactivateAsync — Reactiva diagnóstico (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando diagnóstico {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Diagnóstico no encontrado", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El diagnóstico ya está activo.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar el diagnóstico.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Diagnóstico reactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar diagnóstico {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de diagnósticos por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<DiagnosticoResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando diagnósticos con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<DiagnosticoResponseDto>>.Success(
                    new List<DiagnosticoResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            var entities = await _repo.SearchAsync(clinicaId, term.Trim());
            var dtos = new List<DiagnosticoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<DiagnosticoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar diagnósticos en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<DiagnosticoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static DiagnosticoResponseDto MapToDto(Diagnostico d)
    {
        return new DiagnosticoResponseDto
        {
            Id = d.Id,
            ClinicaId = d.ClinicaId,
            Nombre = d.Nombre,
            CodigoCie10 = d.CodigoCie10,
            TipoDiagnosticoId = d.TipoDiagnosticoId,
            TipoDiagnosticoNombre = d.TipoDiagnosticoNombre,
            Activo = d.Activo,
            FechaCreacion = d.FechaCreacion,
            FechaModificacion = d.FechaModificacion
        };
    }
}
