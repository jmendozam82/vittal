using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Repositories;
using Vittal.DTO.Clinica;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de clínicas. Implementa IClinicaService.
/// CASO ESPECIAL: Tabla raíz multi-tenant — NO tiene clinicaId.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// </summary>
public class ClinicaService : IClinicaService
{
    private readonly IClinicaRepository _repo;
    private readonly ILogger<ClinicaService> _logger;

    public ClinicaService(IClinicaRepository repo, ILogger<ClinicaService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista clínicas activas (sin tenant filter)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<ClinicaResponseDto>>> GetAllAsync(
        bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo clínicas (inactivos: {Incluir})",
                incluirInactivos);

            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync()
                : await _repo.GetAllAsync();

            var dtos = new List<ClinicaResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapClinicaToDto(entity));
            }

            return ServiceResult<IEnumerable<ClinicaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clínicas");
            return ServiceResult<IEnumerable<ClinicaResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Detalle de una clínica por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaResponseDto>> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando clínica {Id}", id);

            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Clínica no encontrada", ServiceErrorType.NotFound);
            }

            return ServiceResult<ClinicaResponseDto>.Success(MapClinicaToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar clínica {Id}", id);
            return ServiceResult<ClinicaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. GetCurrentClinicaAsync — Obtiene la clínica del usuario actual
    //    Usa app.current_clinica_id del contexto PostgreSQL
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaResponseDto>> GetCurrentClinicaAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo clínica del contexto actual");

            var entity = await _repo.GetCurrentClinicaAsync();
            if (entity == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "No se pudo determinar la clínica actual del usuario.",
                    ServiceErrorType.NotFound);
            }

            return ServiceResult<ClinicaResponseDto>.Success(
                MapClinicaToDto(entity), "Clínica actual cargada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clínica actual");
            return ServiceResult<ClinicaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. CreateAsync — Crea una nueva clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaResponseDto>> CreateAsync(ClinicaRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Creando clínica {Nombre}", dto.Nombre);

            // Validar nombre único
            if (await _repo.ExistsByNameAsync(dto.Nombre))
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Ya existe una clínica con ese nombre.",
                    ServiceErrorType.Conflict);
            }

            var entity = new Clinica
            {
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Email = dto.Email,
                LogoUrl = dto.LogoUrl,
                TiempoEsperaMinutos = dto.TiempoEsperaMinutos,
                BdExterna1 = dto.BdExterna1,
                BdExterna2 = dto.BdExterna2,
                Activo = true
            };

            var newId = await _repo.CreateAsync(entity);
            _logger.LogInformation("Clínica creada con ID: {NewId}", newId);

            // Fetch created entity to return full DTO
            var created = await _repo.GetByIdAsync(newId);
            if (created == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Clínica creada pero no se pudo recuperar la información.",
                    ServiceErrorType.InternalError);
            }

            return ServiceResult<ClinicaResponseDto>.Success(
                MapClinicaToDto(created), "Clínica creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear clínica {Nombre}", dto.Nombre);
            return ServiceResult<ClinicaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. UpdateAsync — Actualiza datos de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaResponseDto>> UpdateAsync(
        Guid id, ClinicaRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Actualizando clínica {Id}", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Clínica no encontrada", ServiceErrorType.NotFound);
            }

            // Validar nombre único (excluyendo la propia clínica)
            if (await _repo.ExistsByNameAsync(dto.Nombre, id))
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Ya existe otra clínica con ese nombre.",
                    ServiceErrorType.Conflict);
            }

            // Update entity fields
            existing.Nombre = dto.Nombre;
            existing.Direccion = dto.Direccion;
            existing.Telefono = dto.Telefono;
            existing.Email = dto.Email;
            existing.LogoUrl = dto.LogoUrl;
            existing.TiempoEsperaMinutos = dto.TiempoEsperaMinutos;
            existing.BdExterna1 = dto.BdExterna1;
            existing.BdExterna2 = dto.BdExterna2;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "No se pudo actualizar la clínica.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id);
            if (refreshed == null)
            {
                return ServiceResult<ClinicaResponseDto>.Failure(
                    "Clínica actualizada pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<ClinicaResponseDto>.Success(
                MapClinicaToDto(refreshed), "Clínica actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar clínica {Id}", id);
            return ServiceResult<ClinicaResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. DeactivateAsync — Desactiva clínica (activo = false)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Desactivando clínica {Id}", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Clínica no encontrada", ServiceErrorType.NotFound);
            }

            if (!existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La clínica ya está inactiva.", ServiceErrorType.Validation);
            }

            var deactivated = await _repo.DeactivateAsync(id);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar la clínica.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Clínica desactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar clínica {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. ReactivateAsync — Reactiva clínica (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Reactivando clínica {Id}", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Clínica no encontrada", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "La clínica ya está activa.", ServiceErrorType.Validation);
            }

            var reactivated = await _repo.ReactivateAsync(id);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar la clínica.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Clínica reactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar clínica {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Entity → DTO
    // ────────────────────────────────────────────────────────────────────────
    private static ClinicaResponseDto MapClinicaToDto(Clinica c)
    {
        return new ClinicaResponseDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Direccion = c.Direccion,
            Telefono = c.Telefono,
            Email = c.Email,
            LogoUrl = c.LogoUrl,
            TiempoEsperaMinutos = c.TiempoEsperaMinutos,
            BdExterna1 = c.BdExterna1,
            BdExterna2 = c.BdExterna2,
            Activo = c.Activo,
            FechaCreacion = c.FechaCreacion,
            FechaModificacion = c.FechaModificacion
        };
    }
}
