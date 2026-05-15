using Vittal.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Medicamento;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de medicamentos. Implementa IMedicamentoService.
/// Historia de Usuario: HU08 — Gestión de Medicamentos
/// </summary>
public class MedicamentoService : IMedicamentoService
{
    private readonly IMedicamentoRepository _repo;
    private readonly ILogger<MedicamentoService> _logger;

    public MedicamentoService(IMedicamentoRepository repo, ILogger<MedicamentoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista medicamentos de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<MedicamentoResponseDto>>> GetAllAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo medicamentos de la clínica {ClinicaId} (inactivos: {Incluir})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);

            var dtos = new List<MedicamentoResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }

            return ServiceResult<IEnumerable<MedicamentoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener medicamentos de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<MedicamentoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de un medicamento por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<MedicamentoResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando medicamento {Id} en clínica {ClinicaId}", id, clinicaId);

            var entity = await _repo.GetByIdAsync(id, clinicaId);
            if (entity == null)
            {
                return ServiceResult<MedicamentoResponseDto>.Failure(
                    "Medicamento no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<MedicamentoResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar medicamento {Id}", id);
            return ServiceResult<MedicamentoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Crea un nuevo medicamento
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<MedicamentoResponseDto>> CreateAsync(
        MedicamentoRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando medicamento {Nombre} en clínica {ClinicaId}",
                dto.Nombre, clinicaId);

            // Validate uniqueness of name
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre))
            {
                return ServiceResult<MedicamentoResponseDto>.Failure(
                    "Ya existe un medicamento con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            var entity = new Medicamento
            {
                ClinicaId = clinicaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Concentracion = dto.Concentracion,
                UnidadMedida = dto.UnidadMedida,
                CreadoPor = creadoPor,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Medicamento creado con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId, clinicaId);
            if (created == null)
            {
                return ServiceResult<MedicamentoResponseDto>.Failure(
                    "Medicamento creado pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<MedicamentoResponseDto>.Success(
                MapToDto(created), "Medicamento creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear medicamento en clínica {ClinicaId}", clinicaId);
            return ServiceResult<MedicamentoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del medicamento
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<MedicamentoResponseDto>> UpdateAsync(
        Guid id, MedicamentoRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando medicamento {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<MedicamentoResponseDto>.Failure(
                    "Medicamento no encontrado", ServiceErrorType.NotFound);
            }

            // Validate uniqueness (exclude current)
            if (await _repo.ExistsByNombreAsync(clinicaId, dto.Nombre, id))
            {
                return ServiceResult<MedicamentoResponseDto>.Failure(
                    "Ya existe otro medicamento con ese nombre en esta clínica.",
                    ServiceErrorType.Conflict);
            }

            // Update entity fields
            existing.Nombre = dto.Nombre;
            existing.Descripcion = dto.Descripcion;
            existing.Concentracion = dto.Concentracion;
            existing.UnidadMedida = dto.UnidadMedida;
            existing.ModificadoPor = modificadoPor;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<MedicamentoResponseDto>.Failure(
                    "No se pudo actualizar el medicamento.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<MedicamentoResponseDto>.Failure(
                    "Medicamento actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<MedicamentoResponseDto>.Success(
                MapToDto(refreshed), "Medicamento actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar medicamento {Id}", id);
            return ServiceResult<MedicamentoResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva medicamento (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando medicamento {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Medicamento no encontrado", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El medicamento ya está inactivo.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el medicamento.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Medicamento desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar medicamento {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. ReactivateAsync — Reactiva medicamento (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando medicamento {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Medicamento no encontrado", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El medicamento ya está activo.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar el medicamento.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Medicamento reactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar medicamento {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de medicamentos por término
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<MedicamentoResponseDto>>> SearchAsync(
        Guid clinicaId, string term)
    {
        try
        {
            _logger.LogInformation("Buscando medicamentos con término '{Term}' en clínica {ClinicaId}",
                term, clinicaId);

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return ServiceResult<IEnumerable<MedicamentoResponseDto>>.Success(
                    new List<MedicamentoResponseDto>(), "Ingrese al menos 2 caracteres para buscar.");
            }

            // Get all active medications and filter in-memory
            var entities = await _repo.GetAllAsync(clinicaId);
            var lowerTerm = term.ToLowerInvariant();

            var filtered = new List<MedicamentoResponseDto>();
            foreach (var entity in entities)
            {
                var nombreCompleto = entity.NombreCompleto.ToLowerInvariant();
                var hasDescripcion = entity.Descripcion?.ToLowerInvariant().Contains(lowerTerm) ?? false;

                if (nombreCompleto.Contains(lowerTerm) || hasDescripcion)
                {
                    filtered.Add(MapToDto(entity));
                }
            }

            return ServiceResult<IEnumerable<MedicamentoResponseDto>>.Success(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar medicamentos en clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<MedicamentoResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static MedicamentoResponseDto MapToDto(Medicamento m)
    {
        return new MedicamentoResponseDto
        {
            Id = m.Id,
            ClinicaId = m.ClinicaId,
            Nombre = m.Nombre,
            Descripcion = m.Descripcion,
            Concentracion = m.Concentracion,
            UnidadMedida = m.UnidadMedida,
            Activo = m.Activo,
            FechaCreacion = m.FechaCreacion,
            FechaModificacion = m.FechaModificacion,
            NombreCompleto = m.NombreCompleto
        };
    }
}
