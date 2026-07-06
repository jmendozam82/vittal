using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Interfaces;
using Vittal.DTO.UsuarioSala;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Implementación de lógica de negocio para asignación de doctores a salas.
/// Historia de Usuario: HU06 — Asignar Doctores a Salas
/// </summary>
public class UsuarioSalaService : IUsuarioSalaService
{
    private readonly IUsuarioSalaRepository _repository;
    private readonly ILogger<UsuarioSalaService> _logger;

    public UsuarioSalaService(IUsuarioSalaRepository repository, ILogger<UsuarioSalaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<UsuarioSalaResponseDto>>> GetAllBySalaAsync(
        Guid clinicaId, Guid salaId)
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo asignaciones de la sala {SalaId} en clínica {ClinicaId}",
                salaId, clinicaId);

            var result = await _repository.GetBySalaAsync(clinicaId, salaId);
            return ServiceResult<IEnumerable<UsuarioSalaResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener asignaciones de la sala {SalaId} en clínica {ClinicaId}",
                salaId, clinicaId);
            return ServiceResult<IEnumerable<UsuarioSalaResponseDto>>.Failure(
                "Error interno al consultar asignaciones de la sala.");
        }
    }

    public async Task<ServiceResult<UsuarioSalaResponseDto?>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            var asignacion = await _repository.GetByIdAsync(id, clinicaId);
            if (asignacion == null)
                return ServiceResult<UsuarioSalaResponseDto?>.Failure(
                    "Asignación no encontrada en esta clínica.", ServiceErrorType.NotFound);

            return ServiceResult<UsuarioSalaResponseDto?>.Success(asignacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener asignación {Id}", id);
            return ServiceResult<UsuarioSalaResponseDto?>.Failure(
                "Error interno al consultar la asignación.");
        }
    }

    public async Task<ServiceResult<UsuarioSalaResponseDto>> CreateAsync(
        UsuarioSalaRequestDto dto, Guid clinicaId)
    {
        // Validación de negocio: campos obligatorios
        if (dto.UsuarioId == Guid.Empty)
            return ServiceResult<UsuarioSalaResponseDto>.Failure(
                "El usuario es obligatorio.", ServiceErrorType.Validation);
        if (dto.SalaId == Guid.Empty)
            return ServiceResult<UsuarioSalaResponseDto>.Failure(
                "La sala es obligatoria.", ServiceErrorType.Validation);

        try
        {
            var entity = new UsuarioSala
            {
                UsuarioId = dto.UsuarioId,
                SalaId = dto.SalaId,
                ClinicaId = clinicaId
            };

            var newId = await _repository.CreateAsync(entity);

            var created = await _repository.GetByIdAsync(newId, clinicaId);
            if (created == null)
                return ServiceResult<UsuarioSalaResponseDto>.Failure(
                    "Error al obtener la asignación creada.", ServiceErrorType.InternalError);

            return ServiceResult<UsuarioSalaResponseDto>.Success(
                created, "Doctor asignado a la sala correctamente.");
        }
        catch (DuplicateEntityException ex)
        {
            _logger.LogWarning(
                "Asignación duplicada en clínica {ClinicaId}: usuario {UsuarioId} → sala {SalaId}",
                clinicaId, dto.UsuarioId, dto.SalaId);
            return ServiceResult<UsuarioSalaResponseDto>.Failure(
                ex.Message, ServiceErrorType.Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al asignar doctor {UsuarioId} a sala {SalaId} en clínica {ClinicaId}",
                dto.UsuarioId, dto.SalaId, clinicaId);
            return ServiceResult<UsuarioSalaResponseDto>.Failure(
                "Error interno al asignar el doctor a la sala.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        // Verificar que la asignación existe y está activa
        var existente = await _repository.GetByIdAsync(id, clinicaId);
        if (existente == null)
            return ServiceResult<bool>.Failure(
                "Asignación no encontrada en esta clínica.", ServiceErrorType.NotFound);

        try
        {
            _logger.LogInformation(
                "Desasignando doctor de sala (Id: {Id}) en clínica {ClinicaId}",
                id, clinicaId);

            var result = await _repository.DeactivateAsync(id, clinicaId);
            if (!result)
                return ServiceResult<bool>.Failure(
                    "No fue posible desasignar el doctor de la sala.");

            return ServiceResult<bool>.Success(true, "Doctor desasignado de la sala correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desasignar doctor de sala (Id: {Id})", id);
            return ServiceResult<bool>.Failure("Error interno al desasignar el doctor de la sala.");
        }
    }
}
