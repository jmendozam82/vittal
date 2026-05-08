using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Repositories;
using Vittal.DTO.Sala;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Implementación de lógica de negocio para Sala.
/// Historia de Usuario: HU06 — Gestión de Salas
/// </summary>
public class SalaService : ISalaService
{
    private readonly ISalaRepository _salaRepository;
    private readonly ILogger<SalaService> _logger;

    public SalaService(ISalaRepository salaRepository, ILogger<SalaService> logger)
    {
        _salaRepository = salaRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<SalaResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo salas de la clínica {ClinicaId} (incluirInactivos: {IncluirInactivos})",
                clinicaId, incluirInactivos);

            var entities = incluirInactivos
                ? await _salaRepository.GetAllIncludingInactiveAsync(clinicaId)
                : await _salaRepository.GetAllAsync(clinicaId);

            var dtos = MapToResponseDto(entities);
            return ServiceResult<IEnumerable<SalaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener salas de la clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<SalaResponseDto>>.Failure("Error interno al consultar salas.");
        }
    }

    public async Task<ServiceResult<SalaResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            var sala = await _salaRepository.GetByIdAsync(id, clinicaId);
            if (sala == null)
                return ServiceResult<SalaResponseDto>.Failure(
                    "Sala no encontrada en esta clínica.", ServiceErrorType.NotFound);

            return ServiceResult<SalaResponseDto>.Success(MapToResponseDto(sala));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sala {Id}", id);
            return ServiceResult<SalaResponseDto>.Failure("Error interno al consultar la sala.");
        }
    }

    public async Task<ServiceResult<SalaResponseDto>> CreateAsync(SalaRequestDto dto, Guid clinicaId)
    {
        // Regla de negocio: nombre único por clínica
        var nombreExiste = await _salaRepository.ExistsByNameAsync(clinicaId, dto.Nombre);
        if (nombreExiste)
            return ServiceResult<SalaResponseDto>.Failure(
                $"Ya existe una sala con el nombre '{dto.Nombre}' en esta clínica.",
                ServiceErrorType.Conflict);

        try
        {
            var sala = MapToEntity(dto);
            sala.ClinicaId = clinicaId;

            var id = await _salaRepository.CreateAsync(sala);

            // Retornar la sala recién creada
            var created = await _salaRepository.GetByIdAsync(id, clinicaId);
            return ServiceResult<SalaResponseDto>.Success(
                MapToResponseDto(created!), "Sala creada exitosamente.");
        }
        catch (DuplicateEntityException ex)
        {
            return ServiceResult<SalaResponseDto>.Failure(ex.Message, ServiceErrorType.Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear sala en clínica {ClinicaId}", clinicaId);
            return ServiceResult<SalaResponseDto>.Failure("Error interno al crear la sala.");
        }
    }

    public async Task<ServiceResult<SalaResponseDto>> UpdateAsync(Guid id, SalaRequestDto dto, Guid clinicaId)
    {
        var existente = await _salaRepository.GetByIdAsync(id, clinicaId);
        if (existente == null)
            return ServiceResult<SalaResponseDto>.Failure(
                "Sala no encontrada en esta clínica.", ServiceErrorType.NotFound);

        // Verificar unicidad excluyendo el registro actual
        var nombreExiste = await _salaRepository.ExistsByNameAsync(clinicaId, dto.Nombre, id);
        if (nombreExiste)
            return ServiceResult<SalaResponseDto>.Failure(
                $"Ya existe una sala con el nombre '{dto.Nombre}' en esta clínica.",
                ServiceErrorType.Conflict);

        try
        {
            existente.Nombre = dto.Nombre;
            existente.Descripcion = dto.Descripcion;

            await _salaRepository.UpdateAsync(existente);

            var updated = await _salaRepository.GetByIdAsync(id, clinicaId);
            return ServiceResult<SalaResponseDto>.Success(
                MapToResponseDto(updated!), "Sala actualizada exitosamente.");
        }
        catch (DuplicateEntityException ex)
        {
            return ServiceResult<SalaResponseDto>.Failure(ex.Message, ServiceErrorType.Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar sala {Id}", id);
            return ServiceResult<SalaResponseDto>.Failure("Error interno al actualizar la sala.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        var existente = await _salaRepository.GetByIdAsync(id, clinicaId);
        if (existente == null)
            return ServiceResult<bool>.Failure(
                "Sala no encontrada en esta clínica.", ServiceErrorType.NotFound);

        if (!existente.Activo)
            return ServiceResult<bool>.Failure(
                "La sala ya está desactivada.", ServiceErrorType.Validation);

        try
        {
            var result = await _salaRepository.DeactivateAsync(id, clinicaId);
            if (!result)
                return ServiceResult<bool>.Failure("No fue posible desactivar la sala.");

            return ServiceResult<bool>.Success(true, "Sala desactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar sala {Id}", id);
            return ServiceResult<bool>.Failure("Error interno al desactivar la sala.");
        }
    }

    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando sala {Id} en clinica {ClinicaId}", id, clinicaId);

            var existing = await _salaRepository.GetByIdAsync(id, clinicaId);
            if (existing == null)
                return ServiceResult<bool>.Failure("Sala no encontrada", ServiceErrorType.NotFound);

            if (existing.Activo)
                return ServiceResult<bool>.Failure("La sala ya está activa.", ServiceErrorType.Validation);

            var reactivated = await _salaRepository.ReactivateAsync(id, clinicaId);
            if (!reactivated)
                return ServiceResult<bool>.Failure("No se pudo reactivar la sala.", ServiceErrorType.InternalError);

            return ServiceResult<bool>.Success(true, "Sala reactivada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar sala {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ── Mapeo manual (sin AutoMapper) ──────────────────────────────────────

    private static Sala MapToEntity(SalaRequestDto dto)
    {
        return new Sala
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion
        };
    }

    private static SalaResponseDto MapToResponseDto(Sala sala)
    {
        return new SalaResponseDto
        {
            Id = sala.Id,
            Nombre = sala.Nombre,
            Descripcion = sala.Descripcion,
            Activo = sala.Activo,
            FechaCreacion = sala.FechaCreacion,
            FechaModificacion = sala.FechaModificacion
        };
    }

    private static IEnumerable<SalaResponseDto> MapToResponseDto(IEnumerable<Sala> salas)
    {
        foreach (var sala in salas)
            yield return MapToResponseDto(sala);
    }
}
